using AutoMapper;
using AwosFramework.Generators.MongoDBUpdateGenerator;
using MongoDBSemesterProjekt.Database.Models;
using System.Text.Json;

namespace MongoDBSemesterProjekt.Api.Models
{
	[AutoMap(typeof(CollectionModel))]
	[MongoDBUpdate(typeof(CollectionModel))]
	public class ApiCollection
	{
		public string Slug { get; set; }
		public string Name { get; set; }
		public TimeSpan? CacheRetentionTime { get; set; }

		[UpdatePropertyIgnore]
		public JsonDocument Schema { get; set; }

		[UpdatePropertyIgnore]
		public ApiTemplate[] Templates { get; set; }

		[UpdatePropertyIgnore]
		public bool IsInbuilt { get; set; }

		public string? DefaultTemplate { get; set; }
		public string? QueryPermission { get; set; }
		public string? InsertPermission { get; set; }
		public string? ModifyPermission { get; set; }
		public string? DeletePermission { get; set; }
		public string? ComplexQueryPermission { get; set; }
	}
}
