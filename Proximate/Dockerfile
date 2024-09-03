FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
COPY --from=build /app/publish .
COPY public ./public
COPY config ./config
EXPOSE 5050
CMD ["dotnet", "Proximate.dll"]