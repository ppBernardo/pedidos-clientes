# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore numa camada própria: só é refeito quando projetos ou versões mudam.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Pedidos.Domain/Pedidos.Domain.csproj src/Pedidos.Domain/
COPY src/Pedidos.Application/Pedidos.Application.csproj src/Pedidos.Application/
COPY src/Pedidos.Infrastructure/Pedidos.Infrastructure.csproj src/Pedidos.Infrastructure/
COPY src/Pedidos.Api/Pedidos.Api.csproj src/Pedidos.Api/
RUN dotnet restore src/Pedidos.Api/Pedidos.Api.csproj

COPY src/ src/
RUN dotnet publish src/Pedidos.Api/Pedidos.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# curl só para o healthcheck do compose: a imagem aspnet não traz.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
# Usuário não root que as imagens oficiais do .NET já trazem.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Pedidos.Api.dll"]
