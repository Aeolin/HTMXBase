﻿using AutoMapper;
using AwosFramework.Generators.MongoDBUpdateGenerator;
using HTMXBase.Api.Models;
using HTMXBase.BsonSchema;

namespace HTMXBase.Database.Models
{
	[AutoMap(typeof(ApiTemplate))]
	[MongoDBUpdate(typeof(CollectionModel), MethodName = "ToAddTemplate")]
	[UpdateProperty(MethodName = "ToAddTemplate", TargetPropertyName = nameof(CollectionModel.Templates), CollectionHandling = CollectionHandling.PushAll)]
	public class TemplateModel
	{
		public required string Slug { get; set; }
		public bool SingleItem { get; set; } = true;
		public required string Template { get; set; }
		public bool Disabled { get; set; }
	}
}
