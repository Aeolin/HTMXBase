using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using HTMXBase.Api.Models;
using HTMXBase.Api.Requests;
using HTMXBase.ApiModels;
using HTMXBase.Authorization;
using HTMXBase.Database.Models;
using HTMXBase.DataBinders.JsonOrForm;
using HTMXBase.Utils;
using System.ComponentModel.DataAnnotations;
using AwosFramework.Generators.MongoDBUpdateGenerator.Extensions;
using MongoDB.Driver.Linq;
using System.Threading.Channels;
using HTMXBase.Services.ModelEvents;
using HTMXBase.Services.Pagination;

namespace HTMXBase.Controllers
{
	[ApiController]
	[Route("api/v1/routes")]
	public class RouteController : HtmxBaseController
	{
		private readonly ChannelWriter<ModifyEvent<ModelData<RouteTemplateModel>>> _routeEvents;
		private readonly IPaginationService<RouteTemplateModel> _routePaginationService;

		public RouteController(IMongoDatabase dataBase, IMapper mapper, ChannelWriter<ModifyEvent<ModelData<RouteTemplateModel>>> routeEvents, IPaginationService<RouteTemplateModel> routePaginationService) : base(dataBase, mapper)
		{
			_routeEvents=routeEvents;
			_routePaginationService=routePaginationService;
		}

		[HttpGet]
		[ProducesResponseType<CursorResult<ApiRouteTemplate[], string>>(StatusCodes.Status200OK)]
		[Permission("routes/get", Constants.ADMIN_ROLE, Constants.BACKEND_USER)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(RouteTemplateModel.CollectionName)]
		public async Task<IActionResult> GetRoutesAsync([FromQuery] ObjectId? cursorNext = null, [FromQuery]ObjectId? cursorPrevious = null, [FromQuery][Range(1, 100)] int limit = 20)
		{	
			var values = PaginationValues.FromRequest(HttpContext.Request);
			var data = await _routePaginationService.PaginateAsync(values, _mapper.Map<RouteTemplateModel, ApiRouteTemplate>);
			return Ok(data);
		}

		[HttpGet("search")]
		[ProducesResponseType<CursorResult<ApiRouteTemplate[], string>>(StatusCodes.Status200OK)]
		[Permission("routes/get", Constants.ADMIN_ROLE, Constants.BACKEND_USER)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(RouteTemplateModel.CollectionName)]
		public async Task<IActionResult> GetRoutesAsync([FromQuery]string? regex = null)
		{
			var filter = Builders<RouteTemplateModel>.Filter.Empty;
			if (!string.IsNullOrEmpty(regex))
				filter = Builders<RouteTemplateModel>.Filter.Regex(x => x.UrlTemplate, new BsonRegularExpression(regex));

			var values = PaginationValues.FromRequest(HttpContext.Request);
			var data = await _routePaginationService.PaginateAsync(values, _mapper.Map<RouteTemplateModel, ApiRouteTemplate>, filter);
			return Ok(data);
		}

		[HttpPost]
		[ProducesResponseType<ApiRouteTemplate>(StatusCodes.Status200OK)]
		[Permission("routes/create", Constants.ADMIN_ROLE, Constants.BACKEND_USER)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(RouteTemplateModel.CollectionName)]
		public async Task<IActionResult> CreateRouteAsync([FromJsonOrForm] ApiRouteTemplateCreateRequest routeTemplate)
		{
			var model = _mapper.Map<RouteTemplateModel>(routeTemplate);
			if(model.Fields?.Length > 0 && (model.IsRedirect || model.IsStaticContentAlias))
				return BadRequest("Fields are not allowed on redirect or static content alias routes");

			var collection = _db.GetCollection<RouteTemplateModel>(RouteTemplateModel.CollectionName);
			await collection.InsertOneAsync(model, null, HttpContext.RequestAborted);
			await _routeEvents.WriteAsync(ModelData.Create(model));
			return Ok(_mapper.Map<ApiRouteTemplate>(model));
		}

		[HttpPost("{id}/fields")]
		[ProducesResponseType<ApiRouteTemplate>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("routes/update", Constants.ADMIN_ROLE, Constants.BACKEND_USER)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(RouteTemplateModel.CollectionName)]
		public async Task<IActionResult> AddFieldAsync([FromRoute] ObjectId id, [FromJsonOrForm] ApiFieldMatchModel field)
		{
			var model = _mapper.Map<FieldMatchModel>(field);
			var collection = _db.GetCollection<RouteTemplateModel>(RouteTemplateModel.CollectionName);
			var options = GetReturnUpdatedOptions<RouteTemplateModel>();
			var result = await collection.FindOneAndUpdateAsync(x => x.Id == id && x.Fields.Any(x => x.ParameterName == field.ParameterName) == false, model.ToAddField(), options, HttpContext.RequestAborted);
			if (result == null)
				return NotFound($"Either no route with id {id} exists or a field with ParameterName {field.ParameterName} already exists");

			await _routeEvents.WriteAsync(ModelData.Update(result));
			return Ok(_mapper.Map<ApiRouteTemplate>(result));
		}

		[HttpPut("{id}/fields/{parameterName}")]
		[ProducesResponseType<ApiFieldMatchModel>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("routes/update", Constants.ADMIN_ROLE, Constants.BACKEND_USER)]
		public async Task<IActionResult> UpdateFieldAsync([FromRoute] ObjectId id, [FromRoute] string parameterName, [FromJsonOrForm] ApiFieldMatchModel field)
		{
			var collection = _db.GetCollection<RouteTemplateModel>(RouteTemplateModel.CollectionName);
			var options = GetReturnUpdatedOptions<RouteTemplateModel>();
			var result = await collection.FindOneAndUpdateAsync(x => x.Id == id && x.Fields.FirstMatchingElement().ParameterName == parameterName,
				field.ToUpdate(),
				options,
				HttpContext.RequestAborted
			);

			if (result == null)
				return NotFound($"Either no route with id {id} exists or a field with ParameterName {parameterName} does not exist");

			await _routeEvents.WriteAsync(ModelData.Update(result));
			return Ok(field);
		}

		[HttpDelete("{id}/fields/{parameterName}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("routes/update", Constants.ADMIN_ROLE, Constants.BACKEND_USER)]
		public async Task<IActionResult> DeleteFieldAsync([FromRoute] ObjectId id, [FromRoute] string parameterName)
		{
			var collection = _db.GetCollection<RouteTemplateModel>(RouteTemplateModel.CollectionName);
			var result = await collection.FindOneAndUpdateAsync(x => x.Id == id,
				Builders<RouteTemplateModel>.Update.PullFilter(x => x.Fields, x => x.ParameterName == parameterName),
				cancellationToken: HttpContext.RequestAborted
			);

			if (result == null)
				return NotFound($"Either no route with id {id} exists or a field with ParameterName {parameterName} does not exist");

			await _routeEvents.WriteAsync(ModelData.Update(result));
			return NoContent();
		}

		[HttpPut("{id}")]
		[ProducesResponseType<ApiRouteTemplate>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("routes/update", Constants.ADMIN_ROLE, Constants.BACKEND_USER)]
		[EndpointGroupName(Constants.HTMX_ENDPOINT)]
		[EndpointMongoCollection(RouteTemplateModel.CollectionName)]
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
			if (result == null)
				return NotFound();

			await _routeEvents.WriteAsync(ModelData.Update(result));
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

			await _routeEvents.WriteAsync(ModelData.Delete<RouteTemplateModel>(id));
			return NoContent();
		}
	}
}
