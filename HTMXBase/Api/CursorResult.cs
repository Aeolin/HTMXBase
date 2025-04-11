using MongoDB.Bson;
using HTMXBase.Database.Models;

namespace HTMXBase.ApiModels
{
	public class CursorResult<TItem, TCursor>
	{
		public TCursor Next { get; init; }
		public TCursor Previous { get; init; }
		public TItem[] Items { get; init; }

		public CursorResult(TItem[] data, TCursor next, TCursor previous)
		{
			Next=next;
			Previous = previous;
			Items=data;
		}

	}

	public class ObjectIdCursorResult<TItem> : CursorResult<TItem, ObjectId?>
	{
		public ObjectIdCursorResult(TItem[] data, ObjectId? next, ObjectId? previous) : base(data, next, previous)
		{
		}
	}


	public static class CursorResult
	{
		public static ObjectId GetId<TItem>(TItem item)
		{
			if (item is EntityBase eBase)
				return eBase.Id;

			if(item is BsonDocument doc && doc.TryGetValue("_id", out var id))
				return id.AsObjectId;

			throw new ArgumentException("Item does not have an id");
		}

		public static ObjectIdCursorResult<TTarget> FromCollection<TSource, TTarget>(IEnumerable<TSource> data, int limit, ObjectId? current, Func<TSource, TTarget> convert, Func<TSource, ObjectId>? idGetter = null)
		{
			idGetter ??= GetId<TSource>;
			var items = data.Select(convert).ToArray();
			ObjectId? next = items.Length == limit ? idGetter(data.Last()) : null;
			return new ObjectIdCursorResult<TTarget>(items, next, current);
		}

		public static ObjectIdCursorResult<TItem> FromCollection<TItem>(IEnumerable<TItem> data, int limit, ObjectId? current, Func<TItem, ObjectId>? idGetter = null)
		{
			idGetter ??= GetId<TItem>;
			var items = data.ToArray();
			ObjectId? next = items.Length == limit ? idGetter(items.Last()) : null;
			return new ObjectIdCursorResult<TItem>(items, next, current);
		}

		public static CursorResult<TItem, TCursor> Create<TItem, TCursor>(IEnumerable<TItem> data, TCursor next, TCursor previous) => new CursorResult<TItem, TCursor>(data.ToArray(), next, previous);
	}
}
