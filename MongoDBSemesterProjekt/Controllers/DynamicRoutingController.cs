using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Services.FileStorage;
using MongoDBSemesterProjekt.Services.TemplateRouter;
using MongoDBSemesterProjekt.Services.TemplateStore;
using MongoDBSemesterProjekt.Utils;
using System.Security.Claims;

namespace MongoDBSemesterProjekt.Controllers
{
	[ApiController]
	public class DynamicRoutingController : HtmxBaseController
	{
		private readonly IFileStorage _fileStore;
		private readonly ITemplateRouter _router;
		private readonly IHtmxTemplateStore _templateStore;

		public DynamicRoutingController(IMongoDatabase dataBase, IMapper mapper, IFileStorage fileStore, ITemplateRouter router, IHtmxTemplateStore templateStore) : base(dataBase, mapper)
		{
			_fileStore=fileStore;
			_router=router;
			_templateStore=templateStore;
		}

		private FilterDefinition<BsonDocument> BuildFilter(RouteTemplateModel template, Dictionary<string, object> filter)
		{
			var builder = Builders<BsonDocument>.Filter;
			var filters = new List<FilterDefinition<BsonDocument>>();
			if (template.Paginate && filter.TryGetValue("cursor", out var cursor) && cursor != null)
				filters.Add(builder.Gt("_id", cursor));

			foreach (var field in template.Fields)
			{
				if (filter.TryGetValue(field.ParameterName, out var value))
				{
					if (value == null && field.IsNullable == false)
						throw new InvalidOperationException($"Field {field.ParameterName} is not nullable");

					switch (field.MatchKind)
					{
						case MatchKind.Equals:
							filters.Add(builder.Eq(field.DocumentFieldName, value));
							break;

						case MatchKind.Contains:
							filters.Add(builder.AnyEq(field.DocumentFieldName, value));
							break;

						case MatchKind.NotContains:
							filters.Add(builder.Not(builder.AnyEq(field.DocumentFieldName, value)));
							break;

						case MatchKind.LargerThan:
							filters.Add(builder.Gt(field.DocumentFieldName, value));
							break;

						case MatchKind.LargerOrEqual:
							filters.Add(builder.Gte(field.DocumentFieldName, value));
							break;

						case MatchKind.SmallerThan:
							filters.Add(builder.Lt(field.DocumentFieldName, value));
							break;

						case MatchKind.SmallerOrEqual:
							filters.Add(builder.Lte(field.DocumentFieldName, value));
							break;
					}
				}
				else if (field.IsOptional == false)
				{
					throw new InvalidOperationException($"Missing required field {field.ParameterName}");
				}
			}

			return builder.And(filters);
		}

		private static readonly SortDefinition<BsonDocument> SortById = Builders<BsonDocument>.Sort.Ascending("_id");

		[AllowAnonymous]
		[HttpGet("~/{**path}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public async Task<IActionResult> GetFileContentAsync(string path)
		{
			var contentCollection = _db.GetCollection<StaticContentModel>(StaticContentModel.CollectionName);
			var lastSlash = path.LastIndexOf('/');
			var slug = path.Substring(lastSlash + 1);
			var virtualPath = lastSlash == -1 ? null : path.Substring(0, lastSlash);

			var file = await contentCollection.Find(x => x.Slug == slug && x.VirtualPath == virtualPath).FirstOrDefaultAsync(HttpContext.RequestAborted);
			var ownerId = User?.GetIdentifierId();
			var permissions = User?.GetPermissions();
			if (file == null)
			{
				if (_router.TryRoute(HttpContext, out var routeMatch) == false)
					return NotFound();

				var collectionCollection = _db.GetCollection<CollectionModel>(CollectionModel.CollectionName);
				var collectionMeta = await collectionCollection
					.Find(x => x.Slug == routeMatch.CollectionSlug &&
					(x.QueryPermission == null ||
					(permissions != null && permissions.Contains(x.QueryPermission))))
					.FirstOrDefaultAsync(HttpContext.RequestAborted);

				if (collectionMeta == null)
					return NotFound();

				var template = await _templateStore.GetTemplateAsync(collectionMeta.Slug, routeMatch.TemplateSlug ?? collectionMeta.DefaultTemplate);
				if (template == null)
					return NotFound();

				var collection = _db.GetCollection<BsonDocument>(collectionMeta.Slug);
				var filter = BuildFilter(routeMatch.RouteTemplateModel, routeMatch.QueryValues);
				var query = collection.Find(filter);
				object? data = null;
				if (routeMatch.RouteTemplateModel.Paginate)
				{
					if (routeMatch.QueryValues.TryGetValue("limit", out var limitObj) == false || limitObj is not int limit)
						limit = 20;

					query = query.Sort(SortById).Limit(limit);
					data = await query.ToListAsync(HttpContext.RequestAborted);
				}
				else
				{
					data = await query.FirstOrDefaultAsync(HttpContext.RequestAborted);
				}

				var user = await GetUserAsync();
				var templateContext = new TemplateContext(user, data);
				var rendered = template(templateContext);
				return Content(rendered, "text/html");
			}
			else
			{
				if (file.ReadPermission != null && User == null && (file.OwnerId == ownerId || (permissions?.Contains(file.ReadPermission) ?? false)) == false)
					return NotFound(); // don't leak if resource exists

				using var content = await _fileStore.GetBlobAsync(file.StorageId);
				if (content == null)
					return NotFound();

				return File(content, file.MimeType, file.Name);
			}
		}
	}
}
