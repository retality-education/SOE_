FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

# Установка curl для healthcheck
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SOE/SOE.csproj", "SOE/"]  # Обратите внимание на путь
RUN dotnet restore "SOE/SOE.csproj"
COPY . .
RUN dotnet publish "SOE/SOE.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SOE.dll"]  # Должно совпадать с именем проекта