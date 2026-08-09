FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj
RUN dotnet publish samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "FoundationKit.Workbench.Api.dll"]
