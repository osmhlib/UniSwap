import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, NavLink, Route, Routes, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { BookOpen, Headphones, Package, Recycle, Sprout, ArrowRight, ArrowUpRight, Check, ChevronDown, CircleUserRound, Heart, Leaf, LogOut, Menu, PackagePlus, Search, ShieldCheck, SlidersHorizontal, Sparkles, X } from 'lucide-react';
import { api, getToken, setToken } from './api';
import type { Listing, ListingImage, ListingPage, Profile } from './api';
import type { ExchangeListItem } from './api';
import ExchangeOfferForm from './ExchangeOfferForm';

const categories = ['Усі категорії','Електроніка','Одяг','Книги','Дім і побут','Спорт','Дитячі речі','Інше'];
const categoryMap: Record<string,string> = { 'Електроніка':'Electronics', 'Одяг':'Clothing', 'Книги':'Books', 'Дім і побут':'Home', 'Спорт':'Sports', 'Дитячі речі':'Kids', 'Інше':'Other' };
const categoryLabel = (v:string) => Object.entries(categoryMap).find(([,value])=>value.toLowerCase()===v.toLowerCase())?.[0] || v;
const listingReturnPath = (value:string|null) => value && /^\/listing\/[a-z0-9-]+$/i.test(value) ? value : '/';
function authLink(path:string,register = false) {
  const params = new URLSearchParams();
  if (register) params.set('mode','register');
  if (listingReturnPath(path) !== '/') params.set('returnTo',path);
  return `/auth${params.size ? `?${params}` : ''}`;
}

export default function App() {
  const [profile,setProfile] = useState<Profile|null>(null);
  const [authReady,setAuthReady] = useState(false);
  const [mobileOpen,setMobileOpen] = useState(false);
  const [toast,setToast] = useState('');
  const navigate = useNavigate();
  const location = useLocation();
  const authReturnTo = location.pathname === '/auth' ? new URLSearchParams(location.search).get('returnTo') || '/' : location.pathname;
  useLayoutEffect(() => { window.scrollTo({top:0,left:0,behavior:'instant'}); },[location.pathname]);
useEffect(() => {
  if (!getToken()) {
    setAuthReady(true);
    return;
  }

  api.profile()
    .then(setProfile)
    .catch(() => {
      setProfile(null);
    })
    .finally(() => setAuthReady(true));
}, []);
  const notify = (message:string) => { setToast(message); window.setTimeout(()=>setToast(''),3500); };
  const signOut = async () => { try { await api.logout(); } catch {} setToken(''); setProfile(null); navigate('/'); notify('Ви вийшли з акаунта'); };
  return <div className="app-shell">
    <header className="topbar"><div className="nav-wrap">
      <Link to="/" className="brand"><span className="brand-mark"><Leaf size={20}/></span><span>uni<span className="brand-accent">swap</span></span></Link>
      <nav className={mobileOpen?'nav-links open':'nav-links'}><NavLink to="/" end onClick={()=>setMobileOpen(false)}>Каталог</NavLink>{profile&&<><NavLink to="/requests" onClick={()=>setMobileOpen(false)}>Мої запити</NavLink><NavLink to="/account" onClick={()=>setMobileOpen(false)}>Кабінет</NavLink></>}{profile?.role==='Admin'&&<NavLink to="/moderation">Модерація</NavLink>}</nav>
      <div className="nav-actions">{profile?<><Link to="/create" className="button button-primary button-small"><PackagePlus size={16}/> Додати річ</Link><button className="avatar-button" title={profile.name} onClick={()=>navigate('/account')}>{profile.name.slice(0,1).toUpperCase()}</button><button className="icon-button logout" title="Вийти" onClick={signOut}><LogOut size={18}/></button></>:<><Link to={authLink(authReturnTo)} className="button button-ghost button-small">Увійти</Link><Link to={authLink(authReturnTo,true)} className="button button-primary button-small">Приєднатися <ArrowUpRight size={15}/></Link></>}<button className="icon-button mobile-menu" onClick={()=>setMobileOpen(!mobileOpen)}>{mobileOpen?<X/>:<Menu/>}</button></div>
    </div></header>
    <main>{!authReady?<div className="loading">Завантажуємо UniSwap…</div>:<Routes>
      <Route path="/" element={<Catalog profile={profile} notify={notify}/>}/>
      <Route path="/auth" element={<Auth onAuth={p=>{setProfile(p);navigate(listingReturnPath(new URLSearchParams(location.search).get('returnTo')),{replace:true});notify('Раді вітати в UniSwap!')}}/>}/>
      <Route path="/listing/:id" element={<ListingDetails profile={profile} notify={notify}/>}/>
      <Route path="/create" element={<ListingForm key={location.pathname} notify={notify}/>}/>
      <Route path="/edit/:id" element={<ListingForm key={location.pathname} notify={notify}/>}/>
      <Route path="/account" element={<Account profile={profile} setProfile={setProfile} notify={notify}/>}/>
      <Route path="/requests" element={<Requests notify={notify}/>}/>
      <Route path="/moderation" element={<Moderation profile={profile} notify={notify}/>}/>
      <Route path="*" element={<div className="empty-state"><h2>Сторінку не знайдено</h2><Link to="/" className="text-link">На головну</Link></div>}/>
    </Routes>}</main>
    <footer><div className="footer-inner"><Link to="/" className="brand"><span className="brand-mark"><Leaf size={18}/></span><span>uni<span className="brand-accent">swap</span></span></Link><span>Речі знаходять нових власників.</span><span>© 2026 UniSwap</span></div></footer>
    {toast&&<div className="toast"><Check size={17}/>{toast}</div>}
  </div>;
}


function CatalogSelect({label,value,options,onChange,icon = false}:{label:string;value:string;options:{value:string;label:string}[];onChange:(value:string)=>void;icon?:boolean}) {
  const ref = useRef<HTMLDetailsElement>(null);
  useEffect(() => {
    const close = (event:MouseEvent) => { if (!ref.current?.contains(event.target as Node)) ref.current?.removeAttribute('open'); };
    document.addEventListener('click',close);
    return () => document.removeEventListener('click',close);
  },[]);
  return <details className="catalog-select" ref={ref} onKeyDown={event => { if (event.key === 'Escape') { ref.current?.removeAttribute('open'); ref.current?.querySelector('summary')?.focus(); } }}>
    <summary aria-label={label}>{icon && <SlidersHorizontal size={16}/>}<span>{options.find(option => option.value === value)?.label}</span><ChevronDown size={15}/></summary>
    <div className="catalog-select-menu" role="group" aria-label={label}>
      {options.map(option => <button key={option.value} type="button" className={option.value === value ? 'selected' : ''} onClick={() => { onChange(option.value); ref.current?.removeAttribute('open'); ref.current?.querySelector('summary')?.focus(); }}>{option.label}{option.value === value && <Check size={16} aria-hidden="true" />}</button>)}
    </div>
  </details>;
}

