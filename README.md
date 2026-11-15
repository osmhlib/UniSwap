# UniSwap backend

UniSwap — ASP.NET Core API для обміну й безоплатної передачі речей. Рішення складається з Domain, Application, Infrastructure і WebApi. Консольний проєкт SmokeTests перевіряє домен, сценарії, кеш і PostgreSQL.

Для фронтенд-команди є окремий [гайд з інтеграції API](docs/frontend-integration.md).

## Локальний запуск

Потрібні .NET SDK 10, EF CLI 10 і PostgreSQL. SQL міграцій та інтеграційні сценарії перевіряються з PostgreSQL 18. Створіть порожню базу для розробки й надайте її користувачу права створювати та змінювати таблиці. Задайте connection string через `ConnectionStrings__UniSwap`. Значення має містити `Host`, `Port`, `Database`, `Username` і, за потреби, `Password`. Не записуйте секрети у файли репозиторію.

```powershell
dotnet ef database update --project UniSwap.Infrastructure --startup-project UniSwap.Infrastructure
dotnet run --project UniSwap.WebApi --launch-profile https
```

У Development схема доступна за `/openapi/v1.json`. Дозволені адреси фронтенду задаються через `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1` тощо.

Щоб створити першого адміністратора, після міграцій задайте `AdminBootstrap__Email` і `AdminBootstrap__Password` та запустіть API. За потреби задайте `AdminBootstrap__Name`. Повторний запуск із тією самою адресою не змінить пароль адміністратора. Якщо адреса вже належить звичайному користувачу, запуск відхиляється. Після створення адміністратора приберіть пароль із конфігурації запуску.

Memcached опційний. Для локальної розробки можна запустити `docker run --rm -p 127.0.0.1:11211:11211 memcached:1.6`. Налаштування `Memcached__Host` і `Memcached__Port` мають типові значення `localhost` та `11211`. Недоступність кешу не блокує читання з PostgreSQL. Інвалідація кешу відбувається після запису в базу. Якщо інвалідація не відбулася або конкурувала з читанням, запис може бути застарілим до 30 секунд. При влучанні в кеш публічний статус додатково перевіряється в PostgreSQL. Перевірки прав і статусів для запису також звертаються до PostgreSQL.

Файли оголошень зберігаються в `App_Data/listing-images` або в каталозі, заданому через `ListingImages__Path`. Службі потрібні права читання та запису. Для тривалого зберігання використовуйте постійний диск і робіть резервну копію каталогу разом із базою.

## HTTP API

| Метод і шлях | Призначення |
|---|---|
| `POST /api/auth/register` | Приймає `name`, `email`, `password`. Створює користувача з роллю `User`. |
| `POST /api/auth/login` | Приймає `email`, `password`. Повертає `accessToken` та `expiresAtUtc`. |
| `POST /api/auth/logout` | Відкликає поточний токен. |
| `GET, PUT /api/users/me` | Читає профіль або змінює `name`. |
| `GET /api/listings` | Пошук і фільтри `search`, `category`, `condition`, `available`. Сортування `oldestFirst`. Пагінація `page`, `pageSize`. |
| `GET /api/listings/{id}` | Публічний перегляд оголошення. |
| `GET /api/listings/mine` | Сторінка оголошень поточного користувача, включно із чернетками. |
| `GET /api/listings/mine/{id}` | Деталі власного оголошення для редактора та особистого кабінету. |
| `POST /api/listings`, `PUT /api/listings/{id}` | Створення чернетки або редагування. Поля `title`, `description`, `category`, `condition`. |
| `DELETE /api/listings/{id}`, `POST /api/listings/{id}/publish` | Видалення з видачі або публікація. |
| `GET, POST /api/listings/{id}/images` | Список метаданих або завантаження одного файла в полі `file` через multipart. |
| `GET, DELETE /api/listings/{id}/images/{imageId}` | Перегляд або видалення файла. |
| `POST /api/reservations` | Приймає `listingId` та створює запит зі статусом `Pending`. |
| `GET /api/reservations/sent`, `GET /api/reservations/received` | Повертає сторінку надісланих або отриманих запитів. |
| `POST /api/reservations/{id}/accept`, `/reject`, `/cancel` | Змінює стан і повертає актуальний запис резервування. |
| `POST /api/exchanges` | Приймає `listingId` і `offeredListingId` та створює пропозицію обміну. |
| `GET /api/exchanges/sent`, `GET /api/exchanges/received` | Повертає надіслані або отримані пропозиції з їхніми статусами. |
| `POST /api/exchanges/{id}/accept`, `/reject`, `/cancel`, `/complete` | Переходи обміну. Старий префікс `/api/exchange-requests` також підтримується. |
| `GET /api/moderation/users`, `GET /api/moderation/listings` | Перелік користувачів і оголошень для адміністратора. Черга за замовчуванням показує `Published`, фільтр `status` також приймає `Hidden` або `Rejected`. |
| `POST /api/moderation/listings/{id}/hide`, `/reject`, `/remove` | Приховує, відхиляє або видаляє з видачі. Потрібне поле `reason`. |
| `POST /api/moderation/listings/{id}/return-to-draft` | Повертає приховане або відхилене оголошення у чернетку для редагування і повторної публікації. |

