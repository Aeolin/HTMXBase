using MongoDB.Driver;

namespace MongoDBSemesterProjekt.Database.Session
{
	public interface IMongoDatabaseSession : IDisposable
	{
		IMongoDatabase Database { get; }
		Task AbortAsync();
		Task CommitAsync();
		void StartTransaction();
	}
}