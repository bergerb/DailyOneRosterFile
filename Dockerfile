FROM node:20-alpine AS frontend-build
WORKDIR /client
COPY client/package*.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY src/DailyOneRosterFile.Api.csproj ./
COPY NuGet.Config ./
RUN --mount=type=secret,id=github_token sh -c 'dotnet nuget add source https://nuget.pkg.github.com/bergerb/index.json -n bergerb-packages -u bergerb -p "$(cat /run/secrets/github_token)" --store-password-in-clear-text --configfile NuGet.Config'
RUN dotnet restore DailyOneRosterFile.Api.csproj --configfile NuGet.Config
COPY src/ ./
RUN dotnet publish DailyOneRosterFile.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend-build /app/publish ./
COPY --from=frontend-build /client/dist ./wwwroot
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "DailyOneRosterFile.Api.dll"]
