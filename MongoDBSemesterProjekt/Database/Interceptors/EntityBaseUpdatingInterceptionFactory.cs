using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Database.InterceptingShim;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Utils;

namespace MongoDBSemesterProjekt.Database.Interceptors
{
	public static class EntityBaseUpdatingInterceptionFactory
	{
		private static T InsertHandler<T>(T document) where T : EntityBase
		{
			document.CreatedAt = DateTime.UtcNow;
			document.UpdatedAt = DateTime.UtcNow;
			return document;
		}

		private static T ReplaceHandler<T>(T document) where T : EntityBase
		{
			document.UpdatedAt = DateTime.UtcNow;
			return document;
		}

		private static UpdateDefinition<T> UpdateHandler<T>(UpdateDefinition<T> update) where T : EntityBase
		{
			return update.Set(x => x.UpdatedAt, DateTime.UtcNow);
		}

		private static BsonDocument InsertHandler(BsonDocument document)
		{
			var now = DateTime.UtcNow;
			document[Constants.TIMESTAMP_CREATED_FIELD] = now;
			document[Constants.TIMESTAMP_UPDATED_FIELD] = now;
			return document;
		}

		private static BsonDocument ReplaceHandler(BsonDocument document)
		{
			document[Constants.TIMESTAMP_UPDATED_FIELD] = DateTime.UtcNow;
			return document;
		}

		private static UpdateDefinition<BsonDocument> UpdateHandler(UpdateDefinition<BsonDocument> update)
		{
			return update.Set(Constants.TIMESTAMP_UPDATED_FIELD, DateTime.UtcNow);
		}

		public static IInterceptionEvents<BsonDocument> InterceptCustomCollections()
		{
			var events = new InterceptionEvents<BsonDocument>()
			{
				InsertHandler = InsertHandler,
				ReplaceHandler = ReplaceHandler,
				UpdateHandler = UpdateHandler
			};
			return events;
		}

		public static IInterceptionEvents<T> Create<T>() where T : EntityBase
		{
			var events = new InterceptionEvents<T>()
			{
				InsertHandler = InsertHandler,
				ReplaceHandler = ReplaceHandler,
				UpdateHandler = UpdateHandler
			};

			return events;
		}
	}
}
