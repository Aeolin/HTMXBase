using MongoDB.Bson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MongoDBSemesterProjekt.Utils
{
	public class ObjectIdConverter : JsonConverter<ObjectId>
	{
		public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return ObjectId.Parse(reader.GetString());
		}

		public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString());
		}
	}
}
