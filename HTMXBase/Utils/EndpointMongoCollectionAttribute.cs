using Microsoft.AspNetCore.Http.Metadata;
using System.Reflection;

namespace HTMXBase.Utils
{
	[AttributeUsage(AttributeTargets.Method)]
	public class EndpointMongoCollectionAttribute : Attribute, IEndpointMetadataProvider
	{
		public string CollectionSlug { get; set; }

		public EndpointMongoCollectionAttribute(string collectionSlug)
		{
			CollectionSlug=collectionSlug;
		}

		public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
		{
			var attribute = method.GetCustomAttribute<EndpointMongoCollectionAttribute>();
			if(attribute != null)
				builder.Metadata.Add(attribute);
		}
	}
}
