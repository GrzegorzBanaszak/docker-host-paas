# Plan frontendu w React, Vite i Tailwind

## Cel frontendu

Frontend ma zapewnic prosty, czytelny interfejs do uruchamiania i monitorowania procesu konteneryzacji repozytoriow GitHub. Ma byc lekki, szybki i skupiony na przeplywie zadanie -> status -> wynik.

## Proponowany stack

- `React`
- `Vite`
- `TypeScript`
- `Tailwind CSS`
- `React Router`
- `TanStack Query`
- `Zod`
- `React Hook Form`

## Glowny zakres MVP

Frontend w pierwszej wersji powinien obslugiwac:

- formularz tworzenia joba z URL repozytorium
- liste zadan
- widok szczegolow zadania
- podglad logow
- podglad wygenerowanych plikow
- podstawowa obsluge bledow i stanow ladowania

## Proponowana struktura aplikacji

```text
src/
  app/
  components/
  features/
    jobs/
    logs/
    files/
  pages/
  lib/
  hooks/
  types/
  styles/
```

## Glowne widoki

### 1. Dashboard

Widok startowy pokazujacy:

- formularz dodania repozytorium
- ostatnie zadania
- podstawowe statystyki, np. liczba sukcesow i bledow

### 2. Lista zadan

Tabela lub lista zawierajaca:

- identyfikator joba
- URL repozytorium
- wykryty stack
- status
- data utworzenia
- link do szczegolow

### 3. Szczegoly zadania

Widok powinien zawierac:

- status procesu
- kroki wykonania
- metadane repozytorium
- wynik detekcji stacku
- liste wygenerowanych plikow
- wynik builda

### 4. Podglad logow

Widok lub panel z:

- logami w porzadku chronologicznym
- oznaczeniem bledow i ostrzezen
- automatycznym odswiezaniem dla aktywnego joba

### 5. Podglad plikow

Widok pozwalajacy:

- otwierac wygenerowane pliki
- przegladac zawartosc `Dockerfile`, `.dockerignore` i `docker-compose.yml`
- kopiowac lub pobierac wynik

## Architektura frontendu

Frontend warto podzielic funkcjonalnie:

- `features/jobs` - tworzenie i listing jobow
- `features/logs` - logi i stream statusow
- `features/files` - podglad wygenerowanych plikow
- `components` - wspolne elementy UI
- `lib/api` - klient HTTP i konfiguracja zapytan

## Model komunikacji z backendem

Frontend powinien komunikowac sie z backendem przez REST API:

- `POST /api/jobs`
- `GET /api/jobs`
- `GET /api/jobs/{id}`
- `GET /api/jobs/{id}/logs`
- `GET /api/jobs/{id}/files`

W MVP wystarczy odswiezanie pollingiem co kilka sekund dla aktywnych jobow. WebSocket lub SSE mozna dodac pozniej.

## Walidacja i formularze

Formularz tworzenia joba powinien walidowac:

- czy URL repozytorium ma poprawny format
- czy opcjonalny branch nie jest pusty
- czy wymagane pola zostaly uzupelnione

Do tego dobrze nadaja sie:

- `React Hook Form`
- `Zod`

## Styl i UX

Interfejs powinien byc techniczny, czytelny i bez zbednego rozproszenia. W praktyce:

- jedna dominujaca siatka layoutu
- wyrazne rozroznienie statusow jobow
- czytelne logi w monospace
- szybki dostep do ostatnich zadan
- sensowne stany pustych danych, ladowania i bledow

Warto od razu przygotowac:

- badge dla statusow
- komponent terminal-like dla logow
- prosty viewer plikow tekstowych

## Proponowane komponenty

- `JobCreateForm`
- `JobList`
- `JobStatusBadge`
- `JobDetailsCard`
- `JobTimeline`
- `LogViewer`
- `GeneratedFilesList`
- `FilePreviewPanel`
- `PageHeader`
- `EmptyState`

## Zarzadzanie stanem

Do danych serwerowych:

- `TanStack Query`

Do lokalnego stanu UI:

- `useState`
- ewentualnie `useReducer` dla bardziej zlozonych formularzy lub viewerow

Nie ma potrzeby dokladac globalnego store na starcie.

## Routing

Minimalny zestaw tras:

- `/`
- `/jobs`
- `/jobs/:jobId`

Opcjonalnie:

- `/settings`
- `/about`

## Plan implementacji frontendu

### Etap 1. Fundament

- inicjalizacja projektu Vite z React i TypeScript
- konfiguracja Tailwind CSS
- konfiguracja routera
- konfiguracja klienta API

### Etap 2. Dashboard i tworzenie joba

- formularz dodania repozytorium
- obsluga walidacji
- wysylka do backendu
- komunikaty sukcesu i bledu

### Etap 3. Lista zadan

- pobieranie listy
- filtrowanie po statusie
- podstawowe sortowanie

### Etap 4. Szczegoly zadania

- odczyt statusu joba
- prezentacja wyniku detekcji
- prezentacja statusu builda

### Etap 5. Logi i pliki

- viewer logow
- auto-refresh
- viewer wygenerowanych plikow

### Etap 6. UX i dopracowanie

- skeletony i stany ladowania
- lepsza obsluga bledow
- dopracowanie responsywnosci

## Styl Tailwind

Warto od razu ustalic kilka zasad:

- osobne klasy dla statusow: success, running, failed, queued
- wspolna paleta dla paneli, tla i kodu
- uzycie utility classes + male komponenty zamiast rozbudowanego custom CSS
- przygotowanie podstawowych klas layoutu i spacingu

## Testy frontendu

Frontend MVP powinien miec:

- testy komponentow dla formularza i statusow
- testy integracyjne widokow z mockowanym API
- test walidacji formularza

Do tego dobrze pasuja:

- `Vitest`
- `React Testing Library`
- `MSW`

## Definicja gotowosci frontendu MVP

Frontend MVP jest gotowy, gdy:

- uzytkownik moze utworzyc job z poziomu przegladarki
- widzi liste i szczegoly zadan
- moze sledzic logi
- moze przegladac wygenerowane pliki
- interfejs dziala poprawnie na desktopie i mobile
