FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src

COPY TecChallenge.sln .
COPY src ./src

RUN dotnet restore TecChallenge.sln

RUN dotnet publish src/TecChallenge.Application/TecChallenge.Application.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY src/TecChallenge.Application/appsettings.json /app/

EXPOSE 8080
ENTRYPOINT ["dotnet", "TecChallenge.Application.dll"]