# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
RUN mkdir -p ProfileInfo
COPY ./ProfileInfo/*.csproj ./ProfileInfo
COPY ./ProfileInfo/*.csproj .
RUN dotnet restore DynamicProfileInfo.sln

# copy everything else and build app
COPY ./ProfileInfo ./ProfileInfo 
RUN dotnet publish -c Release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProfileInfo.dll"]