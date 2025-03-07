
namespace MongoDBSemesterProjekt.Utils.StartupTasks
{
	public class StartupTask<TDelegate> : IHostedService where TDelegate : System.Delegate
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly TDelegate _action;

		public StartupTask(IServiceProvider serviceProvider, TDelegate action)
		{
			_serviceProvider=serviceProvider;
			_action = action;
		}


		public async Task StartAsync(CancellationToken cancellationToken)
		{
			using var scope = _serviceProvider.CreateScope();
			var input = _action.Method.GetParameters().Select(x => x.ParameterType == typeof(CancellationToken) ? cancellationToken : scope.ServiceProvider.GetRequiredService(x.ParameterType)).ToArray();
			var result = _action.DynamicInvoke(input);
			if (result is Task task)
				await task;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
