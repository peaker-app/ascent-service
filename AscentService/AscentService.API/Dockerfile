# Contexto de build: la raíz del monorepo (peaker-app), para incluir common-library.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY common-library/ common-library/
COPY ascent-service/ ascent-service/

RUN dotnet restore ascent-service/AscentService/AscentService.API/AscentService.API.csproj
RUN dotnet publish ascent-service/AscentService/AscentService.API/AscentService.API.csproj \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM build AS migrator-build
ARG BUILD_CONFIGURATION=Release
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 10.0.10
RUN dotnet ef migrations bundle --self-contained --target-runtime linux-x64 \
    --configuration $BUILD_CONFIGURATION \
    --project ascent-service/AscentService/AscentService.Infrastructure/AscentService.Infrastructure.csproj \
    --startup-project ascent-service/AscentService/AscentService.API/AscentService.API.csproj \
    --output /app/efbundle

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrator
WORKDIR /app
COPY --from=migrator-build /app/efbundle .
USER $APP_UID
ENTRYPOINT ["/bin/sh", "-c", "./efbundle --connection \"$ConnectionStrings__AscentDatabase\""]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "AscentService.API.dll"]
