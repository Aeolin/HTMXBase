using System.Text.Json;

namespace MongoDBSemesterProjekt.ApiModels
{
	public class ApiCollection
	{
		public string Slug { get; set; }
		public string Name { get; set; }
		public TimeSpan? CacheRetentionTime { get; set; }
		public JsonDocument Schema { get; set; }
		public ApiTemplate[] Temapltes { get; set; }

		public string? DefaultTemplate { get; set; }
		public string? QueryPermission { get; set; }
		public string? InsertPermission { get; set; }
		public string? ModifyPermission { get; set; }
		public string? DeletePermission { get; set; }
		public string? ComplexQueryPermission { get; set; }
	}
}
