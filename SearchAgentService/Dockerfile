FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shared/Shared.csproj Shared/
COPY SearchAgentService/SearchAgentService.csproj SearchAgentService/
RUN dotnet restore SearchAgentService/SearchAgentService.csproj

COPY Shared/ Shared/
COPY SearchAgentService/ SearchAgentService/
RUN dotnet publish SearchAgentService/SearchAgentService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SearchAgentService.dll"]