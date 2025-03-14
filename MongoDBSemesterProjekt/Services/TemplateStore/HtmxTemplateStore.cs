using HandlebarsDotNet;
using Microsoft.VisualBasic;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Services.ObjectCache;
using MongoDBSemesterProjekt.Utils;
using System.Collections.Frozen;

namespace MongoDBSemesterProjekt.Services.TemplateStore
{
	public class HtmxTemplateStore : IHtmxTemplateStore
	{
		private readonly IMongoDatabase _db;
		private readonly IInMemoryCache<string, HandlebarsTemplate<object, object>> _cache;
		private readonly IHandlebars _handleBars;
		private readonly HashSet<string> _collectionIds = new HashSet<string>();
		private Task _initTask;

		public HtmxTemplateStore(IMongoDatabase mongoClient, IInMemoryCache<string, HandlebarsTemplate<object, object>> cache, IHandlebars handleBars)
		{
			_db=mongoClient;
			_cache=cache;
			_handleBars=handleBars;
			_initTask = InitializeAsync_Impl();
		}

		public async Task InitializeAsync()
		{
			await _initTask;
		}

		private async Task InitializeAsync_Impl()
		{
			var collections = await _db.GetCollection<CollectionModel>("collections").Find(x => x.Templates.Count > 0).ToListAsync();
			foreach (var collection in collections)
			{
				_collectionIds.AddRange(collection.Templates.Where(x => x.Disabled == false).Select(x => MakeKey(collection.Slug, x.Slug)));
				if(string.IsNullOrEmpty(collection.DefaultTemplate) == false && collection.Templates.Any(y => y.Slug == collection.DefaultTemplate))
				{
					_collectionIds.Add(MakeKey(collection.Slug));
				}
			}
		}

		private async Task UpdateAsync(string collectionSlug)
		{
			var collection = await _db.GetCollection<CollectionModel>("collections").Find(x => x.Templates.Count > 0 && x.Slug == collectionSlug).FirstOrDefaultAsync();
			if (collection != null)
				_collectionIds.AddRange(collection.Templates.Where(x => x.Disabled == false).Select(x => MakeKey(collection.Slug, x.Slug)));
		}

		public void NotifyTemplateChanged(ModifyMode mode, string? collectionSlug = null, string? templateSlug = null)
		{
			switch (mode)
			{
				case ModifyMode.Delete:
					{
						if (templateSlug == null)
						{
							var toRemove = _collectionIds.Where(x => x.StartsWith(collectionSlug + ":")).ToArray();
							toRemove.ForEach(x =>
							{
								_collectionIds.Remove(x);
								_cache.Unset(x);
							});
						}
						else
						{
							_collectionIds.Remove(MakeKey(collectionSlug, templateSlug));
							_cache.Unset(MakeKey(collectionSlug, templateSlug));
						}
						break;
					}

				case ModifyMode.Modify:
					{
						if (collectionSlug != null && templateSlug != null)
							_cache.Unset(MakeKey(collectionSlug, templateSlug));

						break;
					}

				case ModifyMode.Add:
					{
						if (collectionSlug != null)
						{
							if (templateSlug == null)
							{
								_initTask.Wait();
								_initTask = UpdateAsync(collectionSlug);
							}
							else
							{
								_collectionIds.Add(MakeKey(collectionSlug, templateSlug));
							}
						}
						break;
					}
			}
		}

		private string MakeKey(string collectionId, string? templateId = null) => $"{collectionId}:{templateId}";

		public async Task<HandlebarsTemplate<object, object>> GetTemplateAsync(string collectionId, string? templateId = null)
		{
			await _initTask;
			var key = MakeKey(collectionId, templateId);
			if (_cache.TryGet(key, out var template))
				return template;

			template = await CompileTemplateAsync(collectionId, templateId);
			_cache.Set(key, template);
			return template;
		}

		private async Task<HandlebarsTemplate<object, object>> CompileTemplateAsync(string collectionId, string? templateId = null)
		{
			var collection = await _db.GetCollection<CollectionModel>("collections").FindAsync(x => x.Slug == collectionId);
			var model = await collection.FirstOrDefaultAsync();
			templateId ??= model.DefaultTemplate;
			if (templateId == null)
				return null;

			var template = model?.Templates.First(x => string.Equals(x.Slug, templateId));
			return _handleBars.Compile(template.Template);
		}

		public bool HasTemplate(string collectionId, string? templateId = null)
		{
			_initTask.GetAwaiter().GetResult();
			return _collectionIds.Contains(MakeKey(collectionId, templateId));
		}

		public bool HasDefaultTemplate(string collectionId) => HasTemplate(collectionId);
	}
}
