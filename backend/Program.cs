using backend.Database;
using backend.Domains.Users;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MongoDB settings from environment or configuration. If a connection string
// isn't provided but root credentials are present, build an authenticated
// connection string using host/port and authSource=admin.
var mongoConnection = Environment.GetEnvironmentVariable("MONGODB__CONNECTION_STRING") ?? builder.Configuration.GetValue<string>("MongoDb:ConnectionString");
var mongoDbName = Environment.GetEnvironmentVariable("MONGODB__DATABASE") ?? builder.Configuration.GetValue<string>("MongoDb:DatabaseName");

if (string.IsNullOrWhiteSpace(mongoConnection)) {
    var host = Environment.GetEnvironmentVariable("MONGODB__HOST") ?? builder.Configuration.GetValue<string>("MongoDb:Host") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("MONGODB__PORT") ?? builder.Configuration.GetValue<string>("MongoDb:Port") ?? "27017";
    var rootUser = Environment.GetEnvironmentVariable("MONGODB__ROOT_USER");
    var rootPassword = Environment.GetEnvironmentVariable("MONGODB__ROOT_PASSWORD");

    if (!string.IsNullOrWhiteSpace(rootUser) && !string.IsNullOrWhiteSpace(rootPassword)) {
        var userEsc = System.Uri.EscapeDataString(rootUser);
        var pwdEsc = System.Uri.EscapeDataString(rootPassword);
        mongoConnection = $"mongodb://{userEsc}:{pwdEsc}@{host}:{port}/?authSource=admin";
    } else {
        mongoConnection = $"mongodb://{host}:{port}";
    }
}

var mongoSettings = new MongoDbSettings { ConnectionString = mongoConnection, DatabaseName = mongoDbName ?? "apprendsmoi" };
builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<MongoDbContext>();

// Application services
builder.Services.AddScoped<UserHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
