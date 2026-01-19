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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT__SECRET") 
                    ?? throw new InvalidOperationException("JWT__SECRET environment variable is not configured"))
            ),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization services
builder.Services.AddAuthorization();


// Database services
var defaultCo = Environment.GetEnvironmentVariable("POSTGRES__CONNECTION_STRING");
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

// Authentication middleware
app.UseAuthentication();

// Authorization middleware
app.UseAuthorization();

app.MapControllers();

// Handle 404 - Not Found
app.Use(async (context, next) => {
    await next();
    if (context.Response.StatusCode == 404 && !context.Response.HasStarted) {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new {
            error = "Not Found",
            message = $"The requested resource '{context.Request.Path}' was not found",
            statusCode = 404
        });
    }
});

app.Run();
