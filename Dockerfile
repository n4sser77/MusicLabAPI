# -------------------------
# Build stage
# -------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files (paths relative to Dockerfile)
COPY ["HttpServer/", "MusiclabApi"]
COPY ["Shared/Shared.csproj", "Shared/"]

# Restore NuGet packages
RUN dotnet restore "MusiclabApi/MusiclabApi.csproj"

# Copy the rest of the code
COPY . .

# Build and publish
RUN dotnet build "MusiclabApi/MusiclabApi.csproj" -c Release -o /app/build

RUN dotnet publish "MusiclabApi/MusiclabApi.csproj" \
    -c Release \
    -o /app/publish \
    /p:PublishSingleFile=true \
    /p:RuntimeIdentifier=linux-x64 \
    /p:SelfContained=true

# -------------------------
# Runtime stage
# -------------------------
FROM ubuntu:24.04 AS runtime
WORKDIR /app

# Install ICU and SQLite
RUN apt-get update && \
    apt-get install -y libicu-dev sqlite3 && \
    rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["./MusiclabApi"]
