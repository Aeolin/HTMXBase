using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Search;

namespace MongoDBSemesterProjekt.Database.InterceptingShim
{
	public class InterceptingCollectionShim<T> : IMongoCollection<T>
	{
		private readonly IMongoCollection<T> col;
		private readonly IInterceptionEvents<T> _events;

		public InterceptingCollectionShim(IMongoCollection<T> collection, IInterceptionEvents<T> events = null)
		{
			col=collection;
			_events = events ?? new InterceptionEvents<T>();
		}

		public CollectionNamespace CollectionNamespace => col.CollectionNamespace;
		public IMongoDatabase Database => col.Database;
		public IBsonSerializer<T> DocumentSerializer => col.DocumentSerializer;
		public IMongoIndexManager<T> Indexes => col.Indexes;
		public IMongoSearchIndexManager SearchIndexes => col.SearchIndexes;
		public MongoCollectionSettings Settings => col.Settings;

		public IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => col.Aggregate(pipeline, options, cancellationToken);
		public IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => col.Aggregate(session, pipeline, options, cancellationToken);
		public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => col.AggregateAsync(pipeline, options, cancellationToken);
		public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => col.AggregateAsync(session, pipeline, options, cancellationToken);

		public void AggregateToCollection<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => col.AggregateToCollection(pipeline, options, cancellationToken);
		public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => col.AggregateToCollection(session, pipeline, options, cancellationToken);
		public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => col.AggregateToCollectionAsync<TResult>(pipeline, options, cancellationToken);
		public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => col.AggregateToCollectionAsync(session, pipeline, options, cancellationToken);
		 
		public BulkWriteResult<T> BulkWrite(IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default) => col.BulkWrite(requests, options, cancellationToken);
		public BulkWriteResult<T> BulkWrite(IClientSessionHandle session, IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default) => col.BulkWrite(session, requests, options, cancellationToken);

		public Task<BulkWriteResult<T>> BulkWriteAsync(IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default) => col.BulkWriteAsync(requests, options, cancellationToken);
		public Task<BulkWriteResult<T>> BulkWriteAsync(IClientSessionHandle session, IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default) => col.BulkWriteAsync(session, requests, options, cancellationToken);

