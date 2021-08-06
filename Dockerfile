FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["DevExchangeBot.csproj", "./"]
RUN dotnet nuget add source https://nuget.emzi0767.com/api/v3/index.json
RUN dotnet restore "DevExchangeBot.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "DevExchangeBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DevExchangeBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DevExchangeBot.dll"]
