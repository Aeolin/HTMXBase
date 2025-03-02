using HandlebarsDotNet;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
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

		public static FrozenSet<string> GetPermissions(this ClaimsPrincipal principal) => principal == null ? FrozenSet<string>.Empty : principal.FindAll(x => x.Type == Constants.PERMISSION_CLAIM).Select(x => x.Value).ToFrozenSet();
		public static bool HasPermission(this ClaimsPrincipal principal, string permission) => principal.HasClaim(x => x.Type == Constants.PERMISSION_CLAIM && x.Value == permission);
		public static string GetIdentifier(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier);
		public static ObjectId GetIdentifierId(this ClaimsPrincipal principal) => ObjectId.Parse(principal.GetIdentifier());

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
