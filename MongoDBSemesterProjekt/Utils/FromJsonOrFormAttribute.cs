using Microsoft.AspNetCore.Mvc;

namespace MongoDBSemesterProjekt.Utils
{
	public class FromJsonOrFormAttribute : ModelBinderAttribute<JsonOrFormDataBinder>
	{
	}
}
