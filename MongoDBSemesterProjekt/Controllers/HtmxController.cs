using Microsoft.AspNetCore.Mvc;
using MongoDBSemesterProjekt.Services.TemplateStore;

namespace MongoDBSemesterProjekt.Controllers
{
	public class HtmxController : ControllerBase
	{
		private readonly IHtmxTemplateStore _store;
		T