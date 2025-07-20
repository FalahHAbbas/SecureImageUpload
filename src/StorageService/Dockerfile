
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY src/StorageService/*.csproj .
RUN dotnet restore

COPY src/StorageService/. .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENV JAEGER_AGENT_HOST=jaeger
ENV JAEGER_AGENT_PORT=6831
ENTRYPOINT ["dotnet", "StorageService.dll"]
