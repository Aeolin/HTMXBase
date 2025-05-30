﻿using MongoDB.Driver;
using HTMXBase.ApiModels;
using HTMXBase.Database;
using HTMXBase.Database.Models;
using HTMXBase.Utils;
using System.Buffers.Text;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Reflection.Metadata;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Namotion.Reflection;
using NJsonSchema;

namespace HTMXBase.Services.Pagination
{
	public class PaginationService<T> : IPaginationService<T>
	{
		private static Dictionary<string, PaginationCollectionMeta> _collectionMetaCache = new();
		private static readonly TypeMetaCache _metaCache = new(typeof(T));

		private readonly IMongoDatabase _db;

		public PaginationService(IMongoDatabase db)
		{
			_db=db;
		}

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

		private static FilterDefinition<T>? BuildFilter(PaginationValues values, PaginationCollectionMeta meta, PaginationDirection direction, string propertyName, string propertyValue)
		{
			if (values.Columns.Contains(propertyName) &&
				meta.TryGetPropertyType(propertyName, out var type) && 
				BsonHelper.TryParseFromBsonType(type, propertyValue, out var parsed, BsonHelper.ParseDateTimeTicks))
			{
				var builder = Builders<T>.Filter;
				return (direction == PaginationDirection.Backward) ^ values.Ascending ? builder.Gt(propertyName, parsed) : builder.Lte(propertyName, parsed);
			}

			return null;
		}

		private static PaginationDirection DecomposeCursor(PaginationValues values, PaginationCollectionMeta meta, ICollection<FilterDefinition<T>> filterList)
		{
			var cursor = string.Empty;
			var direction = PaginationDirection.Default;
			if (string.IsNullOrEmpty(values.CursorPrevious) == false)
			{
				direction = PaginationDirection.Backward;
				cursor = values.CursorPrevious;
			}
			else if (string.IsNullOrEmpty(values.CursorNext) == false)
			{
				direction = PaginationDirection.Forward;
				cursor = values.CursorNext;
			}
			else
			{
				return direction;
			}


			unsafe
			{
				var byteLen = Encoding.UTF8.GetByteCount(cursor);
				Span<byte> buffer = stackalloc byte[byteLen];
				Encoding.UTF8.GetBytes(cursor, buffer);
				if (Base64.DecodeFromUtf8InPlace(buffer, out var written) == OperationStatus.Done)
				{
					var cursorString = Encoding.UTF8.GetString(buffer.Slice(0, written));
					foreach (var property in cursorString.Split('&'))
					{
						var split = property.Split('=');
						if (split.Length < 2)
							continue;

						var filter = BuildFilter(values, meta, direction, split[0], HttpUtility.UrlDecode(split[1]));
						if (filter != null)
							filterList.Add(filter);
					}
				}
			}

			return direction;
		}

		private static SortDefinition<T> BuildSort(PaginationValues values, PaginationDirection direction, string column)
		{
			return (direction == PaginationDirection.Backward) ^ values.Ascending ? Builders<T>.Sort.Ascending(column) : Builders<T>.Sort.Descending(column);
		}

		private static SortDefinition<T> BuildSort(PaginationValues values, PaginationDirection direction)
		{
			var builder = Builders<T>.Sort;
			var sorts = values.Columns.Select(x => BuildSort(values, direction, x)).ToArray();
			return sorts.Length == 1 ? sorts.First() : builder.Combine(sorts);
		}

		private static string? EncodeCursorElement(string col, T doc)
		{	
			if (_metaCache.Properties.TryGetValue(col, out var property))
			{
				var value = property.GetValue(doc);
				if (value is DateTime dt)
					return $"{col}={dt.Ticks}";

				if (value != null)
					return $"{col}={HttpUtility.UrlEncode(value.ToString())}";
			}

			return null;
		}

		private static string ToB64String(string @string)
		{
			var bytes = Encoding.UTF8.GetBytes(@string);
			return Convert.ToBase64String(bytes);
		}

