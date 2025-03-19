using MongoDBSemesterProjekt.Database.InterceptingShim;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Utils;
using System.Reflection;

namespace MongoDBSemesterProjekt.Database.Interceptors
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddEntityUpdateInterceptors(this IServiceCollection services)
		{
			var entityTypes = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsAssignableTo<EntityBase>() && x.IsAbstract == false).ToArray();
			foreach (var entityType in entityTypes)
			{
				var interceptor = typeof(EntityBaseUpdatingInterceptionFactory).GetMethod(nameof(EntityBaseUpdatingInterceptionFactory.Create)).MakeGenericMethod(entityType).Invoke(null, null);
				services.AddInterceptionEvents(interceptor);
			}

			services.AddInterceptionEvents(EntityBaseUpdatingInterceptionFactory.InterceptCustomCollections());
			return services;
		}
	}
}
