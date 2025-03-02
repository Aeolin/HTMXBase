using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

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

			IncrementalValuesProvider<UpdateApiModel?> updatesToGenerate = context.SyntaxProvider
				.CreateSyntaxProvider(
					predicate: IsSyntaxTargetForGeneration,
					transform: Transform
				).Where(x => x!=null);


			context.RegisterSourceOutput(updatesToGenerate, GenerateSource);
		}

		static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken token) => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;

		private static string? GeneratePropertyIfSource(UpdateProperty property, string modelParamName)
		{
			if (property.IsEnumerable)
			{
				if (property.IgnoreNull && property.IgnoreEmpty)
					return $"if({modelParamName}.{property.SourceName} != null && {modelParamName}.{property.SourceName}.Count() > 0)";
				else if (property.IgnoreNull)
					return $"if({modelParamName}.{property.SourceName} != null)";
				else if (property.IgnoreEmpty)
					return $"if({modelParamName}.{property.SourceName}.Count() > 0)";
				else
					return null;
			}
			else
			{
				if (property.IgnoreNull)
					return $"if({modelParamName}.{property.SourceName} != null)";
				else
					return null;
			}
		}


		private static string GeneratePropertySource(UpdateApiModel model, UpdateProperty property, string listName, string modelParamName)
		{
			if (property.IsEnumerable)
			{
				var ifLine = GeneratePropertyIfSource(property, modelParamName);
				var setName = property.CollectionHandling switch
				{
					CollectionHandling.Set => "Set",
					CollectionHandling.AddToSet => "AddToSetEach",
					CollectionHandling.PushAll => "PushAll",
					CollectionHandling.PullAll => "PullAll",
					_ => null
				};

				if (setName == null)
				{
					model.Diagnostics.Add(Diagnostic.Create(Constants.CollectionHandlingNotSupported, property.SourceLocation));
					return null;
				}

				var listAdd = $"{listName}.Add(builder.{setName}(x => x.{property.TargetName}, {modelParamName}.{property.SourceName}));";
				return ifLine == null ? listAdd : $"{ifLine}\n\t{listAdd}";
			}
			else if (property.UseStringEmpty)
			{
				return
				$$"""
				if(string.IsNullOrEmpty({{modelParamName}}.{{property.SourceName}}) == false)
					{{listName}}.Add(builder.Set(x => x.{{property.TargetName}}, {{modelParamName}}.{{property.SourceName}}));
				""";
			}
			else
			{
				var ifLine = GeneratePropertyIfSource(property, modelParamName);
				var listAdd = $"{listName}.Add(builder.Set(x => x.{property.TargetName}, {modelParamName}.{property.SourceName}));";
				return ifLine == null ? listAdd : $"{ifLine}\n\t{listAdd}";
			}
		}

		private static string GenerateExtensionUpdateMethod(SourceProductionContext ctx, UpdateApiModel apiModel, UpdateMethod method)
		{
			var propertySources = method.Properties.SelectNonNull(x => GeneratePropertySource(apiModel, x, "list", Constants.ModelParameterName)).ToArray();
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
			var propertySources = method.Properties.SelectNonNull(x => GeneratePropertySource(apiModel, x, "list", "this")).ToArray();
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

		private static void GenerateSource(SourceProductionContext ctx, UpdateApiModel apiModel)
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

		private static IEnumerable<UpdateProperty> GetProperties(GeneratorSyntaxContext ctx, List<Diagnostic> diagnostics, PropertyDeclarationSyntax property)
		{
			var ignore = property.AttributeLists.SelectMany(x => x.Attributes)
				.Select(x => ctx.SemanticModel.GetSymbolInfo(x).Symbol)
				.Any(x => x != null && x.ContainingType.ToDisplayString() == Constants.UpdatePropertyIgnoreAttributeClassFullName);

			if (ignore)
				yield break;

			var updateAttributes = property.AttributeLists.SelectMany(x => x.Attributes)
				.Select(x => (symbol: ctx.SemanticModel.GetSymbolInfo(x).Symbol, attr: x))
				.Where(x => x.symbol != null && x.symbol.ContainingType.ToDisplayString() == Constants.UpdatePropertyAttributeClassFullName);

			var isEnumerable = IsEnumerableOrArrayType(ctx, property, out var isString);
			var isNullable = ctx.SemanticModel.GetSymbolInfo(property.Type).Symbol is INamedTypeSymbol namedType && (namedType.IsReferenceType || (namedType.IsGenericType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T));

			if (updateAttributes.Any())
			{

				foreach (var updateAttribute in updateAttributes)
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
						Constants.UpdatePropertyAttribute_TargetPropertyName_DefaultValue, out _);

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

					var update = new UpdateProperty(property.Identifier.Text, isEnumerable, methodName, applyToAll, false, updateAttribute.attr.GetLocation(), targetPropertyName, ignoreNull && isNullable, ignoreEmpty && isEnumerable, collectionHandling, isString && useStringEmpty);

					if(isNullable == false && ignoreNullSet)
						diagnostics.Add(Diagnostic.Create(Constants.IgnoreNullNotApplicable, updateAttribute.attr.GetLocation()));

					if (isString == false && useStringEmptySet)
						diagnostics.Add(Diagnostic.Create(Constants.UseStringEmptyNotApplicable, updateAttribute.attr.GetLocation()));

					if (isEnumerable == false && collectionHandlingSet)
						diagnostics.Add(Diagnostic.Create(Constants.CollectionHandlingNotApplicable, updateAttribute.attr.GetLocation()));

					if (isEnumerable == false && ignoreEmptySet)
						diagnostics.Add(Diagnostic.Create(Constants.IgnoreEmptyNotApplicable, updateAttribute.attr.GetLocation()));

					yield return update;
				}
			}
			else
			{
				yield return new UpdateProperty(property.Identifier.Text, isEnumerable, Constants.UpdatePropertyAttribute_MethodName_DefaultValue, Constants.UpdatePropertyAttribute_ApplyToAllMethods_DefaultValue, true, property.GetLocation());
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

			var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>()
				.SelectMany(x => GetProperties(ctx, diagnostics, x))
				.ToArray();

			var className = classDeclaration.Identifier.ToString();
			var nameSpace = ctx.SemanticModel.GetDeclaredSymbol(classDeclaration)?.ContainingNamespace?.ToDisplayString();

			foreach (var attribute in attributes)
			{
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

				var propertiesForMethod = properties.Where(x => (x.ApplyToAllMethods || x.MethodName == methodName) && (ignoreUnmarkedProperty && x.IsUnmarked) == false).ToArray();
				var updateMethod = new UpdateMethod(entityType, methodName, propertiesForMethod, usePartialClass && isPartialClass);
				methods.Add(updateMethod);
			}

			return new UpdateApiModel(className, nameSpace, methods.ToArray(), diagnostics.ToArray());
		}
	}
}
