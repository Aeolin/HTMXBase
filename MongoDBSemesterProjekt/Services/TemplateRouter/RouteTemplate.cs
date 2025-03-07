using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Services.TemplateRouter
{
	public class RouteTemplate
	{
		public CollectionModel Collection { get; init; }

		public RouteTemplate(CollectionModel collection)
		{
			Collection=collection;
		}
	}
}
