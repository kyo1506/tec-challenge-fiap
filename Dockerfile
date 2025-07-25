ARG DOTNET_VERSION=9.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build
WORKDIR /src

ENV HOME=/app
ENV PATH="${PATH}:${HOME}/.dotnet/tools"
ENV ASPNETCORE_URLS=http://+:5001
# This tells the .NET runtime to use globalization features.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# This installs the necessary ICU libraries that provide culture data (like pt-BR) for Alpine Linux.
RUN apk add --no-cache icu-libs

# Copy the solution file
COPY *.sln .

# Copy the source code (this creates /src/TecChallenge.Application, etc.)
COPY src/ ./src/

# Restore dependencies for the entire solution
RUN dotnet restore "TecChallenge.sln"

# Publish the application
RUN dotnet publish src/TecChallenge.Application/TecChallenge.Application.csproj -c Release -o /app/publish --no-restore

# --- Final Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS final
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:5001
ENV HOME=/app

RUN apk add --no-cache icu-libs

COPY --from=build /app/publish .

# This is good practice for security if you need it

RUN chown -R 0:0 /app && \
    chmod -R g+w /app

EXPOSE 5001

# The entrypoint should now correctly point to your application's DLL
ENTRYPOINT ["dotnet", "TecChallenge.Application.dll"]