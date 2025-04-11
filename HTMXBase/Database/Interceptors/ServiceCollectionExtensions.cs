using HTMXBase.Database.InterceptingShim;
using HTMXBase.Database.Models;
using HTMXBase.Utils;
using System.Reflection;

namespace HTMXBase.Database.Interceptors
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
