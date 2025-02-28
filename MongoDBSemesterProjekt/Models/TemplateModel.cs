namespace MongoDBSemesterProjekt.Models
{
	public class TemplateModel
	{

		public bool SingleItem { get; set; } = true;
		public required string Slug { get; set; }
		public required string Template { get; set; }
	}
}
