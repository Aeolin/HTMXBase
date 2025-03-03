using System;
using System.Collections.Generic;
using System.Text;

namespace AwosFramework.Generators.MongoDBUpdateGenerator
{
	class TargetProperty
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public bool IsEnumerable { get; set; }
		public bool IsString { get; set; }

		public TargetProperty(string name, string type, bool isEnumerable, bool isString)
		{
			Name = name;
			Type = type;
			IsEnumerable = isEnumerable;
			IsString=isString;
		}
	}
}
