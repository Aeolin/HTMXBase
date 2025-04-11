using MongoDB.Driver;

namespace HTMXBase.Database.InterceptingShim
{
	public interface IInterceptionEvents<TDocument>
	{	
		TDocument OnInsert(TDocument document);
		TDocument OnReplace(TDocument document);
		UpdateDefinition<TDocument> OnUpdate(UpdateDefinition<TDocument> update);
	}
}