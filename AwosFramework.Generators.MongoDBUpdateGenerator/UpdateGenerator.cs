using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
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
			});

			IncrementalValuesProvider<UpdateToGenerate?> updatesToGenerate = context.SyntaxProvider
				.CreateSyntaxProvider(
					predicate: IsSyntaxTargetForGeneration,       
					transform: Transform
				)
				.Where(x => x != null);

			context.RegisterSourceOutput(updatesToGenerate, GenerateSource);
		}

		static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken token) => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;
	
		static void GenerateSource(SourceProductionContext ctx, UpdateToGenerate update)
		{

		}

		static UpdateToGenerate? Transform(GeneratorSyntaxContext ctx, CancellationToken token)
		{
			var classDeclaration = (ClassDeclarationSyntax)ctx.Node;
			var attribute = classDeclaration.AttributeLists.SelectMany(x => x.Attributes)
				.Select(x => (symbol: ctx.SemanticModel.GetSymbolInfo(x).Symbol, attr: x))
				.Where(x => x.symbol != null && x.symbol.ContainingType.ToDisplayString() == Constants.MarkerAttributeClassFullName)
				.Select(x => x.attr)
				.FirstOrDefault();

			if(attribute == null)
				return null;

			var fullClassName = classDeclaration.Identifier.ToString();
			var typeOf = (TypeOfExpressionSyntax)attribute.ArgumentList.Arguments[0].Expression;
			var entityType = ctx.SemanticModel.GetSymbolInfo(typeOf.Type).Symbol?.ToDisplayString();
			if(fullClassName == null || entityType == null)
				return null;

			var propertyNames = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().Select(x => x.Identifier.Text).ToArray();
			return new UpdateToGenerate(entityType, fullClassName, propertyNames);
		}
	}
}
