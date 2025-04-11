using MongoDB.Driver;

namespace HTMXBase.Database.InterceptingShim
{
	public class InterceptionEvents<TDocument> : IInterceptionEvents<TDocument>
	{
		public Func<TDocument, TDocument> ReplaceHandler { get; init; }
		public Func<UpdateDefinition<TDocument>, UpdateDefinition<TDocument>> UpdateHandler { get; init; }
		public Func<TDocument, TDocument> InsertHandler { get; init; }

		TDocument IInterceptionEvents<TDocument>.OnInsert(TDocument document)
		{
			return InsertHandler == null ? document : InsertHandler(document);
		}

		TDocument IInterceptionEvents<TDocument>.OnReplace(TDocument document)
		{
			return ReplaceHandler == null ? document : ReplaceHandler(document);
		}

		UpdateDefinition<TDocument> IInterceptionEvents<TDocument>.OnUpdate(UpdateDefinition<TDocument> update)
		{
			return UpdateHandler == null ? update : UpdateHandler(update);
		}
	}
}
