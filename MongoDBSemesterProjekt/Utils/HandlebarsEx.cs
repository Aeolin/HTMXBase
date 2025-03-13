using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using HandlebarsDotNet.PathStructure;
using Markdig;
using System.Collections.Frozen;

namespace MongoDBSemesterProjekt.Utils
{
	public static class HandlebarsEx
	{
		public static IHandlebars Create(Action<HandlebarsConfiguration> configure = null)
		{
			var cfg = new HandlebarsConfiguration();
			configure?.Invoke(cfg);
			var handleBars = Handlebars.Create(cfg);
			HandlebarsHelpers.Register(handleBars);
			handleBars.RegisterExistsLengthHelper();
			return handleBars;
		}

		public static IHandlebars RegisterEqualsHelper(this IHandlebars handleBars)
		{
			handleBars.RegisterHelper("eq", EqualsImpl);
			return handleBars;
		}

		public static IHandlebars RegisterMarkdownHelper(this IHandlebars handleBars)
		{
			handleBars.RegisterHelper("render-md", MarkdownImpl);
			return handleBars;
		}

		public static IHandlebars RegisterExistsLengthHelper(this IHandlebars handleBars)
		{
			handleBars.RegisterHelper("exists-lengt", ExistsMinLengthImpl);
			return handleBars;
		}

		public static IHandlebars RegisterNumberComparisonHelper(this IHandlebars handleBars)
		{
			handleBars.RegisterHelper("ncmp", NumberComparisonImpl);
			return handleBars;
		}

		private static void EqualsImpl(EncodedTextWriter output, BlockHelperOptions options, Context ctx, Arguments args)
		{
			if (args.Length != 2)
				throw new HandlebarsException("{{eq}} helper must have 2 arguments");

			if (args[0]?.Equals(args[1]) ?? (args[1] == null))
			{
				options.Template(output, ctx);
			}
			else
			{
				options.Inverse(output, ctx);
			}
		}

		private static readonly MarkdownPipeline AdvancedPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

		private static void MarkdownImpl(EncodedTextWriter output, Context context, Arguments arguments)
		{
			if (arguments.Length != 1)
				throw new HandlebarsException("{{render-md}} helper must have 1 argument");

			var markdown = arguments[0] as string;
			if (markdown == null)
				throw new HandlebarsException("{{render-md}} helper must have a string argument");

			output.WriteSafeString(Markdig.Markdown.ToHtml(markdown, AdvancedPipeline));
		}

		private static void NumberComparisonImpl(EncodedTextWriter output, BlockHelperOptions options, Context ctx, Arguments args)
		{
			if (args.Length != 3)
				throw new HandlebarsException("{{ncmp}} helper must have 3 arguments");

			var op = args[1] as string;
			if (op == null || INT_COMPARISON_OPS.ContainsKey(op) == false)
				throw new HandlebarsException("{{ncmp}} helper must have a valid comparison operator as the second argument");

			if (args[0] is not int leftInt)
				leftInt = ctx.GetValue<int?>(args[0] as string) ?? throw new HandlebarsException("{{ncmp}} expected a number from left hand property");

			if (args[2] is not int rightInt)
				rightInt = ctx.GetValue<int?>(args[2] as string)?? throw new HandlebarsException("{{ncmp}} expected a number from right hand property");

			if (INT_COMPARISON_OPS[op](leftInt, rightInt))
			{
				options.Template(output, ctx);
			}
			else
			{
				options.Inverse(output, ctx);
			}
		}

		private static readonly FrozenDictionary<string, Func<int, int, bool>> INT_COMPARISON_OPS = new Dictionary<string, Func<int, int, bool>>
		{
			["=="] = (a, b) => a == b,
			["!="] = (a, b) => a != b,
			[">"] = (a, b) => a > b,
			[">="] = (a, b) => a >= b,
			["<"] = (a, b) => a < b,
			["<="] = (a, b) => a <= b
		}.ToFrozenDictionary();

		private static void ExistsMinLengthImpl(EncodedTextWriter output, BlockHelperOptions options, Context ctx, Arguments args)
		{
			void handleImpl(string propertyExists, string propertyLength, string op, int minLength)
			{
				if (string.IsNullOrEmpty(propertyExists) == false && INT_COMPARISON_OPS[op](propertyLength?.Length ?? 0, minLength))
				{
					options.Template(output, ctx);
				}
				else
				{
					options.Inverse(output, ctx);
				}
			}

			if (args.Length == 3)
			{
				if (args[0] is not string existsProperty || args[1] is not string op || INT_COMPARISON_OPS.ContainsKey(op) == false || args[2] is not int minLength)
					throw new HandlebarsException("{{exists-length}} helper must have 3 arguments of type string, string and int");

				handleImpl(existsProperty, existsProperty, op, minLength);
			}
			else if (args.Length == 4)
			{
				if (args[0] is not string existsProperty || args[1] is not string lengthProperty || args[2] is not string op || INT_COMPARISON_OPS.ContainsKey(op) == false || args[3] is not int minLength)
					throw new HandlebarsException("{{exists-length}} helper must have 4 arguments of type string, string, string and int");

				handleImpl(existsProperty, lengthProperty, op, minLength);
			}
			else
			{
				throw new HandlebarsException("{{exists-length}} helper must have 2 or 3 arguments");
			}
		}
	}
}
