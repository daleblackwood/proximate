# BUILD
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# RUN
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
COPY --from=build /app/publish .
COPY public ./public
COPY config ./config_default
COPY scripts/startup.sh ./startup.sh
RUN chmod +x ./startup.sh
EXPOSE 80
ENTRYPOINT ["./startup.sh"]
