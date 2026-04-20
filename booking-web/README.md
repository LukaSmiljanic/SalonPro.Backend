# SalonPro — javno online zakazivanje

Odvojen od `client-app` (admin). Istovetno se kači na **isti** SalonPro API.

## Pokretanje (razvoj)

1. Podigni API (npr. `SalonPro.API` na `http://localhost:5000` — proveri `launchSettings.json`).
2. Ovde: `npm install` → `npm run dev` (podrazumevano port **5174**, proxy šalje `/api` na 5000).
3. Otvori npr. `http://localhost:5174/demo-salon` — `slug` mora postojati na tenantu u bazi (seed: `demo-salon`).

Opciono: kopiraj `.env.example` u `.env` i postavi `VITE_API_BASE_URL` ako API nije na istom hostu (npr. produkcija).

## Produkcija

- `npm run build` → statički `dist/` (Netlify, Cloudflare Pages, S3+CDN, itd.).
- `VITE_API_BASE_URL` mora pokazivati na javni URL API-ja (HTTPS).
- API već dozvoljava CORS sa bilo kog porekla; za strožiju politiku ograniči u `Program.cs` na domen tvog booking sajta.

## API ugovor (backend)

| Metoda | Putanja |
|--------|---------|
| GET | `/api/public/booking/{slug}` — podaci o salonu |
| GET | `/api/public/booking/{slug}/services` |
| GET | `/api/public/booking/{slug}/staff` |
| POST | `/api/public/booking/{slug}/appointments` — telo: ime, prezime, telefon, email?, staffMemberId, serviceIds[], startTime (ISO), notes? |

Salon je dostupan samo ako je tenant aktivan i pretplata važi.

## Napomene (MVP)

- Nema automatskog predlaganja slobodnih slotova — korisnik bira datum/vreme; validacija je radno vreme + pravila iz `CreateAppointment`.
- Za ozbiljniji produkt: endpoint za slobodne termine, CAPTCHA na POST, rate limit.

## Ko je odgovoran

Operativno i pravno (UGOVOR, obrada ličnih podataka krajnjeg korisnika) definišeš ti kao provajder prema salonima; ovo je tehnički samo klijent API-ja.
