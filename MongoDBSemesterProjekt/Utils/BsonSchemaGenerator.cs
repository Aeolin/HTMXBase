using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YamlDotNet.Serialization;
using System.ComponentModel;

namespace MongoDBSemesterProjekt.Utils
{
	public static class BsonSchemaGenerator
	{
		public static BsonType GetBsonType(IBsonSerializer serializer)
		{
			if (serializer is IHasRepresentationSerializer bsonTypeSerializer)
			{
				return bsonTypeSerializer.Representation;
			}
			else if (serializer is IBsonArraySerializer)
			{
				return BsonType.Array;
			}
			else if (serializer is IBsonDocumentSerializer)
			{
				return BsonType.Document;
			}
			else
			{
				return BsonType.Undefined;
			}
		}

		public static string ToBsonTypeName(this BsonType type)
		{
			if (type == BsonType.DateTime)
				return "date";
			else
				return type.ToString().ToCamelCase();
		}

		private static BsonType WriteElementType(IBsonWriter writer, IBsonSerializer serializer, bool isNullable)
		{
			var type = GetBsonType(serializer);
			writer.WriteName("bsonType");
			if (isNullable)
			{
				writer.WriteStartArray();
				writer.WriteString("null");
				writer.WriteString(type.ToBsonTypeName());
				writer.WriteEndArray();
			}
			else
			{
				writer.WriteString(type.ToBsonTypeName());
			}

			if (type == BsonType.Array && serializer is IBsonArraySerializer aSerializer && aSerializer.TryGetItemSerializationInfo(out var itemInfo))
			{
				writer.WriteName("items");
				writer.WriteStartDocument();
				if (itemInfo.Serializer is IBsonDocumentSerializer docSerializer)
				{
					var map = BsonClassMap.LookupClassMap(docSerializer.ValueType);
					WriteSchema(writer, map);
				}
				else
				{
					WriteElementType(writer, itemInfo.Serializer, false);
				}
				writer.WriteEndDocument();
			}

			return type;
		}

		private static void HandleMinLengthAttribute(IBsonWriter writer, BsonType bsonType, MemberInfo info)
		{
			info.IfAttribute<MinLengthAttribute>(x =>
			{
				switch (bsonType)
				{
					case BsonType.Array:
						writer.WriteName("minItems");
						writer.WriteInt32((int)x.Length);
						break;

					case BsonType.String:
						writer.WriteName("minLength");
						writer.WriteInt32((int)x.Length);
						break;
				}
			});
		}

		private static void HandleMaxLengthAttribute(IBsonWriter writer, BsonType bsonType, MemberInfo info)
		{
			info.IfAttribute<MaxLengthAttribute>(x =>
			{
				switch (bsonType)
				{
					case BsonType.Array:
						writer.WriteName("maxItems");
						writer.WriteInt32((int)x.Length);
						break;

					case BsonType.String:
						writer.WriteName("maxLength");
						writer.WriteInt32((int)x.Length);
						break;
				}
			});
		}

		private static void HandleRangeAttribute(IBsonWriter writer, BsonType bsonType, MemberInfo info)
		{
			info.IfAttribute<RangeAttribute>(x =>
			{
				writer.WriteName("minimum");
				if (x.Minimum is int intMinimum)
					writer.WriteInt32(intMinimum);
				else if (x.Minimum is double doubleMinimum)
					writer.WriteDouble(doubleMinimum);
				else
					throw new NotImplementedException($"Unsupported minimum type: {x.Minimum.GetType()}");

				writer.WriteName("maximum");
				if (x.Maximum is int intMaximum)
					writer.WriteInt32(intMaximum);
				else if (x.Maximum is double doubleMaximum)
					writer.WriteDouble(doubleMaximum);
				else
					throw new NotImplementedException($"Unsupported maximum type: {x.Maximum.GetType()}");
			});
		}


		private static void HandleEnum(IBsonWriter writer, BsonType bsonType, MemberInfo info)
		{
			var type = info switch
			{
				PropertyInfo prop => prop.PropertyType,
				FieldInfo field => field.FieldType,
				_ => throw new NotSupportedException($"Unsupported member type: {info.GetType()}")
			};


			if (type.IsEnum)
			{
				writer.WriteName("enum");
				writer.WriteStartArray();
				switch (bsonType)
				{
					case BsonType.Int32:
						{
							foreach (var value in Enum.GetValues(type))
								writer.WriteInt32((int)value);
							break;
						}

					case BsonType.Int64:
						{
							foreach (var value in Enum.GetValues(type))
								writer.WriteInt64((long)value);
							break;
						}

					case BsonType.String:
						{
							foreach (var value in Enum.GetNames(type))
								writer.WriteString(value);
							break;
						}

					default:
						throw new NotSupportedException($"Unsupported enum representation: {bsonType}");
				}

				writer.WriteEndDocument();
			}
		}

		private static void HandleDescriptionAttribute(IBsonWriter writer, BsonType type, MemberInfo info)
		{
			info.IfAttribute<DescriptionAttribute>(x =>
			{
				writer.WriteName("description");
				writer.WriteString(x.Description);
			});
		}

		private static void WriteConstrainAttributes(IBsonWriter writer, BsonType type, MemberInfo info)
		{
			HandleMinLengthAttribute(writer, type, info);
			HandleMaxLengthAttribute(writer, type, info);
			HandleRangeAttribute(writer, type, info);
			HandleDescriptionAttribute(writer, type, info);
			HandleEnum(writer, type, info);
		}


		private static void WriteElementSchema(IBsonWriter writer, BsonMemberMap map)
		{
			var serializer = map.GetSerializer();
			writer.WriteName(map.ElementName);
			writer.WriteStartDocument();

			var nullable = map.IsRequired == false && (map.MemberType.IsClass || (map.MemberType.IsGenericType && map.MemberType.GetGenericTypeDefinition() == typeof(Nullable<>)));
			var bsonType = WriteElementType(writer, serializer, nullable);

			if (serializer is IBsonDocumentSerializer docSerializer)
			{
				var nestedMap = BsonClassMap.LookupClassMap(docSerializer.ValueType);
				WriteSchema(writer, nestedMap);
			}

			WriteConstrainAttributes(writer, bsonType, map.MemberInfo);
			writer.WriteEndDocument();
		}

		public static void WriteSchema(IBsonWriter writer, BsonClassMap map)
		{
			writer.WriteName("properties");
			writer.WriteStartDocument();
			foreach (var member in map.AllMemberMaps)
				WriteElementSchema(writer, member);

			writer.WriteEndDocument();
			var required = map.AllMemberMaps.Where(x => x.IsRequired).ToArray();
			if (required.Length > 0)
			{
				writer.WriteName("required");
				writer.WriteStartArray();
				foreach (var req in required)
					writer.WriteString(req.ElementName);

				writer.WriteEndArray();
			}
		}

		public static void WriteSchema<T>(IBsonWriter writer)
		{
			var classMap = BsonClassMap.LookupClassMap(typeof(T));
			writer.WriteStartDocument();
			WriteSchema(writer, classMap);
			writer.WriteEndDocument();
		}

		public static void WriteValidator<T>(IBsonWriter writer)
		{
			writer.WriteStartDocument();
			writer.WriteName("$jsonSchema");
			WriteSchema<T>(writer);
			writer.WriteEndDocument();
		}
	}
}
