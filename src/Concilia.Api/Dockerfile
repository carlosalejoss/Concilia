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
# ONNX Runtime (embeddings) brings its native library for every platform
# (Android, iOS, macOS, Windows, Linux: about 240 MB). The image is Linux, so
# only the Linux ones are kept, for x64 and arm64.
RUN find /app/runtimes -mindepth 1 -maxdepth 1 ! -name 'linux-*' -exec rm -rf {} +

# The embeddings model (milestone 17): paraphrase-multilingual-MiniLM-L12-v2
# (Apache 2.0), exported to ONNX in fp32 by sentence-transformers. It is
# downloaded while building the image, at a fixed Hugging Face commit and with
# the SHA-256 of each file checked (the same as
# scripts/descargar-modelo-embeddings.ps1), so the running API never connects
# anywhere to compute an embedding. In a stage of its own because a remote
# ADD leaves the files readable only by root: the chmod lets the unprivileged
# "app" user read them, without a second copy of the 470 MB in the final image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS models
ARG EMBEDDINGS_MODEL_URL=https://huggingface.co/sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2/resolve/e8f8c211226b894fcb81acc59f3b34ba3efd5f42
ADD --checksum=sha256:10f7a088420252b26caf819236ca2c9d2987afd0fc06fec7553b542a5655a05a \
    ${EMBEDDINGS_MODEL_URL}/onnx/model.onnx /models/paraphrase-multilingual-MiniLM-L12-v2/model.onnx
ADD --checksum=sha256:cfc8146abe2a0488e9e2a0c56de7952f7c11ab059eca145a0a727afce0db2865 \
    ${EMBEDDINGS_MODEL_URL}/sentencepiece.bpe.model /models/paraphrase-multilingual-MiniLM-L12-v2/sentencepiece.bpe.model
RUN chmod -R a+rX /models

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

# The embeddings model, before the application files, so Docker keeps this
# 470 MB layer while the code changes.
COPY --from=models /models /opt/models

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
