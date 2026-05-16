# === Stage 1: Build ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MediaDownloaderMVP/MediaDownloaderTgBotMVP.csproj", "MediaDownloaderMVP/"]
RUN dotnet restore "MediaDownloaderMVP/MediaDownloaderTgBotMVP.csproj"
COPY . .
WORKDIR "/src/MediaDownloaderMVP"
RUN dotnet publish "MediaDownloaderTgBotMVP.csproj" -c Release -o /app/publish /p:UseAppHost=false

# === Stage 2: Runtime ===
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y ffmpeg python3 python3-pip curl \
    && rm -rf /var/lib/apt/lists/*
RUN pip install curl_cffi --break-system-packages
RUN ARCH=$(uname -m) && \
    if [ "$ARCH" = "x86_64" ]; then \
        URL="https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp"; \
    elif [ "$ARCH" = "aarch64" ] || [ "$ARCH" = "arm64" ]; then \
        URL="https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux_arm64"; \
    fi && \
    curl -L "$URL" -o /usr/local/bin/yt-dlp && \
    chmod a+rx /usr/local/bin/yt-dlp
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MediaDownloaderTgBotMVP.dll"]
