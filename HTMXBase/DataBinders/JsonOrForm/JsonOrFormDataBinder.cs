using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using HTMXBase.Utils;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HTMXBase.DataBinders.JsonOrForm
{
	public class JsonOrFormDataBinder : IModelBinder
	{

		public JsonOrFormDataBinder()
		{
		}


		public static JsonDocument ToJson(IFormCollection formCollection)
		{
			Dictionary<string, object?> structuredData = new();
			if(formCollection.Keys.Count == 0)
				return JsonDocument.Parse("{}");

			foreach (var key in formCollection.Keys)
			{
				SetValue(structuredData, key, formCollection[key]);
			}

			if (structuredData.Keys.All(k => k.StartsWith("[")))
			{
				var list = structuredData[string.Empty];
				return JsonSerializer.SerializeToDocument(list);
			}
			else
			{
				return JsonSerializer.SerializeToDocument(structuredData);
			}

		}

		private static void SetValue(Dictionary<string, object?> data, string key, string value)
		{
			var segments = key.Split('.');
			AddToNestedDictionary(data, segments, value);
		}

		private static void AddToNestedDictionary(Dictionary<string, object?> data, string[] segments, string value)
		{
			object? current = data;
			for (int i = 0; i < segments.Length; i++)
			{
				string segment = segments[i];
				bool isArray = segment.EndsWith("]");

				if (isArray)
				{
					int startIdx = segment.IndexOf('[');
					string propName = startIdx == 0 ? string.Empty : segment.Substring(0, startIdx);
					int index = int.Parse(segment.Substring(startIdx + 1, segment.Length - startIdx - 2));

					if (current is Dictionary<string, object?> objDict)
					{
						if (!objDict.ContainsKey(propName))
						{
							objDict[propName] = new List<object?>();
						}

						if (objDict[propName] is List<object?> list)
						{
							EnsureListSize(list, index);

							if (i == segments.Length - 1)
							{
								list[index] = value;
							}
							else
							{
								if (list[index] == null)
								{
									list[index] = new Dictionary<string, object?>();
								}
								current = list[index];
							}
						}
					}
				}
				else
				{
					if (current is Dictionary<string, object?> objDict)
					{
						if (!objDict.ContainsKey(segment))
						{
							objDict[segment] = i == segments.Length - 1 ? value : new Dictionary<string, object?>();
						}
						current = objDict[segment];
					}
				}
			}
		}

		private static void EnsureListSize(List<object?> list, int index)
		{
			while (list.Count <= index)
			{
				list.Add(null);
			}
		}

		private object? Deserialize(IFormCollection collection, ModelMetadata metadata, string? path = null)
		{
			var filter = path; // path == null ? metadata.Name : $"{path}.{metadata.Name}";
			if (metadata.IsEnumerableType)
			{
				var count = collection.Count(x => x.Key.StartsWith(filter ?? "["));
				var arr = Array.CreateInstance(metadata.ModelType.GetElementType(), count);
				for (int i = 0; i < count; i++)
					arr.SetValue(Deserialize(collection, metadata.ElementMetadata, $"{filter}.{metadata.Name}[{i}]"), i);

				// convert array to target collection
				var collectionType = metadata.ModelType;
				if (collectionType.IsArray)
					return arr;
				else
					return Activator.CreateInstance(collectionType, arr);
			}
			else
			{
				if (metadata.ModelType.IsAssignableTo<JsonDocument>())
				{
					return ToJson(collection);
				}
				else
				{
					var instance = Activator.CreateInstance(metadata.ModelType);
					if (instance  == null)
						return null;

					foreach (var property in metadata.Properties)
					{
						if (property.IsComplexType)
						{
							var value = Deserialize(collection, property, filter);
							property?.PropertySetter?.Invoke(instance, value);
						}
						else
						{
							var key = filter == null ? property.Name : $"{filter}.{property.Name}";
							if (collection.TryGetValue(key, out var value))
							{
								var converted = Convert.ChangeType(value.First(), property.ModelType);
								property?.PropertySetter?.Invoke(instance, converted);
							}
						}
					}
					return instance;
				}
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
				var options = bindingContext.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>();
				var model = await JsonSerializer.DeserializeAsync(json, bindingContext.ModelType, options.Value.JsonSerializerOptions);
				bindingContext.Result = ModelBindingResult.Success(model);
			}
		}
	}
}
