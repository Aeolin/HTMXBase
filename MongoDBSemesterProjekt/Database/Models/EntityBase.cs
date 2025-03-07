using AwosFramework.Generators.MongoDBUpdateGenerator;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using System.Text.Json.Serialization;

namespace MongoDBSemesterProjekt.Database.Models
{
	public abstract class EntityBase
	{
		[BsonId(IdGenerator = typeof(ObjectIdGenerator))]
		[BsonElement("_id")]
		[JsonPropertyName("_id")]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}
}
