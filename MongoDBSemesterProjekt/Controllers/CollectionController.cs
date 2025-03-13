using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.ApiModels;
using MongoDBSemesterProjekt.Authorization;
using MongoDBSemesterProjekt.Utils;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AwosFramework.Generators.MongoDBUpdateGenerator.Extensions;
using MongoDBSemesterProjekt.Services.TemplateStore;
using MongoDB.Driver.Linq;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Api.Models;
using MongoDBSemesterProjekt.DataBinders.JsonOrForm;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace MongoDBSemesterProjekt.Controllers
{
	[ApiController]
	[Route("/api/v1/collections")]
	[Authorize]
	public class CollectionController : HtmxBaseController
	{
		private readonly IHtmxTemplateStore _templateStore;

		public CollectionController(IMongoDatabase dataBase, IMapper mapper, IHtmxTemplateStore templateStore) : base(dataBase, mapper)
		{
			_templateStore=templateStore;
		}

		[HttpPost("{collectionSlug}/paginate")]
		[ProducesResponseType<CursorResult<JsonDocument, ObjectId?>>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		public Task<IActionResult> PaginateFormAsync(string collectionSlug, [FromForm][Range(1, 100)] int limit = 20, [FromForm] ObjectId? cursor = null)
		{
			return PaginateAsync(collectionSlug, limit, cursor);
		}

		[HttpGet("{collectionSlug}/paginate")]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType<CursorResult<JsonDocument, ObjectId?>>(StatusCodes.Status200OK)]
		public async Task<IActionResult> PaginateAsync(string collectionSlug, [FromQuery][Range(1, 100)] int limit = 20, [FromQuery] ObjectId? cursor = null)
		{
			var permissions = User.GetPermissions();
			var collection = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName).Find(x => x.Slug == collectionSlug && x.IsInbuilt == false && (x.QueryPermission == null || permissions.Contains(x.QueryPermission))).FirstOrDefaultAsync();
			if (collection == null)
				return NoPermission("No permission to access collection");

			var sort = Builders<BsonDocument>.Sort.Ascending("_id");
			var filter = Builders<BsonDocument>.Filter.Gt("_id", cursor);
			var data = await _db.GetCollection<BsonDocument>(collectionSlug).Find(filter).Sort(sort).Limit(limit).ToListAsync();
			ObjectId? nextCursor = data.Count == limit ? data.Last()["_id"].AsObjectId : null;

			return Ok(CursorResult.Create(nextCursor, data.Select(x => JsonDocument.Parse(x.ToJson()))));
		}

		[HttpPost("{collectionSlug}")]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		public async Task<IActionResult> CreateDocumentAsync(string collectionSlug, [FromJsonOrForm] JsonDocument document)
		{
			var permissions = User.GetPermissions();
			var collection = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName).Find(x => x.Slug == collectionSlug && x.IsInbuilt == false && (x.InsertPermission == null || permissions.Contains(x.InsertPermission))).FirstOrDefaultAsync();
			if (collection == null)
				return NoPermission("No permission to insert data into collection");


			var doc = BsonHelper.JsonToBsonDocumentWithSchema(document, collection.Schema);
			doc[Constants.OWNER_ID_FIELD] = User?.GetIdentifierId();
			await _db.GetCollection<BsonDocument>(collectionSlug).InsertOneAsync(doc);
			return Ok(JsonDocument.Parse(doc.ToJson()));
		}

		private async Task<bool> IsOwnerAsync(IMongoCollection<BsonDocument> documentCollection, ObjectId documentId)
		{
			if (User == null)
				return false;

			var ownerId = User.GetIdentifierId();
			var builder = Builders<BsonDocument>.Filter;
			var docFilter = builder.And(
				builder.Eq("_id", documentId),
				builder.Eq(Constants.OWNER_ID_FIELD, ownerId)
			);

			return (await documentCollection.CountDocumentsAsync(docFilter) == 0);
		}

		[HttpPut("{collectionSlug}/{documentId}")]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(CollectionModel.CollectionName)]
		public async Task<IActionResult> UpdateDocumentAsync(string collectionSlug, ObjectId documentId, [FromJsonOrForm] JsonDocument document)
		{
			var permissions = User.GetPermissions();
			var documentCollection = _db.GetCollection<BsonDocument>(collectionSlug);
			var isOwner = await IsOwnerAsync(documentCollection, documentId);
			if (isOwner == false)
			{
				var collectionMeta = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName)
					.Find(x => x.Slug == collectionSlug && x.IsInbuilt == false &&
					(x.ModifyPermission == null || permissions.Contains(x.ModifyPermission)))
					.FirstOrDefaultAsync();

				if (collectionMeta == null)
					return NoPermission("No permission to update data in collection");
			}

			var filter = Builders<BsonDocument>.Filter.Eq("_id", documentId);
			var response = await documentCollection.FindOneAndReplaceAsync(filter, BsonDocument.Parse(document.RootElement.GetRawText()));
			return Ok(JsonDocument.Parse(response.ToJson()));
		}

		[HttpDelete("{collectionSlug}/{documentId}")]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<IActionResult> DeleteDocumentAsync(string collectionSlug, ObjectId documentId)
		{
			var permissions = User.GetPermissions();
			var documentCollection = _db.GetCollection<BsonDocument>(collectionSlug);
			var isOwner = await IsOwnerAsync(documentCollection, documentId);
			if (isOwner == false)
			{
				var collection = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName)
					.Find(x => x.Slug == collectionSlug && x.IsInbuilt == false && 
					(x.DeletePermission == null || permissions.Contains(x.DeletePermission)))
					.FirstOrDefaultAsync();
				
				if (collection == null)
					return NoPermission("No permission to delete data in collection");
			}

			var filter = Builders<BsonDocument>.Filter.Eq("_id", documentId);
			await _db.GetCollection<BsonDocument>(collectionSlug).DeleteOneAsync(filter);
			return Ok();
		}

		[HttpPost("{collectionSlug}/query")]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType<CursorResult<JsonDocument, ObjectId?>>(StatusCodes.Status200OK)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(CollectionModel.CollectionName)]
		public async Task<IActionResult> QueryAsync(string collectionSlug, [FromJsonOrForm] JsonDocument query, [FromQuery][Range(1, 250)] int limit = 50, [FromQuery] ObjectId? cursor = null)
		{
			var permissions = User.GetPermissions();
			var collection = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName).Find(x => x.Slug == collectionSlug && x.IsInbuilt == false && (x.ComplexQueryPermission == null || permissions.Contains(x.ComplexQueryPermission))).FirstOrDefaultAsync();
			if (collection == null)
				return NoPermission("No permission to query collection");

			var gtId = Builders<BsonDocument>.Filter.Gt("_id", cursor);
			var filter = Builders<BsonDocument>.Filter.And(gtId, BsonDocument.Parse(query.RootElement.GetRawText()));
			var sort = Builders<BsonDocument>.Sort.Ascending("_id");
			var data = await _db.GetCollection<BsonDocument>(collectionSlug).Find(filter).Sort(sort).Limit(limit).ToListAsync();
			ObjectId? nextCursor = data.Count == limit ? data.Last()["_id"].AsObjectId : null;

			return Ok(CursorResult.Create(nextCursor, data.Select(x => JsonDocument.Parse(x.ToJson()))));
		}

		private async Task HandleContentFromFormFileAsync(ApiTemplate template, IFormFile templateFile)
		{
			if (templateFile != null)
			{
				var encoding = HeaderUtils.GetEncodingFromContentTypeOrDefault(templateFile.ContentType, Encoding.UTF8);
				using var stream = templateFile.OpenReadStream();
				using var reader = new StreamReader(stream, encoding);
				template.Template = await reader.ReadToEndAsync();
			}
		}

		[HttpPost("{collectionSlug}/templates")]
		[ProducesResponseType<ApiTemplate>(StatusCodes.Status200OK)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[Permission("collection/create-template", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		public async Task<IActionResult> CreateTemplateAsync(string collectionSlug, [FromJsonOrForm] ApiTemplate template, IFormFile templateFile)
		{
			await HandleContentFromFormFileAsync(template, templateFile);
			var model = _mapper.Map<TemplateModel>(template);
			var dbCollection = _db.GetCollection<CollectionModel>(CollectionModel.CollectionName);
			var collection = await dbCollection.FindOneAndUpdateAsync(x => x.Slug == collectionSlug && x.Templates.Any(y => y.Slug == template.Slug) == false, model.ToAddTemplate(), GetReturnUpdatedOptions<CollectionModel>());
			if (collection == null)
				return NotFound("Collection not found");

			_templateStore.NotifyTemplateChanged(ModifyMode.Add, collectionSlug, template.Slug);
			return Ok(_mapper.Map<ApiTemplate>(collection.Templates.Last()));
		}

		[HttpPut("{collectionSlug}/templates/{templateSlug}")]
		[ProducesResponseType<ApiTemplate>(StatusCodes.Status200OK)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[Permission("collection/modify-template", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		public async Task<IActionResult> UpdateTemplateAsync(string collectionSlug, string templateSlug, [FromJsonOrForm] ApiTemplate template, IFormFile templateFile)
		{
			await HandleContentFromFormFileAsync(template, templateFile);
			var dbCollection = _db.GetCollection<CollectionModel>(CollectionModel.CollectionName);
			var filter = Builders<CollectionModel>.Filter.Where(x => x.Slug == collectionSlug && x.Templates.Any(x => x.Slug == templateSlug));

			var collection = await dbCollection.FindOneAndUpdateAsync(
				x => x.Slug == collectionSlug && x.Templates.FirstMatchingElement().Slug == templateSlug,
				template.ToUpdate(),
				GetReturnUpdatedOptions<CollectionModel>());

			if (collection == null)
				return NotFound("Collection not found");

			_templateStore.NotifyTemplateChanged(ModifyMode.Modify, collectionSlug, templateSlug);
			return Ok(template);
		}

		[HttpDelete("{collectionSlug}/templates/{templateSlug}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("collection/delete-template", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		public async Task<IActionResult> DeleteTemplateAsync(string collectionSlug, string templateSlug)
		{
			var dbCollection = _db.GetCollection<CollectionModel>(CollectionModel.CollectionName);
			var filter = Builders<CollectionModel>.Filter.Where(x => x.Slug == collectionSlug && x.Templates.Any(x => x.Slug == templateSlug));
			var collection = await dbCollection.FindOneAndUpdateAsync(
				x => x.Slug == collectionSlug && x.Templates[-1].Slug == templateSlug,
				Builders<CollectionModel>.Update.PullFilter(x => x.Templates, Builders<TemplateModel>.Filter.Eq(x => x.Slug, templateSlug)),
				GetReturnUpdatedOptions<CollectionModel>());

			if (collection == null)
				return NotFound("Collection not found");

			_templateStore.NotifyTemplateChanged(ModifyMode.Delete, collectionSlug, templateSlug);
			return Ok();
		}

		[HttpPut("{collectionSlug}/default-template/{templateSlug}")]
		[ProducesResponseType<ApiCollection>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("collection/modify", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(CollectionModel.CollectionName)]
		public async Task<IActionResult> SetDefaultTemplateAsync(string collectionSlug, string templateSlug)
		{
			var collections = _db.GetCollection<CollectionModel>(CollectionModel.CollectionName);
			var filter = Builders<CollectionModel>.Filter.Where(x => x.Slug == collectionSlug && x.Templates.Any(x => x.Slug == templateSlug));
			var collectionModel = await collections.FindOneAndUpdateAsync(filter, Builders<CollectionModel>.Update.Set(x => x.DefaultTemplate, templateSlug));
			return Ok(_mapper.Map<ApiCollection>(collectionModel));
		}

		[HttpPut("{collectionSlug}")]
		[ProducesResponseType<ApiCollection>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("collection/modify", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(CollectionModel.CollectionName)]
		public async Task<IActionResult> UpdateCollectionAsync(string collectionSlug, [FromBody][FromForm] ApiCollection collection)
		{
			var collections = _db.GetCollection<CollectionModel>(CollectionModel.CollectionName);
			var filter = Builders<CollectionModel>.Filter.Where(x => x.Slug == collectionSlug && x.IsInbuilt == false);
			var collectionModel = await collections.FindOneAndUpdateAsync(filter, collection.ToUpdate(), GetReturnUpdatedOptions<CollectionModel>());
			if (collectionModel == null)
				return NotFound("Collection not found");

			_templateStore.NotifyTemplateChanged(ModifyMode.Modify, collectionSlug);
			return Ok(collection);
		}

		[HttpGet]
		[ProducesResponseType<ApiCollection[]>(StatusCodes.Status200OK)]
		[Permission("collections/list", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(CollectionModel.CollectionName)]
		public async Task<IActionResult> GetCollectionAsync()
		{
			var collections = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName).Find(x => true).ToListAsync();
			return Ok(_mapper.Map<ApiCollection[]>(collections));
		}

		[HttpPost("paginate")]
		[ProducesResponseType<CursorResult<ApiCollection[], ObjectId?>>(StatusCodes.Status200OK)]
		[Permission("collections/list", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(CollectionModel.CollectionName)]
		public Task<IActionResult> PaginateCollectionFormAsync([FromForm] ObjectId? cursor, [FromForm][Range(1, 50)] int limit = 20)
		{
			return PaginateCollectionAsync(cursor, limit);
		}


		[HttpGet("paginate")]
		[ProducesResponseType<CursorResult<ApiCollection[], ObjectId?>>(StatusCodes.Status200OK)]
		[Permission("collections/list")]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(CollectionModel.CollectionName)]
		public async Task<IActionResult> PaginateCollectionAsync([FromQuery] ObjectId? cursor, [FromQuery][Range(1, 50)] int limit = 20)
		{
			var collections = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName)
				.Find(x => cursor.HasValue == false || x.Id > cursor)
				.Limit(limit)
				.ToListAsync();

			ObjectId? next = collections.Count == limit ? collections.Last().Id : null;
			return Ok(CursorResult.Create(next, _mapper.Map<ApiCollection[]>(collections)));
		}

		private static readonly BsonDocument OwnerField = new BsonDocument()
		{
			["bsonType"] = new BsonArray(["objectId", "null"]),
			["description"] = "The owner of the document"
		};

		private static readonly BsonDocument IdField = new BsonDocument()
		{
			["bsonType"] = "objectId",
			["description"] = "The id of the document"
		};

		[HttpDelete("{collectionSlug}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("collection/delete", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		public async Task<IActionResult> DeleteCollectionAsync(string collectionSlug)
		{
			var result = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName)
				.DeleteOneAsync(x => x.Slug == collectionSlug && x.IsInbuilt == false);

			if (result.DeletedCount == 0)
				return NotFound("Collection either doesnt exist or is inbuilt");

			_templateStore.NotifyTemplateChanged(ModifyMode.Delete, collectionSlug);
			await _db.DropCollectionAsync(collectionSlug);
			return Ok();
		}

		[HttpPost]
		[ProducesResponseType<ApiCollection>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[Permission("collections/create", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(CollectionModel.CollectionName)]
		public async Task<IActionResult> CreateCollectionAsync([FromJsonOrForm] ApiCollection collection, IFormFile schemaFile)
		{
			if(schemaFile != null && schemaFile.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
			{
				using var stream = schemaFile.OpenReadStream();
				collection.Schema = await JsonDocument.ParseAsync(stream);
			}

			var collectionCollection = _db.GetCollection<CollectionModel>(CollectionModel.CollectionName);
			var collectionMeta = await collectionCollection.Find(x => x.Slug == collection.Slug).FirstOrDefaultAsync();
			if (collectionMeta != null)
				return BadRequest("Collection with slug already exists");

			var model = _mapper.Map<CollectionModel>(collection);
				
			var schema = BsonDocument.Parse(collection.Schema.RootElement.GetRawText());
			if (schema.TryGetElement("properties", out var propertiesElement) == false || propertiesElement.Value is not BsonDocument properties)
			{
				properties = new BsonDocument();
				schema["properties"] = properties;
			}

			properties[Constants.OWNER_ID_FIELD] = OwnerField;
			if (properties.TryGetElement("_id", out _) == false)
				properties["_id"] = IdField;

			var options = new CreateCollectionOptions<object>
			{
				Validator = new BsonDocument
				{
					{ "$jsonSchema", schema }
				}
			};

			await _db.CreateCollectionAsync(collection.Slug, options);
			model.Schema = JsonDocument.Parse(schema.ToJson());
			await collectionCollection.InsertOneAsync(model);

			if (collection.Templates.Length > 0)
				_templateStore.NotifyTemplateChanged(ModifyMode.Add, collection.Slug);

			return Ok(_mapper.Map<ApiCollection>(model));
		}
	}
}
