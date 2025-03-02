using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwosFramework.Generators.MongoDBUpdateGenerator
{
	internal class UpdateProperty
	{
		public string SourceName { get; set; }
		public string TargetName { get; set; }
		public bool IsEnumerable { get; set; }
		public bool IgnoreNull { get; set; }
		public bool IgnoreEmpty { get; set; }
		public bool UseStringEmpty { get; set; }
		public bool ApplyToAllMethods { get; set; }
		public string MethodName { get; set; }
		public bool IsUnmarked { get; set; }
		public Location SourceLocation { get; set; }
		public CollectionHandling CollectionHandling { get; set; }

		public UpdateProperty(string sourceName, bool isEnumerable, string methodName, bool applyToAllMethods, bool isUnmarked, Location sourceLocation, 
			string targetName = Constants.UpdatePropertyAttribute_TargetPropertyName_DefaultValue, 
			bool ignoreNull = Constants.UpdatePropertyAttribute_IgnoreNull_DefaultValue, 
			bool ignoreEmpty = Constants.UpdatePropertyAttribute_IgnoreEmpty_DefaultValue, 
			CollectionHandling collectionHandling = Constants.UpdatePropertyAttribute_CollectionHandling_DefaultValue, 
			bool useStringEmpty = Constants.UpdatePropertyAttribute_UseStringEmpty_DefaultValue
		)
		{
			SourceName = sourceName;
			TargetName = targetName ?? sourceName;
			MethodName = methodName;
			IsUnmarked = isUnmarked;
			IsEnumerable = isEnumerable;
			IgnoreNull = ignoreNull;
			IgnoreEmpty = ignoreEmpty;
			SourceLocation = sourceLocation;
			CollectionHandling = collectionHandling;
			ApplyToAllMethods = applyToAllMethods;
			UseStringEmpty = useStringEmpty;
		}
	}
}
