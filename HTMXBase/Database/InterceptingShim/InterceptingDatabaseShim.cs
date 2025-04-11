using MongoDB.Bson;
using MongoDB.Driver;

namespace HTMXBase.Database.InterceptingShim
{
	public class InterceptingDatabaseShim : IMongoDatabase
	{
		private readonly IMongoDatabase _db;
		private readonly IServiceProvider _provider;

		public InterceptingDatabaseShim(IMongoDatabase db, IServiceProvider provider)
		{
			_db=db;
			_provider=provider;
		}

		public IMongoClient Client => _db.Client;
		public DatabaseNamespace DatabaseNamespace => _db.DatabaseNamespace;
		public MongoDatabaseSettings Settings => _db.Settings;

		public IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _db.Aggregate(pipeline, options, cancellationToken);
		public IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _db.Aggregate(session, pipeline, options, cancellationToken);
		public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _db.AggregateAsync(pipeline, options, cancellationToken);
		public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _db.AggregateAsync(session, pipeline, options, cancellationToken);
		
		public void AggregateToCollection<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _db.AggregateToCollection(pipeline, options, cancellationToken);
		public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _db.AggregateToCollection(session, pipeline, options, cancellationToken);
		public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _db.AggregateToCollectionAsync(pipeline, options, cancellationToken);
		public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => _db.AggregateToCollectionAsync(session, pipeline, options, cancellationToken);
		
		public void CreateCollection(string name, CreateCollectionOptions options = null, CancellationToken cancellationToken = default) => _db.CreateCollection(name, options, cancellationToken);
		public void CreateCollection(IClientSessionHandle session, string name, CreateCollectionOptions options = null, CancellationToken cancellationToken = default) => _db.CreateCollection(session, name, options, cancellationToken);
		public Task CreateCollectionAsync(string name, CreateCollectionOptions options = null, CancellationToken cancellationToken = default) => _db.CreateCollectionAsync(name, options, cancellationToken);
		public Task CreateCollectionAsync(IClientSessionHandle session, string name, CreateCollectionOptions options = null, CancellationToken cancellationToken = default) => _db.CreateCollectionAsync(session, name, options, cancellationToken);

		public void CreateView<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default) => _db.CreateView(viewName, viewOn, pipeline, options, cancellationToken);
		public void CreateView<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default) => _db.CreateView(session, viewName, viewOn, pipeline, options, cancellationToken);
		public Task CreateViewAsync<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default) => _db.CreateViewAsync(viewName, viewOn, pipeline, options, cancellationToken);
		public Task CreateViewAsync<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options = null, CancellationToken cancellationToken = default) => _db.CreateViewAsync(session, viewName, viewOn, pipeline, options, cancellationToken);

		public void DropCollection(string name, CancellationToken cancellationToken = default) => _db.DropCollection(name, cancellationToken);
		public void DropCollection(string name, DropCollectionOptions options, CancellationToken cancellationToken = default) => _db.DropCollection(name, options, cancellationToken);
		public void DropCollection(IClientSessionHandle session, string name, CancellationToken cancellationToken = default) => _db.DropCollection(session, name, cancellationToken);
		public void DropCollection(IClientSessionHandle session, string name, DropCollectionOptions options, CancellationToken cancellationToken = default) => _db.DropCollection(session, name, options, cancellationToken);

		public Task DropCollectionAsync(string name, CancellationToken cancellationToken = default) => _db.DropCollectionAsync(name, cancellationToken);
		public Task DropCollectionAsync(string name, DropCollectionOptions options, CancellationToken cancellationToken = default) => _db.DropCollectionAsync(name, options, cancellationToken);
		public Task DropCollectionAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default) => _db.DropCollectionAsync(session, name, cancellationToken);
		public Task DropCollectionAsync(IClientSessionHandle session, string name, DropCollectionOptions options, CancellationToken cancellationToken = default) => _db.DropCollectionAsync(session, name, options, cancellationToken);

		public IAsyncCursor<string> ListCollectionNames(ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default) => _db.ListCollectionNames(options, cancellationToken);
		public IAsyncCursor<string> ListCollectionNames(IClientSessionHandle session, ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default) => _db.ListCollectionNames(session, options, cancellationToken);
		public Task<IAsyncCursor<string>> ListCollectionNamesAsync(ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default) => _db.ListCollectionNamesAsync(options, cancellationToken);
		public Task<IAsyncCursor<string>> ListCollectionNamesAsync(IClientSessionHandle session, ListCollectionNamesOptions options = null, CancellationToken cancellationToken = default) => _db.ListCollectionNamesAsync(session, options, cancellationToken);

		public IAsyncCursor<BsonDocument> ListCollections(ListCollectionsOptions options = null, CancellationToken cancellationToken = default) => _db.ListCollections(options, cancellationToken);
		public IAsyncCursor<BsonDocument> ListCollections(IClientSessionHandle session, ListCollectionsOptions options = null, CancellationToken cancellationToken = default) => _db.ListCollections(session, options, cancellationToken);
		public Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(ListCollectionsOptions options = null, CancellationToken cancellationToken = default) => _db.ListCollectionsAsync(options, cancellationToken);
		public Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(IClientSessionHandle session, ListCollectionsOptions options = null, CancellationToken cancellationToken = default) => _db.ListCollectionsAsync(session, options, cancellationToken);

		public void RenameCollection(string oldName, string newName, RenameCollectionOptions options = null, CancellationToken cancellationToken = default) => _db.RenameCollection(oldName, newName, options, cancellationToken);
		public void RenameCollection(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions options = null, CancellationToken cancellationToken = default) => _db.RenameCollection(session, oldName, newName, options, cancellationToken);
		public Task RenameCollectionAsync(string oldName, string newName, RenameCollectionOptions options = null, CancellationToken cancellationToken = default) => _db.RenameCollectionAsync(oldName, newName, options, cancellationToken);
		public Task RenameCollectionAsync(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions options = null, CancellationToken cancellationToken = default) => _db.RenameCollectionAsync(session, oldName, newName, options, cancellationToken);

		public TResult RunCommand<TResult>(Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default) => _db.RunCommand(command, readPreference, cancellationToken);
		public TResult RunCommand<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default) => _db.RunCommand(session, command, readPreference, cancellationToken);
		public Task<TResult> RunCommandAsync<TResult>(Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default) => _db.RunCommandAsync(command, readPreference, cancellationToken);
		public Task<TResult> RunCommandAsync<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default) => _db.RunCommandAsync(session, command, readPreference, cancellationToken);

		public IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => _db.Watch(pipeline, options, cancellationToken);
		public IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => _db.Watch(session, pipeline, options, cancellationToken);
		public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => _db.WatchAsync(pipeline, options, cancellationToken);
		public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => _db.WatchAsync(session, pipeline, options, cancellationToken);

		public IMongoDatabase WithReadConcern(ReadConcern readConcern) => _db.WithReadConcern(readConcern);
		public IMongoDatabase WithReadPreference(ReadPreference readPreference) => _db.WithReadPreference(readPreference);
		public IMongoDatabase WithWriteConcern(WriteConcern writeConcern) => _db.WithWriteConcern(writeConcern);

		public IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings settings = null)
		{
			var _collection = _db.GetCollection<TDocument>(name, settings);
			var events = _provider.GetService<IInterceptionEvents<TDocument>>();
			return new InterceptingCollectionShim<TDocument>(_collection, events);
		}
	}
}
