using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwosFramework.Generators.MongoDBUpdateGenerator
{
	internal class UpdateApiModel
	{
		public string SourceClassName { get; set; }
		public string SourceClassNameSpace { get; set; }
		public UpdateMethod[] UpdateMethods { get; set; }
		public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

		public UpdateApiModel(string typeName, string nameSpace, UpdateMethod[] updates, Diagnostic[] diagnostics)
		{
			SourceClassName=typeName;
			SourceClassNameSpace=nameSpace;
			UpdateMethods=updates;
			if (diagnostics !=null)
				Diagnostics.AddRange(diagnostics);
		}
	}
}
