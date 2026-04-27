# Analiza backendu: czy na obecnym etapie potrafi wystawic kontener z linku GitHub

## Wniosek

Na obecnym etapie backend jest w stanie:

- przyjac URL repozytorium GitHub
- zapisac job w bazie
- wrzucic job do kolejki
- sklonowac repozytorium
- wykryc podstawowy stack
- wygenerowac `Dockerfile` i `.dockerignore`, jesli ich brakuje
- uruchomic `docker build`
- zapisac tag zbudowanego obrazu

Backend nie jest jeszcze w stanie:

- uruchomic kontenera jako dzialajacej uslugi
- wystawic aplikacji pod portem lub URL
- zarzadzac zyciem kontenera po buildzie
- zwrocic linku do uruchomionej aplikacji

To oznacza, ze system obecnie realizuje **konteneryzacje i build obrazu**, ale jeszcze nie realizuje **wystawienia kontenera**.

## Co dziala w backendzie

### 1. API przyjmuje job z URL repozytorium

Endpoint `POST /api/jobs` przyjmuje `RepositoryUrl` i opcjonalny `Branch`, wykonuje podstawowa walidacje URI i tworzy job.

Plik:

- `src/Dockerizer.Api/Controllers/JobsController.cs`

### 2. Job trafia do kolejki i jest przetwarzany przez workera

Po zapisaniu joba backend wysyla jego identyfikator do kolejki Redis. Worker pobiera job i uruchamia pipeline przetwarzania.

Pliki:

- `src/Dockerizer.Infrastructure/Jobs/JobsService.cs`
- `src/Dockerizer.Infrastructure/Queue/RedisJobQueue.cs`
- `src/Dockerizer.Worker/Services/JobExecutionService.cs`

### 3. Repozytorium jest klonowane

Worker wywoluje `git clone --depth 1`, opcjonalnie z branchem.

Plik:

- `src/Dockerizer.Worker/Services/GitRepositoryCloner.cs`

Uwagi:

- dziala dla repozytoriow publicznych
- nie ma jeszcze obslugi repozytoriow prywatnych

### 4. Dziala podstawowa detekcja stacku

Detekcja opiera sie na obecnosci takich plikow jak:

- `package.json`
- `requirements.txt`
- `pyproject.toml`
- `composer.json`
- `go.mod`
- `pom.xml`
- `build.gradle`
- `*.csproj`
- `*.sln`

Plik:

- `src/Dockerizer.Worker/Services/RepositoryStackDetector.cs`

### 5. Backend generuje podstawowe pliki Dockera

Jesli w repozytorium nie ma `Dockerfile`, generator tworzy prosty szablon zaleznny od wykrytego stacku. Analogicznie generowany jest `.dockerignore`.

Plik:

- `src/Dockerizer.Worker/Services/ContainerizationTemplateGenerator.cs`

Uwagi:

- jezeli `Dockerfile` juz istnieje, generator go nie nadpisuje
- nie ma jeszcze generowania `docker-compose.yml`
- szablony sa podstawowe i nie analizuja rzeczywistego entrypointu projektu

### 6. Backend buduje obraz Dockera

Worker uruchamia:

```bash
docker build -t dockerizer:{jobId} "<repositoryPath>"
```

Plik:

- `src/Dockerizer.Worker/Services/DockerImageBuilder.cs`

Po sukcesie tag obrazu trafia do pola `GeneratedImageTag`.

Plik:

- `src/Dockerizer.Domain/Entities/Job.cs`

## Czego backend jeszcze nie robi

### 1. Nie uruchamia kontenera

W kodzie nie ma kroku:

- `docker run`
- `docker compose up`
- integracji z Docker Engine API do tworzenia kontenera

Pipeline konczy sie po `docker build` i zapisaniu tagu obrazu jako sukces joba.

Kluczowy plik:

- `src/Dockerizer.Worker/Services/JobExecutionService.cs`

### 2. Nie publikuje portu ani nie wystawia URL

Brakuje logiki:

- wykrycia realnego portu aplikacji
- mapowania portu hosta do kontenera
- przechowywania informacji o uruchomionym kontenerze
- budowania publicznego URL
- integracji z reverse proxy

Obecnie model `Job` przechowuje tylko:

- URL repozytorium
- branch
- status
- wykryty stack
- tag zbudowanego obrazu
- blad
- znaczniki czasu

Nie ma tam danych takich jak:

- `ContainerId`
- `PublishedPort`
- `DeploymentUrl`
- `RuntimeStatus`

