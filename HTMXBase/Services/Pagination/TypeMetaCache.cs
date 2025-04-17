using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Frozen;
using System.Reflection;

namespace HTMXBase.Services.Pagination
{
	public class TypeMetaCache
	{
		public Type Type { get; init; }
		public FrozenDictionary<string, PropertyInfo> Properties { get; init; }

		public TypeMetaCache(Type type)
		{
			Type = type;
			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			var propertyDict = properties.Where(x => x.IsSpecialName).ToDictionary(x => x.Name, x => x);
			foreach (var property in properties)
			{
				var attr = property.GetCustomAttribute<BsonElementAttribute>();
				if (attr != null && attr.ElementName != property.Name)
					propertyDict[attr.ElementName] = property;
				
			}

			Properties = propertyDict.ToFrozenDictionary();
		}
	}
}
