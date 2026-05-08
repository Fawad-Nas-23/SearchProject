FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shared/Shared.csproj Shared/
COPY OrchestratorAPI/OrchestratorAPI.csproj OrchestratorAPI/
RUN dotnet restore OrchestratorAPI/OrchestratorAPI.csproj

COPY Shared/ Shared/
COPY OrchestratorAPI/ OrchestratorAPI/
RUN dotnet publish OrchestratorAPI/OrchestratorAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrchestratorAPI.dll"]