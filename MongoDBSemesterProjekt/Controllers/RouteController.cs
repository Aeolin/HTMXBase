using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Api.Models;
using MongoDBSemesterProjekt.Api.Requests;
using MongoDBSemesterProjekt.ApiModels;
using MongoDBSemesterProjekt.Authorization;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.DataBinders.JsonOrForm;
using MongoDBSemesterProjekt.Utils;
using System.ComponentModel.DataAnnotations;
using AwosFramework.Generators.MongoDBUpdateGenerator.Extensions;
using System.Security.Cryptography.X509Certificates;

namespace MongoDBSemesterProjekt.Controllers
{
	[ApiController]
	[Route("api/v1/routes")]
	public class RouteController : HtmxBaseController
	{
		public RouteController(IMongoDatabase dataBase, IMapper mapper) : base(dataBase, mapper)
		{

		}

		[HttpGet]
		[ProducesResponseType<CursorResult<ApiRouteTemplate[], ObjectId?>>(StatusCodes.Status200OK)]
		[Permission("routes/get", Constants.ADMIN_ROLE, Constants.BACKEND_USER)]
		public async Task<IActionResult> GetRoutesAsync([FromQuery] ObjectId? cursor, [FromQuery][Range(1, 100)] int limit = 20)
		{
			var collection = _db.GetCollection<RouteTemplateModel>(RouteTemplateModel.CollectionName);
			var list = await collection.Find(x => cursor == null || x.Id > cursor).Limit(limit).ToListAsync();
			var newCursor = list.Count == limit ? list.Last().Id : (ObjectId?)null;
			return Ok(CursorResult.Create(cursor, _mapper.Map<ApiRouteTemplate[]>(list)));
		}

		[HttpPost]
		[ProducesResponseType<ApiRouteTemplate>(StatusCodes.Status200OK)]
		[Permission("routes/create", Constants.ADMIN_ROLE, Constants.BACKEND_USER)]
		public async Task<IActionResult> CreateRouteAsync([FromJsonOrForm] ApiRouteTemplateRequest routeTemplate)
		{
			var collection = _db.GetCollection<RouteTemplateModel>(RouteTemplateModel.CollectionName);
			var model = _mapper.Map<RouteTemplateModel>(routeTemplate);
			await collection.InsertOneAsync(model, null, HttpContext.RequestAborted);
			return Ok(_mapper.Map<ApiRouteTemplate>(model));
		}

		[HttpPut("{id}")]
		[ProducesResponseType<ApiRouteTemplate>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("routes/update", Constants.ADMIN_ROLE, Constants.BACKEND_USER)]
		public async Task<IActionResult> UpdateRouteAsync([FromRoute] ObjectId id, [FromJsonOrForm] ApiRouteTemplate routeTemplate)
		{
			var collection = _db.GetCollection<RouteTemplateModel>(RouteTemplateModel.CollectionName);
			var options = GetReturnUpdatedOptions<RouteTemplateModel>();
			var update = routeTemplate.ToUpdate();

			if (routeTemplate.Fields?.Length > 0)
			{
				var fields = _mapper.Map<FieldMatchModel[]>(routeTemplate.Fields);
				update = update.Set(x => x.Fields, fields);
			}

			var result = await collection.FindOneAndUpdateAsync(x => x.Id == id, update, options, HttpContext.RequestAborted);
			return Ok(_mapper.Map<ApiRouteTemplate>(result));
		}

		[HttpDelete("{id}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("routes/delete", Constants.ADMIN_ROLE, Constants.BACKEND_USER)]
		public async Task<IActionResult> DeleteRouteAsync([FromRoute] ObjectId id)
		{
			var collection = _db.GetCollection<RouteTemplateModel>(RouteTemplateModel.CollectionName);
			var result = await collection.DeleteOneAsync(x => x.Id == id, HttpContext.RequestAborted);
			if (result.DeletedCount == 0)
				return NotFound();

			return NoContent();
		}
	}
}
