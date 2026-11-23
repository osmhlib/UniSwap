# UniSwap API для фронтенд-команди

Цей гайд описує контракт для React + TypeScript. Усі шляхи нижче відносні до базової адреси API. Фактична схема доступна за `GET /openapi/v1.json` у середовищі Development.

## Перший запуск

1. Отримайте адресу тестового API від бекенд-команди або запустіть його локально за [інструкцією в README](../README.md). Локально потрібні .NET 10, PostgreSQL, налаштована змінна `ConnectionStrings__UniSwap` і застосовані міграції. Для тестів використовуйте окрему базу.
2. Для локального HTTPS виконайте `dotnet dev-certs https --trust`. Запуск `dotnet run --project UniSwap.WebApi --launch-profile https` відкриває API на `https://localhost:7258`. Перевірте `/openapi/v1.json` і `/api/listings` у браузері.
3. Якщо фронтенд працює на окремому origin, дозвольте його на бекенді **до запуску API**. Наприклад, у PowerShell встановіть `$env:Cors__AllowedOrigins__0 = 'http://localhost:5173'`. Origin має точно збігатися зі схемою, хостом і портом фронтенду, без останнього `/`. Для Vite можна задати `VITE_API_BASE_URL=https://localhost:7258` у локальній конфігурації фронтенду.
4. Зареєструйте два звичайні акаунти, щоб перевірити дії власника й іншого користувача. Для панелі модерації потрібен окремий `Admin`. Локальне створення адміністратора описано в [README](../README.md).

## Спільні правила

- Запити з тілом зазвичай використовують `Content-Type: application/json`. Завантаження фото використовує `multipart/form-data`.
- Відповіді використовують `camelCase`. Ідентифікатори є рядками UUID, дати — рядками ISO 8601 в UTC. Значення статусів і ролей чутливі до регістру.
- Захищені запити потребують `Authorization: Bearer <accessToken>`. Токен діє 12 годин, дату завершення повертає вхід. Вихід відкликає поточний токен. Окремого маршруту оновлення токена немає.
- Ідентифікатори виконавця сервер бере з токена. `ownerId`, `requesterId`, `recipientId` у відповідях потрібні для відображення й визначення доступних кнопок.
- `204 No Content` має порожнє тіло. Для такого результату не викликайте `response.json()`.
- Пагінація починається зі сторінки 1. Типові значення — `page=1&pageSize=20`, межі — `page=1…10000` та `pageSize=1…100`. При зміні фільтрів поверніться на першу сторінку.

## Типи відповідей

```ts
type UUID = string;
type UtcDateTime = string;
type Role = 'User' | 'Admin';

type Profile = { id: UUID; name: string; email: string; role: Role };
type Session = { accessToken: string; expiresAtUtc: UtcDateTime };
type Created = { id: UUID };

type ListingStatus = 'Draft' | 'Published' | 'Reserved' | 'InExchange'
  | 'Completed' | 'Hidden' | 'Rejected';

type ListingInput = {
  title: string;
  description: string;
  category: string;
  condition: string;
};

type Listing = ListingInput & {
  id: UUID;
  ownerId: UUID;
  status: ListingStatus;
  createdAtUtc: UtcDateTime;
  publishedAtUtc: UtcDateTime | null;
  moderationReason: string | null;
};

type ListingPage = {
  items: Listing[];
  page: number;
  pageSize: number;
  totalCount: number;
};

type ListingImage = {
  id: UUID;
  contentType: 'image/jpeg' | 'image/png' | 'image/webp';
  sizeBytes: number;
};

type ReservationStatus = 'Pending' | 'Accepted' | 'Rejected' | 'Cancelled';

type Reservation = {
  id: UUID;
  listingId: UUID;
  ownerId: UUID;
  requesterId: UUID;
  status: ReservationStatus;
  createdAtUtc: UtcDateTime;
  updatedAtUtc: UtcDateTime;
};

type RequestListing = {
  id: UUID;
  title: string;
  status: ListingStatus;
  coverImageId: UUID | null;
};
type RequestParticipant = { id: UUID; name: string };
type ReservationListItem = Reservation & {
  listing: RequestListing | null;
  counterparty: RequestParticipant;
  canAccept: boolean;
};

type Exchange = {
  id: UUID;
  listingId: UUID;        // оголошення отримувача
  offeredListingId: UUID; // оголошення ініціатора
  requesterId: UUID;
  recipientId: UUID;
  status: ReservationStatus | 'Completed';
  createdAtUtc: UtcDateTime;
  updatedAtUtc: UtcDateTime;
};
type ExchangeListItem = Exchange & {
  listing: RequestListing | null;
  offeredListing: RequestListing | null;
  counterparty: RequestParticipant;
  canAccept: boolean;
};
```

