using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text;
using System.Text.Json;

namespace MongoDBSemesterProjekt.Serializers
{
	public class JsonDocumentSerializer : IBsonSerializer<JsonDocument>, IHasRepresentationSerializer
	{
		public JsonDocumentSerializer(BsonType underlyingType = BsonType.Binary)
		{
			if (underlyingType != BsonType.String && underlyingType != BsonType.Binary)
				throw new ArgumentException("Invalid representation type", nameof(underlyingType));

			Representation = underlyingType;
		}

		public BsonType Representation { get; init; }
		public Type ValueType => typeof(JsonDocument);

		public JsonDocument Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			return Representation switch
			{
				BsonType.String => JsonDocument.Parse(context.Reader.ReadString()),
				BsonType.Binary => JsonDocument.Parse(context.Reader.ReadBinaryData().Bytes),
			};
		}

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonDocument value)
		{
			switch (Representation)
			{
				case BsonType.String:
					context.Writer.WriteString(value.RootElement.ToString());
					break;

				case BsonType.Binary:
					{
						using var memoryStream = new MemoryStream();
						using var writer = new Utf8JsonWriter(memoryStream);
						value.WriteTo(writer);
						context.Writer.WriteBinaryData(memoryStream.ToArray());
						break;
					}
			}
		}

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
		{
			if (value is JsonDocument document)
				Serialize(context, args, document);
		}

		object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			return Deserialize(context, args);
		}
	}
}
