FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SearchAPI/*.csproj SearchAPI/
COPY Shared/*.csproj Shared/
RUN dotnet restore SearchAPI/SearchAPI.csproj

COPY . .
WORKDIR /src/SearchAPI
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./
COPY SearchData/SearchDB-1.db /data/SearchDB-1.db

COPY SearchAPI/NLog.config ./

ENV ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production \
    INSTANCE=Unknown \
    SQLITE_DB=/data/SearchDB-1.db

EXPOSE 80

ENTRYPOINT ["dotnet", "SearchAPI.dll"]