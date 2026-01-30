# Usar imagen base de .NET 8
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copiar archivos csproj y restaurar como una capa distinta
COPY ["ControlInventario.csproj", "."]
RUN dotnet restore "./ControlInventario.csproj"
# Copiar el resto de los archivos
COPY . .
WORKDIR "/src/."
RUN dotnet build "ControlInventario.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ControlInventario.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Variables de entorno para PostgreSQL
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Iniciar la aplicación
ENTRYPOINT ["dotnet", "ControlInventario.dll"]
