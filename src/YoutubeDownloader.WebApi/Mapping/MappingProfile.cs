using AutoMapper;
using YoutubeDownloader.Infrastructure.Models;
using YoutubeDownloader.WebApi.Models;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.WebApi.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // VideoInfo mappings
        CreateMap<IVideo, VideoInfoDto>()
            .ForMember(dest => dest.VideoId, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.DownloadOptions, opt => opt.Ignore());

        CreateMap<VideoInfo, VideoInfoDto>()
            .ForMember(dest => dest.DownloadOptions, opt => opt.Ignore());

        CreateMap<VideoInfoDto, VideoInfo>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        // DownloadTask mappings
        CreateMap<DownloadTask, DownloadTaskDto>();
        CreateMap<DownloadTaskDto, DownloadTask>()
            .ForMember(dest => dest.HangfireJobId, opt => opt.Ignore());

        CreateMap<DownloadRequest, DownloadTask>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.VideoId, opt => opt.Ignore())
            .ForMember(dest => dest.VideoTitle, opt => opt.Ignore())
            .ForMember(dest => dest.Author, opt => opt.Ignore())
            .ForMember(dest => dest.Duration, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Progress, opt => opt.Ignore())
            .ForMember(dest => dest.ErrorMessage, opt => opt.Ignore())
            .ForMember(dest => dest.FileSizeBytes, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.StartedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CompletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.HangfireJobId, opt => opt.Ignore())
            .ForMember(dest => dest.OutputPath, opt => opt.MapFrom(src => src.OutputPath ?? "./downloads"));
    }
}