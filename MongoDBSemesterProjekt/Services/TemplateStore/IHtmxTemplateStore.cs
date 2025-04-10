using HandlebarsDotNet;
using MongoDBSemesterProjekt.Services.ModelEvents;

namespace MongoDBSemesterProjekt.Services.TemplateStore
{
	public interface IHtmxTemplateStore
	{
		public bool HasTemplate(string collectionId, string? templateId = null);
		public bool HasDefaultTemplate(string collectionId);
		public Task<HandlebarsTemplate<object, object>> GetTemplateAsync(string collectionId, string? templateId = null);

		public Task<HandlebarsTemplate<object, object>?> GetStaticTemplateAsync(string identifier, CancellationToken token = default);
	}
}
