# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копіюємо тільки файли для API
COPY *.sln .
COPY Flickoo.Api/*.csproj ./Flickoo.Api/
COPY Flickoo.Telegram/*.csproj ./Flickoo.Telegram/

RUN dotnet restore

# Копіюємо решту файлів і будуємо
COPY . .
WORKDIR /app/Flickoo.Api
RUN dotnet publish -c Release -o /out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

ENTRYPOINT ["dotnet", "Flickoo.Api.dll"]
