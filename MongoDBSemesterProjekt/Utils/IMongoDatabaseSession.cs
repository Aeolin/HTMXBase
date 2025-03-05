using MongoDB.Driver;

namespace MongoDBSemesterProjekt.Utils
{
	public interface IMongoDatabaseSession : IDisposable
	{
		IMongoDatabase Db { get; }
		Task AbortAsync();
		Task CommitAsync();
		void StartTransaction();
	}
}