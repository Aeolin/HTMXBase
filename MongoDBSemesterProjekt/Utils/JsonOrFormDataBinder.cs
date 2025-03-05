using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MongoDBSemesterProjekt.Utils
{
	public class JsonOrFormDataBinder : IModelBinder
	{
		private readonly IOptions<JsonSerializerOptions> _options;

		public JsonOrFormDataBinder(IOptions<JsonSerializerOptions> options)
		{
			_options=options;
		}


		private object Deserialize(IFormCollection collection, ModelMetadata metadata, string? path = null)
		{
			var filter = path; // path == null ? metadata.Name : $"{path}.{metadata.Name}";
			if (metadata.IsEnumerableType)
			{
				var count = collection.Count(x => x.Key.StartsWith(filter ?? "["));
				var arr = Array.CreateInstance(metadata.ModelType.GetElementType(), count);
				for(int i = 0; i < count; i++)
					 arr.SetValue(Deserialize(collection, metadata.ElementMetadata, $"{filter}.{metadata.Name}[{i}]"), i);

				// convert array to target collection
				var collectionType = metadata.ModelType;
				if(collectionType.IsArray)
					return arr;
				else
					return Activator.CreateInstance(collectionType, arr);
			}
			else
			{
				var instance = Activator.CreateInstance(metadata.ModelType);
				foreach (var property in metadata.Properties)
				{
					if (property.IsComplexType)
					{
						var value = Deserialize(collection, property, filter);
						property.PropertySetter(instance, value);
					}
					else
					{
						var key = filter == null ? property.Name : $"{filter}.{property.Name}";
						if (collection.TryGetValue(key, out var value))
						{
							var converted =  Convert.ChangeType(value.First(), property.ModelType);
							property.PropertySetter(instance, converted);
						}
					}
				}
				return instance;
			}
		}	

		public async Task BindModelAsync(ModelBindingContext bindingContext)
		{
			var request = bindingContext.HttpContext.Request;
			var source = bindingContext.BindingSource;
			if (request.HasFormContentType)
			{
				var form = await request.ReadFormAsync();
				var value = Deserialize(form, bindingContext.ModelMetadata);
				bindingContext.Result = ModelBindingResult.Success(value);
			}
			else 
			{
				var json = request.Body;
				var model = await JsonSerializer.DeserializeAsync(json, bindingContext.ModelType, _options.Value);
				bindingContext.Result = ModelBindingResult.Success(model);
			}
		}
	}
}
