FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["KVS.Api/KVS.Api.csproj", "KVS.Api/"]
RUN dotnet restore "KVS.Api/KVS.Api.csproj"
COPY . .
WORKDIR "/src/KVS.Api"
RUN dotnet build "./KVS.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./KVS.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KVS.Api.dll", "--host", "0.0.0.0", "--port", "8080"]
