using HandlebarsDotNet;

namespace MongoDBSemesterProjekt.Services.TemplateStore
{
	public interface IHtmxTemplateStore
	{
		public bool HasTemplate(string collectionId, string? templateId = null);
		public Task<HandlebarsTemplate<object, object>> GetTemplateAsync(string collectionId, string? templateId = null);
	}
}
