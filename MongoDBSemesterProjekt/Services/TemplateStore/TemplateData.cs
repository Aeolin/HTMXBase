using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Services.ModelEvents;

namespace MongoDBSemesterProjekt.Services.TemplateStore
{
	public class TemplateData
	{
		public TemplateModel? TemplateModel { get; init; }
		public string CollectionSlug { get; init; }
		public string? TemplateSlug { get; init; }

		public bool HasTemplate => TemplateSlug != null;

		public TemplateData(string collectionSlug, TemplateModel templateModel)
		{
			TemplateModel = templateModel;
			CollectionSlug = collectionSlug;
			TemplateSlug = templateModel.Slug;
		}

		public TemplateData(string collectionSlug, string? templateSlug)
		{
			CollectionSlug = collectionSlug;
			TemplateSlug = templateSlug;
		}

		public static ModifyEvent<TemplateData> Create(string collectionSlug) => new ModifyEvent<TemplateData>(ModifyMode.Add, new TemplateData(collectionSlug, (string?)null));
		public static ModifyEvent<TemplateData> Update(string collectionSlug) => new ModifyEvent<TemplateData>(ModifyMode.Modify, new TemplateData(collectionSlug, (string?)null));
		public static ModifyEvent<TemplateData> Create(string collectionSlug, TemplateModel model) => new ModifyEvent<TemplateData>(ModifyMode.Add, new TemplateData(collectionSlug, model));
		public static ModifyEvent<TemplateData> Update(string collectionSlug, TemplateModel model) => new ModifyEvent<TemplateData>(ModifyMode.Modify, new TemplateData(collectionSlug, model));
		public static ModifyEvent<TemplateData> Delete(string collectionSlug, string templateSlug) => new ModifyEvent<TemplateData>(ModifyMode.Delete, new TemplateData(collectionSlug, templateSlug));
		public static ModifyEvent<TemplateData> Delete(string collectionSlug) => new ModifyEvent<TemplateData>(ModifyMode.Delete, new TemplateData(collectionSlug, (string?)null));
	}
}
