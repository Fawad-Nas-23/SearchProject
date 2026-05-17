FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shared/Shared.csproj Shared/
COPY WebAppSearch/WebAppSearch.csproj WebAppSearch/
RUN dotnet restore WebAppSearch/WebAppSearch.csproj

COPY Shared/ Shared/
COPY WebAppSearch/ WebAppSearch/
RUN dotnet publish WebAppSearch/WebAppSearch.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebAppSearch.dll"]