using FluentValidation;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Serilog;
using YoutubeDownloader.Infrastructure;
using YoutubeDownloader.WebApi.Hubs;
using YoutubeDownloader.WebApi.Mapping;
using YoutubeDownloader.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/youtubedownloader-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "YouTube Downloader API", 
        Version = "v1",
        Description = "A powerful API for downloading YouTube videos with real-time progress tracking"
    });
    
    // Enable XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add SignalR
builder.Services.AddSignalR();

// Bind settings
builder.Services.Configure<YoutubeDownloader.WebApi.Models.DownloadSettings>(
    builder.Configuration.GetSection("DownloadSettings"));

// Add Hangfire with in-memory storage and cleanup options
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage(new MemoryStorageOptions
    {
        JobExpirationCheckInterval = TimeSpan.FromMinutes(10), // 检查过期作业间隔
        CountersAggregateInterval = TimeSpan.FromMinutes(10)   // 聚合计数器间隔
    }));

builder.Services.AddHangfireServer();

// Add application services
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<IDownloadService, DownloadService>();
builder.Services.AddScoped<IBackgroundDownloadService, BackgroundDownloadService>();
builder.Services.AddHostedService<FileCleanupService>();
builder.Services.AddHostedService<DataCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "YouTube Downloader API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

// No database migrations needed for in-memory storage

app.UseSerilogRequestLogging();

// Configure static files for frontend
app.UseDefaultFiles();
app.UseStaticFiles();

// Only use HTTPS redirection in development (Railway handles SSL termination)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<DownloadHub>("/downloadhub");

// Map Hangfire dashboard
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.Run();

// Simple authorization filter for Hangfire dashboard
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In production, implement proper authorization
        return true;
    }
}

// Make Program class accessible for testing
public partial class Program { }
