using MongoDB.Bson;

namespace MongoDBSemesterProjekt.Database.Models
{
	public class FieldMatchModel
	{
		public required string ParameterName { get; set; }
		public required string DocumentFieldName { get; set; }
		public MatchKind MatchKind { get; set; }
		public BsonType BsonType { get; set; }
		public bool IsOptional { get; set; }
		public bool IsNullable { get; set; }
	}
}
