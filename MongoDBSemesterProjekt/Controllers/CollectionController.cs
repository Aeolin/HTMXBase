using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.ApiModels;
using MongoDBSemesterProjekt.Authorization;
using MongoDBSemesterProjekt.Models;
using MongoDBSemesterProjekt.Utils;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MongoDBSemesterProjekt.Controllers
{
	[ApiController]
	[Route("/api/v1/collections")]
	public class CollectionController : HtmxBaseController
	{
		public CollectionController(IMongoDatabase dataBase, IMapper mapper) : base(dataBase, mapper)
		{
		}

		[HttpGet("{collectionSlug}/paginate")]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType<CursorResult<JsonDocument, ObjectId?>>(StatusCodes.Status200OK)]
		public async Task<IActionResult> PaginateAsync(string collectionSlug, [FromQuery][Range(1, 100)] int limit = 20, [FromQuery] ObjectId? cursor = null)
		{
			var permissions = User.GetPermissions();
			var collection = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName).Find(x => x.Slug == collectionSlug && (x.QueryPermission == null || permissions.Contains(x.QueryPermission))).FirstOrDefaultAsync();
			if(collection == null)
				return Forbid("No permission to access collection");

			var sort = Builders<BsonDocument>.Sort.Ascending("_id");
			var filter = Builders<BsonDocument>.Filter.Gt("_id", cursor);
			var data = await _db.GetCollection<BsonDocument>(collectionSlug).Find(filter).Sort(sort).Limit(limit).ToListAsync();
			ObjectId? nextCursor = data.Count == limit ? data.Last()["_id"].AsObjectId : null;

			return Ok(CursorResult.Create(nextCursor, data.Select(x => JsonDocument.Parse(x.ToJson()))));
		}

		[HttpPost("{collectionSlug}")]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<IActionResult> CreateDocumentAsync(string collectionSlug, [FromBody][FromForm] JsonDocument document)
		{
			var permissions = User.GetPermissions();
			var collection = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName).Find(x => x.Slug == collectionSlug && (x.InsertPermission == null || permissions.Contains(x.InsertPermission))).FirstOrDefaultAsync();
			if (collection == null)
				return Forbid("No permission to insert data into collection");

			await _db.GetCollection<BsonDocument>(collectionSlug).InsertOneAsync(BsonDocument.Parse(document.RootElement.GetRawText()));
			return Ok();
		}

		[HttpPut("{collectionSlug}/{documentId}")]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<IActionResult> UpdateDocumentAsync(string collectionSlug, ObjectId documentId, [FromBody][FromForm] JsonDocument document)
		{
			var permissions = User.GetPermissions();
			var collection = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName).Find(x => x.Slug == collectionSlug && (x.ModifyPermission == null || permissions.Contains(x.ModifyPermission))).FirstOrDefaultAsync();
			if (collection == null)
				return Forbid("No permission to update data in collection");

			var filter = Builders<BsonDocument>.Filter.Eq("_id", documentId);
			var response = await _db.GetCollection<BsonDocument>(collectionSlug).FindOneAndReplaceAsync(filter, BsonDocument.Parse(document.RootElement.GetRawText()));
			return Ok(JsonDocument.Parse(response.ToJson()));
		}

		[HttpDelete("{collectionSlug}/{documentId}")]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<IActionResult> DeleteDocumentAsync(string collectionSlug, ObjectId documentId)
		{
			var permissions = User.GetPermissions();
			var collection = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName).Find(x => x.Slug == collectionSlug && (x.DeletePermission == null || permissions.Contains(x.DeletePermission))).FirstOrDefaultAsync();
			if (collection == null)
				return Forbid("No permission to delete data in collection");

			var filter = Builders<BsonDocument>.Filter.Eq("_id", documentId);
			await _db.GetCollection<BsonDocument>(collectionSlug).DeleteOneAsync(filter);
			return Ok();
		}

		[HttpPost("{collectionSlug}/query")]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType<CursorResult<JsonDocument, ObjectId?>>(StatusCodes.Status200OK)]
		public async Task<IActionResult> QueryAsync(string collectionSlug, [FromBody][FromForm] JsonDocument query, [FromQuery][Range(1, 250)] int limit = 50, [FromQuery] ObjectId? cursor = null)
		{
			var permissions = User.GetPermissions();
			var collection = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName).Find(x => x.Slug == collectionSlug && (x.ComplexQueryPermission == null || permissions.Contains(x.ComplexQueryPermission))).FirstOrDefaultAsync();
			if (collection == null)
				return Forbid("No permission to query collection");

			var gtId = Builders<BsonDocument>.Filter.Gt("_id", cursor);
			var filter = Builders<BsonDocument>.Filter.And(gtId, BsonDocument.Parse(query.RootElement.GetRawText()));
			var sort = Builders<BsonDocument>.Sort.Ascending("_id");
			var data = await _db.GetCollection<BsonDocument>(collectionSlug).Find(filter).Sort(sort).Limit(limit).ToListAsync();
			ObjectId? nextCursor = data.Count == limit ? data.Last()["_id"].AsObjectId : null;

			return Ok(CursorResult.Create(nextCursor, data.Select(x => JsonDocument.Parse(x.ToJson()))));
		}

		[HttpPost]
		[ProducesResponseType<ApiCollection>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[Permission("collection/create", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		public async Task<IActionResult> CreateCollectionAsync([FromBody][FromForm] ApiCollection collection)
		{
			var dbCollection = _db.GetCollection<object>(collection.Slug);
			if (dbCollection != null)
				return BadRequest("Collection with slug already exists");

			var model = _mapper.Map<CollectionModel>(collection);
			await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName).InsertOneAsync(model);

			var options = new CreateCollectionOptions<object>
			{
				Validator = new BsonDocument
				{
					{ "$jsonSchema", BsonDocument.Parse(collection.Schema.RootElement.GetRawText()) }
				}
			};

			await _db.CreateCollectionAsync(collection.Slug, options);
			return Ok(_mapper.Map<ApiCollection>(model));
		}

		[HttpDelete("{collectionSlug}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("collection/delete", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		public async Task<IActionResult> DeleteCollectionAsync(string collectionSlug)
		{
			var result = await _db.GetCollection<CollectionModel>(CollectionModel.CollectionName).DeleteOneAsync(x => x.Slug == collectionSlug);
			if (result.DeletedCount == 0)
				return NotFound();

			await _db.DropCollectionAsync(collectionSlug);
			return Ok();
		}

	}
}
