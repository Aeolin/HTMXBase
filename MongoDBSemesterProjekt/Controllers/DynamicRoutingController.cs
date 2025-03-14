using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDBSemesterProjekt.ApiModels;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Services.FileStorage;
using MongoDBSemesterProjekt.Services.TemplateRouter;
using MongoDBSemesterProjekt.Services.TemplateStore;
using MongoDBSemesterProjekt.Utils;
using System.IO;
using System.Security.Claims;
using System.Text.Json;

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

		private FilterDefinition<BsonDocument> BuildFilter(RouteTemplateModel template, Dictionary<string, object> filter, out ObjectId? cursorQuery)
		{
			var builder = Builders<BsonDocument>.Filter;
			var filterList = new List<FilterDefinition<BsonDocument>>();
			if (template.Paginate && filter.TryGetValue("cursor", out var cursor) && cursor != null && cursor is ObjectId cursorObjId )
			{
				cursorQuery = cursorObjId;
				filterList.Add(builder.Gt("_id", cursorObjId));
			}
			else
			{
				cursorQuery = null;
			}

			foreach (var field in template.Fields)
			{
				if (filter.TryGetValue(field.ParameterName, out var value))
				{
					if (value == null && field.IsNullable == false)
						throw new InvalidOperationException($"Field {field.ParameterName} is not nullable");

					switch (field.MatchKind)
					{
						case MatchKind.Equals:
							filterList.Add(builder.Eq(field.DocumentFieldName, value));
							break;

						case MatchKind.Contains:
							filterList.Add(builder.AnyEq(field.DocumentFieldName, value));
							break;

						case MatchKind.NotContains:
							filterList.Add(builder.Not(builder.AnyEq(field.DocumentFieldName, value)));
							break;

						case MatchKind.LargerThan:
							filterList.Add(builder.Gt(field.DocumentFieldName, value));
							break;

						case MatchKind.LargerOrEqual:
							filterList.Add(builder.Gte(field.DocumentFieldName, value));
							break;

						case MatchKind.SmallerThan:
							filterList.Add(builder.Lt(field.DocumentFieldName, value));
							break;

						case MatchKind.SmallerOrEqual:
							filterList.Add(builder.Lte(field.DocumentFieldName, value));
							break;
					}
				}
				else if (field.IsOptional == false)
				{
					throw new InvalidOperationException($"Missing required field {field.ParameterName}");
				}
			}

			if (filterList.Count == 0)
			{
				return builder.Empty;
			}
			else if (filterList.Count == 1)
			{
				return filterList.First();
			}
			else
			{
				return builder.And(filterList);
			}
		}

		private static readonly SortDefinition<BsonDocument> SortById = Builders<BsonDocument>.Sort.Ascending("_id");

		private async Task<string?> GetStaticTemplateAsync(RouteTemplateModel template)
		{
			if (string.IsNullOrEmpty(template.StaticTemplate))
				return null;

			StaticContentModel contentModel;
			var contentCollection = _db.GetCollection<StaticContentModel>(StaticContentModel.CollectionName);
			if (ObjectId.TryParse(template.StaticTemplate, out var objId))
				contentModel = await contentCollection.Find(x => x.Id == objId).FirstOrDefaultAsync(HttpContext.RequestAborted);
			else
				contentModel = await FindByPath(template.StaticTemplate).FirstOrDefaultAsync(HttpContext.RequestAborted);

			if (contentModel == null)
				return null;

			using var stream = await _fileStore.GetBlobAsync(contentModel.StorageId);
			if (stream == null)
				return null;

			using var reader = new StreamReader(stream);
			return await reader.ReadToEndAsync();
		}

		private IFindFluent<StaticContentModel, StaticContentModel> FindByPath(string path)
		{
			var contentCollection = _db.GetCollection<StaticContentModel>(StaticContentModel.CollectionName);
			var lastSlash = path.LastIndexOf('/');
			var slug = path.Substring(lastSlash + 1);
			var virtualPath = lastSlash == -1 ? null : path.Substring(0, lastSlash);
			return contentCollection.Find(x => x.Slug == slug && x.VirtualPath == virtualPath);
		}

		[AllowAnonymous]
		[HttpGet("~/{**path}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public async Task<IActionResult> GetFileContentAsync(string path)
		{	
			var file = await FindByPath(path).FirstOrDefaultAsync(HttpContext.RequestAborted);
			var ownerId = User?.GetIdentifierId();
			var permissions = User?.GetPermissions();
			if (file == null)
			{
				if (_router.TryRoute(HttpContext, out var mRouteMatch) == false || mRouteMatch.HasValue == false)
					return NotFound();

				var routeMatch = mRouteMatch.Value; 
				if(routeMatch.RouteTemplateModel.IsRedirect)
					return Redirect(routeMatch.RouteTemplateModel.RedirectUrl!);

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
				var filter = BuildFilter(routeMatch.RouteTemplateModel, routeMatch.QueryValues, out var cursor);
				var query = collection.Find(filter);
				object? data = null;
				if (routeMatch.RouteTemplateModel.Paginate)
				{
					if (routeMatch.QueryValues.TryGetValue("limit", out var limitObj) == false || limitObj is not int limit)
						limit = 20;

					if(limit > 100 || limit < 1)
						return BadRequest("Limit must be in range 1 to 100");

					query = query.Sort(SortById).Limit(limit);
					var list = await query.ToListAsync(HttpContext.RequestAborted);
					
					data = CursorResult.FromCollection(list, limit, cursor, x => BsonSerializer.Deserialize<Dictionary<string, object>>(x));
				}
				else
				{
					var doc = await query.FirstOrDefaultAsync(HttpContext.RequestAborted);
					data = BsonSerializer.Deserialize<Dictionary<string, object>>(doc);
				}

				var user = await GetUserAsync();
				var templateContext = new TemplateContext(user, data);
				var rendered = template(templateContext);
				var staticTemplate = await GetStaticTemplateAsync(routeMatch.RouteTemplateModel);
				if(string.IsNullOrEmpty(staticTemplate) == false)
					rendered = staticTemplate.Replace("{{content}}", rendered);

				return Content(rendered, "text/html");
			}
			else
			{
				if (file.ReadPermission != null && User == null && (file.OwnerId == ownerId || (permissions?.Contains(file.ReadPermission) ?? false)) == false)
					return NotFound(); // don't leak if resource exists

				var content = await _fileStore.GetBlobAsync(file.StorageId);
				if (content == null)
					return NotFound();

				return File(content, file.MimeType, file.Name);
			}
		}
	}
}
