using AutoMapper;
using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDBSemesterProjekt.ApiModels;
using MongoDBSemesterProjekt.Authorization;
using MongoDBSemesterProjekt.Models;
using MongoDBSemesterProjekt.OutputFormatters;
using MongoDBSemesterProjekt.Services.FileStorage;
using MongoDBSemesterProjekt.Services.JWTAuth;
using MongoDBSemesterProjekt.Services.ObjectCache;
using MongoDBSemesterProjekt.Services.TemplateStore;
using MongoDBSemesterProjekt.Utils;
using System.Collections.Frozen;
using System.Reflection;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Add services to the container.

var handlebars = Handlebars.Create();
builder.Services.AddSingleton<IHandlebars>(HandlebarsEx.Create(cfg =>
{
	cfg.UseJson();
}));


builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>(); 
builder.Services.AddSingleton<PasswordHasher<UserModel>>();

builder.Services.AddControllers(opts =>
{
	opts.OutputFormatters.Add(new HtmxOutputFormatter());
	
}).AddJsonOptions(x =>
{
	x.JsonSerializerOptions.Converters.Add(new ObjectIdConverter());
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.Configure<FlatFileStorageConfig>(config.GetSection("FlatFileStorage"));
builder.Services.AddScoped<IFileStorage, FlatFileStorage>();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IMongoClient>(x => new MongoClient(x.GetRequiredService<IConfiguration>().GetConnectionString("MongoDB")));
builder.Services.AddScoped<IMongoDatabase>(x =>
{
	var client = x.GetRequiredService<IMongoClient>();
	var config = x.GetRequiredService<IConfiguration>();
	var url = new MongoUrl(config.GetConnectionString("MongoDB"));
	return client.GetDatabase(url.DatabaseName);
});

builder.Services.AddScoped<IMongoDatabaseSession>(x =>
{
	var client = x.GetRequiredService<IMongoClient>();
	var config = x.GetRequiredService<IConfiguration>();
	var url = new MongoUrl(config.GetConnectionString("MongoDB"));
	return new MongoDatabaseSession(client, url.DatabaseName);
});

builder.Services.Configure<InMemoryCacheConfig>(x =>
{
	x.MaxRetentionTime = TimeSpan.FromMinutes(5);
	x.RefreshOnRead = true;
});

builder.Services.AddSingleton<IInMemoryCache<string, HandlebarsTemplate<object, object>>, InMemoryCache<string, HandlebarsTemplate<object, object>>>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IHtmxTemplateStore, HtmxTemplateStore>();
builder.Services.AddEndpointsApiExplorer();
var jwtConfig = config.GetSection("JwtOptions").Get<JwtOptions>();
if (jwtConfig == null)
	throw new InvalidOperationException("JwtOptions not found in configuration");

builder.Services.AddSingleton(jwtConfig);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, x =>
	{
		x.TokenValidationParameters = new TokenValidationParameters
		{
			ValidIssuer = jwtConfig.Issuer,
			ValidAudience = jwtConfig.Audience,
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.PrivateKey)),
		};

		x.Events = new JwtBearerEvents();
		x.Events.OnMessageReceived += (ctx) =>
			{
				if (ctx.Request?.Cookies.TryGetValue("jwt", out var jwt) ?? false)
					ctx.Token = jwt;

				return Task.CompletedTask;
			};

	});

builder.Services.AddAuthorization();

builder.Services.AddAutoMapper(opts => {
	opts.AddMaps(Assembly.GetExecutingAssembly());
	opts.CreateMap<CollectionModel, ApiCollection>(MemberList.Destination);
});



var app = builder.Build();

BsonSerializer.RegisterSerializer(new JsonDocumentSerializer(BsonType.String));


using (var scope = app.Services.CreateScope())
{
	var session = scope.ServiceProvider.GetRequiredService<IMongoDatabaseSession>();
	session.StartTransaction();
	try
	{
		var db = session.Db;
		var collections = await db.ListCollections().ToListAsync();
		var collectionNames = collections.Select(x => x["name"]).ToFrozenSet();

		var uniqueIndexOptions = new CreateIndexOptions { Unique = true };
		if (collectionNames.Any() == false)
		{
			await db.CreateCollectionWithSchemaAsync<CollectionModel>(CollectionModel.CollectionName);
			await db.CreateCollectionWithSchemaAsync<UserModel>(UserModel.CollectionName);
			await db.CreateCollectionWithSchemaAsync<GroupModel>(GroupModel.CollectionName);

			var userCollection = db.GetCollection<UserModel>(UserModel.CollectionName);
			await userCollection.CreateUniqueKeyAsync(x => x.Email);
			await userCollection.CreateUniqueKeyAsync(x => x.Username);

			var groupCollection = db.GetCollection<GroupModel>(GroupModel.CollectionName);
			await groupCollection.CreateUniqueKeyAsync(x => x.Slug);

			var collectionCollection = db.GetCollection<CollectionModel>(CollectionModel.CollectionName);
			await collectionCollection.CreateUniqueKeyAsync(x => x.Slug);

			collections = await db.ListCollections().ToListAsync();
			var userSchema = collections.FirstOrDefault(x => x["name"] == UserModel.CollectionName)?["options"]?["validator"]?["$jsonSchema"];
			var userCollectionModel = new CollectionModel
			{
				CacheRetentionTime = null,
				Slug = UserModel.CollectionName,
				Schema = JsonDocument.Parse(userSchema.ToJson()),
				Name = "Users",
				IsInbuilt = true
			};

			await collectionCollection.InsertOneAsync(userCollectionModel);

			var groupSchema = collections.FirstOrDefault(x => x["name"] == GroupModel.CollectionName)?["options"]?["validator"]?["$jsonSchema"];
			var groupCollectionModel = new CollectionModel
			{
				CacheRetentionTime = null,
				Slug = GroupModel.CollectionName,
				Schema = JsonDocument.Parse(groupSchema.ToJson()),
				Name = "Groups",
				IsInbuilt = true
			};

			await collectionCollection.InsertOneAsync(groupCollectionModel);

			var collectionSchema = collections.FirstOrDefault(x => x["name"] == CollectionModel.CollectionName)?["options"]?["validator"]?["$jsonSchema"];
			var collectionCollectionModel = new CollectionModel
			{
				CacheRetentionTime = null,
				Slug = CollectionModel.CollectionName,
				Schema = JsonDocument.Parse(collectionSchema.ToJson()),
				Name = "Collections",
				IsInbuilt = true
			};

			await collectionCollection.InsertOneAsync(collectionCollectionModel);

			var permissionAttributes = typeof(Program).Assembly.GetTypes()
				.SelectMany(x => x.GetMethods())
				.SelectMany(x => x.GetCustomAttributes<PermissionAttribute>())
				.SelectMany(x => x.Groups.Select(y => new { Permission = x.Permission, Group = y }))
				.GroupBy(x => x.Group);

			List<GroupModel> groups = new();
			foreach (var permission in permissionAttributes)
			{
				groups.Add(
					new GroupModel
					{
						Name = permission.Key,
						Slug = permission.Key.ToLower(),
						Permissions = permission.Select(x => x.Permission).Distinct().ToList(),
						Description = $"Autogenrated group {permission.Key}"
					}
				);
			}

			await groupCollection.InsertManyAsync(groups);
			await session.CommitAsync();
		}

	}
	catch (Exception ex)
	{
		await session.AbortAsync();
	}
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();

	app.UseSwaggerUi(opts =>
	{
		opts.DocumentPath = "openapi/v1.json";
	});
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