Для захищених запитів передавайте `Authorization: Bearer <accessToken>` через HTTPS. Токен діє 12 годин. Вихід відкликає лише токен поточного сеансу. API визначає особу за сеансом. Поля з ідентифікаторами користувачів з тіла запиту для цього не використовуються.

У публічній видачі доступні `Published`, `Reserved` та `InExchange`. Параметр `available=true` обмежує результат статусом `Published`, а `available=false` повертає `Reserved` та `InExchange`. Видалення виключає оголошення з видачі й зберігає зв’язки з історичними запитами. Для зображень дозволені JPEG, PNG і WebP, до 5 МіБ і до 40 мегапікселів на файл, до п’яти файлів на оголошення. Зображення чернеток доступні власнику.

## Міграції та зв'язки

- `InitialCreate` додає Users, Listings, Reservations та ExchangeRequests. Користувач володіє багатьма оголошеннями. Резервування пов’язане з одним оголошенням та двома користувачами. Пропозиція обміну пов’язана з двома оголошеннями та двома користувачами.
- `AddListingImages` додає зв’язок між оголошенням і його файлами через таблицю метаданих.
- `AddAccountsAndSessions` додає унікальну нормалізовану пошту, хеш пароля, роль та сеанси користувача. Для наявних записів без хешу вхід недоступний.
- `AddListingDeletion` додає позначку видалення оголошення.
- `AddListingImageRevision` синхронізує зміни зображень із версією оголошення `xmin`.
- `LimitPendingReservations` дозволяє лише один активний запит від одного користувача на те саме оголошення.

## Перевірки

```powershell
dotnet build UniSwap.slnx
dotnet run --project UniSwap.SmokeTests/UniSwap.SmokeTests.csproj
```

Щоб увімкнути перевірку PostgreSQL, задайте `UniSwapTests__Postgres` із параметрами доступу до окремої тестової бази перед запуском SmokeTests. Тест застосовує міграції та додає дані до цієї бази. Для HTTP-перевірки запустіть API в Development на окремій тестовій базі, задайте `UniSwapTests__ApiUrl` і виконайте `python UniSwap.SmokeTests/http_smoke.py`. Цей сценарій додає тестових користувачів, оголошення й файли.

## Межі перевірки

Інтеграційний сценарій використовує окремий PostgreSQL і запускає запити до справжнього HTTP API. Перевіряються права, модерація, конкурентні зміни й відкат. HTTP-сценарій також вимірює час відповіді списку, пошуку і деталей на своїх тестових даних. Окремі перевірки кешу відтворюють інвалідацію та недоступність кешу. Для перевірки з реальним Memcached запустіть сервер і повторіть HTTP-сценарій із відповідними параметрами API.
