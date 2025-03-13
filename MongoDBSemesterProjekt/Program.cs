using AutoMapper;
using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using HandlebarsDotNet.Features;
using HandlebarsDotNet.Helpers;
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
using MongoDBSemesterProjekt.Api.Models;
using MongoDBSemesterProjekt.Authorization;
using MongoDBSemesterProjekt.Database;
using MongoDBSemesterProjekt.Database.InterceptingShim;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Database.Session;
using MongoDBSemesterProjekt.OutputFormatters;
using MongoDBSemesterProjekt.Serializers;
using MongoDBSemesterProjekt.Services.FileStorage;
using MongoDBSemesterProjekt.Services.JWTAuth;
using MongoDBSemesterProjekt.Services.ObjectCache;
using MongoDBSemesterProjekt.Services.TemplateRouter;
using MongoDBSemesterProjekt.Services.TemplateStore;
using MongoDBSemesterProjekt.Utils;
using MongoDBSemesterProjekt.Utils.StartupTasks;
using System.Collections.Frozen;
using System.Reflection;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Add services to the container.

builder.Services.AddSingleton<IHandlebars>(HandlebarsEx.Create(cfg =>
{
	cfg.UseJson();
	cfg.UseCollectionMemberAliasProvider();
}));


builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<PasswordHasher<UserModel>>();

builder.Services.AddControllers(opts =>
{
	opts.OutputFormatters.Add(new HtmxOutputFormatter());

}).AddJsonOptions(x =>
{
	x.JsonSerializerOptions.Converters.Add(new ObjectIdConverter());
	x.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.Configure<FlatFileStorageConfig>(config.GetSection("FlatFileStorage"));
builder.Services.AddScoped<IFileStorage, FlatFileStorage>();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IMongoClient>(x => new MongoClient(x.GetRequiredService<IConfiguration>().GetConnectionString("MongoDB")));
builder.Services.AddScoped<IMongoDatabase>(x =>
{
	var client = x.GetRequiredService<IMongoClient>();
	var config = x.GetRequiredService<IConfiguration>();
	var url = new MongoUrl(config.GetConnectionString("MongoDB"));
	return new InterceptingDatabaseShim(client.GetDatabase(url.DatabaseName), x);
});

builder.Services.AddScoped<IMongoDatabaseSession>(x =>
{
	var client = x.GetRequiredService<IMongoClient>();
	var config = x.GetRequiredService<IConfiguration>();
	var url = new MongoUrl(config.GetConnectionString("MongoDB"));
	return new MongoDatabaseSession(client, x, url.DatabaseName);
});

builder.Services.AddEntityUpdateInterceptors();
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

builder.Services.AddAutoMapper(opts =>
{
	opts.AddMaps(Assembly.GetExecutingAssembly());
	opts.CreateMap<CollectionModel, ApiCollection>(MemberList.Destination);
});

builder.Services.UseAsyncSeeding(Seeding.CreateCollectionsAsync);
builder.Services.UseAsyncSeeding(Seeding.UpdatePermissionsAsync);
builder.Services.AddSingleton<InMemoryTemplateRouter>();
builder.Services.AddSingleton<ITemplateRouter>(x => x.GetRequiredService<InMemoryTemplateRouter>());
builder.Services.AddSingleton<IHostedService>(x => x.GetRequiredService<InMemoryTemplateRouter>());


var app = builder.Build();

BsonSerializer.RegisterSerializer(new JsonDocumentSerializer(BsonType.String));


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
