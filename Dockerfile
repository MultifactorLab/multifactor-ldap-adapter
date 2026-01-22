# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY src/*.csproj ./src/
RUN dotnet restore

# copy everything else and build app
COPY src/. ./src/
WORKDIR /source/src
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:3.1
ENV ASPNETCORE_ENVIRONMENT=Production
WORKDIR /app
RUN apt-get update && \
     apt-get install -y apt-transport-https

RUN  useradd -r mfa  \
    &&  chown -R mfa: /app \
    &&  chmod -R 700 /app

COPY --from=build /app ./
USER mfa
ENTRYPOINT ["dotnet", "multifactor-ldap-adapter.dll"]


