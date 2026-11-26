import { useEffect, useId, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { Check, ChevronDown } from 'lucide-react';
import { api } from './api';
import type { Listing } from './api';

export default function ExchangeOfferForm({listingId,userId,onClose,onCreated,onFailed}:{
  listingId:string;
  userId:string;
  onClose:()=>void;
  onCreated:()=>void;
  onFailed:(message:string)=>Promise<void>;
}) {
  const [items,setItems] = useState<Listing[]>([]);
  const [offeredId,setOfferedId] = useState('');
  const [loading,setLoading] = useState(true);
  const [busy,setBusy] = useState(false);
  const [error,setError] = useState('');
  const [sent,setSent] = useState(false);
  const [retry,setRetry] = useState(0);

  useEffect(() => {
    let active = true;
    const load = async () => {
      setLoading(true);
      setItems([]);
      setError('');
      setOfferedId('');
      try {
        const listings:Listing[] = [];
        let page = 1;
        let result;
        do {
          result = await api.mine(page++);
          if (!active) return;
          listings.push(...result.items);
        } while (result.items.length && listings.length < result.totalCount);
        if (active) setItems(listings.filter(item => item.ownerId === userId && item.id !== listingId && item.status === 'Published'));
      } catch (e) {
        if (active) setError((e as Error).message);
      } finally {
        if (active) setLoading(false);
      }
    };
    void load();
    return () => { active = false; };
  },[listingId,userId,retry]);

  const submit = async (event:FormEvent) => {
    event.preventDefault();
    if (busy || sent) return;
    if (!items.some(item => item.id === offeredId)) {
      setError('Обери власну опубліковану річ для обміну.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      await api.createExchange(listingId,offeredId);
      setSent(true);
      onCreated();
    } catch (e) {
      const message = (e as Error).message;
      setError(message);
      await onFailed(message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <form className="form-stack exchange-offer-form" onSubmit={submit}>
      <h3>Пропозиція обміну</h3>
      {sent ? (
        <div role="status">
          <p>Пропозицію надіслано. Обидві речі залишаються доступними до її прийняття.</p>
          <Link to="/requests?tab=sent" className="text-link">Переглянути надіслані пропозиції</Link>
        </div>
      ) : (
        <>
          {loading ? <p className="muted" role="status">Завантажуємо твої оголошення…</p> : error ? (
            <>
              <div className="form-error" role="alert">{error}</div>
              <button type="button" className="text-link" disabled={busy} onClick={() => setRetry(value => value + 1)}>Оновити список речей</button>
            </>
          ) : !items.length ? (
            <p className="muted">Для обміну потрібне власне опубліковане оголошення. <Link to="/create" className="text-link">Додати річ</Link></p>
          ) : null}
          {!loading && items.length > 0 && (
            <>
              <ExchangeListingSelect items={items} value={offeredId} disabled={busy} onChange={setOfferedId}/>
              <p className="muted">Можна запропонувати лише опубліковану річ, яка ще не зарезервована й не перебуває в обміні.</p>
              <button className="button button-primary button-full" disabled={busy || !offeredId}>{busy ? 'Надсилаємо…' : 'Надіслати пропозицію'}</button>
            </>
          )}
        </>
      )}
      <button type="button" className="button button-ghost button-full" disabled={busy} onClick={onClose}>{sent ? 'Закрити' : 'Скасувати'}</button>
    </form>
  );
}

function ExchangeListingSelect({items,value,disabled,onChange}:{items:Listing[];value:string;disabled:boolean;onChange:(id:string)=>void}) {
  const id = useId();
  const root = useRef<HTMLDivElement>(null);
  const trigger = useRef<HTMLButtonElement>(null);
  const [open,setOpen] = useState(false);
  const [active,setActive] = useState(0);
  const selected = items.find(item => item.id === value);
  const choose = (index:number) => {
    onChange(items[index].id);
    setOpen(false);
    trigger.current?.focus();
  };

  useEffect(() => {
    const close = (event:PointerEvent) => {
      if (!root.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener('pointerdown',close);
    return () => document.removeEventListener('pointerdown',close);
  },[]);

  useEffect(() => {
    if (open) document.getElementById(`${id}-option-${active}`)?.scrollIntoView({block:'nearest'});
  },[open,active,id]);

  return (
    <div className="exchange-listing-field">
      <label id={`${id}-label`} htmlFor={id}>Твоя річ для обміну</label>
      <div className="catalog-select exchange-listing-select" ref={root} onBlur={event => {
        if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false);
      }}>
        <button
          ref={trigger}
          id={id}
          type="button"
          className="exchange-select-trigger"
          role="combobox"
          aria-labelledby={`${id}-label`}
          aria-controls={`${id}-options`}
          aria-expanded={open}
          aria-haspopup="listbox"
          aria-activedescendant={open ? `${id}-option-${active}` : undefined}
          disabled={disabled}
          onClick={() => { setActive(Math.max(0,items.findIndex(item => item.id === value))); setOpen(!open); }}
          onKeyDown={event => {
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
              event.preventDefault();
              setActive(open ? (active + (event.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length : Math.max(0,items.findIndex(item => item.id === value)));
              setOpen(true);
            } else if (event.key === 'Enter' || event.key === ' ') {
              event.preventDefault();
              if (open) choose(active);
              else { setActive(Math.max(0,items.findIndex(item => item.id === value))); setOpen(true); }
            } else if (event.key === 'Escape') {
              event.preventDefault();
              setOpen(false);
            } else if (event.key === 'Tab') setOpen(false);
            else if (open && (event.key === 'Home' || event.key === 'End')) {
              event.preventDefault();
              setActive(event.key === 'Home' ? 0 : items.length - 1);
            }
          }}
        >
          <span>{selected ? `${selected.title} · ${selected.condition}` : 'Обери оголошення'}</span>
          <ChevronDown size={16} aria-hidden="true"/>
        </button>
        {open && (
          <div id={`${id}-options`} className="catalog-select-menu" role="listbox" aria-labelledby={`${id}-label`}>
            {items.map((item,index) => (
              <button
                key={item.id}
                id={`${id}-option-${index}`}
                type="button"
                role="option"
                aria-selected={value === item.id}
                tabIndex={-1}
                className={index === active ? 'selected' : ''}
                onPointerMove={() => setActive(index)}
                onClick={() => choose(index)}
              >
                <span>{item.title} · {item.condition}</span>
                {value === item.id && <Check size={16} aria-hidden="true"/>}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
