using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwosFramework.Generators.MongoDBUpdateGenerator
{
	public static class Extensions
	{
		public static T GetNamedAttributeValueOrDefault<T>(this AttributeSyntax attribute, SemanticModel model, string propertyName, T defaultValue, out bool wasSet)
		{
			if (attribute == null)
			{
				wasSet = false;
				return defaultValue;
			}

			var argument = attribute.ArgumentList.Arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.Text == propertyName);
			if (argument == null)
			{
				wasSet = false; 
				return defaultValue;
			}

			var value = model.GetConstantValue(argument.Expression);

			if (value.HasValue == false)
			{
				wasSet = false;
				return defaultValue;
			}

			if (value.Value is T valueT)
			{
				wasSet = true;
				return valueT;
			}
			else
			{
				try
				{
					wasSet = true;
					return (T)value.Value;
				}
				catch (InvalidCastException)
				{
					wasSet = false;
					return defaultValue;
				}
			}
		}

		public static string JoinTabbed(this IEnumerable<string> strings, int tabLevel, string separator = "\n")
		{
			var tabs = new string('\t', tabLevel);
			return string.Join($"{separator}{tabs}", strings.Select(x => x.Replace("\n", $"\n{tabs}")));
		}

		public static IEnumerable<P> SelectNonNull<T, P>(this IEnumerable<T> collection, Func<T, P> selector)
		{
			foreach (var item in collection)
			{
				var result = selector(item);
				if (result != null)
					yield return result;
			}
		}

		public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action, int? maxCount = short.MaxValue)
		{
			foreach (var item in enumerable)
			{
				action(item);
				if (maxCount.HasValue && --maxCount == 0)
					break;
			}
		}

		public static void PublishInternalEnum<T>(this IncrementalGeneratorPostInitializationContext ctx, string fileName = null, string nameSpace = Constants.NameSpace) where T : Enum
		{
			var enumType = typeof(T);
			var underlyingType = enumType.GetEnumUnderlyingType();
			fileName ??= $"{enumType.Name}.g.cs";
			var enumValues = Enumerable.Zip(Enum.GetNames(enumType), (T[])Enum.GetValues(enumType), (n, v) => $"{n} = {Convert.ChangeType(v, underlyingType)}");


			var source =
			$$"""
			using System;

			namespace {{nameSpace}}
			{
				public enum {{enumType.Name}} : {{underlyingType.FullName}}
				{
					{{enumValues.JoinTabbed(2, "\n,")}}
				}
			}
			""";

			ctx.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
		}
	}
}
