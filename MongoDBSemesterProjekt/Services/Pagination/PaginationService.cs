using MongoDB.Driver;
using MongoDBSemesterProjekt.ApiModels;
using MongoDBSemesterProjekt.Database;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Utils;

namespace MongoDBSemesterProjekt.Services.Pagination
{
	public class PaginationService<T> : IPaginationService<T>
	{
		private static Dictionary<string, PaginationCollectionMeta> _collectionMetaCache = new();

		private readonly IMongoDatabase _db;

		private async Task<PaginationCollectionMeta?> GetCollectionMetaAsync(string collectionSlug)
		{
			if(_collectionMetaCache.TryGetValue(collectionSlug, out var meta) == false)
			{
				var collectionMeta = await _db.GetCollection<CollectionModel>().Find(x => x.Slug == collectionSlug).FirstOrDefaultAsync();
				if (collectionMeta == null)
					return null;

				meta = new PaginationCollectionMeta(collectionMeta);
				_collectionMetaCache[collectionSlug] = meta;
			}

			return meta;
		}


		public Task<CursorResult<T, string>> PaginateAsync(PaginationValues values, ICollection<FilterDefinition<T>>? filterList = null)
		{
			var collectionName = _db.GetCollectionName<T>();
			if(string.IsNullOrEmpty(collectionName))
				throw new ArgumentException($"Can't derive collection name from type {typeof(T).FullName}");

			return PaginateAsync(collectionName, values, filterList);
		}

		public async Task<CursorResult<T, string>> PaginateAsync(string collectionName, PaginationValues values, ICollection<FilterDefinition<T>>? filterList = null)
		{
			filterList ??= new List<FilterDefinition<T>>();
			var meta = await GetCollectionMetaAsync(collectionName);
			if (meta == null)
				throw new ArgumentException($"Unknown Collection {collectionName}");

			var direction = values.DecomposeCursor(out var body);
			foreach(var column in values.Columns)
			{

			}

		}

	}
}
