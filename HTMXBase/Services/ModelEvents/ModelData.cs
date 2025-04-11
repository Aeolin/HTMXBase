using MongoDB.Bson;
using HTMXBase.Database.Models;
using System.Diagnostics.CodeAnalysis;

namespace HTMXBase.Services.ModelEvents
{
	public class ModelData<T> 
	{
		public T? Model { get; init; }
		public ObjectId ModelId { get; set; }

		public ModelData(ObjectId modelId)
		{
			ModelId = modelId;
		}

		public ModelData(T model, ObjectId? modelId = null)
		{
			Model=model;
			ModelId = modelId ?? model switch
			{
				BsonDocument bdoc => bdoc["id"].AsObjectId,
				EntityBase ebase => ebase.Id,
				_ => throw new NotSupportedException("Expected model to have an id")
			};
		}
	}

	public static class ModelData
	{
		public static ModifyEvent<ModelData<T>> Create<T>(T model, ObjectId? modelId = null) => new ModifyEvent<ModelData<T>>(ModifyMode.Add, new ModelData<T>(model, modelId));
		public static ModifyEvent<ModelData<T>> Update<T>(T model, ObjectId? modelId = null) => new ModifyEvent<ModelData<T>>(ModifyMode.Modify, new ModelData<T>( model, modelId));
		public static ModifyEvent<ModelData<T>> Delete<T>(ObjectId modelId) => new ModifyEvent<ModelData<T>>(ModifyMode.Delete, new ModelData<T>(modelId));
	}
}
