using System.Text.Json;

namespace MongoDBSemesterProjekt.Models
{
	public class CollectionModel : EntityBase
	{
		public const string CollectionName = "collections";


		public string Slug { get; set; }
		public string Name { get; set; }
		public TimeSpan? CacheRetentionTime { get; set; }
		public required JsonDocument Schema { get; set; }
		public IList<TemplateModel> Templates { get; set; } = new List<TemplateModel>();

		public string? DefaultTemplate { get; set; }
		public string? QueryPermission { get; set; }
		public string? InsertPermission { get; set; }
		public string? ModifyPermission { get; set; }
		public string? DeletePermission { get; set; }
	}
}
