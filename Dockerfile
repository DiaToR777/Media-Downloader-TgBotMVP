# === Стейдж 1: Сборка ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MediaDownloaderMVP/MediaDownloaderTgBotMVP.csproj", "MediaDownloaderMVP/"]
RUN dotnet restore "MediaDownloaderMVP/MediaDownloaderTgBotMVP.csproj"
COPY . .
WORKDIR "/src/MediaDownloaderMVP"
RUN dotnet publish "MediaDownloaderTgBotMVP.csproj" -c Release -o /app/publish /p:UseAppHost=false

# === Стейдж 2: Рантайм ===
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y ffmpeg python3 curl \
    && rm -rf /var/lib/apt/lists/*
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp \
    && chmod a+rx /usr/local/bin/yt-dlp
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MediaDownloaderTgBotMVP.dll"]
