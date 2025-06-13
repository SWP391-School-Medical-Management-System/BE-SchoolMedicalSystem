FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5200
EXPOSE 7280

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SchoolMedicalManagementSystem.API/SchoolMedicalManagementSystem.API.csproj", "SchoolMedicalManagementSystem.API/"]
COPY ["SchoolMedicalManagementSystem.BusinessLogicLayer/SchoolMedicalManagementSystem.BusinessLogicLayer.csproj", "SchoolMedicalManagementSystem.BusinessLogicLayer/"]
COPY ["SchoolMedicalManagementSystem.DataAccessLayer/SchoolMedicalManagementSystem.DataAccessLayer.csproj", "SchoolMedicalManagementSystem.DataAccessLayer/"]
RUN dotnet restore "./SchoolMedicalManagementSystem.API/SchoolMedicalManagementSystem.API.csproj"
COPY . .
WORKDIR "/src/SchoolMedicalManagementSystem.API"
RUN dotnet build "./SchoolMedicalManagementSystem.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SchoolMedicalManagementSystem.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy Templates folder if exists (similar to IntelligentDoorManagementSystem)
COPY ["SchoolMedicalManagementSystem.API/Templates", "/app/Templates"]

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5200
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy appsettings files
COPY ["SchoolMedicalManagementSystem.API/appsettings.json", "/app/"]
COPY ["SchoolMedicalManagementSystem.API/appsettings.Production.json", "/app/"]

ENTRYPOINT ["dotnet", "SchoolMedicalManagementSystem.API.dll"]