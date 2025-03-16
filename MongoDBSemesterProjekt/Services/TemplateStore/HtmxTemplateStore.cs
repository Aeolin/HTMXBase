using HandlebarsDotNet;
using Microsoft.VisualBasic;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Services.ModelEvents;
using MongoDBSemesterProjekt.Services.ObjectCache;
using MongoDBSemesterProjekt.Utils;
using System.Collections.Frozen;
using System.Threading.Channels;

namespace MongoDBSemesterProjekt.Services.TemplateStore
{
	public class HtmxTemplateStore : BackgroundService, IHtmxTemplateStore
	{
		private readonly IMongoDatabase _db;
		private readonly IInMemoryCache<string, HandlebarsTemplate<object, object>> _cache;
		private readonly IHandlebars _handleBars;
		private readonly ChannelReader<ModifyEvent<TemplateData>> _modifyEvents;
		private readonly HashSet<string> _collectionIds = new HashSet<string>();

		public HtmxTemplateStore(IMongoDatabase mongoClient, IInMemoryCache<string, HandlebarsTemplate<object, object>> cache, IHandlebars handleBars, ChannelReader<ModifyEvent<TemplateData>> modifyEvents)
		{
			_db=mongoClient;
			_cache=cache;
			_handleBars=handleBars;
			_modifyEvents=modifyEvents;
		}

		public override async Task StartAsync(CancellationToken cancellationToken)
		{
			await InitializeAsync();
			await base.StartAsync(cancellationToken);
		}

		public override Task StopAsync(CancellationToken cancellationToken)
		{
			return base.StopAsync(cancellationToken);
		}


		private async Task InitializeAsync()
		{
			var collections = await _db.GetCollection<CollectionModel>("collections").Find(x => x.Templates.Count > 0).ToListAsync();
			foreach (var collection in collections)
			{
				_collectionIds.AddRange(collection.Templates.Where(x => x.Disabled == false).Select(x => MakeKey(collection.Slug, x.Slug)));
				if (string.IsNullOrEmpty(collection.DefaultTemplate) == false && collection.Templates.Any(y => y.Slug == collection.DefaultTemplate))
				{
					_collectionIds.Add(MakeKey(collection.Slug));
				}
			}
		}

		private async Task HandleUpdateCollectionAsync(string collectionSlug)
		{
			var collection = await _db.GetCollection<CollectionModel>("collections").Find(x => x.Templates.Count > 0 && x.Slug == collectionSlug).FirstOrDefaultAsync();
			if (collection != null)
				_collectionIds.AddRange(collection.Templates.Where(x => x.Disabled == false).Select(x => MakeKey(collection.Slug, x.Slug)));
		}

		private void HandleUpdateTemplate(TemplateModel template, string collectionSlug)
		{
			var key = MakeKey(collectionSlug, template.Slug);
			_collectionIds.Add(key);
			_cache.Set(key, CompileTemplate(template));
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (await _modifyEvents.WaitToReadAsync(stoppingToken))
			{
				while (_modifyEvents.TryRead(out var modifyEvent))
				{
					await HandleTemplateChangeAsync(modifyEvent);
				}
			}
		}

		private void HandleRemoveTemplate(string collectionSlug, string templateSlug)
		{
			var key = MakeKey(collectionSlug, templateSlug);
			_collectionIds.Remove(key);
			_cache.Unset(key);
		}

		private void HandleRemoveCollection(string collectionSlug)
		{
			var toRemove = _collectionIds.Where(x => x.StartsWith(collectionSlug + ":")).ToArray();
			toRemove.ForEach(x =>
			{
				_collectionIds.Remove(x);
				_cache.Unset(x);
			});
		}

		private async Task HandleTemplateChangeAsync(ModifyEvent<TemplateData> modifyEvent)
		{
			var template = modifyEvent.Item;
			switch (modifyEvent.Mode)
			{
				case ModifyMode.Add:
				case ModifyMode.Modify:
					if (template.HasTemplate)
						HandleUpdateTemplate(template.TemplateModel!, template.CollectionSlug);
					else
						await HandleUpdateCollectionAsync(template.CollectionSlug);

					break;

				case ModifyMode.Delete:
					if (template.HasTemplate)
						HandleRemoveTemplate(template.CollectionSlug, template.TemplateSlug!);
					else
						HandleRemoveCollection(template.CollectionSlug);

					break;
			}
		}


		private string MakeKey(string collectionId, string? templateId = null) => $"{collectionId}:{templateId}";

		public async Task<HandlebarsTemplate<object, object>> GetTemplateAsync(string collectionId, string? templateId = null)
		{
			var key = MakeKey(collectionId, templateId);
			if (_cache.TryGet(key, out var template))
				return template;

			template = await CompileTemplateAsync(collectionId, templateId);
			_cache.Set(key, template);
			return template;
		}

		private HandlebarsTemplate<object, object> CompileTemplate(TemplateModel template)
		{
			return _handleBars.Compile(template.Template);
		}

		private async Task<HandlebarsTemplate<object, object>?> CompileTemplateAsync(string collectionId, string? templateId = null)
		{
			var collection = await _db.GetCollection<CollectionModel>("collections").FindAsync(x => x.Slug == collectionId);
			var model = await collection.FirstOrDefaultAsync();
			if (model == null)
				return null;

			templateId ??= model.DefaultTemplate;
			if (templateId == null)
				return null;

			var template = model.Templates.First(x => string.Equals(x.Slug, templateId));
			return CompileTemplate(template);
		}

		public bool HasTemplate(string collectionId, string? templateId = null)
		{
			return _collectionIds.Contains(MakeKey(collectionId, templateId));
		}

		public bool HasDefaultTemplate(string collectionId) => HasTemplate(collectionId);

	}
}
