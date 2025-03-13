
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.IdentityModel.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Utils;

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
				}

				current.Children.Add(node);
				current = node;
			}

			current.RouteTemplate = routeTemplateModel;
		}

		public override async Task StartAsync(CancellationToken cancellationToken)
		{
			await _routes.Find(FilterDefinition<RouteTemplateModel>.Empty).ForEachAsync(InsertIntoTree, cancellationToken);
			_watch = await _routes.WatchAsync();
			await base.StartAsync(cancellationToken);
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			_watch.Dispose();
			_scope.Dispose();
			await base.StopAsync(cancellationToken);
		}

		private bool TryParseRouteTemplate(RouteTemplateModel model, Dictionary<string, string> raw, Dictionary<string, object?> parsed)
		{
			if (model.IsRedirect)
				return true;

			foreach (var field in model.Fields)
			{
				if (raw.TryGetValue(field.ParameterName, out var rawValue) == false && field.IsOptional == false)
					return false;

				if (string.IsNullOrEmpty(rawValue))
				{
					if (field.IsNullable)
					{
						parsed[field.ParameterName] = null;
						continue;
					}

					return false;
				}

				if (BsonHelper.TryParseFromBsonType(field.BsonType, raw[field.ParameterName], out var value) == false)
					return false;
				
				parsed[field.ParameterName] = value;
			}

			if (model.Paginate)
			{
				if (raw.TryGetValue("limit", out var limit) && int.TryParse(limit, out var limitValue))
					parsed["limit"] = limitValue;

				if (raw.TryGetValue("cursor", out var cursor) && ObjectId.TryParse(cursor, out var cursorValue))	
					parsed["cursor"] = cursorValue;
			}

			return true;
		}

		public bool TryRoute(HttpContext context, out RouteMatch? match)
		{
			if (context.Request.Path.HasValue)
			{
				var path = context.Request.Path.Value.AsSpan();
				var index = -1;
				var lastIndex = 0;
				Queue<RouteTreeNode> queue = new(16);
				var routeValues = context.Request.Query.ToDictionary(x => x.Key, x => x.Value.First());

				while ((index = path.IndexOf("/")) >= 0)
				{
					var part = path.Slice(lastIndex, index);
					lastIndex = index;
					var next = queue.Dequeue();
					foreach (var child in next.Children)
						if (child.Matches(part, routeValues))
							queue.Enqueue(child);
				}

				var parsedValues = new Dictionary<string, object?>();
				var template = queue.FirstOrDefault(x => x.RouteTemplate != null && TryParseRouteTemplate(x.RouteTemplate, routeValues, parsedValues)).RouteTemplate;
				var collectionSlug = template.CollectionSlug ?? routeValues.GetValueOrDefault("collectionSlug");
				if (template != null && collectionSlug != null)
				{
					var templateSlug = template.TemplateSlug ?? routeValues.GetValueOrDefault("templateSlug");
					match = new RouteMatch(collectionSlug, templateSlug, parsedValues, template);
					return true;
				}
			}


			match = null;
			return false;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await _watch.ForEachAsync(ProcessChange, stoppingToken);
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
