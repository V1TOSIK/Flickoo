# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копіюємо всі необхідні файли для Telegram
COPY *.sln .
COPY Flickoo.Telegram/*.csproj ./Flickoo.Telegram/
COPY Flickoo.Api/*.csproj ./Flickoo.Api/

RUN dotnet restore

# Копіюємо решту файлів і будуємо
COPY . .
WORKDIR /app/Flickoo.Telegram
RUN dotnet publish -c Release -o /out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

ENTRYPOINT ["dotnet", "Flickoo.Telegram.dll"]
