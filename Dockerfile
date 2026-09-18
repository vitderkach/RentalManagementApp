# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first to leverage Docker layer caching for restore
COPY RentalManagementApp.sln ./
COPY RentalManagementApp/RentalManagementApp.csproj RentalManagementApp/
COPY RentalManagementApp.Tests/RentalManagementApp.Tests.csproj RentalManagementApp.Tests/
RUN dotnet restore RentalManagementApp/RentalManagementApp.csproj

# Copy the rest of the source and publish
COPY . .
WORKDIR /src/RentalManagementApp
RUN dotnet publish RentalManagementApp.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RentalManagementApp.dll"]
