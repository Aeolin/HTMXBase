using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Models;
using MongoDBSemesterProjekt.OutputFormatters;
using MongoDBSemesterProjekt.Services.FileStorage;
using MongoDBSemesterProjekt.Services.JWTAuth;
using MongoDBSemesterProjekt.Services.ObjectCache;
using MongoDBSemesterProjekt.Utils;
using System.Collections.Frozen;
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


builder.Services.AddSingleton<PasswordHasher<UserModel>>();
builder.Services.AddControllers(opts =>
{
	opts.OutputFormatters.Add(new HtmxOutputFormatter());
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

builder.Services.Configure<InMemoryCacheConfig>(x =>
{
	x.MaxRetentionTime = TimeSpan.FromMinutes(5);
	x.RefreshOnRead = true;
});
builder.Services.AddSingleton<IInMemoryCache<string, HandlebarsTemplate<object, object>>, InMemoryCache<string, HandlebarsTemplate<object, object>>>();
builder.Services.AddScoped<IJwtService, JwtService>();
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
	var collections = await db.ListCollections().ToListAsync();
	var collectionNames = collections.Select(x => x["name"]).ToFrozenSet();


	if (collectionNames.Contains(UserModel.CollectionName) == false)
	{
		var userCollection = db.GetCollection<UserModel>(UserModel.CollectionName);
		await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<UserModel>(Builders<UserModel>.IndexKeys.Ascending(x => x.Email), new CreateIndexOptions { Unique = true }));
		await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<UserModel>(Builders<UserModel>.IndexKeys.Ascending(x => x.Username), new CreateIndexOptions { Unique = true }));
		
		
		var groupCollection = db.GetCollection<GroupModel>(GroupModel.CollectionName);
		await groupCollection.Indexes.CreateOneAsync(new CreateIndexModel<GroupModel>(Builders<GroupModel>.IndexKeys.Ascending(x => x.Slug), new CreateIndexOptions { Unique = true }));

		var collectionCollection = db.GetCollection<CollectionModel>(CollectionModel.CollectionName);
		await collectionCollection.Indexes.CreateOneAsync(new CreateIndexModel<CollectionModel>(Builders<CollectionModel>.IndexKeys.Ascending(x => x.Slug), new CreateIndexOptions { Unique = true }));
		await collectionCollection.Indexes.CreateOneAsync(new CreateIndexModel<CollectionModel>(Builders<CollectionModel>.IndexKeys.Ascending(x => x.Templates[-1].Slug), new CreateIndexOptions { Unique = true }));

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
	}

	if (collectionNames.Contains(GroupModel.CollectionName) == false)
	{
		var groupCollection = db.GetCollection<GroupModel>(GroupModel.CollectionName);
	}

	if (collectionNames.Contains(CollectionModel.CollectionName) == false)
	{
		var collectionsCollection = db.GetCollection<CollectionModel>(CollectionModel.CollectionName);
		await collectionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<CollectionModel>(Builders<CollectionModel>.IndexKeys.Ascending(x => x.Slug), new CreateIndexOptions { Unique = true }));
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
