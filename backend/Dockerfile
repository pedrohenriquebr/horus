# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./*.sln .
COPY ./Api/RootBootstrapper/*.csproj ./Api/RootBootstrapper/
COPY ./Shared/LuzInga.Contracts/*.csproj ./Shared/LuzInga.Contracts/
COPY ./Shared/Horus.Modules.Shared.Infra/*.csproj ./Shared/Horus.Modules.Shared.Infra/
COPY ./Horus.Modules.Core.Contract/*.csproj ./Horus.Modules.Core.Contract/
COPY ./Horus.Modules.DataAnalytics.Contract/*.csproj ./Horus.Modules.DataAnalytics.Contract/
COPY ./LuzInga.Api/*.csproj ./LuzInga.Api/
COPY ./LuzInga.Application/*.csproj ./LuzInga.Application/
COPY ./LuzInga.Domain/*.csproj ./LuzInga.Domain/
COPY ./LuzInga.Infra/*.csproj ./LuzInga.Infra/
COPY ./LuzInga.IntegrationTests/*.csproj ./LuzInga.IntegrationTests/
COPY ./LuzInga.Playground/*.csproj ./LuzInga.Playground/
COPY ./LuzInga.Playground2/*.csproj ./LuzInga.Playground2/
COPY ./LuzInga.UnitTests/*.csproj ./LuzInga.UnitTests/
RUN dotnet restore

# Copy everything else and build
COPY ./Api/RootBootstrapper/. ./Api/RootBootstrapper/
COPY ./Shared/LuzInga.Contracts/. ./Shared/LuzInga.Contracts/
COPY ./Shared/Horus.Modules.Shared.Infra/. ./Shared/Horus.Modules.Shared.Infra/
COPY ./Horus.Modules.Core.Contract/. ./Horus.Modules.Core.Contract/
COPY ./Horus.Modules.DataAnalytics.Contract/. ./Horus.Modules.DataAnalytics.Contract/
COPY ./LuzInga.Api/. ./LuzInga.Api/
COPY ./LuzInga.Application/. ./LuzInga.Application/
COPY ./LuzInga.Domain/. ./LuzInga.Domain/
COPY ./LuzInga.Infra/. ./LuzInga.Infra/
COPY ./LuzInga.IntegrationTests/. ./LuzInga.IntegrationTests/
COPY ./LuzInga.Playground/. ./LuzInga.Playground/
COPY ./LuzInga.Playground2/. ./LuzInga.Playground2/
COPY ./LuzInga.UnitTests/. ./LuzInga.UnitTests/

WORKDIR /app/Api/RootBootstrapper
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/Api/RootBootstrapper/out ./
ENTRYPOINT ["dotnet", "Horus.RootBootstrapper.dll"]
