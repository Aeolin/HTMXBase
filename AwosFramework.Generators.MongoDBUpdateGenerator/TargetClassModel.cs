using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace AwosFramework.Generators.MongoDBUpdateGenerator
{
	class TargetClassModel
	{
		public string ClassName { get; set; }
		public string NameSpace { get; set; }
		public string FullName => $"{NameSpace}.{ClassName}";
		public ImmutableDictionary<string, TargetProperty> Properties { get; set; }

		public TargetClassModel(string className, string nameSpace, TargetProperty[] properties)
		{
			ClassName=className;
			NameSpace=nameSpace;
			Properties=properties.ToImmutableDictionary(x => x.Name, x => x);
		}
	}
}