### 3. Nie ma lifecycle management dla uruchomionej uslugi

Brakuje obslugi:

- startu kontenera
- restartu kontenera
- stopu i usuwania kontenera
- cleanupu po nieudanym wdrozeniu
- ponownego wdrozenia nowej wersji

### 4. Nie ma jeszcze przeplywu "build + deploy"

Aktualny przeplyw to:

1. create job
2. clone repo
3. detect stack
4. generate Docker files
5. build image
6. mark job as succeeded

Brakujacy etap:

7. utworzenie i uruchomienie kontenera
8. healthcheck runtime
9. zwrocenie adresu do uslugi

## Ograniczenia obecnej implementacji

### 1. Tylko podstawowa walidacja URL

API sprawdza jedynie, czy `RepositoryUrl` jest poprawnym absolutnym URI.

To nie oznacza jeszcze:

- ze to jest repozytorium GitHub
- ze repo istnieje
- ze branch istnieje
- ze worker ma dostep do repo

### 2. Brak obslugi repozytoriow prywatnych

To jest juz zaznaczone w planie backendu jako `TODO`.

### 3. Brak wykrywania realnej komendy startowej i portu

Generator ma wpisane proste domyslne komendy, np.:

- `npm start`
- `python app.py`
- `php -S 0.0.0.0:8000 -t public`

To wystarczy tylko dla czesci projektow. Dla wielu realnych repozytoriow build moze przejsc, ale kontener i tak nie uruchomi aplikacji poprawnie.

### 4. Brak trwalych metadanych wdrozenia

System zapisuje tylko tag obrazu, bez informacji o:

- wyniku uruchomienia kontenera
- logach runtime
- endpointach sieciowych
- stanie procesu po starcie

## Ryzyka konfiguracyjne w obecnym repo

### 1. `infra/docker-compose.yml` stawia tylko infrastrukture pomocnicza

Compose uruchamia:

- `postgres`
- `redis`

Nie uruchamia:

- `Dockerizer.Api`
- `Dockerizer.Worker`

To znaczy, ze sama platforma backendowa nie jest jeszcze spieta gotowym lokalnym stackiem uruchomieniowym.

### 2. Niespojnosc danych dostepowych do Postgresa

W `infra/docker-compose.yml` Postgres jest tworzony jako:

- user: `postgres`
- password: `postgres`

Natomiast `src/Dockerizer.Api/appsettings.json` i `src/Dockerizer.Worker/appsettings.json` wskazuja:

- user: `dockerizer`
- password: `dockerizer`

Bez uzgodnienia tej konfiguracji API i worker moga nie wystartowac poprawnie po polaczeniu z baza.

## Zgodnosc z dokumentacja projektu

Plik `docs/10-backend-plan-aspnet.md` sam opisuje obecny stan jako:

- `Repository Intake` - `PARTIAL`
- `Detection Engine` - `PARTIAL`
- `Template Generator` - `PARTIAL`
- `Build Runner` - `PARTIAL`

Szczegolnie istotne sa wpisy:

- obsluga repozytoriow prywatnych - `TODO`
- estymacja portu i komendy startowej - `TODO`
- generowanie `docker-compose.yml` - `TODO`
- zapis metadanych obrazu - `PARTIAL`

To potwierdza, ze projekt jest aktualnie na etapie **przygotowania i budowy obrazu**, a nie pelnego **deployu uruchomionego kontenera**.

## Ostateczna odpowiedz

Na obecnym etapie backend jest w stanie **zbudowac obraz Dockera z repozytorium GitHub**, ale **nie jest jeszcze w stanie wystawic gotowego kontenera jako dzialajacej uslugi**.

Najblizsza poprawna formulacja aktualnego stanu brzmi:

> backend potrafi wykonac pipeline: GitHub URL -> clone -> analiza -> generowanie plikow -> docker build

Nie potrafi jeszcze wykonac pipeline:

> GitHub URL -> clone -> build -> uruchomienie kontenera -> publikacja endpointu

## Co trzeba dopisac, zeby mozna bylo mowic o "wystawieniu kontenera"

Minimalny zakres brakujacych funkcji:

1. modul uruchamiania kontenera po udanym buildzie
2. zapis `ContainerId`, portu i statusu runtime
3. mechanizm przydzialu portu lub integracja z reverse proxy
4. healthcheck po starcie kontenera
5. endpoint API zwracajacy adres uruchomionej uslugi
6. obsluga zatrzymania, restartu i cleanupu kontenerow
7. lepsza detekcja komendy startowej i portu

