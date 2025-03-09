using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Services.TemplateStore
{
	public class TemplateContext
	{
		public UserModel User { get; init; }
		public object? Data { get; init; }

		public TemplateContext(UserModel user, object? data	= null)
		{
			User=user;
			Data=data;
		}
	}
}
