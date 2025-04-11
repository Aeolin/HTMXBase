using MongoDB.Driver;

namespace HTMXBase.Database
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class IndexAttribute : Attribute
	{
		public string[] Properties { get; init; }
		public IndexType IndexType { get; init; }
		public bool IsUnique { get; init; } = true;

		public IndexAttribute(params string[] properties) : this(IndexType.Ascending, properties)
		{
		}

		public IndexAttribute(IndexType indexType, params string[] properties)
		{
			Properties=properties;
			IndexType=indexType;
		}

		public IndexKeysDefinition<T> GetIndex<T>(string property)
		{
			var builder = Builders<T>.IndexKeys;
			return IndexType switch
			{
				IndexType.Ascending => builder.Ascending(property),
				IndexType.Descending => builder.Descending(property),
				IndexType.Geo2D => builder.Geo2D(property),
				IndexType.Geo2DSphere => builder.Geo2DSphere(property),
				IndexType.GeoHaystack => builder.GeoHaystack(property),
				IndexType.Hashed => builder.Hashed(property),
				IndexType.Text => builder.Text(property),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public IndexKeysDefinition<T> GetIndex<T>()
		{
			if (Properties.Length == 1)
			{
				return GetIndex<T>(Properties[0]);
			}
			else
			{
				var builder = Builders<T>.IndexKeys;
				return builder.Combine(Properties.Select(GetIndex<T>).ToArray());
			}
		}

		public static IndexKeysDefinition<T> Combined<T>(IEnumerable<IndexAttribute> attributes, string propertyName) => attributes.Count() == 1 ? attributes.First().GetIndex<T>(propertyName) : Builders<T>.IndexKeys.Combine(attributes.Select(x => x.GetIndex<T>(propertyName)).ToArray());
		public static IndexKeysDefinition<T> Combined<T>(string propertyName, params IndexAttribute[] attributes) => Combined<T>(attributes.AsEnumerable(), propertyName);
	}
}
