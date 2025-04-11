using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HTMXBase.Utils
{
	public static class HeaderUtils
	{

		public static bool TryGetHeaderField(string header, string fieldName, [NotNullWhen(true)][MaybeNullWhen(false)] out string? value)
		{
			var index = header.IndexOf($"{fieldName}=", StringComparison.OrdinalIgnoreCase);
			if (index == -1)
			{
				value = null;
				return false;
			}

			var end = header.IndexOf(';', index);
			if (end == -1)
				end = header.Length - 1;

			value = header[index..end];
			return true;
		}

		public static Encoding GetEncodingFromContentTypeOrDefault(string header, Encoding? defaultEncoding = null)
		{
			defaultEncoding ??= Encoding.UTF8;
			if (TryGetHeaderField(header, "charset", out var charsetName))
			{
				try
				{
					return Encoding.GetEncoding(charsetName);
				}
				catch (ArgumentException)
				{
					return defaultEncoding;
				}
			}

			return defaultEncoding;
		}
	}
}
