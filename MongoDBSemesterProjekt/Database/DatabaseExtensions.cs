using MongoDB.Bson.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.BsonSchema;
using System.Linq.Expressions;
using MongoDBSemesterProjekt.Utils;
using MongoDBSemesterProjekt.Database.Models;
using System.Text.Json;
using System.Xml.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using AutoMapper.Internal;

namespace MongoDBSemesterProjekt.Database
{
	public static class DatabaseExtensions
	{
		public static async Task CreateInbuiltCollectionAsync(this IMongoDatabase database, Type type)
		{
			var method = typeof(DatabaseExtensions)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(x => x.Name == nameof(CreateInbuiltCollectionAsync) && x.GetGenericArguments().Length == 1);

			var task = (Task)method.MakeGenericMethod(type).Invoke(null, [database]);
			await task;
		}

		public static async Task CreateInbuiltCollectionAsync<T>(this IMongoDatabase database)
		{
			var collectionName = database.GetCollectionName<T>();
			var schema = await CreateCollectionWithSchemaAsync_Impl<T>(database, collectionName);
			await FindAndCreateIndexesAsync_Impl<T>(database, collectionName);
			await InsertInbuiltCollectionAsync_Impl(database, collectionName, JsonDocument.Parse(schema.ToJson()));
		}

		private static async Task InsertInbuiltCollectionAsync_Impl(IMongoDatabase database, string collectionName, JsonDocument schema)
		{
			var model = new CollectionModel
			{
				Name = collectionName.ToPascalCase(),
				Slug = collectionName.ToLower(),
				IsInbuilt = true,
				Schema = schema,
				CacheRetentionTime = null
			};

			var collectionCollection = database.GetCollection<CollectionModel>(CollectionModel.CollectionName);
			await collectionCollection.InsertOneAsync(model);
		}

		public static async Task InsertInbuiltCollectionAsync<T>(this IMongoDatabase database)
		{
			var collectionName = database.GetCollectionName<T>();
			var opts = new ListCollectionsOptions { Filter = Builders<BsonDocument>.Filter.Eq("name", collectionName) };
			var collectionInfo = await database.ListCollections(opts).FirstOrDefaultAsync();
			var schemaDoc = collectionInfo?["options"]?["validator"]?["$jsonSchema"];
			if (schemaDoc == null)
				throw new ArgumentException($"Collection {collectionName} does not have a schema, call {nameof(CreateCollectionWithSchemaAsync)} first");

			var schema = JsonDocument.Parse(schemaDoc.ToJson());
			await InsertInbuiltCollectionAsync_Impl(database, collectionName, schema);
		}

		private static async Task<BsonDocument> CreateCollectionWithSchemaAsync_Impl<T>(IMongoDatabase database, string name)
		{
			var schema = new BsonDocument();
			using var writer = new BsonDocumentWriter(schema);
			BsonSchemaGenerator.WriteValidator<T>(writer);

			var creationOptions = new CreateCollectionOptions<T>
			{
				Validator = schema
			};

			await database.CreateCollectionAsync(name, creationOptions);
			return schema;
		}

		public static async Task CreateCollectionWithSchemaAsync<T>(this IMongoDatabase database, string name)
		{
			await CreateCollectionWithSchemaAsync_Impl<T>(database, name);
		}

		public static string GetCollectionName<T>(this IMongoDatabase database)
		{
			var type = typeof(T);
			if (type.TryGetStaticValue<string>("CollectionName", out var collectionName) == false)
				throw new ArgumentException($"Type {type} does not have a static CollectionName field");

			return collectionName;
		}

		private static readonly CreateIndexOptions UniqueIndexOptions = new CreateIndexOptions { Unique = true };
		private static readonly CreateIndexOptions NonUniqueIndexOptions = new CreateIndexOptions { Unique = false };
		private static async Task FindAndCreateIndexesAsync_Impl<T>(IMongoDatabase database, string collectionName)
		{
			var type = typeof(T);
			var collection = database.GetCollection<T>(collectionName);
			var classIndexes = type.GetCustomAttributes<IndexAttribute>().ToArray();
			var createIndexModels = classIndexes.Select(x =>
			{
				var opts = x.IsUnique ? UniqueIndexOptions : NonUniqueIndexOptions;
				return new CreateIndexModel<T>(x.GetIndex<T>(), opts);
			}).ToList();

			var propertyIndexes = type.GetProperties()
				.Select(x => (name: x.Name, attr: x.GetCustomAttributes<IndexAttribute>()))
				.Where(x => x.attr.Any());

			foreach (var (name, indexGroup) in propertyIndexes)
			{
				var options = indexGroup.Any(x => x.IsUnique) ? UniqueIndexOptions : NonUniqueIndexOptions;
				var model = new CreateIndexModel<T>(IndexAttribute.Combined<T>(indexGroup, name), options);
				createIndexModels.Add(model);
			}

			if(createIndexModels.Count > 0)
				await collection.Indexes.CreateManyAsync(createIndexModels);
		}

		public static async Task FindAndCreateIndexesAsync<T>(IMongoDatabase database)
		{
			var collectionName = database.GetCollectionName<T>();
			await FindAndCreateIndexesAsync_Impl<T>(database, collectionName);
		}

		public static async Task CreateUniqueKeyAsync<T>(this IMongoCollection<T> collection, Expression<Func<T, object>> selector, bool ascending = true)
		{
			var keys = ascending ? Builders<T>.IndexKeys.Ascending(selector) : Builders<T>.IndexKeys.Descending(selector);
			var model = new CreateIndexModel<T>(keys, UniqueIndexOptions);
			await collection.Indexes.CreateOneAsync(model);
		}
	}
}
