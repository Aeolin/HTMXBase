using System;
using System.Collections.Generic;
using System.Text;

namespace AwosFramework.Generators.MongoDBUpdateGenerator
{
	internal enum CollectionHandling
	{
		Set = 0,
		AddToSet = 1,
		PushAll = 2,
		PullAll = 3,
	}
}
