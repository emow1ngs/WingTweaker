FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Копируем файлы проекта
COPY *.csproj ./
RUN dotnet restore

# Копируем остальные файлы
COPY . ./

# Собираем приложение
RUN dotnet publish -c Release -o out

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/out .

# Открываем порт
EXPOSE 80

# Запускаем приложение
ENTRYPOINT ["dotnet", "SimpleAPI.dll"]
