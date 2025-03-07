using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Authorization;
using MongoDBSemesterProjekt.Database;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Database.Session;
using System.Reflection;
using AwosFramework.Generators.MongoDBUpdateGenerator.Extensions;

namespace MongoDBSemesterProjekt.Utils
{
	public static class Seeding
	{
		public static async Task CreateCollectionsAsync(IMongoDatabase db)
		{
			var collectionNames = await db.ListCollectionNames().ToListAsync();

			if (collectionNames.Count == 0)
			{
				await db.CreateInbuiltCollectionAsync<CollectionModel>();
				var tasks = Assembly.GetExecutingAssembly()
					.GetTypes()
					.Where(x => x.IsAssignableTo<EntityBase>() && x.IsAbstract == false && x.IsNot<CollectionModel>())
					.Select(x => db.CreateInbuiltCollectionAsync(x));

				await Task.WhenAll(tasks);
			}
		}

		public static async Task UpdatePermissionsAsync(IMongoDatabase db)
		{
			var permissionAttributes = typeof(Program).Assembly.GetTypes()
				.SelectMany(x => x.GetMethods())
				.SelectMany(x => x.GetCustomAttributes<PermissionAttribute>())
				.SelectMany(x => x.Groups.Select(y => new { Permission = x.Permission, Group = y }))
				.GroupBy(x => x.Group);

			var collection = db.GetCollection<GroupModel>(GroupModel.CollectionName);
			foreach (var permission in permissionAttributes)
			{
				var model = new GroupModel
				{
					Name = permission.Key,
					Slug = permission.Key.ToLower(),
					Permissions = permission.Select(x => x.Permission).Distinct().ToList(),
					Description = $"Autogenrated group {permission.Key}",
				};

				var updated = await collection.UpdateOneAsync(x => x.Slug == model.Slug, model.ToUpdate());
				if (updated.MatchedCount == 0)
					await collection.InsertOneAsync(model);
			}
		}
	}
}
