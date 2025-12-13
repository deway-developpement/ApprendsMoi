using Microsoft.EntityFrameworkCore;
using backend.Database;
using backend.Domains.Users;
using FluentMigrator.Runner;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for local dev (adjust origins as needed)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Database services
var defaultCo = Environment.GetEnvironmentVariable("POSTGRES__CONNECTION_STRING");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultCo)
);

// Application services
builder.Services.AddScoped<UserHandler>();

// Zoom services
builder.Services.AddHttpClient<backend.Domains.Zoom.ZoomService>();
builder.Services.AddScoped<backend.Domains.Zoom.ZoomService>();

// Register FluentMigrator services
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(defaultCo)
        .ScanIn(typeof(backend.Database.Migrations.InitialDbSetup).Assembly).For.All()
    )
    .AddLogging(lb => lb.AddFluentMigratorConsole());

var app = builder.Build();

// Apply database migrations at startup
using (var scope = app.Services.CreateScope()) {
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
