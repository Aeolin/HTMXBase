using System;
using System.Collections.Generic;
using System.Text;

namespace AwosFramework.Generators.MongoDBUpdateGenerator
{
	public static class Constants
	{
		public const string NameSpace = "AwosFramework.Generators.MongoDBUpdateGenerator";

		public const string MarkerAttributeClassName = "MongoDBUpdateAttribute";
		public const string MarkerAttributeClassFullName = $"{NameSpace}.{MarkerAttributeClassName}";

		public const string MarkerAttributeClass =
		$$"""
		using System;

		namespace {{NameSpace}}
		{
			[AttributeUsage(AttributeTargets.Class)]
			public class {{MarkerAttributeClassName}} : Attribute
			{
				public static Type EntityType { get; set; }

				public {{MarkerAttributeClassName}}(Type entityType)
				{
					this.EntityType = entityType;
				}
			}
		}
		""";

	}
}
