using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDBSemesterProjekt.BsonSchema;
using MongoDBSemesterProjekt.Database.InterceptingShim;
using MongoDBSemesterProjekt.Database.Models;
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

namespace MongoDBSemesterProjekt.Utils
{
	public static class Extensions
	{
		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			foreach (var item in items)
				collection.Add(item);
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

			if(propertyFound && result is T tResult)
			{
				value = tResult;
				return true;
			}

			value = default;
			return false;
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

		public static IEnumerable<T> SelectWhere<T, S>(this IEnumerable<S> items, Func<S, (bool keep, T mapped)> mapper)
		{
			foreach(var item in items)
			{
				var (keep, mapped) = mapper(item);
				if (keep)
					yield return mapped;
			}
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

		public static IServiceCollection AddEntityUpdateInterceptors(this IServiceCollection services)
		{
			var entityTypes = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsAssignableTo<EntityBase>() && x.IsAbstract == false).ToArray();
			foreach (var entityType in entityTypes)
			{
				var interceptor = typeof(EntityBaseUpdatingInterceptionFactory).GetMethod(nameof(EntityBaseUpdatingInterceptionFactory.Create)).MakeGenericMethod(entityType).Invoke(null, null);
				services.AddSingleton(typeof(IInterceptionEvents<>).MakeGenericType(entityType), interceptor);
			}

			services.AddSingleton(EntityBaseUpdatingInterceptionFactory.InterceptCustomCollections());
			return services;
		}

		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var item in collection)
				action?.Invoke(item);
		}

	}
}
