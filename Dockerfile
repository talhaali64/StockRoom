FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY StockRoom.sln .
COPY src/StockRoom/StockRoom.csproj src/StockRoom/
COPY tests/StockRoom.Tests/StockRoom.Tests.csproj tests/StockRoom.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/StockRoom -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ConnectionStrings__Default="Data Source=/data/stockroom.db"
ENV ASPNETCORE_URLS=http://+:8080
VOLUME /data
EXPOSE 8080

ENTRYPOINT ["dotnet", "StockRoom.dll"]
