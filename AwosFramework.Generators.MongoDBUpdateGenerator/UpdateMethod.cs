using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AwosFramework.Generators.MongoDBUpdateGenerator
{
	internal class UpdateMethod
	{
		public string TargetClassName { get; set; }
		public string NestedTargetClassName { get; set; }

		public string GetTargetClassFullName() => NestedProperty == null ? TargetClassName : NestedTargetClassName;

		public string MethodName { get; set; }
		public UpdateProperty[] Properties { get; set; }
		public bool UsePartialClass { get; set; }
		public Location SourceLocation { get; set; }
		public string? NestedProperty { get; set; }

		public UpdateMethod(string targetClass, string methodName, UpdateProperty[] properties, Location sourceLocation,
			bool usePartialClass = Constants.MarkerAttribute_UsePartialClass_DefaultValue,
			string? nestedProperty = Constants.MarkerAttribute_NestedProperty_DefaultValue
		)
		{
			TargetClassName=targetClass;
			MethodName=methodName;
			Properties=properties;
			UsePartialClass =usePartialClass;
			SourceLocation=sourceLocation;
			NestedProperty=nestedProperty;
		}

	}
}