		public long Count(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => col.Count(filter, options, cancellationToken);
		public long Count(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => col.Count(session, filter, options, cancellationToken);

		public Task<long> CountAsync(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => col.CountAsync(filter, options, cancellationToken);
		public Task<long> CountAsync(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => col.CountAsync(session, filter, options, cancellationToken);
		
		public long CountDocuments(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => col.CountDocuments(filter, options, cancellationToken);
		public long CountDocuments(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => col.CountDocuments(session, filter, options, cancellationToken);
		public Task<long> CountDocumentsAsync(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => col.CountDocumentsAsync(filter, options, cancellationToken);
		public Task<long> CountDocumentsAsync(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = default) => col.CountDocumentsAsync(session, filter, options, cancellationToken);
		
		public DeleteResult DeleteMany(FilterDefinition<T> filter, CancellationToken cancellationToken = default) => col.DeleteMany(filter, cancellationToken);
		public DeleteResult DeleteMany(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default) => col.DeleteMany(filter, options, cancellationToken);
		public DeleteResult DeleteMany(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = default) => col.DeleteMany(session, filter, options, cancellationToken);
		public Task<DeleteResult> DeleteManyAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default) => col.DeleteManyAsync(filter, cancellationToken);
		public Task<DeleteResult> DeleteManyAsync(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default) => col.DeleteManyAsync(filter, options, cancellationToken);
		public Task<DeleteResult> DeleteManyAsync(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = default) => col.DeleteManyAsync(session, filter, options, cancellationToken);

		public DeleteResult DeleteOne(FilterDefinition<T> filter, CancellationToken cancellationToken = default) => col.DeleteOne(filter, cancellationToken);
		public DeleteResult DeleteOne(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default) => col.DeleteOne(filter, options, cancellationToken);
		public DeleteResult DeleteOne(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = default) => col.DeleteOne(session, filter, options, cancellationToken);

		public Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default) => col.DeleteOneAsync(filter, cancellationToken);
		public Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default) => col.DeleteOneAsync(filter, options, cancellationToken);
		public Task<DeleteResult> DeleteOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = default) => col.DeleteOneAsync(session, filter, options, cancellationToken);

		public IAsyncCursor<TField> Distinct<TField>(FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => col.Distinct(field, filter, options, cancellationToken);
		public IAsyncCursor<TField> Distinct<TField>(IClientSessionHandle session, FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => col.Distinct(session, field, filter, options, cancellationToken);
		public Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => col.DistinctAsync(field, filter, options, cancellationToken);
		public Task<IAsyncCursor<TField>> DistinctAsync<TField>(IClientSessionHandle session, FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => col.DistinctAsync(session, field, filter, options, cancellationToken);
		
		public IAsyncCursor<TItem> DistinctMany<TItem>(FieldDefinition<T, IEnumerable<TItem>> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => col.DistinctMany(field, filter, options, cancellationToken);
		public IAsyncCursor<TItem> DistinctMany<TItem>(IClientSessionHandle session, FieldDefinition<T, IEnumerable<TItem>> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => col.DistinctMany(session, field, filter, options, cancellationToken);
		public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(FieldDefinition<T, IEnumerable<TItem>> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => col.DistinctManyAsync(field, filter, options, cancellationToken);
		public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(IClientSessionHandle session, FieldDefinition<T, IEnumerable<TItem>> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = default) => col.DistinctManyAsync(session, field, filter, options, cancellationToken);
		
		public long EstimatedDocumentCount(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = default) => col.EstimatedDocumentCount(options, cancellationToken);
		public Task<long> EstimatedDocumentCountAsync(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = default) => col.EstimatedDocumentCountAsync(options, cancellationToken);

		public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => col.FindAsync(filter, options, cancellationToken);
		public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => col.FindAsync(session, filter, options, cancellationToken);

		public TProjection FindOneAndDelete<TProjection>(FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => col.FindOneAndDelete(filter, options, cancellationToken);
		public TProjection FindOneAndDelete<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => col.FindOneAndDelete(session, filter, options, cancellationToken);
		public Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => col.FindOneAndDeleteAsync(filter, options, cancellationToken);
		public Task<TProjection> FindOneAndDeleteAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => col.FindOneAndDeleteAsync(session, filter, options, cancellationToken);

		public TProjection FindOneAndReplace<TProjection>(FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = default)
		{
			if(_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.FindOneAndReplace(filter, replacement, options, cancellationToken);
		}

		public TProjection FindOneAndReplace<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.FindOneAndReplace(session, filter, replacement, options, cancellationToken);
		}

		public Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.FindOneAndReplaceAsync(filter, replacement, options, cancellationToken);
		}

		public Task<TProjection> FindOneAndReplaceAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.FindOneAndReplaceAsync(session, filter, replacement, options, cancellationToken);
		}

		public TProjection FindOneAndUpdate<TProjection>(FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = default)
		{
			if(_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.FindOneAndUpdate(filter, update, options, cancellationToken);
		}

		public TProjection FindOneAndUpdate<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.FindOneAndUpdate(session, filter, update, options, cancellationToken);
		}

		public Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
		}

		public Task<TProjection> FindOneAndUpdateAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.FindOneAndUpdateAsync(session, filter, update, options, cancellationToken);
		}

		public IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => col.FindSync(filter, options, cancellationToken);
		public IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = default) => col.FindSync(session, filter, options, cancellationToken);

		public void InsertMany(IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default)
		{
			if(_events.OnInsert != null)
				documents = documents.Select(_events.OnInsert);

			col.InsertMany(documents, options, cancellationToken);
		}

		public void InsertMany(IClientSessionHandle session, IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnInsert != null)
				documents = documents.Select(_events.OnInsert);

			col.InsertMany(session, documents, options, cancellationToken);
		}

		public Task InsertManyAsync(IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnInsert != null)
				documents = documents.Select(_events.OnInsert);

			return col.InsertManyAsync(documents, options, cancellationToken);
		}

		public Task InsertManyAsync(IClientSessionHandle session, IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnInsert != null)
				documents = documents.Select(_events.OnInsert);

			return col.InsertManyAsync(session, documents, options, cancellationToken);
		}

		public void InsertOne(T document, InsertOneOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnInsert != null)
				document = _events.OnInsert(document);

			col.InsertOne(document, options, cancellationToken);
		}

		public void InsertOne(IClientSessionHandle session, T document, InsertOneOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnInsert != null)
				document = _events.OnInsert(document);

			col.InsertOne(session, document, options, cancellationToken);
		}

