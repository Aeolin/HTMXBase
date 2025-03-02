using HandlebarsDotNet;
using System.Security.Claims;

namespace MongoDBSemesterProjekt.Utils
{
	public static class Extensions
	{
		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			foreach (var item in items)
				collection.Add(item);
		}

		public static string UrlEncode(this string @string) => System.Web.HttpUtility.UrlEncode(@string);

		public static bool HasPermission(this ClaimsPrincipal principal, string permission) => principal.HasClaim(x => x.Type == Constants.PERMISSION_CLAIM_TYPE && x.Value == permission);
		public static string GetIdentifier(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier);
		public static Guid GetIdentifierGuid(this ClaimsPrincipal principal) => Guid.Parse(principal.GetIdentifier());

		public static V GetOrAdd<K, V>(this Dictionary<K, V> dict, K key, Func<V> value)
		{
			if(dict.TryGetValue(key, out var result) == false)
			{
				result = value();
				dict.Add(key, result);
			}
			
			return result;
		}

		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var item in collection)
				action?.Invoke(item);
		}

	}
}
