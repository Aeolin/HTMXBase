using AutoMapper;
using MongoDBSemesterProjekt.Models;
using System.Text.Json;

namespace MongoDBSemesterProjekt.ApiModels
{
	[AutoMap(typeof(CollectionModel))]
	public class ApiCollection
	{
		public string Slug { get; set; }
		public string Name { get; set; }
		public TimeSpan? CacheRetentionTime { get; set; }
		public JsonDocument Schema { get; set; }
		public ApiTemplate[] Templates { get; set; }
		public bool IsInbuilt { get; set; }
		public string? DefaultTemplate { get; set; }
		public string? QueryPermission { get; set; }
		public string? InsertPermission { get; set; }
		public string? ModifyPermission { get; set; }
		public string? DeletePermission { get; set; }
		public string? ComplexQueryPermission { get; set; }
	}
}
