# Развертывание API на Railway.app

## 🚀 Быстрый старт

### 1. Подготовка
1. Зарегистрируйтесь на [railway.app](https://railway.app)
2. Подключите GitHub аккаунт
3. Создайте новый проект

### 2. Настройка базы данных
1. В Railway добавьте MySQL сервис
2. Скопируйте строку подключения
3. Обновите `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=containers-us-west-xxx.railway.app;Port=xxxx;Database=railway;Uid=root;Pwd=xxxxx;"
  }
}
```

### 3. Развертывание
1. Загрузите папку `KeyAPI` в GitHub репозиторий
2. В Railway подключите репозиторий
3. Railway автоматически развернет приложение

### 4. Получение URL
После развертывания получите URL вида:
`https://your-app-name.railway.app`

### 5. Обновление CloudKeyManager
Замените URL в `CloudKeyManager.cs`:
```csharp
private string apiUrl = "https://your-app-name.railway.app/api/keys";
```

## 🔧 Альтернативные способы

### Heroku (бесплатно)
1. Установите Heroku CLI
2. Создайте `Procfile`:
```
web: dotnet KeyAPI.dll --urls=http://0.0.0.0:$PORT
```
3. Разверните:
```bash
heroku create your-app-name
git push heroku main
```

### Render.com (бесплатно)
1. Зарегистрируйтесь на render.com
2. Подключите GitHub
3. Выберите "Web Service"
4. Укажите команду: `dotnet KeyAPI.dll`

### DigitalOcean App Platform
1. Создайте приложение в DigitalOcean
2. Подключите GitHub репозиторий
3. Настройте переменные окружения

## 📋 Переменные окружения

Для всех платформ настройте:
- `ConnectionStrings__DefaultConnection` - строка подключения к MySQL
- `ASPNETCORE_ENVIRONMENT` - Production

## 🗄️ Настройка MySQL

### Railway MySQL
1. Добавьте MySQL сервис в Railway
2. Скопируйте строку подключения
3. Выполните SQL скрипт из `create_database.sql`

### Внешний MySQL
Можно использовать:
- **PlanetScale** (бесплатно)
- **Supabase** (бесплатно)
- **Aiven** (бесплатно)
- **Clever Cloud** (бесплатно)

## 🔗 Обновление приложения

После развертывания обновите `CloudKeyManager.cs`:

```csharp
private string apiUrl = "https://your-deployed-url.com/api/keys";
```

## ✅ Проверка работы

1. Откройте `https://your-app-name.railway.app/api/keys/stats`
2. Должна вернуться статистика
3. Протестируйте генератор ключей
