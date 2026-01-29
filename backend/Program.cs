using Microsoft.EntityFrameworkCore;
using backend.Database;
using backend.Domains.Users;
using backend.Domains.Zoom;
using backend.Domains.Availabilities;
using backend.Domains.Chat;
using backend.Domains.Courses.Services;
using backend.Domains.Payments.Services;
using backend.Domains.Ratings.Services;
using backend.Domains.Documents;
using backend.Helpers;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Validate all required environment variables at startup
EnvironmentValidator.ValidateRequiredVariables();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Swagger services
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(doc => {
        var scheme = new OpenApiSecuritySchemeReference("Bearer", doc);
        var requirement = new OpenApiSecurityRequirement();
        requirement.Add(scheme, new List<string>());
        return requirement;
    });
});

// Authentication services
var jwtSecret = Environment.GetEnvironmentVariable("JWT__SECRET")!;
var backendUrl = Environment.GetEnvironmentVariable("BACKEND__URL")!;
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND__URL")!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = backendUrl,
            ValidAudience = frontendUrl,
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization services
builder.Services.AddAuthorization();

// CORS configuration
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins(frontendUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Database services
var defaultCo = Environment.GetEnvironmentVariable("POSTGRES__CONNECTION_STRING")!;
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultCo)
);

// Application services
builder.Services.AddScoped<UserAuthService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<UserManagementService>();

// Zoom services
builder.Services.AddHttpClient<ZoomService>();
builder.Services.AddHttpClient<ZoomTokenProvider>();
builder.Services.AddScoped<ZoomSignatureService>();

// Availability services
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddScoped<AvailabilityQueryService>();
builder.Services.AddScoped<UnavailableSlotService>();

// Chat services
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<ChatAttachmentService>();

// Course services
builder.Services.AddScoped<ICourseService, CourseService>();

// Payment services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();

// Rating services
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// Document services
builder.Services.AddScoped<DocumentService>();

// SignalR for real-time chat
builder.Services.AddSignalR();

// Register FluentMigrator services
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(defaultCo)
        .ScanIn(typeof(backend.Database.Migrations.InitialDbSetup).Assembly).For.All()
    )
    .AddLogging(lb => lb.AddFluentMigratorConsole().SetMinimumLevel(LogLevel.Warning));

var app = builder.Build();

// Apply database migrations at startup
using (var scope = app.Services.CreateScope()) {
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

    if (args.Contains("--reset-migrations") || args.Contains("--reset-database")) {
        Console.WriteLine("🔄 Resetting database...");
        runner.MigrateDown(0);
        runner.MigrateUp();
        Console.WriteLine("✓ Database reset complete!\n");
        
        // Seed database after reset
        var shouldPopulate = args.Contains("--populate");
        Console.WriteLine($"🌱 Seeding database (populate={shouldPopulate})...");
        try {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Disable EF Core change tracking during seeding
            dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            
            await DatabaseSeeder.SeedAsync(dbContext, shouldPopulate);
            
            dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
            
            Console.WriteLine("✓ Seeding complete!\n");
        } catch (Exception ex) {
            Console.WriteLine($"⚠ Seeding failed: {ex.Message}");
        }
    } else {
        runner.MigrateUp();
        Console.WriteLine("✓ Migrations applied\n");
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

// SignalR hubs
app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();

app.Run();
