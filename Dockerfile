# Use official .NET SDK for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution files
COPY AccessLensCli.sln ./
COPY AccessLensApi/AccessLensApi.csproj AccessLensApi/

# Restore packages
RUN dotnet restore AccessLensApi/AccessLensApi.csproj

# Copy everything
COPY . ./

# Build and publish
WORKDIR /src/AccessLensApi
RUN dotnet publish AccessLensApi.csproj -c Release -o /app/publish

# Build Angular frontend and copy to wwwroot
WORKDIR /src/AccessLens
RUN npm ci && npm run build
RUN mkdir -p /app/publish/wwwroot && cp -r dist/access-lens/* /app/publish/wwwroot/

RUN curl -sL https://dl.min.io/server/minio/release/linux-amd64/minio \
    -o /usr/local/bin/minio && \
    chmod +x /usr/local/bin/minio

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080


COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AccessLensApi.dll"]
