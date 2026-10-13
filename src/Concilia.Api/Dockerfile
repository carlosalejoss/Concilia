# Build context: the repository root (see docker-compose.yml and .dockerignore),
# because the API embeds the versioned prompt files from prompts/.

# Stage 1: compile and publish the API with the full .NET SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Restore first, copying only the project files, so Docker can cache the
# NuGet packages while the code changes.
COPY src/Concilia.Api/Concilia.Api.csproj src/Concilia.Api/
COPY src/Concilia.Core/Concilia.Core.csproj src/Concilia.Core/
COPY src/Concilia.Runner/Concilia.Runner.csproj src/Concilia.Runner/
RUN dotnet restore src/Concilia.Api/Concilia.Api.csproj

# The same folder layout as the repository, so the relative paths of the
# embedded prompt files (..\..\prompts\...) still work.
COPY src/ src/
COPY prompts/ prompts/
RUN dotnet publish src/Concilia.Api/Concilia.Api.csproj -c Release -o /app --no-restore

# Source of the Node.js runtime (LTS) that Claude Code needs.
FROM node:24-bookworm-slim AS node

# Stage 2: run it on the smaller ASP.NET runtime image (no SDK inside).
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Node and npm copied from the official Node image (both images are glibc
# Linux), then Claude Code at the exact version tested by the author.
# npm installs it in /usr/local/lib/node_modules and links /usr/local/bin/claude.
COPY --from=node /usr/local/bin/node /usr/local/bin/node
COPY --from=node /usr/local/lib/node_modules/npm /usr/local/lib/node_modules/npm
RUN ln -s ../lib/node_modules/npm/bin/npm-cli.js /usr/local/bin/npm \
    && npm install -g @anthropic-ai/claude-code@2.1.283 \
    && npm cache clean --force

WORKDIR /app
COPY --from=build /app .

# The .NET images include an unprivileged "app" user (id in $APP_UID) but
# still start as root unless told otherwise. Run the API, and the claude
# processes it starts, as that user. Its HOME (/home/app) is writable; each
# run also gets its own HOME inside data/runs/{runId}/.
USER $APP_UID

# The .NET 10 images listen on port 8080.
EXPOSE 8080
ENTRYPOINT ["dotnet", "Concilia.Api.dll"]
