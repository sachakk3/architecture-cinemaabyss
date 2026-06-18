FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY EventsService.csproj ./
COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8082
ENV ASPNETCORE_URLS=http://0.0.0.0:8082

ENTRYPOINT ["dotnet", "EventsService.dll"]
