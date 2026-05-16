# Этап рантайма (базовый образ на Debian 12)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Устанавливаем зависимости под root
USER root
RUN apt-get update && apt-get install -y \
    ffmpeg \
    python3 \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Автоопределение архитектуры (x86_64 или arm64) и скачивание нужного yt-dlp
RUN ARCH=$(uname -m) && \
    if [ "$ARCH" = "x86_64" ]; then \
        URL="https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp"; \
    elif [ "$ARCH" = "aarch64" ] || [ "$ARCH" = "arm64" ]; then \
        URL="https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux_arm64"; \
    else \
        echo "Unsupported architecture: $ARCH" && exit 1; \
    fi && \
    curl -L "$URL" -o /usr/local/bin/yt-dlp && \
    chmod a+rx /usr/local/bin/yt-dlp

# Возвращаем пользователя .NET для безопасности
USER $APP_UID


# Этап сборки проекта
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем и восстанавливаем зависимости
COPY ["MediaDownloaderMVP/MediaDownloaderTgBotMVP.csproj", "MediaDownloaderMVP/"]
RUN dotnet restore "./MediaDownloaderMVP/MediaDownloaderTgBotMVP.csproj"

# Копируем весь код и собираем
COPY . .
WORKDIR "/src/MediaDownloaderMVP"
RUN dotnet build "./MediaDownloaderTgBotMVP.csproj" -c $BUILD_CONFIGURATION -o /app/build


# Этап публикации
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MediaDownloaderTgBotMVP.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


# Финальный этап рантайма
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MediaDownloaderTgBotMVP.dll"]