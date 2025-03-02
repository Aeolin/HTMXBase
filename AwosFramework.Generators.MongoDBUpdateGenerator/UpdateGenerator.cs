using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
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

		private static string? GeneratePropertyIfSource(UpdateProperty property)
		{
			if (property.IsEnumerable)
			{
				if (property.IgnoreNull && property.IgnoreEmpty)
					return $"if({Constants.ModelParameterName}.{property.SourceName} != null && {Constants.ModelParameterName}{property.SourceName}.Count() > 0)";
				else if (property.IgnoreNull)
					return $"if({Constants.ModelParameterName}.{property.SourceName} != null)";
				else if (property.IgnoreEmpty)
					return $"if({Constants.ModelParameterName}{property.SourceName}.Count() > 0)";
				else
					return null;
			}
			else
			{
				if (property.IgnoreNull)
					return $"if({Constants.ModelParameterName}.{property.SourceName} != null)";
				else
					return null;
			}
		}


		private static string GeneratePropertySource(UpdateApiModel model, UpdateProperty property, string listName)
		{
			if (property.IsEnumerable)
			{
				var ifLine = GeneratePropertyIfSource(property);
				var setName = property.CollectionHandling switch
				{
					CollectionHandling.Set => "Set",
					CollectionHandling.AddToSet => "AddToSet",
					CollectionHandling.PushAll => "PushAll",
					CollectionHandling.PullAll => "PullAll",
					_ => null
				};

				if (setName == null)
				{
					model.Diagnostics.Add(Diagnostic.Create(Constants.CollectionHandlingNotSupported, property.SourceLocation));
					return null;
				}

				var listAdd = $"{listName}.Add(builder.{setName}(x => x.{property.TargetName}, {Constants.ModelParameterName}.{property.SourceName}));";
				return ifLine == null ? listAdd : $"{ifLine}\n\t{listAdd}";
			}
			else if (property.UseStringEmpty)
			{
				return
				$$"""
				if(string.IsNullOrEmpty({{Constants.ModelParameterName}}.{{property.SourceName}}) == false)
					{{listName}}.Add(builder.Set(x => x.{{property.TargetName}}, {{Constants.ModelParameterName}}.{{property.SourceName}}));
				""";
			}
			else
			{
				var ifLine = GeneratePropertyIfSource(property);
				var listAdd = $"{listName}.Add(builder.Set(x => x.{property.TargetName}, {Constants.ModelParameterName}.{property.SourceName}));";
				return ifLine == null ? listAdd : $"{ifLine}\n\t{listAdd}";
			}
		}

		private static string GenerateUpdateMethod(SourceProductionContext ctx, UpdateApiModel apiModel, UpdateMethod method)
		{
			var propertySources = method.Properties.SelectNonNull(x => GeneratePropertySource(apiModel, x, "list")).ToArray();
			var methodTemplate =
			$$"""
			public static UpdateDefinition<{{method.TargetClassName}}> {{method.MethodName}}(this {{apiModel.SourceClassName}} {{Constants.ModelParameterName}})
			{
				var builder = Builders<{{method.TargetClassName}}>.Update;
				var list = new List<UpdateDefinition<{{method.TargetClassName}}>>({{method.Properties.Length}});

				{{propertySources.JoinTabbed(1, "\n\n")}}

				return builder.Combine(list);
			}
			""";

			return methodTemplate;
		}

		private static void GenerateSource(SourceProductionContext ctx, UpdateApiModel model)
		{
			if (model.UpdateMethods.Any() == false)
				return;


			var methods = model.UpdateMethods.Select(x => GenerateUpdateMethod(ctx, model, x)).ToArray();
			var className = string.Format(Constants.ExtensionClassNameFormat, model.SourceClassName);
			var classCode =
			$$"""
			using System;
			using System.Collections.Generic;
			using MongoDB.Driver;	

			namespace {{Constants.ExtensionsNameSpace}}
			{
				public static class {{className}}
				{
					{{methods.JoinTabbed(2, "\n\n")}}
				}
			}
			""";

			var source = SourceText.From(classCode, Encoding.UTF8);
			ctx.AddSource($"{className}.g.cs", source);
			model.Diagnostics.ForEach(ctx.ReportDiagnostic);
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

		private static UpdateProperty? GetProperty(GeneratorSyntaxContext ctx, List<Diagnostic> diagnostics, PropertyDeclarationSyntax property)
		{
			var ignore = property.AttributeLists.SelectMany(x => x.Attributes)
				.Select(x => ctx.SemanticModel.GetSymbolInfo(x).Symbol)
				.Any(x => x != null && x.ContainingType.ToDisplayString() == Constants.UpdatePropertyIgnoreAttributeClassFullName);

			if (ignore)
				return null;

			var updateAttribute = property.AttributeLists.SelectMany(x => x.Attributes)
				.Select(x => (symbol: ctx.SemanticModel.GetSymbolInfo(x).Symbol, attr: x))
				.FirstOrDefault(x => x.symbol != null && x.symbol.ContainingType.ToDisplayString() == Constants.UpdatePropertyAttributeClassFullName);

			var methodName = updateAttribute.attr.GetNamedAttributeValueOrDefault(ctx.SemanticModel,
				Constants.UpdatePropertyAttribute_MethodName_PropertyName,
				Constants.UpdatePropertyAttribute_MethodName_DefaultValue, out _);

			var applyToAll = updateAttribute.attr.GetNamedAttributeValueOrDefault(ctx.SemanticModel,
				Constants.UpdatePropertyAttribute_ApplyToAllMethods_PropertyName,
				Constants.UpdatePropertyAttribute_ApplyToAllMethods_DefaultValue, out _);

			var isEnumerable = IsEnumerableOrArrayType(ctx, property, out var isString);

			if (updateAttribute.attr == null)
			{
				return new UpdateProperty(property.Identifier.Text, isEnumerable, methodName, applyToAll, true, property.GetLocation());
			}
			else
			{
				var semantic = ctx.SemanticModel;
				var targetPropertyName = updateAttribute.attr.GetNamedAttributeValueOrDefault(semantic,
					Constants.UpdatePropertyAttribute_TargetPropertyName_PropertyName,
					Constants.UpdatePropertyAttribute_TargetPropertyName_DefaultValue, out _);

				var ignoreEmpty = updateAttribute.attr.GetNamedAttributeValueOrDefault(semantic,
					Constants.UpdatePropertyAttribute_IgnoreEmpty_PropertyName,
					Constants.UpdatePropertyAttribute_IgnoreEmpty_DefaultValue, out var ignoreEmptyIsSet);

				var ignoreNull = updateAttribute.attr.GetNamedAttributeValueOrDefault(semantic,
					Constants.UpdatePropertyAttribute_IgnoreNull_PropertyName,
					Constants.UpdatePropertyAttribute_IgnoreNull_DefaultValue, out _);

				var collectionHandling = updateAttribute.attr.GetNamedAttributeValueOrDefault(semantic,
					Constants.UpdatePropertyAttribute_CollectionHandling_PropertyName,
					Constants.UpdatePropertyAttribute_CollectionHandling_DefaultValue, out var collectionHandlingSet);

				var useStringEmpty = updateAttribute.attr.GetNamedAttributeValueOrDefault(semantic,
					Constants.UpdatePropertyAttribute_UseStringEmpty_PropertyName,
					Constants.UpdatePropertyAttribute_UseStringEmpty_DefaultValue, out var useStringEmptySet);

				var update = new UpdateProperty(property.Identifier.Text, isEnumerable, methodName, applyToAll, false, updateAttribute.attr.GetLocation(), targetPropertyName, ignoreNull, ignoreEmpty, collectionHandling, isString && useStringEmpty);

				if(isString == false && useStringEmptySet)
					diagnostics.Add(Diagnostic.Create(Constants.UseStringEmptyNotApplicable, updateAttribute.attr.GetLocation()));

				if (isEnumerable == false && collectionHandlingSet)
					diagnostics.Add(Diagnostic.Create(Constants.CollectionHandlingNotApplicable, updateAttribute.attr.GetLocation()));

				if (isEnumerable == false && ignoreEmptyIsSet)
					diagnostics.Add(Diagnostic.Create(Constants.IgnoreEmptyNotApplicable, updateAttribute.attr.GetLocation()));

				return update;
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
				.SelectNonNull(x => GetProperty(ctx, diagnostics, x))
				.ToArray();

			var fullClassName = classDeclaration.Identifier.ToString();

			foreach (var attribute in attributes)
			{
				if (attribute == null)
					continue;

				var typeOf = (TypeOfExpressionSyntax)attribute.ArgumentList.Arguments[0].Expression;
				var entityType = ctx.SemanticModel.GetSymbolInfo(typeOf.Type).Symbol?.ToDisplayString();
				if (fullClassName == null || entityType == null)
					continue;

				var ignoreUnmarkedProperty = attribute.GetNamedAttributeValueOrDefault(ctx.SemanticModel,
					Constants.MarkerAttribute_IgnoreUnmarkedProperties_PropertyName,
					Constants.MarkerAttribute_IgnoreUnmarkedProperties_DefaultValue, out _
				);

				var methodName = attribute.GetNamedAttributeValueOrDefault(ctx.SemanticModel,
					Constants.MarkerAttribute_MethodName_PropertyName,
					Constants.MarkerAttribute_MethodName_DefaultValue, out _
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

				var propertiesForMethod = properties.Where(x => (x.ApplyToAllMethods || x.MethodName == methodName) && (x.IsUnmarked || ignoreUnmarkedProperty)).ToArray();
				var updateMethod = new UpdateMethod(entityType, methodName, propertiesForMethod);
				methods.Add(updateMethod);
			}

			return new UpdateApiModel(fullClassName, methods.ToArray(), diagnostics.ToArray());
		}
	}
}
