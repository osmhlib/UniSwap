# UniSwap

UniSwap — вебзастосунок для обміну та безоплатної передачі речей. Користувачі можуть публікувати оголошення з фотографіями, резервувати речі та пропонувати обмін; адміністратор може переглядати користувачів і модерувати оголошення.

## Технології та структура

- **Frontend:** React, TypeScript, Vite, React Router, Lucide.
- **Backend:** ASP.NET Core Web API, Entity Framework Core, PostgreSQL, FluentValidation; Memcached — необов’язковий кеш.
- `UniSwap.Domain` — доменна модель; `UniSwap.Application` — сценарії використання; `UniSwap.Infrastructure` — робота з даними, кешем і файлами; `UniSwap.WebApi` — HTTP API.
- `uniswap-frontend` — клієнтський застосунок; `docs/frontend-integration.md` — подробиці інтеграції з API.

## Локальний запуск

Потрібні .NET SDK 10, Node.js 22.13+ або 24, PostgreSQL та інструмент `dotnet-ef` версії 10. Команди нижче наведено для PowerShell із кореня репозиторію. Створіть порожню базу PostgreSQL і користувача з правами на створення таблиць.

1. Налаштуйте з'єднання, CORS і застосуйте міграції. Замініть значення на власні; не додавайте пароль до репозиторію.

   ```powershell
   $env:ConnectionStrings__UniSwap = 'Host=localhost;Port=5432;Database=uniswap;Username=postgres;Password=YOUR_PASSWORD'
   $env:Cors__AllowedOrigins__0 = 'http://localhost:5173'
   dotnet tool install --global dotnet-ef --version 10.*
   dotnet ef database update --project UniSwap.Infrastructure --startup-project UniSwap.Infrastructure
   dotnet dev-certs https --trust
   ```

   Якщо `dotnet-ef` уже встановлено, повторна установка не потрібна. Довіра до сертифіката потрібна для локального HTTPS.

2. У цьому ж терміналі запустіть API:

   ```powershell
   dotnet run --project UniSwap.WebApi --launch-profile https
   ```

   API доступне на `https://localhost:7258`, OpenAPI у Development — на `https://localhost:7258/openapi/v1.json`.

3. В іншому терміналі запустіть клієнт:

   ```powershell
   cd uniswap-frontend
   npm ci
   Copy-Item .env.example .env
   npm run dev
   ```

   Відкрийте адресу, показану Vite, зазвичай `http://localhost:5173`. Якщо адреса API інша, змініть `VITE_API_BASE_URL` у локальному `.env`; якщо адреса фронтенду інша — також змініть дозволений CORS origin і перезапустіть API.

У Windows після первинного налаштування можна скористатися `start-api.cmd` і `start-frontend.cmd`. Для API перед запуском скрипта задайте `ConnectionStrings__UniSwap`; скрипт не застосовує міграції. Для фронтенду спочатку виконайте `npm ci`.

## Адміністратор і сховище

Для створення першого адміністратора після міграцій задайте змінні `AdminBootstrap__Email`, `AdminBootstrap__Password` та, за потреби, `AdminBootstrap__Name` перед запуском API. Повторний запуск із тією ж адресою не змінює пароль. Після створення облікового запису приберіть ці змінні з конфігурації запуску. Пароль має відповідати правилам реєстрації.

Метадані оголошень і сеансів зберігаються в PostgreSQL. Зображення зберігаються локально в `UniSwap.WebApi/App_Data/listing-images` або за шляхом `ListingImages__Path`. Для перенесення чи резервного копіювання потрібні і база, і файли зображень. Локальні дані, `.env`, залежності та результати збірки виключені з Git.

Memcached необов’язковий: стандартна адреса — `localhost:11211`, її можна змінити через `Memcached__Host` і `Memcached__Port`. Якщо кеш недоступний, читання використовує PostgreSQL.

## API та збірка

Основні групи маршрутів: `/api/auth`, `/api/users`, `/api/listings`, `/api/reservations`, `/api/exchanges`, `/api/moderation`. Захищені запити використовують `Authorization: Bearer <accessToken>`; токен пов’язаний із серверним сеансом. Опис операцій, помилок і роботи із зображеннями наведено в [гайді API](docs/frontend-integration.md).

Перевірка збірки:

```powershell
dotnet build UniSwap.slnx
cd uniswap-frontend
npm ci
npm run build
```

Ця інструкція описує локальне середовище розробки; перед публічним розгортанням потрібно окремо налаштувати HTTPS, секрети, зберігання файлів, резервне копіювання та доступи.
