using MongoDB.Bson;
using HTMXBase.Database.Models;
using HTMXBase.Utils;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace HTMXBase.Services.Pagination
{
	public class PaginationCollectionMeta
	{
		public string CollectionName { get; init; }
		public JsonDocument Schema { get; init; }
		private FrozenDictionary<string, BsonType> _propertyTypes;

		private static void ParsePropertyType(JsonElement schema, Dictionary<string, BsonType> types, string path = null)
		{
			if (schema.TryGetProperty("properties", out var propertyDoc))
			{
				foreach (var property in propertyDoc.EnumerateObject())
				{
					var name = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
					if (property.Value.TryGetProperty("bsonType", out var bsonTypeElement) && bsonTypeElement.ValueKind == JsonValueKind.String)
					{
						var bsonType = BsonHelper.FromBsonTypeName(bsonTypeElement.GetString()!);
						types[name] = bsonType;
						if (bsonType == BsonType.Document)
						{
							ParsePropertyType(property.Value, types, name);
						}
						else if (bsonType == BsonType.Array && property.Value.TryGetProperty("items", out var arrayElement))
						{
							ParsePropertyType(arrayElement, types, name);
						}
					}
				}
			}
		}

		public PaginationCollectionMeta(CollectionModel model)
		{
			this.CollectionName = model.Slug;
			this.Schema = model.Schema;
			var typeDict = new Dictionary<string, BsonType>();
			ParsePropertyType(model.Schema.RootElement, typeDict);
			_propertyTypes = typeDict.ToFrozenDictionary();
		}

		public bool TryGetPropertyType(string property, out BsonType type)
		{
			return _propertyTypes.TryGetValue(property, out type);
		}

		public bool TryGetPropertyValue(string property, Dictionary<string, object?> routeValues, [NotNullWhen(true)] out object? parsed)
		{
			if (TryGetPropertyType(property, out var bsonType) && routeValues.TryGetValue(property, out var routeValObj) && routeValObj is string routeVal)
				return BsonHelper.TryParseFromBsonType(bsonType, routeVal, out parsed);

			parsed = null;
			return false;
		}
	}
}