		private static string? EncodeCursor(PaginationValues values, T? doc)
		{
			if (doc == null)
				return null;

			if (doc is BsonDocument bDoc)
				return EncodeCursor(values, bDoc);

			var cursor = string.Join('&', values.Columns.Select(x => EncodeCursorElement(x, doc)).Where(x => x != null));
			return ToB64String(cursor);
		}

		private static string? EncodeCursorElement(string column, BsonDocument doc)
		{
			if (doc.TryGetValue(column, out var value))
			{
				var obj = value.ToObject();
				if (obj is DateTime dt)
					return $"{column}={dt.Ticks}";

				if (obj != null)
					return $"{column}={HttpUtility.UrlEncode(obj.ToString())}";
			}

			return null;
		}

		private static string EncodeCursor(PaginationValues values, BsonDocument doc)
		{
			var cursor = string.Join('&', values.Columns.Select(x => EncodeCursorElement(x, doc)).Where(x => x != null));
			return ToB64String(cursor);
		}

		public Task<CursorResult<T, string?>> PaginateAsync(PaginationValues values, ICollection<FilterDefinition<T>>? filterList = null)
		{
			var collectionName = _db.GetCollectionName<T>();
			if (string.IsNullOrEmpty(collectionName))
				throw new ArgumentException($"Can't derive collection name from type {typeof(T).FullName}");

			return PaginateAsync(collectionName, values, filterList);
		}

		public Task<CursorResult<T, string?>> PaginateAsync(string collectionName, PaginationValues values, ICollection<FilterDefinition<T>>? filterList = null)
		{
			return PaginateAsync(collectionName, values, x => x.ToAsyncEnumerable(), filterList);
		}

		public Task<CursorResult<TRes, string?>> PaginateAsync<TRes>(PaginationValues values, Func<IEnumerable<T>, IAsyncEnumerable<TRes>> mapper, ICollection<FilterDefinition<T>>? filterList = null)
		{
			var collectionName = _db.GetCollectionName<T>();
			return PaginateAsync(collectionName, values, mapper, filterList);
		}


		public async Task<CursorResult<TRes, string?>> PaginateAsync<TRes>(string collectionName, PaginationValues values, Func<IEnumerable<T>, IAsyncEnumerable<TRes>> mapper, ICollection<FilterDefinition<T>>? filterList = null)
		{
			filterList ??= new List<FilterDefinition<T>>();
			var meta = await GetCollectionMetaAsync(collectionName);
			if (meta == null)
				throw new ArgumentException($"Unknown Collection {collectionName}");

			var direction = DecomposeCursor(values, meta, filterList);
			var sort = BuildSort(values, direction);
			var data = await _db.GetCollection<T>(collectionName)
				.Find(filterList.CombineWithAnd())
				.Sort(sort)
				.Limit(values.Limit + 1)
				.ToListAsync();

			bool hasMore = data.Count > values.Limit;
			IEnumerable<T> toMap = data;
			if (direction == PaginationDirection.Backward)
			{
				data.Reverse();
				if (hasMore)
					toMap = toMap.Skip(1);
			}

			var mapped = await mapper(toMap.Take(values.Limit)).ToArrayAsync();
			switch (direction)
			{
				case PaginationDirection.Forward:
					{
						var next = hasMore ? EncodeCursor(values, data[^2]) : null;
						return new CursorResult<TRes, string?>(mapped, next, values.CursorNext);
					}

				case PaginationDirection.Backward:
					{
						var previous = hasMore ? EncodeCursor(values, data[0]) : null;
						return new CursorResult<TRes, string?>(mapped, values.CursorPrevious, previous);
					}

				case PaginationDirection.Default:
					{
						var next = hasMore ? EncodeCursor(values, data[^2]) : null;
						return new CursorResult<TRes, string?>(mapped, next, null);
					}

				default:
					throw new InvalidOperationException("Unknown PaginationDirection");
			}
		}


	}
}
