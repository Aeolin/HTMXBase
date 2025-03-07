using MongoDB.Driver;
using MongoDBSemesterProjekt.Database.InterceptingShim;

namespace MongoDBSemesterProjekt.Database.Session
{
	public class MongoDatabaseSession : IMongoDatabaseSession
	{
		private readonly IMongoClient _client;
		private readonly IClientSession _session;
		private readonly IMongoDatabase _database;

		public IMongoDatabase Database => _database;

		public MongoDatabaseSession(IMongoClient client, IServiceProvider provider, string dbName)
		{
			_client = client;
			_session = client.StartSession();
			_database = new InterceptingDatabaseShim(client.GetDatabase(dbName), provider);
		}

		public void StartTransaction()
		{
			_session.StartTransaction();
		}

		public async Task CommitAsync()
		{
			await _session.CommitTransactionAsync();
		}

		public async Task AbortAsync()
		{
			await _session.AbortTransactionAsync();
		}

		public void Dispose()
		{
			if (_session.IsInTransaction)
			{
				_session.AbortTransaction();
			}

			_session.Dispose();
		}
	}
}
