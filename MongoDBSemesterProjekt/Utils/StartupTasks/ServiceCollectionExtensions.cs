using MongoDB.Driver;
using MongoDBSemesterProjekt.Database.Session;

namespace MongoDBSemesterProjekt.Utils.StartupTasks
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddStartupTask<TDelegate>(this IServiceCollection services, TDelegate action) where TDelegate : System.Delegate
		{
			return services.AddSingleton<IHostedService>(x => new StartupTask<TDelegate>(x, action));
		}

		public static IServiceCollection UseAsyncSeeding(this IServiceCollection services, Func<IMongoDatabase, Task> seedingFunc)
		{
			return services.AddStartupTask(seedingFunc);
		}

		public static IServiceCollection UseSeeding(this IServiceCollection services, Action<IMongoDatabase> seedingAction)
		{
			return services.AddStartupTask(seedingAction);
		}
	}
}