		public Task InsertOneAsync(T document, CancellationToken _cancellationToken)
		{
			if (_events.OnInsert != null)
				document = _events.OnInsert(document);

			return col.InsertOneAsync(document, _cancellationToken);
		}

		public Task InsertOneAsync(T document, InsertOneOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnInsert != null)
				document = _events.OnInsert(document);

			return col.InsertOneAsync(document, options, cancellationToken);
		}

		public Task InsertOneAsync(IClientSessionHandle session, T document, InsertOneOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnInsert != null)
				document = _events.OnInsert(document);

			return col.InsertOneAsync(session, document, options, cancellationToken);
		}

		public IAsyncCursor<TResult> MapReduce<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = default) => col.MapReduce(map, reduce, options, cancellationToken);
		public IAsyncCursor<TResult> MapReduce<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = default) => col.MapReduce(session, map, reduce, options, cancellationToken);
		public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = default) => col.MapReduceAsync(map, reduce, options, cancellationToken);
		public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = default) => col.MapReduceAsync(session, map, reduce, options, cancellationToken);

		public IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>() where TDerivedDocument : T => col.OfType<TDerivedDocument>();

		public ReplaceOneResult ReplaceOne(FilterDefinition<T> filter, T replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.ReplaceOne(filter, replacement, options, cancellationToken);
		}

		public ReplaceOneResult ReplaceOne(FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken = default)
		{
			if (_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.ReplaceOne(filter, replacement, options, cancellationToken);
		}

		public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.ReplaceOne(session, filter, replacement, options, cancellationToken);
		}

		public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken = default)
		{
			if (_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.ReplaceOne(session, filter, replacement, options, cancellationToken);
		}

		public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<T> filter, T replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.ReplaceOneAsync(filter, replacement, options, cancellationToken);
		}

		public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken = default)
		{
			if (_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.ReplaceOneAsync(filter, replacement, options, cancellationToken);
		}

		public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.ReplaceOneAsync(session, filter, replacement, options, cancellationToken);
		}

		public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken = default)
		{
			if (_events.OnReplace != null)
				replacement = _events.OnReplace(replacement);

			return col.ReplaceOneAsync(session, filter, replacement, options, cancellationToken);
		}

		public UpdateResult UpdateMany(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.UpdateMany(filter, update, options, cancellationToken);
		}

		public UpdateResult UpdateMany(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.UpdateMany(session, filter, update, options, cancellationToken);
		}

		public Task<UpdateResult> UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.UpdateManyAsync(filter, update, options, cancellationToken);
		}

		public Task<UpdateResult> UpdateManyAsync(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.UpdateManyAsync(session, filter, update, options, cancellationToken);
		}

		public UpdateResult UpdateOne(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.UpdateOne(filter, update, options, cancellationToken);
		}

		public UpdateResult UpdateOne(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.UpdateOne(session, filter, update, options, cancellationToken);
		}

		public Task<UpdateResult> UpdateOneAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.UpdateOneAsync(filter, update, options, cancellationToken);
		}

		public Task<UpdateResult> UpdateOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_events.OnUpdate != null)
				update = _events.OnUpdate(update);

			return col.UpdateOneAsync(session, filter, update, options, cancellationToken);
		}

		public IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => col.Watch(pipeline, options, cancellationToken);
		public IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => col.Watch(session, pipeline, options, cancellationToken);
		public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => col.WatchAsync(pipeline, options, cancellationToken);
		public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = default) => col.WatchAsync(session, pipeline, options, cancellationToken);

		public IMongoCollection<T> WithReadConcern(ReadConcern readConcern) => col.WithReadConcern(readConcern);
		public IMongoCollection<T> WithReadPreference(ReadPreference readPreference) => col.WithReadPreference(readPreference);
		public IMongoCollection<T> WithWriteConcern(WriteConcern writeConcern) => col.WithWriteConcern(writeConcern);
	}
}
