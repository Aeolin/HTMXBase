using MongoDB.Driver;
using MongoDBSemesterProjekt.ApiModels;
using MongoDBSemesterProjekt.Database;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Utils;
using System.Buffers.Text;
using System.Buffers;
using System.Text;
using System.Text.Json;

namespace MongoDBSemesterProjekt.Services.Pagination
{
	public class PaginationService<T> : IPaginationService<T>
	{
		private static Dictionary<string, PaginationCollectionMeta> _collectionMetaCache = new();

		private readonly IMongoDatabase _db;

		private async Task<PaginationCollectionMeta?> GetCollectionMetaAsync(string collectionSlug)
		{
			if (_collectionMetaCache.TryGetValue(collectionSlug, out var meta) == false)
			{
				var collectionMeta = await _db.GetCollection<CollectionModel>().Find(x => x.Slug == collectionSlug).FirstOrDefaultAsync();
				if (collectionMeta == null)
					return null;

				meta = new PaginationCollectionMeta(collectionMeta);
				_collectionMetaCache[collectionSlug] = meta;
			}

			return meta;
		}

		private static FilterDefinition<T> BuildFilter(PaginationValues values, PaginationCollectionMeta meta, string propertyName, string propertyValue)
		{
			if(values.Columns.Contains(propertyName) && meta.TryGetPropertyType(propertyName, out var type) && BsonHelper.TryParseFromBsonType(type, propertyValue, out var parsed))
			{
				return Builders<T>.Filter.Eq(propertyName, parsed);
			}

			return null;
		}

		private static PaginationDirection DecomposeCursor(PaginationValues values, PaginationCollectionMeta meta, ICollection<FilterDefinition<T>> filterList)
		{
			var cursor = "{}";
			var direction = PaginationDirection.Forward;
			if (string.IsNullOrEmpty(values.CursorPrevious) == false)
			{
				direction = PaginationDirection.Backward;
				cursor = values.CursorPrevious;
			}

			if (string.IsNullOrEmpty(values.CursorNext) == false)
			{
				direction = PaginationDirection.Forward;
				cursor = values.CursorNext;
			}

			unsafe
			{
				var byteLen = Encoding.UTF8.GetByteCount(cursor);
				Span<byte> buffer = stackalloc byte[Base64.GetMaxDecodedFromUtf8Length(byteLen)];
				Encoding.UTF8.GetBytes(cursor, buffer);
				if (Base64.DecodeFromUtf8InPlace(buffer, out var written) == OperationStatus.Done)
				{
					var reader = new Utf8PaginationValuesReader(buffer.Slice(0, written));
					while(reader.ReadNextProperty(out var name, out var value))
					{
						var filter = BuildFilter(values, meta, name, value);
					}
				}
			}

			return direction;
		}

		public Task<CursorResult<T, string>> PaginateAsync(PaginationValues values, ICollection<FilterDefinition<T>>? filterList = null)
		{
			var collectionName = _db.GetCollectionName<T>();
			if (string.IsNullOrEmpty(collectionName))
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
			foreach (var column in values.Columns)
			{

			}

		}

	}
}
