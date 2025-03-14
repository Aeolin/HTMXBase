using HandlebarsDotNet;

namespace MongoDBSemesterProjekt.Services.TemplateStore
{
	public interface IHtmxTemplateStore
	{
		void NotifyTemplateChanged(ModifyMode mode, string collectionSlug = null, string templateSlug = null);
		public bool HasTemplate(string collectionId, string? templateId = null);
		public bool HasDefaultTemplate(string collectionId);
		public Task<HandlebarsTemplate<object, object>> GetTemplateAsync(string collectionId, string? templateId = null);
	}
}
