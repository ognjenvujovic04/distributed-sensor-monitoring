FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY distributed-sensor-monitoring.sln .
COPY src/SensorMonitoring.Contracts/SensorMonitoring.Contracts.csproj src/SensorMonitoring.Contracts/
COPY src/NotificationService/NotificationService.csproj src/NotificationService/

RUN dotnet restore src/NotificationService/NotificationService.csproj

COPY src/ src/
RUN dotnet publish src/NotificationService/NotificationService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationService.dll"]
