using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MongoDBSemesterProjekt.Serializers
{
	public class BsonDocumentConverter : JsonConverter<BsonDocument>
	{
		public override BsonDocument? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var doc = JsonSerializer.Deserialize<JsonDocument>(ref reader, options);
			return BsonDocument.Parse(doc.RootElement.ToString());
		}

		public override void Write(Utf8JsonWriter writer, BsonDocument value, JsonSerializerOptions options)
		{
			var dict = BsonSerializer.Deserialize<Dictionary<string, object?>>(value);
			JsonSerializer.Serialize(writer, dict, options);
		}
	}
}
