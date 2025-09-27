using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YoutubeDownloader.Infrastructure.Repositories;

namespace YoutubeDownloader.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add in-memory repositories as singletons to maintain state across requests
        services.AddSingleton<IDownloadTaskRepository, DownloadTaskRepository>();
        services.AddSingleton<IVideoInfoRepository, VideoInfoRepository>();

        return services;
    }
}