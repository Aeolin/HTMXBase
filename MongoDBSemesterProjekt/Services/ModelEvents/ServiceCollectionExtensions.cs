using System.Threading.Channels;

namespace MongoDBSemesterProjekt.Services.ModelEvents
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddModelEventChannel<TModel>(this IServiceCollection services)
		{
			var channel = Channel.CreateUnbounded<ModifyEvent<TModel>>();
			services.AddSingleton(channel);
			services.AddSingleton(channel.Writer);
			services.AddSingleton(channel.Reader);
			return services;
		}
	}

}
