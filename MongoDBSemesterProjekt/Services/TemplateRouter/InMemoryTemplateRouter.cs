
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.IdentityModel.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Utils;
using System.Text.RegularExpressions;

namespace MongoDBSemesterProjekt.Services.TemplateRouter
{
	public class InMemoryTemplateRouter : BackgroundService, ITemplateRouter
	{
		private readonly IMongoDatabase _db;
		private readonly IMongoCollection<RouteTemplateModel> _routes;
		private IChangeStreamCursor<ChangeStreamDocument<RouteTemplateModel>> _watch;
		private readonly RouteTreeNode _routeTree = new RouteTreeNode(null, null);
		private readonly IServiceProvider _provider;
		private readonly IServiceScope _scope;

		public InMemoryTemplateRouter(IServiceProvider provider)
		{
			_scope = provider.CreateScope();
			_provider = _scope.ServiceProvider;
			_db=_provider.GetRequiredService<IMongoDatabase>();
			_routes =_db.GetCollection<RouteTemplateModel>(RouteTemplateModel.CollectionName);
		}

		private void RemoveFromTree(RouteTemplateModel routeTemplateModel)
		{
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

		private void InsertIntoTree(RouteTemplateModel routeTemplateModel)
		{
			var path = routeTemplateModel.UrlTemplate.Split("/");
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
		}

		public override async Task StartAsync(CancellationToken cancellationToken)
		{
			await _routes.Find(FilterDefinition<RouteTemplateModel>.Empty).ForEachAsync(InsertIntoTree, cancellationToken);
			//_watch = await _routes.WatchAsync();
			await base.StartAsync(cancellationToken);
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			_watch?.Dispose();
			_scope.Dispose();
			await base.StopAsync(cancellationToken);
		}

		private static readonly Regex VirtualPathTemplatePattern = new Regex(@"(?:\/|^|$)\{(\S*)\}(?:\/|^|$)", RegexOptions.Compiled);

		private bool TryParseRouteTemplate(RouteTemplateModel model, Dictionary<string, string> raw, out RouteMatch? match)
		{
			var parsed = new Dictionary<string, object?>();
			if (model.Fields != null && model.IsRedirect == false && model.IsStaticContentAlias == false)
			{
				foreach (var field in model.Fields)
				{
					if (raw.TryGetValue(field.ParameterName, out var rawValue) == false && field.IsOptional == false)
					{
						match = null;
						return false;
					}

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

					parsed[field.ParameterName] = value;
				}
			}

			if (model.Paginate)
			{
				if (raw.TryGetValue("limit", out var limit) && int.TryParse(limit, out var limitValue))
					parsed["limit"] = limitValue;

				if (raw.TryGetValue("cursor", out var cursor) && ObjectId.TryParse(cursor, out var cursorValue))
					parsed["cursor"] = cursorValue;
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
				var routeValues = context.Request.Query.ToDictionary(x => x.Key, x => x.Value.First());
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
		}

		private void ProcessChange(ChangeStreamDocument<RouteTemplateModel> change)
		{
			switch (change.OperationType)
			{
				case ChangeStreamOperationType.Insert:
					InsertIntoTree(change.FullDocument);
					break;
				case ChangeStreamOperationType.Update:
					RemoveFromTree(change.FullDocumentBeforeChange);
					InsertIntoTree(change.FullDocument);
					break;
				case ChangeStreamOperationType.Delete:
					RemoveFromTree(change.FullDocumentBeforeChange);
					break;
			}
		}
	}
}