## Обліковий запис

| Метод і шлях | Тіло | Успішна відповідь |
|---|---|---|
| `POST /api/auth/register` | `{ name, email, password }` | `201 Profile`. Публічний маршрут, роль нового акаунта — `User`. |
| `POST /api/auth/login` | `{ email, password }` | `200 Session`. Публічний маршрут. |
| `POST /api/auth/logout` | — | `204`. Потрібен токен. |
| `GET /api/users/me` | — | `200 Profile`. Потрібен токен. |
| `PUT /api/users/me` | `{ name }` | `200 Profile`. Потрібен токен. |

Ім’я обов’язкове, до 200 символів. Email обов’язковий, до 320 символів, унікальний без урахування регістру. Пароль під час реєстрації має містити 12–128 символів. Після реєстрації викликайте вхід окремо. Після входу завантажте профіль для відображення імені й ролі. Панель модерації можна показувати за `profile.role === 'Admin'`, сервер перевіряє право на кожну дію.

## Оголошення та фото

| Метод і шлях | Доступ | Тіло або параметри | Успішна відповідь |
|---|---|---|---|
| `GET /api/listings` | Публічний | Фільтри та пагінація | `200 ListingPage` |
| `GET /api/listings/{id}` | Публічний | — | `200 Listing` |
| `GET /api/listings/mine` | Власний акаунт | Пагінація | `200 ListingPage` |
| `GET /api/listings/mine/{id}` | Власник | — | `200 Listing` для будь-якого статусу, поки оголошення існує |
| `POST /api/listings` | З токеном | `ListingInput` | `200 Created`, нова чернетка |
| `PUT /api/listings/{id}` | Власник | Повний `ListingInput` | `200 Listing` |
| `POST /api/listings/{id}/publish` | Власник | — | `204` |
| `DELETE /api/listings/{id}` | Власник | — | `204` |
| `GET /api/listings/{id}/images` | Публічний для видимого оголошення, власник для решти | — | `200 ListingImage[]` |
| `GET /api/listings/{id}/images/{imageId}` | Такий самий доступ, як у списку фото | — | `200`, бінарне зображення |
| `POST /api/listings/{id}/images` | Власник | Multipart, одне поле `file` | `201 ListingImage`, заголовок `Location` |
| `DELETE /api/listings/{id}/images/{imageId}` | Власник | — | `204` |

**Каталог.** `search` шукає підрядок у назві й описі. `category` та `condition` фільтрують за повним значенням. Усі три фільтри працюють без урахування регістру. `available=true` залишає `Published`, `available=false` — `Reserved` та `InExchange`. Без цього параметра видаються всі три публічні статуси. `oldestFirst=true` сортує від старіших публікацій, типове сортування — від новіших. Приклад: `GET /api/listings?search=алгоритми&category=Books&available=true&page=1&pageSize=20`. Формуйте рядок запиту через `URLSearchParams`.

**Форма.** Назва має 3–100 символів після обрізання пробілів, опис обов’язковий і до 2000 символів після обрізання. Категорія та стан речі — обов’язкові рядки до 100 символів. Окремого загальнодоступного довідника категорій і станів немає. Якщо використовуєте списки вибору, узгодьте значення для форми й фільтрів.

