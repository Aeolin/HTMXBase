using MongoDB.Bson;

namespace MongoDBSemesterProjekt.Database.Models
{
	public class FieldMatchModel
	{
		public required string Name { get; set; }
		public MatchKind MatchKind { get; set; }
		public BsonType Type { get; set; }
	}
}
