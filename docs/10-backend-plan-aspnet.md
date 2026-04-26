# Plan backendu w ASP.NET

## Status realizacji

- `[DONE]` - element zaimplementowany
- `[PARTIAL]` - element rozpoczęty, ale niekompletny
- `[TODO]` - element jeszcze niewykonany

## Cel backendu

Backend ma zarzadzac cyklem zycia zadania konteneryzacji, przechowywac stan procesu, udostepniac API dla frontendu oraz delegowac ciezkie operacje do workera. Powinien byc zaprojektowany tak, aby backend API pozostawal lekki, a dlugie operacje byly wykonywane asynchronicznie.

## Proponowany stack

- `ASP.NET Core Web API`
- `Entity Framework Core`
- `PostgreSQL`
- `Redis`
- `Hangfire` albo wlasny worker oparty o `BackgroundService`
- `Serilog`
- `FluentValidation`
- `Mapster` albo `AutoMapper`

## Podzial odpowiedzialnosci

Backend warto podzielic na warstwy:

- `API` - endpointy HTTP, autoryzacja, walidacja wejscia
- `Application` - use case'y i logika biznesowa
- `Domain` - encje, enumy, kontrakty i reguly
- `Infrastructure` - baza danych, GitHub client, storage, kolejka, Docker runner

## Proponowana struktura rozwiazania

```text
src/
  Dockerizer.sln
  Dockerizer.Api/
  Dockerizer.Application/
  Dockerizer.Domain/
  Dockerizer.Infrastructure/
  Dockerizer.Worker/
tests/
  Dockerizer.Api.Tests/
  Dockerizer.Application.Tests/
  Dockerizer.IntegrationTests/
```

## Kluczowe moduly backendu

### 1. Modul Jobs `[PARTIAL]`

Odpowiada za:

- tworzenie zadania
- pobieranie statusu
- liste zadan
- ponowne uruchomienie zadania
- anulowanie zadania

Aktualny stan:

- `[DONE]` `POST /api/jobs`
- `[DONE]` `GET /api/jobs`
- `[DONE]` `GET /api/jobs/{id}`
- `[DONE]` encja `Job` i podstawowe statusy
- `[DONE]` retry
- `[DONE]` cancel

### 2. Modul Repository Intake `[PARTIAL]`

Odpowiada za:

- walidacje URL repozytorium
- obsluge branchy i commitow
- obsluge repozytoriow publicznych i prywatnych
- przygotowanie danych dla workera

Aktualny stan:

- `[DONE]` podstawowa walidacja URL w API
- `[DONE]` przyjecie opcjonalnego brancha w modelu zadania
- `[PARTIAL]` klonowanie repozytorium
- `[PARTIAL]` podstawowa detekcja stacku po strukturze plikow
- `[TODO]` obsluga repozytoriow prywatnych
- `[TODO]` obsluga commit SHA

### 3. Modul Detection Engine `[PARTIAL]`

Odpowiada za:

- wykrycie stacku na podstawie plikow
- wykrycie typu aplikacji
- estymacje portu i komendy startowej
- rozpoznanie czy repo ma juz pliki Docker

Aktualny stan:

- `[DONE]` wykrycie podstawowego stacku na podstawie plikow
- `[DONE]` zapis wyniku do pola `DetectedStack`
- `[TODO]` wykrycie typu aplikacji
- `[TODO]` estymacja portu i komendy startowej
- `[TODO]` detekcja istniejacego `Dockerfile`

### 4. Modul Template Generator `[PARTIAL]`

Odpowiada za:

- dobor szablonu dla stacku
- podstawienie parametrow do `Dockerfile`
- generowanie `.dockerignore`
- opcjonalne generowanie `docker-compose.yml`

Aktualny stan:

- `[DONE]` podstawowy dobor szablonu na podstawie `DetectedStack`
- `[DONE]` generowanie `Dockerfile`
- `[DONE]` generowanie `.dockerignore`
- `[TODO]` generowanie `docker-compose.yml`
- `[TODO]` szablony produkcyjne i bardziej precyzyjne komendy startowe