**Чернетка.** Створіть її, збережіть отриманий `id`, завантажте фото за потреби й викличте `publish`. Екран редагування й прямі посилання на власні чернетки використовують `GET /api/listings/mine/{id}`. Маршрут також доступний власнику для прихованих, відхилених і завершених оголошень. Для чужих, видалених та відсутніх записів він повертає `404`. Редагування, видалення і зміни фото заблоковані для `Reserved`, `InExchange` і `Completed`.

**Фото.** Завантажуйте по одному файлу, бажано послідовно. Ліміт — 5 фото, 5 МіБ і 40 мегапікселів на файл. Браузер сам встановить `Content-Type` з multipart boundary. Для публічних фото можна використовувати URL із таблиці прямо в `img.src`. Для закритих фото виконайте `fetch` з токеном, отримайте `Blob` і створіть URL через `URL.createObjectURL()`. Відкликайте URL після використання. Фото завантажуються окремо від JSON оголошення.

```ts
const data = new FormData();
data.append('file', file);
const response = await fetch(`${apiBase}/api/listings/${listingId}/images`, {
  method: 'POST',
  headers: { Authorization: `Bearer ${accessToken}` },
  body: data,
});
if (!response.ok) throw await response.json().catch(() => ({ status: response.status }));
const uploaded: ListingImage = await response.json();
```

## Резервування й обмін

Усі маршрути в цьому розділі потребують токен. Списки `sent` і `received` повертають масив, підтримують пагінацію й не містять `totalCount`. Якщо отримано повну сторінку, можна запросити наступну. Якщо наступна сторінка порожня, попередня була останньою.

| Метод і шлях | Тіло | Успішна відповідь |
|---|---|---|
| `POST /api/reservations` | `{ listingId }` | `201 Reservation` |
| `GET /api/reservations/sent`, `GET /api/reservations/received` | — | `200 ReservationListItem[]` |
| `POST /api/reservations/{id}/accept` | — | `200 Reservation` зі статусом `Accepted` |
| `POST /api/reservations/{id}/reject` | — | `200 Reservation` зі статусом `Rejected` |
| `POST /api/reservations/{id}/cancel` | — | `200 Reservation` зі статусом `Cancelled` |
| `POST /api/exchanges` | `{ listingId, offeredListingId }` | `200 Created` |
| `GET /api/exchanges/sent`, `GET /api/exchanges/received` | — | `200 ExchangeListItem[]` |
| `POST /api/exchanges/{id}/accept` | — | `204` |
| `POST /api/exchanges/{id}/reject`, `/cancel`, `/complete` | — | `200 Exchange` з оновленим статусом |

Резервувати можна чуже оголошення зі статусом `Published`. Новий запит має статус `Pending`. Власник може прийняти або відхилити його, ініціатор — скасувати. Прийняття змінює стан оголошення на `Reserved`.

Для обміну виберіть чуже цільове оголошення й власне оголошення зі списку `mine`. Обидва мають бути `Published`. Поки пропозиція `Pending`, отримувач може прийняти або відхилити її, ініціатор — скасувати. Після прийняття обидва оголошення мають статус `InExchange`. Завершити обмін може будь-який учасник. Тоді обидва оголошення переходять у `Completed` і зникають із публічної видачі.

У списках `counterparty` містить ID й ім’я іншого учасника. Картка оголошення містить його поточний статус, назву та ID доступної обкладинки. Для обкладинки використовуйте `GET /api/listings/{listing.id}/images/{listing.coverImageId}`. Для непублічних зображень власника потрібен Bearer-токен. Завершене оголошення лишає назву в історії учасників, а обкладинка доступна власнику. `listing` або `offeredListing` може бути `null`, якщо оголошення видалене або його дані недоступні цьому користувачу. У такому разі покажіть «Оголошення недоступне».

Кнопку «Прийняти» показуйте для отриманого запиту, коли `canAccept === true`. Старий запит може лишитися `Pending` із `canAccept: false`, якщо річ уже зайнята, прихована чи видалена. Такий запит можна відхилити або скасувати за звичайними правилами. Сервер повторно перевіряє доступність у момент прийняття. Після зміни стану, відповіді `204` або конфлікту `409` повторно завантажте відповідні списки та деталі.

