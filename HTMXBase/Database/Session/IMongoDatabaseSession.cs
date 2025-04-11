using MongoDB.Driver;

namespace HTMXBase.Database.Session
{
	public interface IMongoDatabaseSession : IDisposable
	{
		IMongoDatabase Database { get; }
		Task AbortAsync();
		Task CommitAsync();
		void StartTransaction();
	}
}