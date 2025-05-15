#FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
#WORKDIR /src
#
#COPY ./src/*.sln ./
#COPY ./src/DBQuery/*.csproj ./DBQuery/
#RUN dotnet restore ./DBQuery/DBQuery.csproj
#
#COPY ./src/ ./
#
#WORKDIR /src/DBQuery
#RUN dotnet publish -c Release -o /app/publish
#
## ---- Runtime Stage ----
#FROM mcr.microsoft.com/dotnet/aspnet:9.0
#WORKDIR /app
#
## Copy published output
#COPY --from=build /app/publish .
#
#ENTRYPOINT ["dotnet", "DockerDemo.Api.dll"]
#