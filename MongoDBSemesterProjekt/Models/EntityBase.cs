using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace MongoDBSemesterProjekt.Models
{
	public class EntityBase
	{
		[BsonId]
		[BsonElement("_id")]
		[JsonPropertyName("_id")]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}
}