### 5. Modul Build Runner `[PARTIAL]`

Odpowiada za:

- uruchomienie builda obrazu
- zbieranie logow builda
- timeout i limity czasu
- zwrot statusu sukces lub blad

Aktualny stan:

- `[DONE]` uruchomienie `docker build` przez worker
- `[DONE]` timeout builda
- `[PARTIAL]` zwrot statusu sukces lub blad przez status `Job`
- `[PARTIAL]` zapis tagu zbudowanego obrazu do `GeneratedImageTag`
- `[TODO]` trwale przechowywanie logow builda

### 6. Modul Artifact Storage `[PARTIAL]`

Odpowiada za:

- zapis wygenerowanych plikow
- przechowywanie logow
- metadane obrazu
- cleanup workspace

Aktualny stan:

- `[DONE]` zapis wygenerowanych plikow w workspace joba
- `[DONE]` podstawowe przechowywanie logow w pliku `job.log`
- `[PARTIAL]` metadane obrazu przez `GeneratedImageTag`
- `[PARTIAL]` cleanup workspace
- `[TODO]` osobny model artefaktow i logow w bazie

## Model domenowy

Przykladowe encje:

- `Job` `[DONE]`
- `RepositorySource` `[TODO]`
- `DetectionResult` `[TODO]`
- `GeneratedFile` `[TODO]`
- `BuildResult` `[TODO]`
- `JobLogEntry` `[TODO]`

Przykladowe statusy joba:

- `Queued` `[DONE]`
- `Running` `[DONE]`
- `Succeeded` `[DONE]`
- `Failed` `[DONE]`
- `Canceled` `[DONE]`

## Proponowane endpointy API

### Zadania

- `POST /api/jobs` `[DONE]`
- `GET /api/jobs` `[DONE]`
- `GET /api/jobs/{id}` `[DONE]`
- `POST /api/jobs/{id}/retry` `[DONE]`
- `POST /api/jobs/{id}/cancel` `[DONE]`

### Logi i pliki

- `GET /api/jobs/{id}/logs` `[DONE]`
- `GET /api/jobs/{id}/files` `[DONE]`
- `GET /api/jobs/{id}/files/{fileId}` `[DONE]`

### Metadane

- `GET /api/stacks` `[TODO]`
- `GET /api/health` `[DONE]`

## Przeplyw backendowy

1. API przyjmuje request z URL repozytorium. `[DONE]`
2. Backend waliduje dane i zapisuje `Job` w bazie. `[DONE]`
3. Backend wysyla job do kolejki. `[DONE]`
4. Worker pobiera job i przygotowuje workspace. `[DONE]`
5. Worker klonuje repozytorium. `[PARTIAL]`
6. Worker uruchamia detekcje stacku. `[PARTIAL]`
7. Worker generuje pliki konteneryzacji. `[PARTIAL]`
8. Worker uruchamia build. `[PARTIAL]`
9. Worker zapisuje wynik i aktualizuje status. `[PARTIAL]`

## Worker

Worker powinien byc osobna aplikacja `ASP.NET Core` albo `Worker Service`, uruchamiana w osobnym kontenerze. Jego zadaniem jest wykonanie dlugich i potencjalnie kosztownych operacji.

Aktualny stan:

- `[DONE]` utworzony projekt `Dockerizer.Worker`
- `[DONE]` podstawowy `BackgroundService`
- `[DONE]` kolejka
- `[PARTIAL]` rzeczywiste przetwarzanie jobow

Odpowiedzialnosci workera:

- pobieranie jobow z kolejki
- izolacja katalogow roboczych
- obsluga klonowania repozytorium
- wywolywanie detektora i generatora
- uruchamianie builda Dockera
- zapis logow krok po kroku

## Integracja z Docker

W pierwszej wersji backend nie powinien sam wykonywac buildow synchronicznie w requestach HTTP. Build powinien byc odpalany przez worker.

Mozliwe podejscia:

- wywolanie `docker build` przez proces systemowy
- integracja z Docker Engine API

Dla MVP praktyczniejsze bedzie:

- worker uruchamiany w kontenerze
- dostep do hostowego Dockera przez socket

