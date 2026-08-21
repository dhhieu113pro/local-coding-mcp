# ── Build ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LocalCodingMcp.sln ./
COPY LocalCodingMcp/LocalCodingMcp.csproj LocalCodingMcp/
RUN dotnet restore LocalCodingMcp/LocalCodingMcp.csproj

COPY LocalCodingMcp/ LocalCodingMcp/
RUN dotnet publish LocalCodingMcp/LocalCodingMcp.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── Runtime ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# git = GitStatus/GitDiff/GitLog tools; curl = HEALTHCHECK
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends git curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Default workspace dir inside container (mount host projects here)
RUN mkdir -p /workspace

ENV ASPNETCORE_URLS=http://0.0.0.0:5000
ENV ASPNETCORE_ENVIRONMENT=Production
# AllowedRoots via ASP.NET env hierarchy (AllowedRoots__0, AllowedRoots__1, ...)
ENV AllowedRoots__0=/workspace

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -fsS http://127.0.0.1:5000/health || exit 1

ENTRYPOINT ["dotnet", "LocalCodingMcp.dll"]
