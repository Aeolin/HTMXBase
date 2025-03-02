using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;

namespace MongoDBSemesterProjekt.Models
{
	public class IdOnly
	{
		[BsonId]
		[BsonElement("_id")]
		[JsonPropertyName("_id")]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; }
	}
}
