FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/EventForge.Web/EventForge.Web.csproj EventForge.Web/
RUN dotnet restore EventForge.Web/EventForge.Web.csproj
COPY src/EventForge.Web/ EventForge.Web/
WORKDIR /src/EventForge.Web
RUN dotnet publish EventForge.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production
RUN mkdir -p /data && chown -R $APP_UID:$APP_UID /app /data
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "EventForge.Web.dll"]