function Catalog({profile,notify}:{profile:Profile|null;notify:(s:string)=>void}) {
  const [params,setParams] = useSearchParams();
  const [data,setData] = useState<ListingPage|null>(null);
  const [loading,setLoading] = useState(true);
  const [loadingMore,setLoadingMore] = useState(false);
  const [error,setError] = useState('');

  const search = params.get('search') || '';
  const category = params.get('category') || '';
  const available = params.get('available') ?? 'true';
  const oldestFirst = params.get('oldestFirst') === 'true';

  const update = (key:string,value:string) => {
    const next = new URLSearchParams(params);
    if (value) next.set(key,value);
    else next.delete(key);
    setParams(next);
  };

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setLoading(true);
      setError('');
      setData(null);

      const q = new URLSearchParams({
        page:'1',
        pageSize:'24'
      });

      if (available !== 'all') q.set('available',available);
      if (search) q.set('search',search);
      if (category) q.set('category',categoryMap[category] || category);
      if (oldestFirst) q.set('oldestFirst','true');

      try {
        const result = await api.listings(q);
        if (!cancelled) setData(result);
      } catch (e) {
        if (!cancelled) setError((e as Error).message);
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void load();
    return () => { cancelled = true; };
  },[search,category,available,oldestFirst]);

  const loadMore = async () => {
    if (!data || loadingMore || data.items.length >= data.totalCount) return;

    setLoadingMore(true);
    setError('');

    const q = new URLSearchParams({
      page:String(data.page + 1),
      pageSize:'24'
    });

    if (available !== 'all') q.set('available',available);
    if (search) q.set('search',search);
    if (category) q.set('category',categoryMap[category] || category);
    if (oldestFirst) q.set('oldestFirst','true');

    try {
      const result = await api.listings(q);
      setData(previous => previous ? {
        ...result,
        items:[...previous.items,...result.items]
      } : result);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoadingMore(false);
    }
  };

  return <>
    <section className="hero">
      <div className="hero-inner">
        <div className="hero-copy">
          <div className="eyebrow"><Sparkles size={14}/> РЕЧІ МАЮТЬ ПРОДОВЖУВАТИ ЖИТИ</div>
          <h1>Знайди нове для себе.<br/><em>Поділись тим, що маєш.</em></h1>
          <p>Обмінюйся речами, віддавай безкоштовно та знаходь те, що давно шукав. Просто, свідомо, по-сусідськи.</p>
          <div className="hero-actions">
            <a href="#catalog" className="button button-dark">Переглянути каталог <ArrowRight size={17}/></a>
            <span className="hero-note"><span className="avatar-stack"><i>А</i><i>М</i><i>О</i></span> Спільнота свідомого обміну</span>
          </div>
        </div>
        <div className="hero-art">
          <div className="art-orbit orbit-one"/>
          <div className="art-orbit orbit-two"/>
          <div className="art-card art-card-main"><Headphones size={72} aria-hidden="true" /><span className="art-tag">Шукає нового власника</span></div>
          <div className="art-card art-card-small"><BookOpen size={48} aria-hidden="true" /><span>Улюблені історії — далі</span></div>
          <div className="art-spark"><Sparkles aria-hidden="true" /></div>
          <div className="art-caption">give a little<br/>get a little</div>
        </div>
      </div>
    </section>

    <section className="benefits">
      <div><span className="benefit-icon"><Heart size={18}/></span><b>Безкоштовно</b><small>Речі без зайвих витрат</small></div>
      <div><span className="benefit-icon"><Leaf size={18}/></span><b>Свідомо</b><small>Менше відходів — більше користі</small></div>
      <div><span className="benefit-icon"><Sparkles size={18}/></span><b>По-людськи</b><small>Обмін між реальними людьми</small></div>
    </section>

    <section className="catalog-section" id="catalog">
      <div className="section-heading">
        <div>
          <div className="eyebrow">ДОБРІ РЕЧІ ЧЕКАЮТЬ</div>
          <h2>Каталог речей <span className="heading-count">{data ? data.totalCount : ''}</span></h2>
          <p>Можливо, саме тут є те, що тобі потрібно.</p>
        </div>

        <CatalogSelect label="Наявність оголошень" value={available} onChange={value => update('available',value)} icon options={[{value:'true',label:'Доступні речі'},{value:'false',label:'Зарезервовані та в обміні'},{value:'all',label:'Усі оголошення'}]} />
      </div>

      <div className="catalog-toolbar">
        <div className="search-box">
          <Search size={18}/>
          <input value={search} onChange={e => update('search',e.target.value)} placeholder="Знайти щось цікаве…"/>
        </div>

        <CatalogSelect label="Сортування оголошень" value={oldestFirst ? 'oldest' : 'newest'} onChange={value => update('oldestFirst',value === 'oldest' ? 'true' : 'false')} options={[{value:'newest',label:'Спочатку новіші'},{value:'oldest',label:'Спочатку старіші'}]} />

        <div className="category-pills">
          {categories.map(c => (
            <button
              key={c}
              className={(c === 'Усі категорії' ? !category : category === c) ? 'category-pill active' : 'category-pill'}
              onClick={() => update('category',c === 'Усі категорії' ? '' : c)}
            >
              {c}
            </button>
          ))}
        </div>
      </div>

      {loading ? (
        <div className="loading">Завантажуємо оголошення…</div>
      ) : error && !data ? (
        <div className="error-panel">
          <b>Не вдалося завантажити каталог</b>
          <p>{error}</p>
          <small>Переконайся, що API запущений і доступний за адресою, вказаною у .env.</small>
        </div>
      ) : !data?.items.length ? (
        <div className="empty-state">
          <div className="empty-emoji"><Sprout size={48} aria-hidden="true" /></div>
          <h3>Поки тут тихо</h3>
          <p>Не знайшли речей за цим запитом. Спробуй змінити фільтри або додай перше оголошення.</p>
          {profile && <Link to="/create" className="button button-primary">Додати річ</Link>}
        </div>
      ) : (
        <>
          <div className="listing-grid">
            {data.items.map((item,i) => <ListingCard key={item.id} item={item} index={i}/>)}
          </div>

          {error && <div className="form-error">{error}</div>}

          <div className="pagination-note">
            Показано {data.items.length} із {data.totalCount} оголошень
          </div>

          {data.items.length < data.totalCount && (
            <div className="pagination-actions">
              <button className="button button-ghost" onClick={loadMore} disabled={loadingMore}>
                {loadingMore ? 'Завантажуємо…' : 'Завантажити ще'}
              </button>
            </div>
          )}
        </>
      )}
    </section>
  </>;
}