To wymaga osobnego etapu hardeningu.

## Przechowywanie danych

### PostgreSQL

Do przechowywania:

- zadan `[DONE]`
- statusow `[DONE]`
- metadanych repozytoriow `[TODO]`
- wynikow detekcji `[TODO]`
- metadanych plikow `[TODO]`
- tagu zbudowanego obrazu `[DONE]`

### Storage na dysku

Do przechowywania:

- workspace per job `[DONE]`
- wygenerowanych plikow `[DONE]`
- logow `[DONE]`
- tymczasowych artefaktow `[PARTIAL]`

## Konfiguracja i sekrety

Konfiguracja powinna byc oparta o:

- `appsettings.json` `[DONE]`
- `appsettings.{Environment}.json` `[DONE]`
- zmienne srodowiskowe `[PARTIAL]`
- Docker secrets lub pliki montowane dla wrazliwych danych `[TODO]`

Przykladowe sekrety:

- token GitHub
- connection string do bazy
- hasla do Redis lub proxy

## Bezpieczenstwo backendu

- walidacja URL repozytorium
- ograniczenie dlugosci i rozmiaru jobow
- timeout na klonowanie oraz build
- limity rownoleglych jobow
- brak wykonywania niekontrolowanych polecen z requestu uzytkownika
- audyt logow i bledow

## Plan implementacji backendu

### Etap 1. Fundament `[PARTIAL]`

- utworzenie solution i projektow `[DONE]`
- konfiguracja EF Core i PostgreSQL `[DONE]`
- podstawowa konfiguracja logowania `[PARTIAL]`
- healthcheck `[DONE]`
- migracje EF Core `[DONE]`

Uwagi:

- aktualnie baza jest przygotowywana przez migracje EF Core
- `Serilog` nie jest jeszcze dodany

### Etap 2. Modul Jobs `[PARTIAL]`

- encje i migracje `[DONE]`
- endpoint tworzenia joba `[DONE]`
- endpoint listy i szczegolow `[DONE]`
- statusy zadania `[DONE]`

Uwagi:

- encje sa gotowe
- po utworzeniu joba backend dodaje identyfikator zadania do kolejki Redis

### Etap 3. Worker i kolejka `[PARTIAL]`

- kolejka Redis lub Hangfire `[DONE]`
- worker service `[DONE]`
- pobieranie i wykonywanie joba `[PARTIAL]`

### Etap 4. Detection Engine `[PARTIAL]`

- reguly wykrywania stacku `[PARTIAL]`
- analiza plikow repozytorium `[PARTIAL]`
- mapowanie na typy szablonow

### Etap 5. Generator plikow `[PARTIAL]`

- katalog szablonow `[PARTIAL]`
- podstawianie wartosci `[PARTIAL]`
- zapis wygenerowanych plikow `[DONE]`

### Etap 6. Build Runner `[PARTIAL]`

- uruchamianie builda `[DONE]`
- przechwytywanie logow `[PARTIAL]`
- timeout i obsluga bledow `[PARTIAL]`

### Etap 7. API rozszerzone `[PARTIAL]`

- retry i cancel `[DONE]`
- pobieranie logow `[DONE]`
- pobieranie wygenerowanych plikow `[DONE]`

### Etap 8. Hardening `[PARTIAL]`

- limity wspolbieznosci
- cleanup workspace `[PARTIAL]`
- lepsza obsluga bledow
- monitoring

## Testy

Backend powinien miec:

- testy jednostkowe dla reguly detekcji `[TODO]`
- testy jednostkowe dla generatora szablonow `[TODO]`
- testy integracyjne API `[TODO]`
- testy integracyjne z baza danych `[TODO]`
- testy end-to-end przeplywu joba na kontrolowanych fixture'ach repozytoriow `[TODO]`

## Definicja gotowosci backendu MVP

Backend MVP jest gotowy, gdy:

- przyjmuje job z URL repozytorium
- zapisuje i kolejkuje zadanie
- worker wykonuje detekcje i generacje plikow
- build testowy moze sie wykonac
- frontend moze pobrac status, logi i wynik
