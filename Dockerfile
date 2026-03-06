# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY EpiAttendance.Api/EpiAttendance.Api.csproj EpiAttendance.Api/
RUN dotnet restore EpiAttendance.Api/EpiAttendance.Api.csproj
COPY EpiAttendance.Api/ EpiAttendance.Api/
RUN dotnet publish EpiAttendance.Api/EpiAttendance.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "EpiAttendance.Api.dll"]
