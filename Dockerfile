# === Стейдж 1: Сборка приложения ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем .csproj из подпапки MediaDownloaderMVP во внутреннюю папку контейнера
COPY ["MediaDownloaderMVP/MediaDownloaderTgBotMVP.csproj", "MediaDownloaderMVP/"]
RUN dotnet restore "MediaDownloaderMVP/MediaDownloaderTgBotMVP.csproj"

# Копируем весь остальной код решения
COPY . .

# Переходим в папку проекта и публикуем его
WORKDIR "/src/MediaDownloaderMVP"
RUN dotnet publish "MediaDownloaderTgBotMVP.csproj" -c Release -o /app/publish /p:UseAppHost=false

# === Стейдж 2: Финальный рантайм-образ ===
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Устанавливаем зависимости для yt-dlp
RUN apt-get update && apt-get install -y \
    ffmpeg \
    python3 \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Скачиваем свежий yt-dlp
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp \
    && chmod a+rx /usr/local/bin/yt-dlp

# Копируем скомпилированные файлы бота
COPY --from=build /app/publish .

# Запуск бота (имя выходной dll совпадает с именем csproj)
ENTRYPOINT ["dotnet", "MediaDownloaderTgBotMVP.dll"]# === Стейдж 1: Сборка приложения ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем .csproj из подпапки MediaDownloaderMVP во внутреннюю папку контейнера
COPY ["MediaDownloaderMVP/MediaDownloaderTgBotMVP.csproj", "MediaDownloaderMVP/"]
RUN dotnet restore "MediaDownloaderMVP/MediaDownloaderTgBotMVP.csproj"

# Копируем весь остальной код решения
COPY . .

# Переходим в папку проекта и публикуем его
WORKDIR "/src/MediaDownloaderMVP"
RUN dotnet publish "MediaDownloaderTgBotMVP.csproj" -c Release -o /app/publish /p:UseAppHost=false

# === Стейдж 2: Финальный рантайм-образ ===
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Устанавливаем зависимости для yt-dlp
RUN apt-get update && apt-get install -y \
    ffmpeg \
    python3 \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Скачиваем свежий yt-dlp
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp \
    && chmod a+rx /usr/local/bin/yt-dlp

# Копируем скомпилированные файлы бота
COPY --from=build /app/publish .

# Запуск бота (имя выходной dll совпадает с именем csproj)
ENTRYPOINT ["dotnet", "MediaDownloaderTgBotMVP.dll"]