FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Install ffmpeg and ffprobe
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/StreamForge.Worker/StreamForge.Worker.csproj",                   "src/StreamForge.Worker/"]
COPY ["src/StreamForge.Core/StreamForge.Core.csproj",                       "src/StreamForge.Core/"]
COPY ["src/StreamForge.Infrastructure/StreamForge.Infrastructure.csproj",   "src/StreamForge.Infrastructure/"]
RUN dotnet restore "src/StreamForge.Worker/StreamForge.Worker.csproj"
COPY . .
WORKDIR "/src/src/StreamForge.Worker"
RUN dotnet publish "StreamForge.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StreamForge.Worker.dll"]
