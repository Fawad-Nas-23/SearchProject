FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shared/Shared.csproj Shared/
COPY indexer/indexer.csproj indexer/
RUN dotnet restore indexer/indexer.csproj

COPY Shared/ Shared/
COPY indexer/ indexer/
RUN dotnet publish indexer/indexer.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "indexer.dll"]