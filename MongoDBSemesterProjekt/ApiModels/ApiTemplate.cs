using AwosFramework.Generators.MongoDBUpdateGenerator;
using MongoDBSemesterProjekt.Models;

namespace MongoDBSemesterProjekt.ApiModels
{
	[MongoDBUpdate(typeof(CollectionModel), NestedProperty="Templates[-1]", IgnoreUnmarkedProperties = true)]
	public class ApiTemplate
	{
		public  string Slug { get; set; }
		
		[UpdateProperty]
		public bool SingleItem { get; set; }
		
		[UpdateProperty]
		public string Template { get; set; }
	}
}