function ListingCard({item,index}:{item:Listing;index:number}) {
  const [image,setImage] = useState<string|null>(null);

  useEffect(() => {
    let objectUrl: string|null = null;
    let cancelled = false;

    const loadImage = async () => {
      try {
        const imgs = await api.images(item.id);

        if (!imgs[0] || cancelled) return;

        const token = getToken();

        const response = await fetch(
          api.imageUrl(item.id, imgs[0].id),
          {
            headers: token
              ? { Authorization: `Bearer ${token}` }
              : {}
          }
        );

        if (!response.ok || cancelled) return;

        const blob = await response.blob();

        if (cancelled) return;

        objectUrl = URL.createObjectURL(blob);
        setImage(objectUrl);
      } catch {
        // Залишаємо емодзі, якщо фото не завантажилося
      }
    };

    setImage(null);
    loadImage();

    return () => {
      cancelled = true;

      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [item.id]);

  const PlaceholderIcon = [Package, Headphones, BookOpen, Sprout, Package, Package][index%6];

  return (
    <Link to={`/listing/${item.id}`} className="listing-card">
      <div className="listing-image">
        {image
          ? <img src={image} alt={item.title}/>
          : <PlaceholderIcon size={48} aria-hidden="true" />
        }

        <span className="listing-status">
          {item.status==='Published'?'Доступно':
           item.status==='Reserved'?'Зарезервовано':
           item.status==='InExchange'?'В обміні':item.status}
        </span>

        <button
          className="favorite"
          onClick={e=>{
            e.preventDefault();
            e.stopPropagation();
          }}
          aria-label="До обраного"
        >
          <Heart size={17}/>
        </button>
      </div>

      <div className="listing-info">
        <span className="listing-category">
          {categoryLabel(item.category)}
        </span>
        <h3>{item.title}</h3>
        <p>{item.condition}</p>

        <div className="listing-bottom">
          <span>Безкоштовно</span>
          <span className="round-arrow">
            <ArrowUpRight size={17}/>
          </span>
        </div>
      </div>
    </Link>
  );
}

function Auth({onAuth}:{onAuth:(p:Profile)=>void}) { const [mode,setMode]=useState<'login'|'register'>(new URLSearchParams(location.search).get('mode')==='register'?'register':'login');const [name,setName]=useState('');const [email,setEmail]=useState('');const [password,setPassword]=useState('');const [error,setError]=useState('');const [busy,setBusy]=useState(false);const submit=async(e:FormEvent)=>{e.preventDefault();setBusy(true);setError('');try{if(mode==='register'){await api.register({name,email,password});}const session=await api.login({email,password});setToken(session.accessToken);const p=await api.profile();onAuth(p);}catch(err){setError((err as Error).message)}finally{setBusy(false)}};return <div className="auth-layout"><div className="auth-aside"><div className="eyebrow">ТВОЯ СПІЛЬНОТА ОБМІНУ</div><h1>Речі об'єднують<br/><em>людей.</em></h1><p>Створи акаунт, щоб ділитися речами, знаходити цікаве та домовлятися про обмін.</p><div className="auth-illustration"><Recycle size={48} aria-hidden="true" /> <span>give · share · repeat</span></div></div><div className="auth-card"><div className="auth-tabs"><button className={mode==='login'?'selected':''} onClick={()=>setMode('login')}>Увійти</button><button className={mode==='register'?'selected':''} onClick={()=>setMode('register')}>Реєстрація</button></div><h2>{mode==='login'?'З поверненням!':'Створи акаунт'}</h2><p className="muted">{mode==='login'?'Увійди, щоб продовжити обмін.':'Це займе лише хвилину.'}</p><form onSubmit={submit} className="form-stack">{mode==='register'&&<label>Твоє ім’я<input required maxLength={200} value={name} onChange={e=>setName(e.target.value)} placeholder="Як до тебе звертатися?"/></label>}<label>Електронна пошта<input required type="email" value={email} onChange={e=>setEmail(e.target.value)} placeholder="name@example.com"/></label><label>Пароль<input required type="password" minLength={mode==='register'?12:1} maxLength={128} value={password} onChange={e=>setPassword(e.target.value)} placeholder={mode==='register'?'Мінімум 12 символів':'Твій пароль'}/></label>{error&&<div className="form-error">{error}</div>}<button className="button button-primary button-full" disabled={busy}>{busy?'Зачекай…':mode==='login'?'Увійти в акаунт':'Зареєструватися'} <ArrowRight size={17}/></button></form><p className="auth-foot">Продовжуючи, ти погоджуєшся користуватися платформою відповідально.</p></div></div>}
function listingStatusLabel(status:string) {
  const labels:Record<string,string> = {
    Draft:'Чернетка',
    Published:'Доступно',
    Reserved:'Зарезервовано',
    InExchange:'В обміні',
    Completed:'Завершено',
    Hidden:'Приховано',
    Rejected:'Відхилено'
  };

  return labels[status] || status;
}

function canManageListing(status:string) {
  return ['Draft','Published','Hidden','Rejected'].includes(status);
}

function ListingDetails({profile,notify}:{profile:Profile|null;notify:(s:string)=>void}) {
  const {id=''} = useParams();
  const [item,setItem] = useState<Listing|null>(null);
  const [imageUrls,setImageUrls] = useState<string[]>([]);
  const [error,setError] = useState('');
  const [busy,setBusy] = useState(false);
  const [exchangeOpen,setExchangeOpen] = useState(false);
  const navigate = useNavigate();

  useEffect(() => { setExchangeOpen(false); },[id,profile?.id]);

  useEffect(() => {
    let active = true;

    const load = async () => {
      try {
        let listing:Listing;

        try {
          listing = await api.listing(id);
        } catch (publicError) {
          if (!profile) throw publicError;
          listing = await api.mineById(id);
        }

        if (!active) return;
        setItem(listing);

        try {
          const images = await api.images(id);
          const urls = await Promise.all(images.map(async image => {
            try {
              return await api.imageBlob(id,image.id);
            } catch {
              return '';
            }
          }));

          if (active) setImageUrls(urls);
          else urls.filter(Boolean).forEach(URL.revokeObjectURL);
        } catch {
          if (active) setImageUrls([]);
        }
      } catch (e) {
        if (active) setError((e as Error).message);
      }
    };

    load();

    return () => {
      active = false;
    };
  },[id,profile]);

  useEffect(() => {
    return () => imageUrls.filter(Boolean).forEach(URL.revokeObjectURL);
  },[imageUrls]);

  const reserve = async () => {
    if (!profile) {
      navigate(authLink(`/listing/${id}`));
      return;
    }

    setBusy(true);
    setError('');

    try {
      await api.reserve(id);
      notify('Запит на резервування надіслано');
      const updated = await api.listing(id);
      setItem(updated);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };

  if (error && !item) {
    return (
      <div className="empty-state">
        <h2>Оголошення недоступне</h2>
        <p>{error}</p>
        <Link to="/" className="text-link">Повернутися до каталогу</Link>
      </div>
    );
  }

  if (!item) return <div className="loading">Завантажуємо оголошення…</div>;

  const own = profile?.id === item.ownerId;
  const editable = canManageListing(item.status);

  return (
    <section className="detail-layout">
      <div>
        <div className="detail-main-image">
          {imageUrls[0] ? (
            <img src={imageUrls[0]} alt={item.title}/>
          ) : (
            <Package size={48} aria-hidden="true" />
          )}
        </div>

        {imageUrls.filter(Boolean).length > 1 && (
          <div className="detail-thumbs">
            {imageUrls.slice(1).filter(Boolean).map((url,index) => (
              <img key={index} src={url} alt={`${item.title} ${index+2}`}/>
            ))}
          </div>
        )}
      </div>

      <div className="detail-content">
        <Link to="/" className="back-link">← Назад до каталогу</Link>
        <span className="listing-category">{categoryLabel(item.category)}</span>
        <h1>{item.title}</h1>

        <div className="detail-price">
          Безкоштовно <span>·</span> {item.condition}
        </div>

        <div className="detail-divider"/>
        <h3>Про цю річ</h3>
        <p className="detail-description">{item.description}</p>

        <div className="detail-meta">
          <span>Статус</span>
          <b>{listingStatusLabel(item.status)}</b>
        </div>

        {item.moderationReason && own && (
          <div className="form-error">
            Причина модерації: {item.moderationReason}
          </div>
        )}

        {error && <div className="form-error">{error}</div>}

        {own ? (
          editable ? (
            <Link
              to={`/edit/${item.id}`}
              className="button button-primary button-full"
            >
              Редагувати оголошення
            </Link>
          ) : (
            <p className="muted">
              Для цього статусу редагування недоступне.
            </p>
          )
        ) : (
          <>
            <div className="listing-actions">
              <button
                disabled={busy || exchangeOpen || item.status !== 'Published'}
                onClick={reserve}
                className="button button-primary button-full"
              >
                {busy ? 'Надсилаємо…' : 'Зарезервувати річ'}
                <ArrowRight size={17}/>
              </button>
              <button
                disabled={busy || exchangeOpen || item.status !== 'Published'}
                onClick={() => {
                  if (!profile) { navigate(authLink(`/listing/${id}`)); return; }
                  setError('');
                  setExchangeOpen(true);
                }}
                className="button button-ghost button-full"
              >
                Запропонувати обмін
              </button>
            </div>
            {exchangeOpen && profile && item.status === 'Published' && (
              <ExchangeOfferForm
                key={id}
                listingId={id}
                userId={profile.id}
                onClose={() => setExchangeOpen(false)}
                onCreated={() => notify('Пропозицію обміну надіслано')}
                onFailed={async message => {
                  try {
                    const updated = await api.listing(id);
                    setItem(updated);
                    if (updated.status !== 'Published') setError(message);
                  } catch {
                    setItem(null);
                    setError(message);
                  }
                }}
              />
            )}
          </>
        )}

        <p className="detail-hint">
          Домовляйся про передачу речей безпечно та відповідально.
        </p>
      </div>
    </section>
  );
}


function ListingForm({notify}:{notify:(s:string)=>void}) {
  const {id} = useParams();
  const navigate = useNavigate();

  const [form,setForm] = useState({
    title:'',
    description:'',
    category:'Books',
    condition:'Добрий стан'
  });

  const [listingId,setListingId] = useState(id || '');
  const [status,setStatus] = useState('Draft');
  const [files,setFiles] = useState<File[]>([]);
  const [existingImages,setExistingImages] = useState<{id:string;url:string}[]>([]);
  const [error,setError] = useState('');
  const [busy,setBusy] = useState(false);
  const [loading,setLoading] = useState(Boolean(id));
  const [loadFailed,setLoadFailed] = useState(false);

  const locked = ['Reserved','InExchange','Completed'].includes(status);

  useEffect(() => {
    if (!id) {
      setLoading(false);
      return;
    }

    let active = true;
    setLoading(true);
    setLoadFailed(false);
    setError('');

    const load = async () => {
      try {
        const listing = await api.mineById(id);
        const images = await api.images(id);

        const loadedImages = await Promise.all(
          images.map(async image => {
            try {
              return {
                id:image.id,
                url:await api.imageBlob(id,image.id)
              };
            } catch {
              return {id:image.id,url:''};
            }
          })
        );

        if (!active) {
          loadedImages.forEach(image => {
            if (image.url) URL.revokeObjectURL(image.url);
          });
          return;
        }

        setListingId(id);
        setStatus(listing.status);
        setForm({
          title:listing.title,
          description:listing.description,
          category:listing.category,
          condition:listing.condition
        });
        setExistingImages(loadedImages);
      } catch (e) {
        if (active) {
          setError((e as Error).message);
          setLoadFailed(true);
        }
      } finally {
        if (active) setLoading(false);
      }
    };

    load();

    return () => {
      active = false;
    };
  },[id]);

  useEffect(() => {
    return () => {
      existingImages.forEach(image => {
        if (image.url) URL.revokeObjectURL(image.url);
      });
    };
  },[existingImages]);

  const refreshImages = async (currentId:string) => {
    const images = await api.images(currentId);

    const loaded = await Promise.all(
      images.map(async image => {
        try {
          return {
            id:image.id,
            url:await api.imageBlob(currentId,image.id)
          };
        } catch {
          return {id:image.id,url:''};
        }
      })
    );

    setExistingImages(loaded);
  };

  const selectFiles = (e:React.ChangeEvent<HTMLInputElement>) => {
    const selected = Array.from(e.target.files || []);
    e.target.value = '';

    if (!selected.length) return;

    if (existingImages.length + files.length + selected.length > 5) {
      setError('Можна додати максимум 5 фотографій загалом.');
      return;
    }

    const invalid = selected.find(file =>
      !['image/jpeg','image/png','image/webp'].includes(file.type) ||
      file.size > 5 * 1024 * 1024 ||
      file.size === 0
    );

    if (invalid) {
      setError('Дозволені JPEG, PNG або WebP розміром до 5 МБ кожна.');
      return;
    }

    setError('');
    setFiles(prev => [...prev,...selected]);
  };

  const removeImage = async (imageId:string) => {
    if (!listingId || busy) return;

    setBusy(true);
    setError('');

    try {
      await api.deleteImage(listingId,imageId);
      await refreshImages(listingId);
      notify('Фотографію видалено');
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const save = async (publishAfter:boolean) => {
    if (busy || loading || loadFailed || locked) return;
    if (publishAfter && status !== 'Draft') return;

    setBusy(true);
    setError('');

    const wasNew = !listingId;

    try {
      let currentId = listingId;

      if (!currentId) {
        const created = await api.createListing(form);
        currentId = created.id;

        setListingId(currentId);
        setStatus('Draft');

      } else {
        await api.updateListing(currentId,form);
      }

      for (const file of files) {
        await api.uploadImage(currentId,file);
        setFiles(prev => prev.filter(item => item !== file));
        await refreshImages(currentId);
      }

      if (publishAfter) {
        await api.publish(currentId);
        setStatus('Published');
        notify('Оголошення опубліковано');
        navigate('/account');
      } else {
        notify(wasNew ? 'Чернетку збережено' : 'Зміни збережено');
        if (!id) navigate(`/edit/${currentId}`,{replace:true});
      }
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="form-page">
      <div className="eyebrow">ПОДІЛИСЯ ЗІ СПІЛЬНОТОЮ</div>
      <h1>{id ? 'Редагувати оголошення' : 'Додати нову річ'}</h1>
      <p className="muted">Опиши річ — і дозволь їй знайти нового власника.</p>

      {loading ? <p className="muted" role="status">Завантаження оголошення...</p> : loadFailed ? (
        <div className="form-error" role="alert">{error} <Link to="/account" className="text-link">Повернутися до кабінету</Link></div>
      ) : <>

      {locked && (
        <div className="form-error">
          Це оголошення має статус «{status}» і більше не доступне для редагування.
        </div>
      )}

      <form
        onSubmit={e => {
          e.preventDefault();
          void save(false);
        }}
        className="editor-card form-stack"
      >
        <label>
          Назва речі
          <input
            required
            minLength={3}
            maxLength={100}
            value={form.title}
            onChange={e => setForm({...form,title:e.target.value})}
            placeholder="Наприклад, Навушники Sony"
            disabled={busy || loading || loadFailed || locked}
          />
        </label>

        <label>
          Опис
          <textarea
            required
            maxLength={2000}
            rows={5}
            value={form.description}
            onChange={e => setForm({...form,description:e.target.value})}
            placeholder="Розкажи про стан, особливості та все, що варто знати..."
            disabled={busy || loading || loadFailed || locked}
          />
        </label>

        <div className="form-row">
          <label>
            Категорія
            <select
              value={form.category}
              onChange={e => setForm({...form,category:e.target.value})}
              disabled={busy || loading || loadFailed || locked}
            >
              {['Books','Electronics','Clothing','Home','Sports','Kids','Other'].map(x =>
                <option key={x} value={x}>{categoryLabel(x)}</option>
              )}
            </select>
          </label>

          <label>
            Стан речі
            <input
              required
              maxLength={100}
              value={form.condition}
              onChange={e => setForm({...form,condition:e.target.value})}
              placeholder="Добрий стан"
              disabled={busy || loading || loadFailed || locked}
            />
          </label>
        </div>

        <div className="form-stack">
          <label>
            Фотографії
            <span className="muted inline-note">
              JPEG, PNG або WebP · до 5 МБ кожна · максимум 5 фото
            </span>
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp"
              multiple
              onChange={selectFiles}
              disabled={busy || loading || loadFailed || locked}
            />
          </label>

          {existingImages.length > 0 && (
            <>
              <p className="muted">Завантажені фотографії:</p>
              <div className="form-row">
                {existingImages.map(image => (
                  <div key={image.id}>
                    {image.url ? (
                      <img
                        src={image.url}
                        alt="Фотографія оголошення"
                        style={{width:120,height:100,objectFit:'cover',borderRadius:10}}
                      />
                    ) : (
                      <div className="muted">Фото недоступне</div>
                    )}
                    <button
                      type="button"
                      className="text-link"
                      disabled={busy || locked}
                      onClick={() => void removeImage(image.id)}
                    >
                      Видалити
                    </button>
                  </div>
                ))}
              </div>
            </>
          )}

          {files.length > 0 && (
            <div>
              <p className="muted">Нові фотографії:</p>
              {files.map((file,index) => (
                <div key={`${file.name}-${index}`} className="muted">
                  {file.name}
                  <button
                    type="button"
                    className="text-link"
                    disabled={busy}
                    onClick={() => setFiles(prev => prev.filter((_,i) => i !== index))}
                  >
                    Видалити
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>

        {error && <div className="form-error">{error}</div>}

        <div className="form-actions">
          <button
            type="button"
            className="button button-ghost"
            onClick={() => navigate(-1)}
            disabled={busy}
          >
            Скасувати
          </button>

          <button
            type="submit"
            className="button button-ghost"
            disabled={busy || loading || loadFailed || locked}
          >
            {busy ? 'Збереження...' : id ? 'Зберегти зміни' : 'Зберегти чернетку'}
          </button>

          {(status === 'Draft') && (
            <button
              type="button"
              className="button button-primary"
              disabled={busy || loading || loadFailed || locked}
              onClick={e => {
                if (e.currentTarget.form?.reportValidity()) {
                  void save(true);
                }
              }}
            >
              {busy ? 'Обробка...' : 'Опублікувати'}
              <ArrowRight size={17}/>
            </button>
          )}
        </div>
      </form>
      </>}
    </section>
  );
}

function Account({profile,setProfile,notify}:{profile:Profile|null;setProfile:(p:Profile)=>void;notify:(s:string)=>void}) {
  const [items,setItems] = useState<Listing[]>([]);
  const [name,setName] = useState(profile?.name || '');
  const [error,setError] = useState('');
  const navigate = useNavigate();

  useEffect(() => {
    if (!profile) {
      navigate('/auth');
      return;
    }

    api.mine()
      .then(p => setItems(p.items))
      .catch(e => setError(e.message));
  },[profile,navigate]);

  if (!profile) return null;

  const save = async (e:FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      const p = await api.updateProfile(name);
      setProfile(p);
      notify('Профіль оновлено');
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const remove = async (id:string) => {
    if (!confirm('Видалити це оголошення?')) return;

    setError('');

    try {
      await api.deleteListing(id);
      setItems(prev => prev.filter(x => x.id !== id));
      notify('Оголошення видалено');
    } catch (e) {
      setError((e as Error).message);
    }
  };

  return (
    <section className="account-page">
      <div className="account-heading">
        <div>
          <div className="eyebrow">ТВІЙ ПРОСТІР</div>
          <h1>Особистий кабінет</h1>
          <p className="muted">Керуйте профілем та своїми оголошеннями.</p>
        </div>

        <Link to="/create" className="button button-primary">
          <PackagePlus size={17}/> Додати річ
        </Link>
      </div>

      <div className="account-grid">
        <aside className="profile-card">
          <div className="profile-avatar">
            {profile.name.slice(0,1).toUpperCase()}
          </div>

          <h2>{profile.name}</h2>
          <p>{profile.email}</p>

          <span className="role-chip">
            <CircleUserRound size={14}/>
            {profile.role === 'Admin' ? 'Адміністратор' : 'Учасник'}
          </span>

          <form onSubmit={save} className="form-stack profile-form">
            <label>
              Ім’я
              <input
                value={name}
                maxLength={200}
                onChange={e => setName(e.target.value)}
              />
            </label>

            <button className="button button-dark button-full">
              Зберегти зміни
            </button>
          </form>
        </aside>

        <div className="my-listings">
          <div className="subheading">
            <h2>Мої оголошення</h2>
            <span>{items.length}</span>
          </div>

          {error && <div className="form-error">{error}</div>}

          {!items.length ? (
            <div className="empty-state compact">
              <div className="empty-emoji"><Package size={48} aria-hidden="true" /></div>
              <h3>Тут поки немає оголошень</h3>
              <p>Додай першу річ, якою хочеш поділитися.</p>
            </div>
          ) : (
            items.map(x => {
              const editable = canManageListing(x.status);

              return (
                <div className="my-item" key={x.id}>
                  <div className="my-item-icon"><Package size={24} aria-hidden="true" /></div>

                  <div className="my-item-info">
                    <b>{x.title}</b>
                    <small>{x.category} · {x.condition}</small>
                  </div>

                  <span className="status-chip">
                    {listingStatusLabel(x.status)}
                  </span>

                  <Link to={`/listing/${x.id}`} className="text-link my-item-view">
                    Переглянути
                  </Link>

                  {editable && (
                    <>
                      <Link to={`/edit/${x.id}`} className="text-link my-item-edit">
                        Редагувати
                      </Link>

                      <button
                        className="icon-button danger my-item-delete"
                        onClick={() => void remove(x.id)}
                        title="Видалити оголошення"
                      >
                        <X size={17}/>
                      </button>
                    </>
                  )}
                </div>
              );
            })
          )}
        </div>
      </div>
    </section>
  );
}
function Requests({notify}:{notify:(s:string)=>void}) {
  const [params] = useSearchParams();
  const [tab,setTab] = useState<'received'|'sent'>(params.get('tab') === 'sent' ? 'sent' : 'received');
  const [reservations,setReservations] = useState<any[]>([]);
  const [exchanges,setExchanges] = useState<ExchangeListItem[]>([]);
  const [error,setError] = useState('');
  const [loading,setLoading] = useState(true);
  const [busyId,setBusyId] = useState('');
  const navigate = useNavigate();

  const load = async (background = false) => {
    if (!background) setLoading(true);
    setError('');

    try {
      const [receivedReservations,sentReservations,receivedExchanges,sentExchanges] =
        await Promise.all([
          api.receivedReservations(),
          api.sentReservations(),
          api.receivedExchanges(),
          api.sentExchanges()
        ]);

      setReservations(tab === 'received' ? receivedReservations : sentReservations);
      setExchanges(tab === 'received' ? receivedExchanges : sentExchanges);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      if (!background) setLoading(false);
    }
  };

  useEffect(() => {
    if (!getToken()) {
      navigate('/auth');
      return;
    }
    void load();
  },[tab]);

  const act = async (
    id:string,
    action:'accept'|'reject'|'cancel'|'complete',
    type:'reservation'|'exchange'
  ) => {
    if (busyId) return;
    setBusyId(id);
    setError('');

    try {
      if (type === 'reservation') {
        if (action === 'complete') return;
        await api.reservationAction(id,action);
      } else {
        await api.exchangeAction(id,action);
      }

      await load(true);
      notify(
        action === 'complete'
          ? 'Обмін завершено'
          : 'Статус запиту оновлено'
      );
    } catch (e) {
      await load(true);
      setError((e as Error).message);
    } finally {
      setBusyId('');
    }
  };

  if (!getToken()) return null;

  return (
    <section className="requests-page">
      <div className="eyebrow">ВЗАЄМОДІЯ</div>
      <h1>Мої запити</h1>
      <p className="muted">Стеж за резервуваннями та пропозиціями обміну.</p>

      <div className="request-tabs">
        <button
          className={tab === 'received' ? 'active' : ''}
          disabled={!!busyId}
          onClick={() => setTab('received')}
        >
          Отримані
        </button>
        <button
          className={tab === 'sent' ? 'active' : ''}
          disabled={!!busyId}
          onClick={() => setTab('sent')}
        >
          Надіслані
        </button>
      </div>

      {error && <div className="form-error">{error}</div>}

      {loading ? (
        <div className="loading">Завантажуємо запити…</div>
      ) : (
        <div className="request-columns">
          <div>
            <h2>Резервування</h2>
            {reservations.length ? reservations.map((r:any) => (
              <RequestCard
                key={r.id}
                data={r}
                type="reservation"
                tab={tab}
                busy={busyId === r.id}
                blocked={!!busyId}
                onAction={action => act(r.id,action,'reservation')}
              />
            )) : (
              <p className="muted">Запитів на резервування поки немає.</p>
            )}
          </div>

          <div>
            <h2>Обмін</h2>
            {exchanges.length ? exchanges.map(r => (
              <RequestCard
                key={r.id}
                data={r}
                type="exchange"
                tab={tab}
                busy={busyId === r.id}
                blocked={!!busyId}
                onAction={action => act(r.id,action,'exchange')}
              />
            )) : (
              <p className="muted">Пропозицій обміну поки немає.</p>
            )}
          </div>
        </div>
      )}
    </section>
  );
}

function RequestCard({
  data,
  type,
  tab,
  busy,
  blocked,
  onAction
}:{
  data:any;
  type:'reservation'|'exchange';
  tab:'received'|'sent';
  busy:boolean;
  blocked:boolean;
  onAction:(action:'accept'|'reject'|'cancel'|'complete')=>void;
}) {
  const pending = data.status === 'Pending';
  const accepted = data.status === 'Accepted';
  const canAccept = data.canAccept === true;

  const statusLabels:Record<string,string> = {
    Pending:'Очікує відповіді',
    Accepted:'Прийнято',
    Confirmed:'Підтверджено',
    Rejected:'Відхилено',
    Cancelled:'Скасовано',
    Completed:'Завершено'
  };

  return (
    <div className="request-card user-request-card" aria-busy={busy}>
      <b>{data.listing?.title || 'Оголошення недоступне'}</b>

      {type === 'exchange' && (
        <p>
          {tab === 'received' ? 'Пропонують натомість' : 'Пропоную натомість'}: {data.offeredListing?.title || 'Оголошення недоступне'}
        </p>
      )}

      {type === 'exchange' && data.listing && data.offeredListing && (
        <p className="muted">
          {tab === 'received' ? 'Твоя річ' : 'Бажана річ'}: {listingStatusLabel(data.listing.status)} · Запропонована річ: {listingStatusLabel(data.offeredListing.status)}
        </p>
      )}

      <p>
        {data.counterparty?.name || 'Користувач'} ·{' '}
        {statusLabels[data.status] || data.status}
      </p>

      {!data.listing && (
        <p className="muted">Оголошення більше недоступне.</p>
      )}

      {type === 'exchange' && !data.offeredListing && (
        <p className="muted">Запропоноване оголошення недоступне.</p>
      )}

      {type === 'exchange' && pending && tab === 'received' && !canAccept && (
        <p className="muted">Прийняття недоступне: одна з речей більше не доступна для обміну.</p>
      )}

      {pending && tab === 'received' && (
        <div className="request-actions">
          {canAccept && (
            <button
              disabled={blocked}
              onClick={() => onAction('accept')}
            >
              Прийняти
            </button>
          )}
          <button
            disabled={blocked}
            onClick={() => onAction('reject')}
          >
            Відхилити
          </button>
        </div>
      )}

      {pending && tab === 'sent' && (
        <button
          className="text-link"
          disabled={blocked}
          onClick={() => onAction('cancel')}
        >
          Скасувати запит
        </button>
      )}

      {type === 'exchange' && accepted && (
        <>
          <p className="muted">Завершуй обмін після фактичної передачі обох речей.</p>
          <div className="request-actions">
            <button
              disabled={blocked}
              onClick={() => onAction('complete')}
            >
              Завершити обмін
            </button>
          </div>
        </>
      )}

      {busy && <small className="muted">Обробляємо запит…</small>}
    </div>
  );
}
function ModerationListingCard({
  item,
  status,
  busyId,
  reason,
  moderate
}: {
  item: Listing;
  status: string;
  busyId: string;
  reason: string;
  moderate: (id: string, action: 'hide'|'reject'|'return-to-draft'|'remove') => void;
}) {
  const [photos, setPhotos] = useState<string[] | null>(null);
  const [reasonError, setReasonError] = useState(false);

  const runAction = (action: 'hide'|'reject'|'return-to-draft'|'remove') => {
    if (action !== 'return-to-draft' && !reason.trim()) {
      setReasonError(true);
      return;
    }
    setReasonError(false);
    moderate(item.id, action);
  };

  useEffect(() => {
    let cancelled = false;
    const urls: string[] = [];

    const loadPhotos = async () => {
      try {
        const images = await api.images(item.id);

        const loaded = await Promise.all(images.map(async image => {
          try {
            const url = await api.imageBlob(item.id, image.id);

            if (cancelled) {
              URL.revokeObjectURL(url);
              return null;
            }

            urls.push(url);
            return url;
          } catch {
            return null;
          }
        }));

        if (!cancelled) {
          setPhotos(loaded.filter((url): url is string => url !== null));
        }
      } catch {
        if (!cancelled) setPhotos([]);
      }
    };

    void loadPhotos();

    return () => {
      cancelled = true;
      urls.forEach(url => URL.revokeObjectURL(url));
    };
  }, [item.id]);

  return (
    <div className="request-card moderation-listing-card" key={item.id}>
      <b>{item.title}</b>

      <p>{item.category} · {item.condition}</p>

      <p>{item.description || 'Опис відсутній.'}</p>

      <p>Статус: {listingStatusLabel(item.status)}</p>

      {item.moderationReason && (
        <p className="muted">Причина: {item.moderationReason}</p>
      )}

      <div style={{display:'flex',gap:10,flexWrap:'wrap',margin:'12px 0'}}>
        {photos === null ? (
          <small className="muted">Завантаження фотографій...</small>
        ) : photos.length > 0 ? (
          photos.map((url,index) => (
            <img
              key={index}
              src={url}
              alt={`Фото оголошення ${index + 1}`}
              style={{
                width:110,
                height:110,
                objectFit:'cover',
                borderRadius:12
              }}
            />
          ))
        ) : (
          <small className="muted">Фотографій немає.</small>
        )}
      </div>

      <div className="request-actions">
        {status === 'Published' && (
          <>
            <button
              disabled={!!busyId}
              onClick={() => runAction('hide')}
            >
              Приховати
            </button>

            <button
              disabled={!!busyId}
              onClick={() => runAction('reject')}
            >
              Відхилити
            </button>
          </>
        )}

        {(status === 'Hidden' || status === 'Rejected') && (
          <button
            disabled={!!busyId}
            onClick={() => runAction('return-to-draft')}
          >
            Повернути в чернетки
          </button>
        )}

        <button
          className="text-link"
          disabled={!!busyId}
          onClick={() => runAction('remove')}
        >
          Видалити з причиною
        </button>
      </div>

      {reasonError && !reason.trim() && <p className="moderation-reason-error" role="alert">Вкажіть причину модерації в полі над оголошеннями.</p>}

      {busyId === item.id && (
        <small className="muted">Обробляємо…</small>
      )}
    </div>
  );
}

function Moderation({profile,notify}:{profile:Profile|null;notify:(s:string)=>void}) {
  const [items,setItems] = useState<Listing[]>([]);
  const [users,setUsers] = useState<Profile[]>([]);
  const [status,setStatus] = useState('Published');
  const [reason,setReason] = useState('');
  const [error,setError] = useState('');
  const [tab,setTab] = useState<'listings'|'users'>('listings');
  const [loading,setLoading] = useState(false);
  const [busyId,setBusyId] = useState('');
  const navigate = useNavigate();

  const base = (import.meta.env.VITE_API_BASE_URL || 'https://localhost:7258');

  const loadListings = async () => {
    const response = await fetch(
      `${base}/api/moderation/listings?status=${status}&page=1&pageSize=100`,
      {headers:{Authorization:`Bearer ${getToken()}`}}
    );
    if (!response.ok) throw new Error('Не вдалося завантажити оголошення');
    const data:ListingPage = await response.json();
    setItems(data.items);
  };

  const loadUsers = async () => {
    const response = await fetch(
      `${base}/api/moderation/users?page=1&pageSize=100`,
      {headers:{Authorization:`Bearer ${getToken()}`}}
    );
    if (!response.ok) throw new Error('Не вдалося завантажити користувачів');
    setUsers(await response.json());
  };

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      if (tab === 'listings') await loadListings();
      else await loadUsers();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (profile?.role !== 'Admin') {
      navigate('/');
      return;
    }
    void load();
  },[profile,tab,status]);

  const moderate = async (id:string,action:'hide'|'reject'|'return-to-draft'|'remove') => {
    if ((action === 'hide' || action === 'reject' || action === 'remove') && !reason.trim()) {
      setError('Вкажи причину для цієї дії.');
      return;
    }

    setBusyId(id);
    setError('');

    try {
      const response = await fetch(`${base}/api/moderation/listings/${id}/${action}`,{
        method:'POST',
        headers:{
          Authorization:`Bearer ${getToken()}`,
          'Content-Type':'application/json'
        },
        ...(action === 'return-to-draft' ? {} : {body:JSON.stringify({reason:reason.trim()})})
      });

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.title || 'Не вдалося виконати дію');
      }

      await loadListings();
      setReason('');
      notify('Дію модерації виконано');
    } catch (e) {
      setError((e as Error).message);
      try { await loadListings(); } catch {}
    } finally {
      setBusyId('');
    }
  };

  if (profile?.role !== 'Admin') return null;

  const labels:Record<string,string> = {
    Published:'Опубліковані',
    Hidden:'Приховані',
    Rejected:'Відхилені'
  };

  return (
    <section className="requests-page">
      <div className="eyebrow">АДМІНІСТРУВАННЯ</div>
      <h1><ShieldCheck size={30}/> Панель модерації</h1>
      <p className="muted">Керування користувачами та оголошеннями UniSwap.</p>

      <div className="category-pills">
        <button className={tab === 'listings' ? 'category-pill active' : 'category-pill'} onClick={() => setTab('listings')}>Оголошення</button>
        <button className={tab === 'users' ? 'category-pill active' : 'category-pill'} onClick={() => setTab('users')}>Користувачі</button>
      </div>

      {tab === 'listings' ? (
        <>
          <div className="category-pills">
            {Object.entries(labels).map(([value,label]) => (
              <button
                key={value}
                className={status === value ? 'category-pill active' : 'category-pill'}
                onClick={() => setStatus(value)}
              >
                {label}
              </button>
            ))}
          </div>

          <label className="reason-input">
            Причина модерації
            <input value={reason} onChange={e => setReason(e.target.value)} placeholder="Вкажи причину для приховування, відхилення або видалення"/>
          </label>
        </>
      ) : null}

      {error && <div className="form-error">{error}</div>}

      {loading ? (
        <div className="loading">Завантаження…</div>
      ) : tab === 'users' ? (
        <div className="moderation-users">
          <div className="moderation-users-heading"><h2>Користувачі</h2><span>Показано: {users.length}</span></div>
          {users.length ? (
            <div className="moderation-users-list">
              {users.map(user => (
                <div className="moderation-user" key={user.id}>
                  <div className="moderation-user-avatar" aria-hidden="true">{user.name?.trim().charAt(0).toUpperCase() || '?'}</div>
                  <div className="moderation-user-details"><strong>{user.name}</strong><span>{user.email}</span></div>
                  <span className={user.role === 'Admin' ? 'moderation-user-role admin' : 'moderation-user-role'}>{user.role === 'Admin' ? 'Адміністратор' : 'Користувач'}</span>
                </div>
              ))}
            </div>
          ) : <div className="empty-state">Користувачів поки немає.</div>}
        </div>
      ) : (
        <div className="request-columns">
          {items.map(item => (
  <ModerationListingCard
    key={item.id}
    item={item}
    status={status}
    busyId={busyId}
    reason={reason}
    moderate={moderate}
  />
))}

{!items.length && (
  <div className="empty-state">
    У цій категорії оголошень немає.
  </div>
)}
        </div>
      )}
    </section>
  );
}
