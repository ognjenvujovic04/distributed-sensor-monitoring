FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY distributed-sensor-monitoring.sln .
COPY src/SensorMonitoring.Contracts/SensorMonitoring.Contracts.csproj src/SensorMonitoring.Contracts/
COPY src/SensorMonitoring.Data/SensorMonitoring.Data.csproj src/SensorMonitoring.Data/
COPY src/ConsensusService/ConsensusService.csproj src/ConsensusService/

RUN dotnet restore src/ConsensusService/ConsensusService.csproj

COPY src/ src/
RUN dotnet publish src/ConsensusService/ConsensusService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConsensusService.dll"]
