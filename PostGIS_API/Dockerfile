FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /PostGIS_API
COPY ./PostGIS_API.csproj .
RUN dotnet restore PostGIS_API.csproj
COPY . .
RUN dotnet publish PostGIS_API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PostGIS_API.dll"]
