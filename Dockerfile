# syntax=docker/dockerfile:1

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Kopírování projektových souborů a obnova závislostí
COPY Drevenka.slnx ./
COPY src/Drevenka.Domain/*.csproj ./src/Drevenka.Domain/
COPY src/Drevenka.Infrastructure/*.csproj ./src/Drevenka.Infrastructure/
COPY src/Drevenka.Web/*.csproj ./src/Drevenka.Web/
COPY tests/Drevenka.Domain.Tests/*.csproj ./tests/Drevenka.Domain.Tests/

RUN dotnet restore ./src/Drevenka.Web/Drevenka.Web.csproj

# Kopírování zbývajícího zdrojového kódu a kompilace
COPY src/ ./src/
RUN dotnet publish ./src/Drevenka.Web/Drevenka.Web.csproj -c Release -o /out

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Drevenka.Web.dll"]
