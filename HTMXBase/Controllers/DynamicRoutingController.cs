using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using HTMXBase.ApiModels;
using HTMXBase.Database.Models;
using HTMXBase.Services.FileStorage;
using HTMXBase.Services.Pagination;
using HTMXBase.Services.TemplateRouter;
using HTMXBase.Services.TemplateStore;
using HTMXBase.Utils;
using System.Collections.Frozen;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Web;

namespace HTMXBase.Controllers
{
	[ApiController]
	public class DynamicRoutingController : HtmxBaseController
	{
		private readonly IFileStorage _fileStore;
		private readonly ITemplateRouter _router;
		private readonly IHtmxTemplateStore _templateStore;
		private readonly IPaginationService<BsonDocument> _pagination;

		public DynamicRoutingController(IMongoDatabase dataBase, IMapper mapper, IFileStorage fileStore, ITemplateRouter router, IHtmxTemplateStore templateStore, IPaginationService<BsonDocument> pagination) : base(dataBase, mapper)
		{
			_fileStore=fileStore;
			_router=router;
			_templateStore=templateStore;
			_pagination=pagination;
		}

		private ICollection<FilterDefinition<BsonDocument>> BuildFilter(RouteTemplateModel template, Dictionary<string, object?> filter)
		{
			var builder = Builders<BsonDocument>.Filter;
			var filterList = new List<FilterDefinition<BsonDocument>>();

			if (template.Fields == null)
				return filterList;

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

			return filterList;
		}

		private async Task<string> HandleStaticTemplateAsync(string? staticTemplate, UserModel? user, string rendered)
		{
			if(string.IsNullOrEmpty(staticTemplate))
				return rendered;

			var template = await _templateStore.GetStaticTemplateAsync(staticTemplate, HttpContext.RequestAborted);
			if (template == null)
				return rendered;

			var ctx = new TemplateContext(user, rendered);
			return template(ctx);	
		}

		private async Task<IActionResult> HandleFile(string? path, ObjectId? ownerId, FrozenSet<string>? permissions, string? staticTemplate = null)
		{	
			var collection = _db.GetCollection<StaticContentModel>(StaticContentModel.CollectionName);
			var file = await collection.FindByPath(path).FirstOrDefaultAsync(HttpContext.RequestAborted);
			if (file == null)
				return null;

			if (file.ReadPermission != null && User == null && (file.OwnerId == ownerId || (permissions?.Contains(file.ReadPermission) ?? false)) == false)
				return NotFound(); // don't leak if resource exists

			var content = await _fileStore.GetBlobAsync(file.StorageId);
			if (content == null)
				return NotFound();

			if (string.IsNullOrEmpty(staticTemplate) == false)
			{
				var template = await collection.FindByPath(staticTemplate).FirstOrDefaultAsync();
				if (template == null)
					return NotFound();

				var templateContent = await _fileStore.GetBlobAsync(template.StorageId);
				if (templateContent == null)
					return NotFound();

				using var innerReader = new StreamReader(content);
				var rendered = await innerReader.ReadToEndAsync();
				var user = await GetUserAsync();
				rendered = await HandleStaticTemplateAsync(staticTemplate, user, rendered);
				return File(Encoding.UTF8.GetBytes(rendered), template.MimeType);
			}
			else
			{
				return File(content, file.MimeType);
			}
		}

		[AllowAnonymous]
		[HttpGet("~/{**path}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public async Task<IActionResult> HandleDynamicRouteAsync(string? path = null)
		{
			var ownerId = User?.GetIdentifierId();
			var permissions = User?.GetPermissions();
			var file = await HandleFile(path, ownerId, permissions);
			if (file != null)
				return file;

			if (_router.TryRoute(HttpContext, out var mRouteMatch) == false || mRouteMatch.HasValue == false)
				return NotFound();

			var routeMatch = mRouteMatch.Value;
			if (routeMatch.RouteTemplateModel.IsRedirect)
				return Redirect(routeMatch.RouteTemplateModel.RedirectUrl!);

			if (routeMatch.IsStaticContentAlias)
				return await HandleFile(routeMatch.StaticContentAlias!, ownerId, permissions, routeMatch.RouteTemplateModel.StaticTemplate) ?? NotFound();


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
			var filters = BuildFilter(routeMatch.RouteTemplateModel, routeMatch.QueryValues);
			object? data = null;
			if (routeMatch.RouteTemplateModel.Paginate)
			{
				var values = PaginationValues.FromRouteMatch(routeMatch);
				data = await _pagination.PaginateAsync(collectionMeta.Slug, values, x => BsonSerializer.Deserialize<Dictionary<string, object?>>(x), filters);
			}
			else
			{
				var filter = Extensions.CombineFilters(null, filters);
				var doc = await collection.Find(filter).FirstOrDefaultAsync(HttpContext.RequestAborted);
				if(doc == null)
					return NotFound();

				data = BsonSerializer.Deserialize<Dictionary<string, object>>(doc);
			}

			var user = await GetUserAsync();
			var templateContext = new TemplateContext(user, data);
			var rendered = template(templateContext);
			rendered = await HandleStaticTemplateAsync(routeMatch.RouteTemplateModel.StaticTemplate, user, rendered);
			return Content(rendered, "text/html");
		}
	}
}
