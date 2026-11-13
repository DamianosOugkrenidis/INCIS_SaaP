# INCIS SaaP - Docker Configuration
# Multi-stage build for optimized image size

# Stage 1: Build & Publish
# NOTE: The docker build context is the SOLUTION root (folder that contains the
# INCIS_SaaP/ project directory). See docker-compose.yml (context: ..).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore (better layer caching)
COPY INCIS_SaaP/INCIS_SaaP.csproj INCIS_SaaP/
RUN dotnet restore INCIS_SaaP/INCIS_SaaP.csproj

# Copy all source files and publish
COPY INCIS_SaaP/ INCIS_SaaP/
RUN dotnet publish INCIS_SaaP/INCIS_SaaP.csproj \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

# Stage 2: Runtime
# NOTE: .NET 9 δεν έχει "-slim" tag. Το βασικό "9.0" tag είναι Debian-based
# (περιλαμβάνει apt) ώστε να εγκαταστήσουμε fontconfig για το QuestPDF.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install timezone data, curl, and fonts (QuestPDF needs fontconfig/libfontconfig
# for PDF text rendering on Linux) and set timezone
RUN apt-get update && apt-get install -y --no-install-recommends \
        tzdata curl fontconfig libfontconfig1 fonts-dejavu-core \
    && ln -sf /usr/share/zoneinfo/Europe/Athens /etc/localtime \
    && echo "Europe/Athens" > /etc/timezone \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -m -u 1000 incisuser

# Copy published files from the build stage
COPY --from=build /app/publish .

# Set ownership
RUN chown -R incisuser:incisuser /app

# Switch to non-root user
USER incisuser

# Expose ports
EXPOSE 8080
EXPOSE 443

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    TZ=Europe/Athens

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "INCIS_SaaP.dll"]
