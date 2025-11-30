FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Копируем только файлы проектов/решения для лучшего кэширования restore
COPY Backends/Backends.sln Backends/
COPY Backends/Employees.Api/Employees.Api.csproj Backends/Employees.Api/
COPY Backends/Projects.Api/Projects.Api.csproj Backends/Projects.Api/
COPY Backends/Shared/Shared.csproj Backends/Shared/
RUN dotnet restore Backends/Employees.Api/Employees.Api.csproj

# Теперь копируем весь исходный код
COPY Backends/ Backends/
WORKDIR /src/Backends/Employees.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Dev stage с Hot Reload (dotnet watch)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev
WORKDIR /src
# Копируем только файлы проектов/решения для лучшего кэширования restore
COPY Backends/Backends.sln Backends/
COPY Backends/Employees.Api/Employees.Api.csproj Backends/Employees.Api/
COPY Backends/Projects.Api/Projects.Api.csproj Backends/Projects.Api/
COPY Backends/Shared/Shared.csproj Backends/Shared/
RUN dotnet restore Backends/Employees.Api/Employees.Api.csproj
# Копируем исходники (будут перекрыты volume в compose для Hot Reload)
COPY Backends/ Backends/
WORKDIR /src/Backends/Employees.Api
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1
ENTRYPOINT ["dotnet", "watch", "run", "--non-interactive", "--urls", "http://0.0.0.0:8080"]

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "Employees.Api.dll"]


