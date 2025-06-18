# ---------- client build ----------
FROM node:20-alpine AS client
WORKDIR /app/AccessLens
COPY AccessLens/package*.json ./
RUN npm ci
COPY AccessLens .
RUN npm run build --prod

# ---------- API build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# ▸ Copy & restore server first (better layer‑cache)
COPY AccessLensCli.sln ./
COPY AccessLensApi/AccessLensApi.csproj ./AccessLensApi/
RUN dotnet restore ./AccessLensApi/AccessLensApi.csproj

# ▸ Copy server sources & publish
COPY AccessLensApi ./AccessLensApi
RUN dotnet publish ./AccessLensApi/AccessLensApi.csproj -c Release -o /publish

# ---------- Node + Angular build -------------------------------------------
# Install Node 20 only once so future builds are cached.
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get update -qq \
    && apt-get install -y --no-install-recommends nodejs curl ca-certificates \
    # ▸ Install MinIO binary (server + mc in one file) \
    && curl -sL https://dl.min.io/server/minio/release/linux-amd64/minio -o /usr/local/bin/minio \
    && chmod +x /usr/local/bin/minio \
    && rm -rf /var/lib/apt/lists/*


# ▸ Copy compiled SPA into wwwroot of the publish folder
RUN mkdir -p /publish/wwwroot
COPY --from=client /app/AccessLens/dist /publish/wwwroot

# ▸ Pre‑install Playwright browser binaries (skip at runtime)
RUN dotnet tool install --tool-path /usr/local/bin Microsoft.Playwright.CLI \
    && playwright install --with-deps chromium

# ---------- Runtime stage ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS="http://+:8080" \
    PLAYWRIGHT_BROWSERS_PATH="/ms-playwright" \
    DOTNET_RUNNING_IN_CONTAINER="true"

# ▸ Create non‑root user
RUN id -u app 2>/dev/null || (groupadd -g 10001 app \
    && useradd -u 10000 -g 10001 -s /usr/sbin/nologin -d /app app)

# ▸ Bring in published output
COPY --from=build /publish .
RUN chown -R app:app /app

# ▸ Bring MinIO binary across from build stage
COPY --from=build /usr/local/bin/minio /usr/local/bin/minio
EXPOSE 8080 9000 9001

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

USER app

CMD ["/entrypoint.sh"]