# ---- build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first: the project files change far less often than the sources.
COPY DeomaBlog.slnx ./
COPY src/DeomaBlog.Domain/DeomaBlog.Domain.csproj src/DeomaBlog.Domain/
COPY src/DeomaBlog.Application/DeomaBlog.Application.csproj src/DeomaBlog.Application/
COPY src/DeomaBlog.Infrastructure/DeomaBlog.Infrastructure.csproj src/DeomaBlog.Infrastructure/
COPY src/DeomaBlog.Web/DeomaBlog.Web.csproj src/DeomaBlog.Web/
COPY tests/DeomaBlog.UnitTests/DeomaBlog.UnitTests.csproj tests/DeomaBlog.UnitTests/
COPY tests/DeomaBlog.IntegrationTests/DeomaBlog.IntegrationTests.csproj tests/DeomaBlog.IntegrationTests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/DeomaBlog.Web/DeomaBlog.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is not part of the slim runtime image, and the healthcheck needs an HTTP client.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

# The SQLite file lives on a named volume so it survives container rebuilds. The directory is
# created and owned by the non-root app user, because a freshly created volume inherits the
# permissions of the image path — otherwise SQLite cannot create its file (error 14).
RUN mkdir -p /data && chown -R "$APP_UID" /data

VOLUME ["/data"]
ENV ConnectionStrings__Default="Data Source=/data/blog.db"

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "DeomaBlog.Web.dll"]
