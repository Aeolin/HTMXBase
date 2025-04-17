using MongoDB.Bson;

namespace HTMXBase.Database.Models
{
	public enum UrlQueryBsonType
	{
		Double = BsonType.Double,
		String = BsonType.String,
		Binary = BsonType.Binary,
		ObjectId = BsonType.ObjectId,
		Boolean = BsonType.Boolean,
		DateTime = BsonType.DateTime,
		Null = BsonType.Null,
		Int32 = BsonType.Int32,
		Int64 = BsonType.Int64,
		Decimal128 = BsonType.Decimal128
	}
}
