﻿using MongoDB.Bson;

namespace HTMXBase.Database.Models
{
	[Index(nameof(Slug), nameof(VirtualPath), IndexType = IndexType.Ascending, IsUnique = true)]
	public class StaticContentModel : EntityBase
	{
		public const string CollectionName = "static-content";
		public ObjectId OwnerId { get; set; }
		public required string Name { get; set; }
		public required string MimeType { get; set; }
		public required long Length { get; set; }
		public required string Slug { get; set; }
		public string? VirtualPath { get; set; }
		public required string StorageId { get; set; }
		public string? ReadPermission { get; set; }
		public string? DeletePermission { get; set; }
	}
}
