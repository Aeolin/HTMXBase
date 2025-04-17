using AutoMapper;
using AutoMapper.Internal;
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
using HTMXBase.Api.Models;
using HTMXBase.Authorization;
using HTMXBase.Database;
using HTMXBase.Database.InterceptingShim;
using HTMXBase.Database.Interceptors;
using HTMXBase.Database.Models;
using HTMXBase.Database.Session;
using HTMXBase.Middleware;
using HTMXBase.OutputFormatters;
using HTMXBase.Serializers;
using HTMXBase.Services.FileStorage;
using HTMXBase.Services.JWTAuth;
using HTMXBase.Services.ModelEvents;
using HTMXBase.Services.ObjectCache;
using HTMXBase.Services.Pagination;
using HTMXBase.Services.TemplateRouter;
using HTMXBase.Services.TemplateStore;
using HTMXBase.Utils;
using HTMXBase.Utils.StartupTasks;
using System.Collections.Frozen;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AutoMapper.Internal;

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
	x.JsonSerializerOptions.Converters.Add(new BsonDocumentConverter());
	x.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.Configure<FlatFileStorageConfig>(config.GetSection("FlatFileStorage"));
builder.Services.AddSingleton<IFileStorage, FlatFileStorage>();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IMongoClient>(x => new MongoClient(x.GetRequiredService<IConfiguration>().GetConnectionString("MongoDB")));
builder.Services.AddScoped<IMongoDatabase>(x =>
{
	var client = x.GetRequiredService<IMongoClient>();
	var config = x.GetRequiredService<IConfiguration>();
	var url = new MongoUrl(config.GetConnectionString("MongoDB"));
	return new InterceptingDatabaseShim(client.GetDatabase(url.DatabaseName), x);
});

builder.Services.AddScoped<IPaginationService<BsonDocument>, PaginationService<BsonDocument>>();
builder.Services.AddScoped<IPaginationService<UserModel>, PaginationService<UserModel>>();
builder.Services.AddScoped<IPaginationService<GroupModel>, PaginationService<GroupModel>>();
builder.Services.AddScoped<IPaginationService<CollectionModel>, PaginationService<CollectionModel>>();
builder.Services.AddModelEventChannel<ModelData<RouteTemplateModel>>();
builder.Services.AddModelEventChannel<TemplateData>();
builder.Services.AddEntityUpdateInterceptors();
builder.Services.Configure<InMemoryCacheConfig>(x =>
{
	x.MaxRetentionTime = TimeSpan.FromMinutes(5);
	x.RefreshOnRead = true;
});

builder.Services.AddSingleton<IInMemoryCache<string, HandlebarsTemplate<object, object>>, InMemoryCache<string, HandlebarsTemplate<object, object>>>();
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddSingleton<HtmxTemplateStore>();
builder.Services.AddSingleton<IHtmxTemplateStore>(x => x.GetRequiredService<HtmxTemplateStore>());
builder.Services.AddSingleton<IHostedService>(x => x.GetRequiredService<HtmxTemplateStore>());

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
	opts.AllowNullDestinationValues = true;
	opts.AddMaps(Assembly.GetExecutingAssembly());
	opts.AllowNullCollections = true;
	opts.CreateMap<CollectionModel, ApiCollection>(MemberList.Destination);
});


builder.Services.UseAsyncSeeding(Seeding.CreateCollectionsAsync);
builder.Services.UseAsyncSeeding(Seeding.UpdatePermissionsAsync);
builder.Services.AddSingleton<InMemoryTemplateRouter>();
builder.Services.AddSingleton<ITemplateRouter>(x => x.GetRequiredService<InMemoryTemplateRouter>());
builder.Services.AddSingleton<IHostedService>(x => x.GetRequiredService<InMemoryTemplateRouter>());
builder.Services.AddSingleton<RedirectHandlingMiddleware>();
builder.Services.AddOpenApiDocument();
builder.Services.AddHttpLogging();
var app = builder.Build();

BsonSerializer.RegisterSerializer(new JsonDocumentSerializer(BsonType.String));


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseHttpLogging();
	app.UseOpenApi();
	app.UseReDoc();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseMiddleware<RedirectHandlingMiddleware>();
app.Run();
