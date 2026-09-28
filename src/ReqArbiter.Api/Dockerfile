# Build context: the src/ folder (see docker-compose.yml).

# Stage 1: compile and publish the API with the full .NET SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, copying only the project files, so Docker can cache the
# NuGet packages while the code changes.
COPY ReqArbiter.Api/ReqArbiter.Api.csproj ReqArbiter.Api/
COPY ReqArbiter.Core/ReqArbiter.Core.csproj ReqArbiter.Core/
RUN dotnet restore ReqArbiter.Api/ReqArbiter.Api.csproj

COPY . .
RUN dotnet publish ReqArbiter.Api/ReqArbiter.Api.csproj -c Release -o /app --no-restore

# Stage 2: run it on the smaller ASP.NET runtime image (no SDK inside).
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# The .NET 10 images listen on port 8080 and run as a non-root user by default.
EXPOSE 8080
ENTRYPOINT ["dotnet", "ReqArbiter.Api.dll"]
