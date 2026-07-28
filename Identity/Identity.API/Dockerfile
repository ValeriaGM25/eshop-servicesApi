# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Identity/Identity.API/Identity.API.csproj", "Identity/Identity.API/"]
COPY ["BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/"]
RUN dotnet restore "./Identity/Identity.API/Identity.API.csproj"
COPY . .
WORKDIR "/src/Identity/Identity.API"
RUN dotnet build "./Identity.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Identity.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Identity.API.dll"]
