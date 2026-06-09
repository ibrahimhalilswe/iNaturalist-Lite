# Use the official .NET 10 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["iNaturalist-Lite.csproj", "./"]
RUN dotnet restore "./iNaturalist-Lite.csproj"

# Copy everything else and build the app
COPY . .
RUN dotnet publish "iNaturalist-Lite.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render assigns a dynamic port via the PORT environment variable.
# ASP.NET Core 8+ defaults to port 8080. We can map the urls variable to pick up PORT.
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

# Expose the default port (Render will override if needed)
EXPOSE 8080

ENTRYPOINT ["dotnet", "iNaturalist-Lite.dll"]
