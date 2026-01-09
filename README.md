# Flight Information System

## Про проєкт

Система для управління інформацією про авіарейси, яка включає три основні компоненти:

1. **FlightStorageService** – серверна частина на ASP.NET Core Web API, що відповідає за збереження та обробку даних про рейси
2. **FlightClientApp** – веб-інтерфейс на ASP.NET MVC / Razor Pages для взаємодії користувача з системою через REST API
3. **CleanUpService** – фоновий сервіс на базі ASP.NET Worker для автоматичного видалення застарілих записів

Всі операції з базою даних реалізовані через **ADO.NET** з використанням **збережених процедур** у MS SQL Server. 

## Стек технологій

- .NET 9 (C# 11)
- ASP.NET Core Web API
- ASP.NET MVC / Razor Pages
- ADO.NET для роботи з БД
- MS SQL Server 2019 або новіше
- Swagger для документації API
- Bootstrap для стилізації інтерфейсу

## Організація проєкту

```
FlightInfoSystem/
├── FlightStorageService/      # Серверна частина API
├── FlightClientApp/           # Клієнтський веб-додаток
├── CleanUpService/            # Фоновий сервіс очищення
├── db/
│   └── init.sql               # Скрипт ініціалізації БД
├── README.md
└── .gitignore
```

## Схема бази даних

**База даних:** `FlightsDb`  
**Основна таблиця:** `dbo.Flights`

| Колонка              | Тип даних     | Призначення                    |
|----------------------|---------------|--------------------------------|
| FlightNumber (PK)    | NVARCHAR(10)  | Унікальний ідентифікатор рейсу |
| DepartureDateTime    | DATETIME2     | Час та дата відправлення       |
| DepartureAirportCity | NVARCHAR(100) | Пункт вильоту                  |
| ArrivalAirportCity   | NVARCHAR(100) | Пункт призначення              |
| DurationMinutes      | INT           | Час польоту в хвилинах         |

**Важливо:** Система працює тільки з рейсами на наступні 7 днів від поточної дати.

## Доступні API маршрути

- `GET /api/flights/{flightNumber}` – рейс за номером
- `GET /api/flights?date={yyyy-MM-dd}` – список усіх рейсів на вказану дату
- `GET /api/flights/departure?city={city}&date={yyyy-MM-dd}` – рейси з певного міста
- `GET /api/flights/arrival?city={city}&date={yyyy-MM-dd}` – рейси до певного міста

## Інструкція з запуску

### Крок 1: Підготовка бази даних

Запустіть SQL-скрипт `db/init.sql` через MS SQL Server Management Studio.

### Кроки 2-3: Запуск серверної та клієнтської частин

```bash
# Запуск API сервера
cd FlightStorageService
dotnet restore
dotnet build
dotnet run
# Swagger документація буде доступна за адресою:
http://localhost:5000/swagger
https://localhost:5050/swagger

# Запуск веб-клієнта
cd ../FlightClientApp
dotnet restore
dotnet build
dotnet run
# Веб-інтерфейс буде доступний за адресою:
http://localhost:5001
https://localhost:5051
```
