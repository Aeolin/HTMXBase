using HTMXBase.Utils;
using System.Reflection;

namespace HTMXBase.Database.InterceptingShim
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddInterceptionEvents(this IServiceCollection services, object interceptionEvents)
		{
			if(interceptionEvents == null)
				throw new ArgumentNullException(nameof(interceptionEvents));

			var type = interceptionEvents.GetType();
			var eventsInterface = type.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IInterceptionEvents<>));
			if (eventsInterface == null)
				throw new ArgumentException("interceptionEvents must implement IInterceptionEvents<>");

			var docType = eventsInterface.GetGenericArguments()[0];
			var method = typeof(ServiceCollectionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
				.First(x => x.Name == nameof(AddInterceptionEvents) && x.IsGenericMethod)
				.MakeGenericMethod(docType);

			return (IServiceCollection)method.Invoke(null, new object[] { services, interceptionEvents });
		}

		public static IServiceCollection AddInterceptionEvents<TDocument>(this IServiceCollection services, IInterceptionEvents<TDocument> events)
		{
			var service = services.FirstOrDefault(x => x.ServiceType == typeof(IInterceptionEvents<TDocument>));
			if (service is InterceptionEventsCollection<TDocument> interceptionEvents)
			{
				interceptionEvents.Add(events);
			}
			else
			{
				InterceptionEventsCollection<TDocument> collection = [events];
				if (service != null)
				{
					if (service.ImplementationInstance is IInterceptionEvents<TDocument> existingEvents)
						collection.Add(existingEvents);

					services.Remove(service);
				}

				services.AddSingleton<IInterceptionEvents<TDocument>>(collection);
			}

			return services;
		}
	}
}
