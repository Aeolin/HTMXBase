using AutoMapper.Internal;
using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using HTMXBase.ApiModels;
using HTMXBase.BsonSchema;
using HTMXBase.Database.InterceptingShim;
using HTMXBase.Database.Models;
using System.Collections;
using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace HTMXBase.Utils
{
	public static class Extensions
	{
		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			foreach (var item in items)
				collection.Add(item);
		}

		public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
		{
			foreach (var item in source)
			{
				yield return item;
				await Task.Yield(); 
			}
		}

		public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source)
		{
			var list = new List<T>();
			await foreach (var item in source)
				list.Add(item);

			return list.ToArray();
		}


		public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> enumerable) => enumerable.Where(x => x != null);

		public static IEnumerable<T> DefaultIfNullOrEmpty<T>(this IEnumerable<T>? enumerable, T defaultValue = default)
		{
			if (enumerable == null || !enumerable.Any())
				return [defaultValue];

			return enumerable;
		}

		public static void IfAttribute<T>(this MemberInfo type, Action<T> attributeAction) where T : Attribute
		{
			var attributes = type.GetCustomAttributes<T>();
			foreach (var attribute in attributes)
				attributeAction(attribute);
		}

		public static bool TryGetStaticValue<T>(this Type type, string name, [MaybeNullWhen(false)][NotNullWhen(true)] out T value)
		{
			var member = type.GetMember(name, BindingFlags.Public | BindingFlags.Static).FirstOrDefault();
			if (member == null)
			{
				value = default;
				return false;
			}

			var (result, propertyFound) = member switch
			{
				PropertyInfo properterty => (properterty.GetValue(null), true),
				FieldInfo field => (field.GetValue(null), true),
				_ => (default, false)
			};

			if (propertyFound && result is T tResult)
			{
				value = tResult;
				return true;
			}

			value = default;
			return false;
		}

		public static object? ToObject(this BsonValue value)
		{
			return value.BsonType switch
			{
				BsonType.Null => null,
				BsonType.Double => value.AsDouble,
				BsonType.String => value.AsString,
				BsonType.Document => value.AsBsonDocument,
				BsonType.Array => value.AsBsonArray,
				BsonType.Binary => value.AsByteArray,
				BsonType.Undefined => null,
				BsonType.ObjectId => value.AsObjectId,
				BsonType.Boolean => value.AsBoolean,
				BsonType.DateTime => value.ToUniversalTime(),
				BsonType.RegularExpression => value.AsBsonRegularExpression,
				BsonType.JavaScript => value.AsBsonJavaScript,
				BsonType.JavaScriptWithScope => value.AsBsonJavaScriptWithScope,
				BsonType.Symbol => value.AsBsonSymbol,
				BsonType.Int32 => value.AsInt32,
				BsonType.Timestamp => value.AsBsonTimestamp,
				BsonType.Int64 => value.AsInt64,
				BsonType.Decimal128 => value.AsDecimal128,
				BsonType.MinKey => value.AsBsonMinKey,
				BsonType.MaxKey => value.AsBsonMaxKey,
				_ => null
			};
		}

		public delegate bool TryParseDelegate<T>(string input, out T result);
		public static TryParseDelegate<T>? GetTryParse<T>()
		{
			var type = typeof(T);
			var tryParseMethod = type.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, [typeof(string), typeof(T).MakeByRefType()]);
			if (tryParseMethod == null)
				return null;

			return tryParseMethod.CreateDelegate<TryParseDelegate<T>>();
		}

		public static bool TryGetValue<K, V>(this IEnumerable<KeyValuePair<K, V>> collection, K key, [MaybeNullWhen(false)][NotNullWhen(true)] out V value)
		{
			foreach (var kvp in collection)
			{
				if (kvp.Key.Equals(key))
				{
					value = kvp.Value;
					return true;
				}
			}
			value = default;
			return false;
		}

		public static V GetParsedValueOrDefault<V>(this IEnumerable<KeyValuePair<string, StringValues>> collection, string key, V defaultValue)
		{
			if (collection.TryGetValue(key, out var value))
			{
				if (value.Count == 0)
					return defaultValue;

				if (typeof(V).IsAssignableFrom(typeof(IEnumerable<string>)))
					return (V)(IEnumerable<string>)value;	

				var del = GetTryParse<V>();
				if (del?.Invoke(value[0], out var result) == true)
				{
					return result;
				}
			}
			return defaultValue;
		}

		public static V GetParsedValueOrDefault<V>(this IReadOnlyDictionary<string, object?> dict, string key, V defaultValue) => GetParsedValueOrDefault<string, V>(dict, key, defaultValue);
		public static V GetParsedValueOrDefault<K, V>(this IReadOnlyDictionary<K, object?> dict, K key, V defaultValue)
		{
			if (dict.TryGetValue(key, out var value))
			{
				if (value is V v)
					return v;

				if (typeof(V).IsAssignableFrom(typeof(IEnumerable<string>)))
					return (V)(IEnumerable<string>)value;

				if (value is string valueStr)
				{
					var del = GetTryParse<V>();
					if (del?.Invoke(valueStr, out var result) == true)
					{
						return result;
					}
				}
			}


			return defaultValue;
		}

		[return: NotNullIfNotNull(nameof(@string))]
		public static string? ToCamelCase(this string @string) => @string == null ? null : (@string.Length > 1 ? char.ToLower(@string[0]) + @string[1..] : @string.ToLower());

		[return: NotNullIfNotNull(nameof(@string))]
		public static string? ToPascalCase(this string @string) => @string == null ? null : (@string.Length > 1 ? char.ToUpper(@string[0]) + @string[1..] : @string.ToUpper());

		public static bool IsAssignableTo<T>(this Type type) => type.IsAssignableTo(typeof(T));
		public static bool IsNot<T>(this Type type) => type != typeof(T);

		public static string UrlEncode(this string @string) => System.Web.HttpUtility.UrlEncode(@string);

		public static FrozenSet<string> GetPermissions(this ClaimsPrincipal principal) => principal == null ? FrozenSet<string>.Empty : principal.FindAll(x => x.Type == Constants.PERMISSION_CLAIM).Select(x => x.Value).ToFrozenSet();
		public static bool HasPermission(this ClaimsPrincipal principal, string permission) => principal.HasClaim(x => x.Type == Constants.PERMISSION_CLAIM && x.Value == permission);
		public static string GetIdentifier(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier);
		public static ObjectId? GetIdentifierId(this ClaimsPrincipal principal)
		{
			var id = principal.GetIdentifier();
			if (ObjectId.TryParse(id, out var objId))
				return objId;

			return null;
		}

		public static IFindFluent<StaticContentModel, StaticContentModel> FindByPath(this IMongoCollection<StaticContentModel> contentCollection, string? path)
		{
			if (ObjectId.TryParse(path, out var objId))
				return contentCollection.Find(x => x.Id == objId);

			if (string.IsNullOrEmpty(path))
				return contentCollection.Find(x => (x.Slug == null || x.Slug == "") && (x.VirtualPath == null || x.VirtualPath == ""));

			var lastSlash = path.LastIndexOf('/');
			var slug = path.Substring(lastSlash + 1);
			var virtualPath = lastSlash == -1 ? null : path.Substring(0, lastSlash);
			return contentCollection.Find(x => x.Slug == slug && x.VirtualPath == virtualPath);
		}




		public static FilterDefinition<TItem> CombineWithAnd<TItem>(this IEnumerable<FilterDefinition<TItem>> filters)
		{
			var count = filters?.Count() ?? 0;
			if (count == 0)
				return Builders<TItem>.Filter.Empty;
			else if (count == 1)
				return filters!.First();
			else
				return Builders<TItem>.Filter.And(filters);
		}

		public static FilterDefinition<TItem> CombineFilters<TItem>(FilterDefinition<TItem>? filter = null, ICollection<FilterDefinition<TItem>> filterList = null)
		{
			if (filterList == null || filterList.Count == 0)
				return filter ?? Builders<TItem>.Filter.Empty;

			if (filter != null)
				filterList.Add(filter);

			return filterList.Count == 1 ? filterList.First() : Builders<TItem>.Filter.And(filterList);
		}

		public static IEnumerable<T> SelectWhere<T, S>(this IEnumerable<S> items, Func<S, (bool keep, T mapped)> mapper)
		{
			foreach (var item in items)
			{
				var (keep, mapped) = mapper(item);
				if (keep)
					yield return mapped;
			}
		}

		public static string Until(this string @string, string until)
		{
			var index = @string.IndexOf(until);
			if (index == -1)
				return @string;

			return @string.Substring(0, index);
		}

		public static JsonDocument ToJsonDocument(this BsonDocument document)
		{
			return JsonDocument.Parse(document.ToJson());
		}

		public static V GetOrAdd<K, V>(this Dictionary<K, V> dict, K key, Func<V> value)
		{
			if (dict.TryGetValue(key, out var result) == false)
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
