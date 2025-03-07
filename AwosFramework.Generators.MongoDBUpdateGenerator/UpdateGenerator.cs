using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlTypes;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;

namespace AwosFramework.Generators.MongoDBUpdateGenerator
{
	[Generator]
	public class UpdateGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			context.RegisterPostInitializationOutput(ctx =>
			{
				ctx.AddSource($"{Constants.MarkerAttributeClassName}.g.cs", SourceText.From(Constants.MarkerAttributeClass, Encoding.UTF8));
				ctx.AddSource($"{Constants.UpdatePropertyAttributeClassName}.g.cs", SourceText.From(Constants.UpdatePropertyAttributeClass, Encoding.UTF8));
				ctx.AddSource($"{Constants.UpdatePropertyIgnoreAttributeClassName}.g.cs", SourceText.From(Constants.UpdatePropertyIgnoreAttributeClass, Encoding.UTF8));
				ctx.PublishInternalEnum<CollectionHandling>();
			});

			IncrementalValuesProvider<UpdateApiModel?> updates = context.SyntaxProvider
				.CreateSyntaxProvider(
					predicate: IsSyntaxTargetForGeneration,
					transform: Transform
				).Where(x => x!=null);

			var classDeclarations = context.SyntaxProvider
				.CreateSyntaxProvider(
					predicate: IsClassSyntax,
					transform: TransformToTargetClass
				).Collect();

			var updatesToGenerate = classDeclarations.Combine(updates.Collect())
				.Select(CombineWithTargetProperties);

