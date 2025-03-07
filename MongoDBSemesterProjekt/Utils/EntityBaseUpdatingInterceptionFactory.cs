using MongoDB.Driver;
using MongoDBSemesterProjekt.Database.InterceptingShim;
using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Utils
{
	public class EntityBaseUpdatingInterceptionFactory
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
