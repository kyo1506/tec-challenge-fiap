FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY TecChallenge.sln .
COPY src ./src

RUN dotnet restore TecChallenge.sln

RUN dotnet publish src/TecChallenge.Application/TecChallenge.Application.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN chown -R 0:0 /app && \
    chmod -R g+w /app

# COPY src/TecChallenge.Application/appsettings.json /app/

EXPOSE 5000
ENTRYPOINT ["dotnet", "TecChallenge.Application.dll"]