			context.RegisterSourceOutput(updatesToGenerate, GenerateSource);
		}

		private static void UpdateSourceMethodProperties(UpdateMethod method, TargetClassModel targetClass, List<Diagnostic> diagnostics)
		{
			var newProperties = new List<UpdateProperty>();

			foreach (var sourceProperty in method.Properties)
			{
				if (targetClass.Properties.TryGetValue(sourceProperty.TargetName, out var targetProperty) == false)
				{
					diagnostics.Add(Diagnostic.Create(Constants.PropertyNotFound, sourceProperty.SourceLocation, sourceProperty.TargetName, targetClass.FullName));
				}
				else
				{
					if (targetProperty.IsEnumerable == false && sourceProperty.IsSourceEnumerable)
					{
						diagnostics.Add(Diagnostic.Create(Constants.PropertyNotEnumerable, sourceProperty.SourceLocation, sourceProperty.TargetName, method.TargetClassName));
						continue;
					}

					sourceProperty.IsTargetEnumerable = targetProperty.IsEnumerable;
					newProperties.Add(sourceProperty);
				}
			}

			method.Properties = newProperties.ToArray();
		}


		private static void ResolveNestedProperties(ImmutableDictionary<string, TargetClassModel> classes, UpdateMethod method, List<Diagnostic> diagnostics)
		{
			if(method.NestedProperty != null )
			{
				if(classes.TryGetValue(method.TargetClassName, out var targetClass) == false)
				{
					diagnostics.Add(Diagnostic.Create(Constants.ClassNotFound, method.SourceLocation, method.TargetClassName));
					return;
				}

				var path = method.NestedProperty.Split('.');
				TargetClassModel current = targetClass;
				foreach(var part in path)
				{
					var isArray = part.EndsWith("[$]");
					var propertyName = isArray ? part.Substring(0, part.Length - 3) : part;

					if (current.Properties.TryGetValue(propertyName, out var nestedProperty) == false)
					{
						diagnostics.Add(Diagnostic.Create(Constants.PropertyNotFound, method.SourceLocation, part, current.FullName));
						return;
					}

					if(nestedProperty.IsEnumerable == false && isArray)
					{
						diagnostics.Add(Diagnostic.Create(Constants.PropertyNotEnumerable, method.SourceLocation, nestedProperty.Name, current.FullName));
						return;
					}

					if(classes.TryGetValue(nestedProperty.Type, out current) == false)
					{
						diagnostics.Add(Diagnostic.Create(Constants.ClassNotFound, method.SourceLocation, nestedProperty.Type));
						return;
					}
				}

				method.NestedProperty = method.NestedProperty.Replace("[$]", ".FirstMatchingElement()");
				method.NestedTargetClassName = current.FullName;
			}
		}

		private static IEnumerable<UpdateApiModel> CombineWithTargetProperties((ImmutableArray<TargetClassModel> targetClasses, ImmutableArray<UpdateApiModel> sourceClasses) pair, CancellationToken token)
		{
			(var targetClasses, var sourceClasses) = pair;
			var classLookup = targetClasses.ToImmutableDictionary(x => x.FullName, x => x);
			foreach (var source in sourceClasses)
			{
				foreach (var method in source.UpdateMethods)
				{
					ResolveNestedProperties(classLookup, method, source.Diagnostics);
					if(classLookup.TryGetValue(method.GetTargetClassFullName(), out var targetClass) == false)
					{
						source.Diagnostics.Add(Diagnostic.Create(Constants.ClassNotFound, method.SourceLocation, method.TargetClassName));
						continue;
					}

					UpdateSourceMethodProperties(method, targetClass, source.Diagnostics);
				}
			}

			return sourceClasses;
		}

		static bool IsClassSyntax(SyntaxNode node, CancellationToken token) => node is ClassDeclarationSyntax;

		private static TargetClassModel TransformToTargetClass(GeneratorSyntaxContext ctx, CancellationToken token)
		{
			var classDeclaration = (ClassDeclarationSyntax)ctx.Node;
			var className = classDeclaration.Identifier.Text;
			var isGeneric = classDeclaration?.TypeParameterList != null;
			if (isGeneric)
			{
				var comaCount = classDeclaration.TypeParameterList.Parameters.Count - 1;
				var comas = comaCount > 0 ? new string(',', comaCount) : string.Empty;
				className += $"<{comas}>";
			}

			var nameSpace = ctx.SemanticModel.GetDeclaredSymbol(classDeclaration)?.ContainingNamespace?.ToDisplayString();
			var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().Select(x =>
			{
				var name = x.Identifier.Text;
				var isEnumerable = IsEnumerableOrArrayType(ctx, x, out var isString);
				var symbol = ctx.SemanticModel.GetSymbolInfo(x.Type).Symbol;
				var type = isEnumerable && symbol is INamedTypeSymbol namedType && namedType.IsGenericType ? namedType.TypeArguments[0].ToDisplayString() : symbol?.ToDisplayString();
				return new TargetProperty(name, type, isEnumerable, isString);
			}).ToArray();

			return new TargetClassModel(className, nameSpace, properties);
		}

		static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken token) => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;

		private static string? GeneratePropertyIfSource(UpdateProperty property, string modelParamName)
		{
			var propertyAccess = property.BuildSourcePropertyAccessCode(modelParamName);
			if (property.IsSourceEnumerable)
			{
				if (property.IgnoreNull && property.IgnoreEmpty)
					return $"if({propertyAccess} != null && {propertyAccess}.Count() > 0)";
				else if (property.IgnoreNull)
					return $"if({propertyAccess} != null)";
				else if (property.IgnoreEmpty)
					return $"if({propertyAccess}.Count() > 0)";
				else
					return null;
			}
			else
			{
				if (property.IgnoreNull)
					return $"if({propertyAccess} != null)";
				else
					return null;
			}
		}


		private static string GeneratePropertySource(UpdateApiModel model, UpdateMethod method, UpdateProperty property, string listName, string modelParamName)
		{
			var sourcePropertyAccess = property.BuildSourcePropertyAccessCode(modelParamName);
			var targetPropertyAccess = property.BuildTargetPropertyAccessCode(method.NestedProperty);
			if (property.IsTargetEnumerable)
			{
				var ifLine = GeneratePropertyIfSource(property, modelParamName);
				var setName = (property.CollectionHandling, property.IsSourceEnumerable) switch
				{
					(CollectionHandling.Set, true) => "Set",
					(CollectionHandling.AddToSet, true) => "AddToSetEach",
					(CollectionHandling.PushAll, true) => "PushAll",
					(CollectionHandling.PullAll, true) => "PullAll",
					(CollectionHandling.Set, false) => "Set",
					(CollectionHandling.AddToSet, false) => "AddToSet",
					(CollectionHandling.PushAll, false) => "Push",
					(CollectionHandling.PullAll, false) => "Pull",
					_ => null
				};

				if (setName == null)
				{
					model.Diagnostics.Add(Diagnostic.Create(Constants.CollectionHandlingNotSupported, property.SourceLocation));
					return null;
				}

				var listAdd = $"{listName}.Add(builder.{setName}(x => x.{targetPropertyAccess}, {sourcePropertyAccess}));";
				return ifLine == null ? listAdd : $"{ifLine}\n\t{listAdd}";
			}
			else if (property.UseStringEmpty && property.IgnoreNull)
			{
				return
				$$"""
				if(string.IsNullOrEmpty({{sourcePropertyAccess}}) == false)
					{{listName}}.Add(builder.Set(x => x.{{targetPropertyAccess}}, {{sourcePropertyAccess}}));
				""";
			}
			else
			{
				var ifLine = GeneratePropertyIfSource(property, modelParamName);
				var listAdd = $"{listName}.Add(builder.Set(x => x.{targetPropertyAccess}, {sourcePropertyAccess}));";
				return ifLine == null ? listAdd : $"{ifLine}\n\t{listAdd}";
			}
		}

		private static string GenerateExtensionUpdateMethod(SourceProductionContext ctx, UpdateApiModel apiModel, UpdateMethod method)
		{
			var propertySources = method.Properties.SelectNonNull(x => GeneratePropertySource(apiModel, method, x, "list", Constants.ModelParameterName)).ToArray();
			var methodTemplate =
			$$"""
			public static UpdateDefinition<{{method.TargetClassName}}> {{method.MethodName}}(this {{apiModel.SourceClassNameSpace}}.{{apiModel.SourceClassName}} {{Constants.ModelParameterName}})
			{
				var builder = Builders<{{method.TargetClassName}}>.Update;
				var list = new List<UpdateDefinition<{{method.TargetClassName}}>>({{method.Properties.Length}});

				{{propertySources.JoinTabbed(1, "\n\n")}}

				return builder.Combine(list);
			}
			""";

			return methodTemplate;
		}

		private static string GeneratePartialUpdateMethod(UpdateApiModel apiModel, UpdateMethod method)
		{
			var propertySources = method.Properties.SelectNonNull(x => GeneratePropertySource(apiModel, method, x, "list", "this")).ToArray();
			var methodTemplate =
			$$"""
			public UpdateDefinition<{{method.TargetClassName}}> {{method.MethodName}}()
			{
				var builder = Builders<{{method.TargetClassName}}>.Update;
				var list = new List<UpdateDefinition<{{method.TargetClassName}}>>({{method.Properties.Length}});

				{{propertySources.JoinTabbed(1, "\n\n")}}

				return builder.Combine(list);
			}
			""";

			return methodTemplate;
		}

		private static void GenerateSource(SourceProductionContext ctx, IEnumerable<UpdateApiModel> models)
		{
			foreach (var model in models)
			{
				if (ctx.CancellationToken.IsCancellationRequested)
					break;

				GenerateSourceItem(ctx, model);
			}
		}

		private static void GenerateSourceItem(SourceProductionContext ctx, UpdateApiModel apiModel)
		{
			if (apiModel.UpdateMethods.Any() == false)
				return;

			var partialMethods = apiModel.UpdateMethods.Where(x => x.UsePartialClass).Select(x => GeneratePartialUpdateMethod(apiModel, x)).ToArray();
			if (partialMethods.Any())
			{
				var classCode =
				$$"""
				using System;
				using System.Collections.Generic;
				using MongoDB.Driver;	
				using MongoDB.Driver.Linq;	
				

				namespace {{apiModel.SourceClassNameSpace}}
				{
					public partial class {{apiModel.SourceClassName}}
					{
						{{partialMethods.JoinTabbed(2, "\n\n")}}
					}
				}
				""";

				var source = SourceText.From(classCode, Encoding.UTF8);
				ctx.AddSource($"{apiModel.SourceClassName}.g.cs", source);
			}

			var extensionsMethods = apiModel.UpdateMethods.Where(x => x.UsePartialClass == false).Select(x => GenerateExtensionUpdateMethod(ctx, apiModel, x)).ToArray();
			if (extensionsMethods.Any())
			{
				var className = string.Format(Constants.ExtensionClassNameFormat, apiModel.SourceClassName);
				var classCode =
				$$"""
				using System;
				using System.Collections.Generic;
				using MongoDB.Driver;	
				using MongoDB.Driver.Linq;

				namespace {{Constants.ExtensionsNameSpace}}
				{
					public static class {{className}}
					{
						{{extensionsMethods.JoinTabbed(2, "\n\n")}}
					}
				}
				""";

				var source = SourceText.From(classCode, Encoding.UTF8);
				ctx.AddSource($"{className}.g.cs", source);
			}
			apiModel.Diagnostics.ForEach(ctx.ReportDiagnostic);
		}


		private static bool IsEnumerableOrArrayType(ITypeSymbol typeSymbol, out bool isString)
		{
			isString = typeSymbol.SpecialType == SpecialType.System_String;
			if (isString)
				return false;

			if (typeSymbol is IArrayTypeSymbol)
				return true;

			return typeSymbol.AllInterfaces.Any(i =>
					i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T ||
					i.SpecialType == SpecialType.System_Collections_IEnumerable);
		}

		private static bool IsEnumerableOrArrayType(GeneratorSyntaxContext ctx, PropertyDeclarationSyntax property, out bool isString)
		{
			var typeSymbol = ctx.SemanticModel.GetSymbolInfo(property.Type).Symbol as ITypeSymbol;
			return IsEnumerableOrArrayType(typeSymbol, out isString);
		}


		private static UpdateProperty ParseUpdatePropertyAttribute(GeneratorSyntaxContext ctx, (ISymbol symbol, AttributeSyntax attr, bool isOnClass) updateAttribute, bool isEnumerable, bool isNullable, bool isString, List<Diagnostic> diagnostics, string? sourceName = null)
		{
			var methodName = updateAttribute.attr.GetNamedAttributeValueOrDefault(ctx.SemanticModel,
				Constants.UpdatePropertyAttribute_MethodName_PropertyName,
				Constants.UpdatePropertyAttribute_MethodName_DefaultValue, out _);

			var applyToAll = updateAttribute.attr.GetNamedAttributeValueOrDefault(ctx.SemanticModel,
				Constants.UpdatePropertyAttribute_ApplyToAllMethods_PropertyName,
				Constants.UpdatePropertyAttribute_ApplyToAllMethods_DefaultValue, out _);

			var semantic = ctx.SemanticModel;
			var targetPropertyName = updateAttribute.attr.GetNamedAttributeValueOrDefault(semantic,
				Constants.UpdatePropertyAttribute_TargetPropertyName_PropertyName,
				Constants.UpdatePropertyAttribute_TargetPropertyName_DefaultValue, out var targetPropertyNameSet);

			var ignoreEmpty = updateAttribute.attr.GetNamedAttributeValueOrDefault(semantic,
				Constants.UpdatePropertyAttribute_IgnoreEmpty_PropertyName,
				Constants.UpdatePropertyAttribute_IgnoreEmpty_DefaultValue, out var ignoreEmptySet);

			var ignoreNull = updateAttribute.attr.GetNamedAttributeValueOrDefault(semantic,
				Constants.UpdatePropertyAttribute_IgnoreNull_PropertyName,
				Constants.UpdatePropertyAttribute_IgnoreNull_DefaultValue, out var ignoreNullSet);

			var collectionHandling = updateAttribute.attr.GetNamedAttributeValueOrDefault(semantic,
				Constants.UpdatePropertyAttribute_CollectionHandling_PropertyName,
				Constants.UpdatePropertyAttribute_CollectionHandling_DefaultValue, out var collectionHandlingSet);

			var useStringEmpty = updateAttribute.attr.GetNamedAttributeValueOrDefault(semantic,
				Constants.UpdatePropertyAttribute_UseStringEmpty_PropertyName,
				Constants.UpdatePropertyAttribute_UseStringEmpty_DefaultValue, out var useStringEmptySet);

			var isSourceArray = updateAttribute.attr.GetNamedAttributeValueOrDefault(semantic,
				Constants.UpdatePropertyAttribute_IsSourceArray_PropertyName,
				Constants.UpdatePropertyAttribute_IsSourceArray_DefaultValue, out var isSourceArraySet);

			var isPropertyEnumerable = updateAttribute.isOnClass ? isSourceArray : isEnumerable;

			var update = new UpdateProperty(sourceName, isPropertyEnumerable, methodName, applyToAll, false,
				updateAttribute.attr.GetLocation(), updateAttribute.isOnClass, targetPropertyName, ignoreNull && isNullable,
				ignoreEmpty && isEnumerable, collectionHandling, isString && useStringEmpty);

			if (isSourceArraySet && updateAttribute.isOnClass == false)
				diagnostics.Add(Diagnostic.Create(Constants.IsSourceArrayNotApplicable, updateAttribute.attr.GetLocation()));

			if (updateAttribute.isOnClass && targetPropertyNameSet == false)
				diagnostics.Add(Diagnostic.Create(Constants.TargetPropertyNameMissing, updateAttribute.attr.GetLocation()));

			if (isNullable == false && ignoreNullSet)
				diagnostics.Add(Diagnostic.Create(Constants.IgnoreNullNotApplicable, updateAttribute.attr.GetLocation()));

			if (isString == false && useStringEmptySet)
				diagnostics.Add(Diagnostic.Create(Constants.UseStringEmptyNotApplicable, updateAttribute.attr.GetLocation()));

			if (isEnumerable == false && ignoreEmptySet)
				diagnostics.Add(Diagnostic.Create(Constants.IgnoreEmptyNotApplicable, updateAttribute.attr.GetLocation()));

			return update;
		}

		private static IEnumerable<UpdateProperty> GetProperties(GeneratorSyntaxContext ctx, List<Diagnostic> diagnostics, PropertyDeclarationSyntax property)
		{
			var ignore = property.AttributeLists.SelectMany(x => x.Attributes)
				.Select(x => ctx.SemanticModel.GetSymbolInfo(x).Symbol)
				.Any(x => x != null && x.ContainingType.ToDisplayString() == Constants.UpdatePropertyIgnoreAttributeClassFullName);

			if (ignore)
				yield break;

			var updateAttributes = property.AttributeLists.SelectMany(x => x.Attributes)
				.Select(x => (symbol: ctx.SemanticModel.GetSymbolInfo(x).Symbol, attr: x, isOnClass: false))
				.Where(x => x.symbol != null && x.symbol.ContainingType.ToDisplayString() == Constants.UpdatePropertyAttributeClassFullName);

			var isEnumerable = IsEnumerableOrArrayType(ctx, property, out var isString);
			var isNullable = ctx.SemanticModel.GetSymbolInfo(property.Type).Symbol is INamedTypeSymbol namedType && (namedType.IsReferenceType || (namedType.IsGenericType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T));

			if (updateAttributes.Any())
			{
				foreach (var updateAttribute in updateAttributes)
				{
					yield return ParseUpdatePropertyAttribute(ctx, updateAttribute, isEnumerable, isNullable, isString, diagnostics, property.Identifier.Text);
				}
			}
			else
			{
				yield return new UpdateProperty(property.Identifier.Text, isEnumerable,
					Constants.UpdatePropertyAttribute_MethodName_DefaultValue,
					Constants.UpdatePropertyAttribute_ApplyToAllMethods_DefaultValue, true, property.GetLocation(), false, ignoreNull: isNullable, useStringEmpty: isString);
			}
		}

		static UpdateApiModel? Transform(GeneratorSyntaxContext ctx, CancellationToken token)
		{
			var classDeclaration = (ClassDeclarationSyntax)ctx.Node;
			var attributes = classDeclaration.AttributeLists.SelectMany(x => x.Attributes)
				.Select(x => (symbol: ctx.SemanticModel.GetSymbolInfo(x).Symbol, attr: x))
				.Where(x => x.symbol != null && x.symbol.ContainingType.ToDisplayString() == Constants.MarkerAttributeClassFullName)
				.Select(x => x.attr);

			if (attributes.Any() == false)
				return null;

			var methodNameSet = new HashSet<string>();
			var diagnostics = new List<Diagnostic>();
			var methods = new List<UpdateMethod>();

			var classProperties = classDeclaration.AttributeLists.SelectMany(x => x.Attributes)
				.Select(x => (symbol: ctx.SemanticModel.GetSymbolInfo(x).Symbol, attr: x, isOnClass: true))
				.Where(x => x.symbol != null && x.symbol.ContainingType.ToDisplayString() == Constants.UpdatePropertyAttributeClassFullName)
				.Select(x => ParseUpdatePropertyAttribute(ctx, x, false, true, false, diagnostics));

			var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>()
				.SelectMany(x => GetProperties(ctx, diagnostics, x))
				.Concat(classProperties)
				.ToArray();


			var className = classDeclaration.Identifier.ToString();
			var nameSpace = ctx.SemanticModel.GetDeclaredSymbol(classDeclaration)?.ContainingNamespace?.ToDisplayString();

			foreach (var attribute in attributes)
			{
				if (token.IsCancellationRequested)
					break;

				if (attribute == null)
					continue;

				var typeOf = (TypeOfExpressionSyntax)attribute.ArgumentList.Arguments[0].Expression;
				var entityType = ctx.SemanticModel.GetSymbolInfo(typeOf.Type).Symbol?.ToDisplayString();
				if (className == null || entityType == null)
					continue;

				var ignoreUnmarkedProperty = attribute.GetNamedAttributeValueOrDefault(ctx.SemanticModel,
					Constants.MarkerAttribute_IgnoreUnmarkedProperties_PropertyName,
					Constants.MarkerAttribute_IgnoreUnmarkedProperties_DefaultValue, out _
				);

				var methodName = attribute.GetNamedAttributeValueOrDefault(ctx.SemanticModel,
					Constants.MarkerAttribute_MethodName_PropertyName,
					Constants.MarkerAttribute_MethodName_DefaultValue, out _
				);

				var usePartialClass = attribute.GetNamedAttributeValueOrDefault(ctx.SemanticModel,
					Constants.MarkerAttribute_UsePartialClass_PropertyName,
					Constants.MarkerAttribute_UsePartialClass_DefaultValue, out var usePartialClassSet
				);

				var nestedProperty = attribute.GetNamedAttributeValueOrDefault(ctx.SemanticModel,
					Constants.MarkerAttribute_NestedProperty_PropertyName,
					Constants.MarkerAttribute_NestedProperty_DefaultValue, out _
				);

				if (methodNameSet.Contains(methodName))
				{
					diagnostics.Add(Diagnostic.Create(Constants.MethodNameAlreadyExists, attribute.GetLocation(), methodName));
					continue;
				}
				else
				{
					methodNameSet.Add(methodName);
				}

				var isPartialClass = ctx.Node is ClassDeclarationSyntax c && c.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword));
				if (usePartialClass && isPartialClass == false && usePartialClassSet)
				{
					diagnostics.Add(Diagnostic.Create(Constants.PartialClassMissing, attribute.GetLocation(), className));
					usePartialClass = false;
				}

				// if method sets one target property directly ignore properties
				var propertiesForMethod = properties
					.Where(x => (x.ApplyToAllMethods || x.MethodName == methodName) && (ignoreUnmarkedProperty && x.IsUnmarked) == false)
					.ToArray();

				var updateMethod = new UpdateMethod(entityType, methodName, propertiesForMethod, attribute.GetLocation(), usePartialClass && isPartialClass, nestedProperty);
				methods.Add(updateMethod);
			}

			return new UpdateApiModel(className, nameSpace, methods.ToArray(), diagnostics.ToArray());
		}
	}
}
