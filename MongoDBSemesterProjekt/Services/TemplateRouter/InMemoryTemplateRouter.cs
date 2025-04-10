
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Services.ModelEvents;
using MongoDBSemesterProjekt.Services.Pagination;
using MongoDBSemesterProjekt.Utils;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Web;

namespace MongoDBSemesterProjekt.Services.TemplateRouter
{
	public class InMemoryTemplateRouter : BackgroundService, ITemplateRouter
	{
		private readonly IMongoCollection<RouteTemplateModel> _routes;
		private readonly RouteTreeNode _routeTree = new RouteTreeNode(null, null);
		private readonly Dictionary<ObjectId, RouteTemplateModel> _routeTempltes = new();
		private readonly IServiceProvider _provider;
		private readonly ChannelReader<ModifyEvent<ModelData<RouteTemplateModel>>> _eventsReader;
		private readonly IServiceScope _scope;

		public InMemoryTemplateRouter(IServiceProvider provider, ChannelReader<ModifyEvent<ModelData<RouteTemplateModel>>> eventsReader)
		{
			_scope = provider.CreateScope();
			_provider = _scope.ServiceProvider;
			var db =_provider.GetRequiredService<IMongoDatabase>();
			_routes = db.GetCollection<RouteTemplateModel>(RouteTemplateModel.CollectionName);
			_eventsReader=eventsReader;
		}

		private void RemoveFromTree(ObjectId id)
		{
			if(_routeTempltes.TryGetValue(id, out var routeTemplateModel) == false)
				return;

			var path = routeTemplateModel.UrlTemplate.Split("/");
			var current = _routeTree;
			foreach (var part in path)
			{
				var isWildcard = part.StartsWith("{") && part.EndsWith("}");
				var node = current.Children.FirstOrDefault(x => x.Path == part || (x.Path == null && isWildcard));
				if (node == null)
					return;

				current = node;
			}

			if (current != null && current.RouteTemplate != null && current.RouteTemplate.Id == routeTemplateModel.Id)
				current.RouteTemplate = null;
		}

		private void InsertIntoTree(RouteTemplateModel? routeTemplateModel)
		{
			if(routeTemplateModel == null)
				return;

			var path = routeTemplateModel.UrlTemplate.Split("/", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			var current = _routeTree;
			foreach (var part in path)
			{
				var node = current.Children.FirstOrDefault(x => x.Path == part);
				if (node == null)
				{
					if (part.StartsWith("{") && part.EndsWith("}"))
					{
						node = RouteTreeNode.Wildcard(part[1..^1]);
					}
					else
					{
						node = RouteTreeNode.Create(part);
					}

					current.Children.Add(node);
				}

				current = node;
			}

			current.RouteTemplate = routeTemplateModel;
			_routeTempltes[routeTemplateModel.Id] = routeTemplateModel;
		}

		public override async Task StartAsync(CancellationToken cancellationToken)
		{
			await _routes.Find(FilterDefinition<RouteTemplateModel>.Empty).ForEachAsync(InsertIntoTree, cancellationToken);
			await base.StartAsync(cancellationToken);
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			_scope.Dispose();
			await base.StopAsync(cancellationToken);
		}

		private static readonly Regex VirtualPathTemplatePattern = new Regex(@"(?:\/|^|$)\{(\S*)\}(?:\/|^|$)", RegexOptions.Compiled);

		private bool TryParseRouteTemplate(RouteTemplateModel model, Dictionary<string, StringValues> raw, out RouteMatch? match)
		{
			var parsed = new Dictionary<string, object?>();
			if (model.Fields != null && model.IsRedirect == false && model.IsStaticContentAlias == false)
			{
				foreach (var field in model.Fields)
				{
					if (raw.TryGetValue(field.ParameterName, out var rawValues) == false && field.IsOptional == false)
					{
						match = null;
						return false;
					}

					var rawValue = rawValues.FirstOrDefault();
					if (string.IsNullOrEmpty(rawValue))
					{
						if (field.IsNullable)
						{
							parsed[field.ParameterName] = null;
							continue;
						}

						match = null;
						return false;
					}

					if (BsonHelper.TryParseFromBsonType(field.BsonType, raw[field.ParameterName], out var value) == false)
					{
						match = null;
						return false;
					}

					if(value is string stringValue && field.UrlEncode)
						value = HttpUtility.UrlEncode(stringValue);

					parsed[field.ParameterName] = value;
				}

				if (model.Paginate)
				{
					foreach(var paginationKey in PaginationValues.PAGINATION_VALUES)
					{
						if(raw.TryGetValue(paginationKey, out var rawValues))
						{
							parsed[paginationKey] = paginationKey == PaginationValues.COLUMNS_KEY ? rawValues : rawValues.FirstOrDefault();
						}
					}
				}
			}
	
			var collectionSlug = model?.CollectionSlug ?? raw.GetValueOrDefault("collectionSlug");
			var templateSlug = model.TemplateSlug ?? raw.GetValueOrDefault("templateSlug");
			if (model.IsStaticContentAlias)
			{
				var constructedAlias = VirtualPathTemplatePattern.Replace(model.VirtualPathTemplate!, (x) => raw[x.Groups[1].Value]);
				match = new RouteMatch(constructedAlias, model);
			}
			else
			{
				match = new RouteMatch(collectionSlug, templateSlug, parsed, model);
			}

			return true;
		}

		public bool TryRoute(HttpContext context, out RouteMatch? match)
		{
			if (context.Request.Path.HasValue)
			{
				var path = context.Request.Path.Value.AsSpan().Slice(1);
				var index = -1;
				Queue<RouteTreeNode> queue = new(16);
				var routeValues = context.Request.Query.ToDictionary(x => x.Key, x => x.Value);
				queue.Enqueue(_routeTree);


				while (path.Length > 0)
				{
					index = path.IndexOf('/');
					if (index == -1)
						index = path.Length;

					var part = path.Slice(0, index);
					path = path.Slice(Math.Min(path.Length, index+1));
					var queueSize = queue.Count;
					while (queueSize-- > 0)
					{
						var next = queue.Dequeue();
						foreach (var child in next.Children)
							if (child.Matches(part, routeValues))
								queue.Enqueue(child);
					}
				}

				match = queue.Where(x => x.RouteTemplate != null).SelectWhere(x => (TryParseRouteTemplate(x.RouteTemplate, routeValues, out var match), match)).FirstOrDefault();
				return match != null;
			}


			match = null;
			return false;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			// await _watch.ForEachAsync(ProcessChange, stoppingToken);
			while (await _eventsReader.WaitToReadAsync(stoppingToken))
			{
				while (_eventsReader.TryRead(out var modelEvent))
				{
					ProcessChange(modelEvent);
				}
			}
		}

		private void ProcessChange(ModifyEvent<ModelData<RouteTemplateModel>> change)
		{
			switch (change.Mode)
			{
				case ModifyMode.Add:
					InsertIntoTree(change.Item.Model);
					break;

				case ModifyMode.Modify:
					RemoveFromTree(change.Item.ModelId);
					InsertIntoTree(change.Item.Model);
					break;

				case ModifyMode.Delete:
					RemoveFromTree(change.Item.ModelId);
					break;
			}
		}
	}
}
