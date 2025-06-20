# ─── client build ──────────────────────────────────────────────
FROM node:20-alpine AS client
WORKDIR /app/AccessLens
COPY AccessLens/package*.json ./
RUN npm ci
COPY AccessLens .
RUN npm run build --prod                       # dist/<project-name>

# ─── api build (no Node install) ───────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN apt-get update && apt-get install -y \
    libglib2.0-0 \
    libnss3 \
    libatk-bridge2.0-0 \
    libgtk-3-0 \
    libxss1 \
    libasound2 \
    libdrm2 \
    libgbm1 \
    libxshmfence1 \
    --no-install-recommends && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY AccessLensCli.sln ./
COPY AccessLensApi/AccessLensApi.csproj ./AccessLensApi/
RUN dotnet restore ./AccessLensApi/AccessLensApi.csproj
COPY AccessLensApi ./AccessLensApi
RUN dotnet publish ./AccessLensApi/AccessLensApi.csproj -c Release -o /publish

# Copy Angular bundle (wild-card catches real folder)
RUN mkdir -p /publish/wwwroot
COPY --from=client /app/AccessLens/dist/** /publish/wwwroot/

# Install Playwright binaries & MinIO
RUN dotnet tool install --tool-path /usr/local/bin Microsoft.Playwright.CLI \
    && playwright install --with-deps chromium \
    && curl -sL https://dl.min.io/server/minio/release/linux-amd64/minio -o /usr/local/bin/minio \
    && chmod +x /usr/local/bin/minio \
    && mkdir -p /ms-playwright \
    && cp -r /root/.cache/ms-playwright/* /ms-playwright

# ─── runtime ───────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright \
    DOTNET_RUNNING_IN_CONTAINER=true

RUN id -u app 2>/dev/null || (groupadd -g 10001 app \
    && useradd -u 10000 -g 10001 -s /usr/sbin/nologin -d /app app)
COPY --from=build /publish .
COPY --from=build /usr/local/bin/minio /usr/local/bin/minio
COPY --from=build /ms-playwright /ms-playwright
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh && chown -R app:app /app
EXPOSE 8080 9000
USER app
ENTRYPOINT ["/entrypoint.sh"]