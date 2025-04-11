using Markdig.Parsers;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using Namotion.Reflection;
using Newtonsoft.Json.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using static HTMXBase.Utils.Extensions;

namespace HTMXBase.Utils
{
	public static class BsonHelper
	{
		public static TryParseDelegate<DateTime> ParseDateTimeDetect = ParseDateTimeDetectImpl;
		public static TryParseDelegate<DateTime> ParseDateTimeTicks = ParseDateTimeTicksImpl;
		public static TryParseDelegate<DateTime> ParseDateTimeNormal = DateTime.TryParse;

		private static bool ParseDateTimeTicksImpl(string value, out DateTime dt)
		{
			if (long.TryParse(value, out var ticks))
			{
				dt = new DateTime(ticks);
				return true;
			}

			dt = default;
			return false;
		}

		private static bool ParseDateTimeDetectImpl(string value, out DateTime dt)
		{
			if (DateTime.TryParse(value, out dt))
				return true;
			
			if(long.TryParse(value, out var ticks))
			{
				dt = new DateTime(ticks);
				return true;
			}

			dt = default;
			return false;
		}



		public static bool TryParseFromBsonType(BsonType type, string rawValue, out object? value, TryParseDelegate<DateTime> dtParser = null)
		{
			dtParser ??= ParseDateTimeDetect;
			if (string.IsNullOrEmpty(rawValue) || rawValue.Equals("null"))
			{
				value = null;
				return true;
			}

			(bool success, object value) result = type switch
			{
				BsonType.String => (true, rawValue),
				BsonType.Int32 => (int.TryParse(rawValue, out var i), i),
				BsonType.Int64 => (long.TryParse(rawValue, out var l), l),
				BsonType.Double => (double.TryParse(rawValue, out var d), d),
				BsonType.Decimal128 => (decimal.TryParse(rawValue, out var d), d),
				BsonType.Boolean => (bool.TryParse(rawValue, out var b), b),
				BsonType.DateTime => (dtParser(rawValue, out var dt), dt),
				BsonType.ObjectId => (ObjectId.TryParse(rawValue, out var oid), oid),
				BsonType.Binary => (true, Convert.FromBase64String(rawValue)),
				_ => (false, null)
			};

			value = result.value;
			return result.success;
		}

		public static BsonType FromBsonTypeName(string name)
		{
			return name switch
			{
				"double" => BsonType.Double,
				"string" => BsonType.String,
				"object" => BsonType.Document,
				"array" => BsonType.Array,
				"binData" => BsonType.Binary,
				"undefined" => BsonType.Undefined,
				"objectId" => BsonType.ObjectId,
				"bool" => BsonType.Boolean,
				"date" => BsonType.DateTime,
				"null" => BsonType.Null,
				"regex" => BsonType.RegularExpression,
				"javascript" => BsonType.JavaScript,
				"symbol" => BsonType.Symbol,
				"int" => BsonType.Int32,
				"timestamp" => BsonType.Timestamp,
				"long" => BsonType.Int64,
				"decimal" => BsonType.Decimal128,
				"minKey" => BsonType.MinKey,
				"maxKey" => BsonType.MaxKey,
				_ => throw new ArgumentException($"Unknown BsonType {name}", nameof(name))
			};
		}

		public static bool TryGetBsonTypeFromSchema(string property, JsonElement properties, out BsonType type, out bool isNullable)
		{
			if (properties.TryGetProperty(property, out var propertySchema))
			{
				var bsonType = propertySchema.GetProperty("bsonType");
				if (bsonType.ValueKind == JsonValueKind.Array)
				{
					var values = bsonType.EnumerateArray().Select(x => FromBsonTypeName(x.GetString())).ToArray();
					isNullable = values.Contains(BsonType.Null);
					type = values.FirstOrDefault(x => x != BsonType.Null);
				}
				else
				{
					type = FromBsonTypeName(bsonType.GetString());
					isNullable = false;
				}

				return true;
			}

			type = BsonType.Undefined;
			isNullable = false;
			return false;
		}

		private static BsonArray JsonToBsonDocumentWithSchema_ImplArray(JsonElement array, JsonElement properties)
		{
			if (array.ValueKind != JsonValueKind.Array)
				throw new InvalidOperationException($"Field is not an array");

			if (TryGetBsonTypeFromSchema("items", array, out var type, out _))
			{
				var result = new BsonArray();
				if(type == BsonType.Array)
				{
					var arrayProperties = properties.GetProperty("items");
					foreach (var item in array.EnumerateArray())
					{
						result.Add(JsonToBsonDocumentWithSchema_ImplArray(item, arrayProperties));
					}
				}
				else if (type == BsonType.Document)
				{
					var docProperties = array.GetProperty("items").GetProperty("properties");
					foreach (var item in array.EnumerateArray())
					{
						result.Add(JsonToBsonDocumentWithSchema_Impl(item, docProperties));
					}
				}
				else
				{
					foreach(var item in array.EnumerateArray())
					{
						var stringValue = item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText();
						if (TryParseFromBsonType(type, stringValue, out var value))
						{
							result.Add(BsonValue.Create(value));
						}
					}
				}

				return result;
			}

			return null;
		}

		private static BsonDocument JsonToBsonDocumentWithSchema_Impl(JsonElement document, JsonElement properties)
		{
			var result = new BsonDocument();
			foreach (var property in document.EnumerateObject())
			{
				var jsonProperty = document.GetProperty(property.Name);
				if (TryGetBsonTypeFromSchema(property.Name, properties, out var bsonType, out var nullable))
				{
					if (bsonType == BsonType.Array)
					{
						result.Add(property.Name, JsonToBsonDocumentWithSchema_ImplArray(jsonProperty, properties.GetProperty(property.Name)));
					}
					else
					{
						var stringValue = jsonProperty.ValueKind == JsonValueKind.String ? jsonProperty.GetString() : jsonProperty.GetRawText();
						if (TryParseFromBsonType(bsonType, stringValue, out var value))
						{
							if (value == null && nullable == false)
								throw new InvalidOperationException($"Field {property.Name} is not nullable");

							result.Add(property.Name, BsonValue.Create(value));
						}
					}
				}
			}

			return result;
		}

		public static BsonDocument JsonToBsonDocumentWithSchema(JsonDocument document, JsonDocument schema)
		{
			return JsonToBsonDocumentWithSchema_Impl(document.RootElement, schema.RootElement.GetProperty("properties"));
		}
	}
}
