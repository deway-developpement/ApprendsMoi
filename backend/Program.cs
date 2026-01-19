using Microsoft.EntityFrameworkCore;
using backend.Database;
using backend.Domains.Users;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Authentication services
var jwtSecret = Environment.GetEnvironmentVariable("JWT__SECRET")
    ?? throw new InvalidOperationException("JWT__SECRET environment variable is not configured");
if (Encoding.UTF8.GetByteCount(jwtSecret) < 32) {
    throw new InvalidOperationException("JWT__SECRET must be at least 32 bytes (256 bits) long for HMAC-SHA256.");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)
            ),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT__ISSUER")
                ?? throw new InvalidOperationException("JWT__ISSUER environment variable is not configured"),
            ValidAudience = Environment.GetEnvironmentVariable("JWT__AUDIENCE")
                ?? throw new InvalidOperationException("JWT__AUDIENCE environment variable is not configured"),
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization services
builder.Services.AddAuthorization();


// CORS configuration

var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND__URL");
if (string.IsNullOrEmpty(frontendUrl)) {
    throw new InvalidOperationException("FRONTEND__URL environment variable is not configured");
}
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins(frontendUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});


// Database services
var defaultCo = Environment.GetEnvironmentVariable("POSTGRES__CONNECTION_STRING");
if (string.IsNullOrEmpty(defaultCo)) {
    throw new InvalidOperationException("POSTGRES__CONNECTION_STRING environment variable is not configured");
}
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultCo)
);

// Application services
builder.Services.AddScoped<UserHandler>();

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
    
    // Check if --reset-migrations flag is passed
    if (args.Contains("--reset-migrations")) {
        Console.WriteLine("Resetting database: rolling back all migrations...");
        runner.MigrateDown(0);
        Console.WriteLine("Reapplying all migrations...");
        runner.MigrateUp();
        Console.WriteLine("Database reset complete!");
    } else {
        runner.MigrateUp();
    }
}

// Configure the HTTP request pipeline

// Global exception handler
app.UseExceptionHandler(errorApp => {
    errorApp.Run(async context => {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (exceptionHandlerFeature != null) {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(exceptionHandlerFeature.Error, "Unhandled exception occurred");
            await context.Response.WriteAsJsonAsync(new {
                error = "An error occurred processing your request",
                message = app.Environment.IsDevelopment() ? exceptionHandlerFeature.Error.Message : null,
                stackTrace = app.Environment.IsDevelopment() ? exceptionHandlerFeature.Error.StackTrace : null
            });
        }
    });
});

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS middleware
app.UseCors("AllowFrontend");

// Authentication middleware
app.UseAuthentication();

// Authorization middleware
app.UseAuthorization();

app.MapControllers();

app.Run();
