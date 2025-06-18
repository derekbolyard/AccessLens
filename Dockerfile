# ---------- Build stage -----------------------------------------------------
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
RUN apt-get update -qq \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    # ▸ Install MinIO binary (server + mc in one file) \
    && curl -sL https://dl.min.io/server/minio/release/linux-amd64/minio -o /usr/local/bin/minio \
    && chmod +x /usr/local/bin/minio \
    && rm -rf /var/lib/apt/lists/*

# ▸ Copy package files first (cache friendly)
COPY AccessLens/package*.json ./AccessLens/
RUN npm --prefix ./AccessLens ci

# ▸ Copy Angular code & build
COPY AccessLens ./AccessLens
RUN npm --prefix ./AccessLens run build

# ▸ Copy compiled SPA into wwwroot of the publish folder
RUN mkdir -p /publish/wwwroot \
 && cp -r ./AccessLens/dist/* /publish/wwwroot/

# ▸ Pre‑install Playwright browser binaries (skip at runtime)
RUN dotnet tool restore --tool-manifest ./AccessLensApi/.config/dotnet-tools.json || true \
 && playwright install chromium --with-deps

# ---------- Runtime stage ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS="http://+:8080" \
    PLAYWRIGHT_BROWSERS_PATH="/ms-playwright" \
    DOTNET_RUNNING_IN_CONTAINER="true"

# ▸ Install MinIO binary (used by the separate `minio` Fly process)
RUN curl -sL "https://dl.min.io/server/minio/release/linux-amd64/minio" -o /usr/local/bin/minio \
 && chmod +x /usr/local/bin/minio

# ▸ Create non‑root user
RUN groupadd -g 10001 app && useradd -u 10000 -g app -s /usr/sbin/nologin -d /app app

# ▸ Bring in published output
COPY --from=build /publish .
RUN chown -R app:app /app

ENV ASPNETCORE_URLS=http://+:8080
# ▸ Bring MinIO binary across from build stage
COPY --from=build /usr/local/bin/minio /usr/local/bin/minio
EXPOSE 8080 9000 9001
USER app

CMD ["dotnet", "AccessLensApi.dll"]