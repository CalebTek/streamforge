FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/StreamForge.Api/StreamForge.Api.csproj",                         "src/StreamForge.Api/"]
COPY ["src/StreamForge.Core/StreamForge.Core.csproj",                       "src/StreamForge.Core/"]
COPY ["src/StreamForge.Infrastructure/StreamForge.Infrastructure.csproj",   "src/StreamForge.Infrastructure/"]
RUN dotnet restore "src/StreamForge.Api/StreamForge.Api.csproj"
COPY . .
WORKDIR "/src/src/StreamForge.Api"
RUN dotnet publish "StreamForge.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StreamForge.Api.dll"]
