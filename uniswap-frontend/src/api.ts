const API_BASE = (import.meta.env.VITE_API_BASE_URL || 'https://localhost:7258').replace(/\/$/, '');

export type Profile = { id: string; name: string; email: string; role: 'User' | 'Admin' };
export type Listing = {
  id: string; ownerId: string; title: string; description: string; category: string;
  condition: string; status: string; createdAtUtc: string; publishedAtUtc?: string | null;
  moderationReason?: string | null;
};
export type ListingPage = { items: Listing[]; page: number; pageSize: number; totalCount: number };
export type ListingImage = { id: string; contentType: string; sizeBytes: number };

let token = localStorage.getItem('uniswap_token') || '';
export function setToken(value: string) { token = value; if (value) localStorage.setItem('uniswap_token', value); else localStorage.removeItem('uniswap_token'); }
export function getToken() { return token; }

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  if (token) headers.set('Authorization', `Bearer ${token}`);
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json');
  let response: Response;
  try { response = await fetch(`${API_BASE}${path}`, { ...options, headers }); }
  catch { throw new Error(`Не вдалося з'єднатися з API (${API_BASE}). Перевір, чи запущений бекенд і чи налаштований CORS.`); }
  if (response.status === 204) return undefined as T;
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    if (response.status === 401) setToken('');
    const code = data?.code;
    const errors = data?.errors ? Object.values(data.errors).flat().join(' ') : '';
    const messages: Record<string, string> = {
      email_already_registered: 'Ця електронна адреса вже зареєстрована.',
      reservation_already_pending: 'Ти вже надіслав запит на цю річ.',
      listing_unavailable: 'Це оголошення вже недоступне.',
      state_conflict: 'Цю дію неможливо виконати для поточного статусу.',
    };
    throw new Error(messages[code] || errors || data?.title || `Помилка запиту: ${response.status}`);
  }
  return data as T;
}
const json = (value: unknown) => JSON.stringify(value);
export const api = {
  register: (body: {name:string;email:string;password:string}) => request<Profile>('/api/auth/register', { method:'POST', body:json(body) }),
  login: (body: {email:string;password:string}) => request<{accessToken:string;expiresAtUtc:string}>('/api/auth/login', { method:'POST', body:json(body) }),
  logout: () => request<void>('/api/auth/logout', { method:'POST' }),
  profile: () => request<Profile>('/api/users/me'),
  updateProfile: (name:string) => request<Profile>('/api/users/me', { method:'PUT', body:json({name}) }),
  listings: (params: URLSearchParams) => request<ListingPage>(`/api/listings?${params}`),
  listing: (id:string) => request<Listing>(`/api/listings/${id}`),
  mine: () => request<ListingPage>('/api/listings/mine?page=1&pageSize=100'),
  mineById: (id: string) => request<Listing>(`/api/listings/mine/${id}`),
  createListing: (body: Pick<Listing,'title'|'description'|'category'|'condition'>) => request<{id:string}>('/api/listings', { method:'POST', body:json(body) }),
  updateListing: (id:string, body: Pick<Listing,'title'|'description'|'category'|'condition'>) => request<Listing>(`/api/listings/${id}`, { method:'PUT', body:json(body) }),
  publish: (id:string) => request<void>(`/api/listings/${id}/publish`, { method:'POST' }),
  deleteListing: (id:string) => request<void>(`/api/listings/${id}`, { method:'DELETE' }),
  images: (id:string) => request<ListingImage[]>(`/api/listings/${id}/images`),
  imageBlob: async (id: string, imageId: string) => {
  const headers = new Headers();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const response = await fetch(`${API_BASE}/api/listings/${id}/images/${imageId}`, { headers });

  if (!response.ok) {
    throw new Error(`Не вдалося завантажити фотографію (${response.status})`);
  }

  return URL.createObjectURL(await response.blob());
},
deleteImage: (id: string, imageId: string) =>
  request<void>(`/api/listings/${id}/images/${imageId}`, { method: 'DELETE' }),
  imageUrl: (id:string, imageId:string) => `${API_BASE}/api/listings/${id}/images/${imageId}`,
  uploadImage: async (id:string, file:File) => { const form = new FormData(); form.append('file', file); return request<ListingImage>(`/api/listings/${id}/images`, {method:'POST', body:form}); },
  reserve: (listingId:string) => request('/api/reservations', {method:'POST', body:json({listingId})}),
  sentReservations: () => request<any[]>('/api/reservations/sent?page=1&pageSize=100'),
  receivedReservations: () => request<any[]>('/api/reservations/received?page=1&pageSize=100'),
  reservationAction: (id:string, action:'accept'|'reject'|'cancel') => request(`/api/reservations/${id}/${action}`, {method:'POST'}),
  sentExchanges: () => request<any[]>('/api/exchanges/sent?page=1&pageSize=100'),
  receivedExchanges: () => request<any[]>('/api/exchanges/received?page=1&pageSize=100'),
  createExchange: (listingId:string, offeredListingId:string) => request('/api/exchanges', {method:'POST', body:json({listingId,offeredListingId})}),
  exchangeAction: (id:string, action:'accept'|'reject'|'cancel'|'complete') => request(`/api/exchanges/${id}/${action}`, {method:'POST'}),
};