## Панель модерації

Усі маршрути потребують ролі `Admin`.

| Метод і шлях | Тіло або параметри | Успішна відповідь |
|---|---|---|
| `GET /api/moderation/users` | Пагінація | `200 Profile[]` |
| `GET /api/moderation/listings` | Пагінація, `status=Published`, `Hidden` або `Rejected` | `200 ListingPage`. Типовий статус — `Published`. |
| `POST /api/moderation/listings/{id}/hide` | `{ reason }` | `200 Listing` зі статусом `Hidden` |
| `POST /api/moderation/listings/{id}/reject` | `{ reason }` | `200 Listing` зі статусом `Rejected` |
| `POST /api/moderation/listings/{id}/return-to-draft` | — | `200 Listing` зі статусом `Draft` |
| `POST /api/moderation/listings/{id}/remove` | `{ reason }` | `204` |

Причина обов’язкова, до 2000 символів. Приховування та відхилення застосовуються до `Published`. У чернетку можна повернути `Hidden` чи `Rejected`. Видалення з видачі доступне для всіх трьох статусів. Після дії оновлюйте чергу. Власник бачить причину в `moderationReason` для прихованих або відхилених оголошень у `mine`.

## Обробка помилок

JSON-помилки можуть містити поля `type`, `title`, `status`, `code`, `traceId`, а помилки полів — `errors: Record<string, string[]>`. Для вбудованої валідації ключі можуть мати вигляд `Title` або `OfferedListingId`. Мапте їх до полів форми. Частина відповідей може мати порожнє тіло, тому орієнтуйтеся також на HTTP-статус.

| Статус | Реакція інтерфейсу |
|---|---|
| `400` | Показати помилки полів або підказку про некоректні дані. |
| `401` | На формі входу показати помилку облікових даних. На захищеному екрані очистити прострочений або відкликаний токен і запропонувати увійти. |
| `403` | Повідомити, що акаунт не має права на дію. Не завершувати сеанс. |
| `404` | Показати стан «оголошення або запит недоступні». |
| `409` | Вибрати повідомлення за полем `code` і за потреби перечитати стан. |
| `413` | Запропонувати менший файл. Код `payload_too_large` означає, що файл перевищує 5 МіБ або перевищено обмеження всього multipart-запиту. |
| `500` | Показати загальне повідомлення про помилку. За наявності зберегти `traceId` для діагностики. |

Для локалізації використовуйте `code` і резервне повідомлення за HTTP-статусом. Текст `title` може змінюватися.

| Код | Значення |
|---|---|
| `validation_failed` | Виправте поля, зазначені в `errors`. |
| `invalid_image` | Файл не відповідає вимогам до зображення. |
| `email_already_registered` | Адреса вже зареєстрована. |
| `image_limit_reached` | Досягнуто ліміту зображень. |
| `reservation_already_pending` | Користувач уже має активний запит на цю річ. |
| `listing_unavailable` | Оголошення більше не можна зарезервувати або обміняти в межах цього запиту. |
| `state_conflict` | Поточний стан запису не дозволяє виконати дію. |
| `concurrent_update` | Інший запит змінив дані. Спочатку перечитайте їх. |

Окремо обробляйте помилку мережі. Якщо браузер не дає прочитати HTTP-відповідь, перевірте базовий URL, довіру до локального HTTPS-сертифіката та дозволений CORS origin.

## Перевірка перед підключенням екранів

- Увійти двома акаунтами та прочитати профілі.
- Першим акаунтом створити чернетку, додати фото й опублікувати її. Другим знайти оголошення в каталозі.
- Другим акаунтом надіслати резервування, першим — прийняти. Перевірити `Reserved` в каталозі та `Accepted` у списках запитів.
- На двох інших опублікованих оголошеннях перевірити прийняття та завершення обміну.
- Звичайним акаунтом перевірити `403` на маршруті модерації. Адміністратором приховати оголошення й перевірити, що каталог більше його не показує.
