using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDBSemesterProjekt.BsonSchema;
using System.Collections;
using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
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

		public static string ToCamelCase(this string @string) => @string.Length > 1 ? char.ToLower(@string[0]) + @string[1..] : @string?.ToLower();

	

		public static async Task CreateCollectionWithSchemaAsync<T>(this IMongoDatabase database, string name)
		{
			var schema = new BsonDocument();
			using var writer = new BsonDocumentWriter(schema);
			BsonSchemaGenerator.WriteValidator<T>(writer);

			var creationOptions = new CreateCollectionOptions<T>
			{
				Validator = schema
			};

			await database.CreateCollectionAsync(name, creationOptions);
		}

		private static readonly CreateIndexOptions UniqueIndexOptions = new CreateIndexOptions { Unique = true };
		public static async Task CreateUniqueKeyAsync<T>(this IMongoCollection<T> collection, Expression<Func<T, object>> selector, bool ascending = true)
		{
			var keys = ascending ? Builders<T>.IndexKeys.Ascending(selector) : Builders<T>.IndexKeys.Descending(selector);
			var model = new CreateIndexModel<T>(keys, UniqueIndexOptions);
			await collection.Indexes.CreateOneAsync(model);
		}

		public static UpdateDefinition<MongoDBSemesterProjekt.Models.CollectionModel> ToAddTemplate(this MongoDBSemesterProjekt.ApiModels.ApiTemplate apiModel)
		{
			var builder = Builders<MongoDBSemesterProjekt.Models.CollectionModel>.Update;
			var list = new List<UpdateDefinition<MongoDBSemesterProjekt.Models.CollectionModel>>(1);

			//if (apiModel != null)
			//	list.Add(builder.Push(x => x.Templates));

			return builder.Combine(list);
		}

		public static string UrlEncode(this string @string) => System.Web.HttpUtility.UrlEncode(@string);

		public static FrozenSet<string> GetPermissions(this ClaimsPrincipal principal) => principal == null ? FrozenSet<string>.Empty : principal.FindAll(x => x.Type == Constants.PERMISSION_CLAIM).Select(x => x.Value).ToFrozenSet();
		public static bool HasPermission(this ClaimsPrincipal principal, string permission) => principal.HasClaim(x => x.Type == Constants.PERMISSION_CLAIM && x.Value == permission);
		public static string GetIdentifier(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier);
		public static ObjectId GetIdentifierId(this ClaimsPrincipal principal) => ObjectId.Parse(principal.GetIdentifier());

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
