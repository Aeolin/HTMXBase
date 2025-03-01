using System;
using System.Collections.Generic;
using System.Text;

namespace AwosFramework.Generators.MongoDBUpdateGenerator
{
	public class UpdateToGenerate
	{
		public string TargetClassName { get; set; }
		public string SourceClassName { get; set; }
		public string[] Fields { get; set; }

		public UpdateToGenerate(string targetClass, string sourceClass, string[] fields)
		{
			TargetClassName=targetClass;
			SourceClassName=sourceClass;
			Fields=fields;
		}
	}
}
