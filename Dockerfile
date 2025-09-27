# Use the .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/YoutubeDownloader.WebApi/YoutubeDownloader.WebApi.csproj", "YoutubeDownloader.WebApi/"]
COPY ["src/YoutubeDownloader.Infrastructure/YoutubeDownloader.Infrastructure.csproj", "YoutubeDownloader.Infrastructure/"]
COPY ["src/YoutubeDownloader.Core/YoutubeDownloader.Core.csproj", "YoutubeDownloader.Core/"]

RUN dotnet restore "YoutubeDownloader.WebApi/YoutubeDownloader.WebApi.csproj"

# Copy all source code
COPY src/ .

# Build the application
RUN dotnet build "YoutubeDownloader.WebApi/YoutubeDownloader.WebApi.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "YoutubeDownloader.WebApi/YoutubeDownloader.WebApi.csproj" -c Release -o /app/publish

# Use the ASP.NET runtime for the final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install FFmpeg for video processing and curl for healthchecks
RUN apt-get update && \
    apt-get install -y ffmpeg curl && \
    rm -rf /var/lib/apt/lists/*

# Create directories for downloads and logs
RUN mkdir -p /app/downloads /app/logs && \
    chmod 755 /app/downloads /app/logs

# Copy the published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "YoutubeDownloader.WebApi.dll"]
