# Ogolny opis aplikacji

## Cel

Celem aplikacji jest automatyzacja procesu konteneryzacji projektow z GitHub. Uzytkownik podaje adres repozytorium, a system analizuje kod, wykrywa stos technologiczny, generuje pliki potrzebne do uruchomienia aplikacji w Dockerze, testuje build obrazu i przygotowuje wynik do dalszego wdrozenia.

Docelowo cala platforma ma dzialac jako wlasny zestaw uslug uruchamiany przez `docker compose` na serwerze Ubuntu hostowanym na Proxmox.

## Problem do rozwiazania

Wiele repozytoriow nie posiada gotowej konfiguracji Dockera albo posiada ja w formie niepelnej, nieaktualnej lub niespojne z obecnym sposobem wdrozenia. Reczne przygotowanie `Dockerfile`, `.dockerignore` i konfiguracji `docker compose` jest czasochlonne i wymaga znajomosci konkretnego stacku.

Aplikacja ma ten proces uproscic przez:

- pobranie repozytorium z GitHub
- wykrycie technologii i typu aplikacji
- wygenerowanie konfiguracji konteneryzacji
- uruchomienie testowego builda obrazu
- prezentacje wyniku oraz logow
- opcjonalne przygotowanie zmian do zapisania w repozytorium

## Zakres MVP

Pierwsza wersja systemu powinna obslugiwac podstawowy, kontrolowany przeplyw:

1. Uzytkownik podaje URL repozytorium GitHub.
2. System tworzy zadanie przetwarzania.
3. Worker klonuje repozytorium do odizolowanego katalogu roboczego.
4. Silnik detekcji okresla technologie na podstawie plikow projektu.
5. Generator wybiera odpowiedni szablon i tworzy pliki konteneryzacji.
6. System uruchamia build testowy obrazu Docker.
7. Uzytkownik otrzymuje status, logi i wygenerowane pliki.

## Zakres technologiczny MVP

Na start warto obsluzyc kilka najczestszych typow repozytoriow:

- Node.js
- Python
- .NET
- PHP
- aplikacje statyczne serwowane przez Nginx

Rozszerzenia na Java, Go lub bardziej niestandardowe struktury powinny wejsc po ustabilizowaniu MVP.

## Architektura wysokiego poziomu

Platforma powinna byc podzielona na kilka logicznych modulow:

- frontend webowy do tworzenia i podgladu zadan
- backend API odpowiedzialny za logike biznesowa
- worker wykonujacy klonowanie, analize i build
- baza danych do przechowywania zadan i metadanych
- Redis jako kolejka lub magazyn stanu dla zadan asynchronicznych
- wolumen plikowy na workspace, logi i artefakty

## Glowny przeplyw danych

1. Frontend wysyla zadanie do backendu.
2. Backend zapisuje rekord zadania w bazie i przekazuje je do workera.
3. Worker klonuje repozytorium i analizuje jego zawartosc.
4. Worker generuje pliki i uruchamia testowy build obrazu.
5. Worker zapisuje wynik, logi i metadane.
6. Frontend odczytuje status i prezentuje rezultat uzytkownikowi.

## Wymagania niefunkcjonalne

- system musi dzialac w kontenerach
- zadania musza byc asynchroniczne
- kazde zadanie musi miec osobny workspace
- build musi miec timeout oraz limity zasobow
- aplikacja musi przechowywac logi i historie zadan
- system powinien byc gotowy do uruchomienia przez `docker compose`

## Bezpieczenstwo

Najwiekszym ryzykiem jest uruchamianie obcego kodu z repozytoriow GitHub. Dlatego od poczatku trzeba zalozyc:

- ograniczenia CPU i RAM dla workerow
- timeout dla klonowania i buildow
- cleanup danych po wykonaniu zadania
- brak dostepu do wrazliwych danych hosta
- logowanie dzialan administracyjnych i bledow
- mozliwosc blokowania wybranych repozytoriow lub hostow

## Srodowisko docelowe

Srodowisko docelowe powinno wygladac nastepujaco:

- Proxmox jako warstwa wirtualizacji
- Ubuntu Server jako system goscia
- Docker Engine i Docker Compose plugin
- reverse proxy, np. Nginx lub Traefik
- osobne wolumeny dla bazy, logow i workspace

## Etapy rozwoju

1. Projekt architektury i modeli danych
2. MVP backendu i workera
3. MVP frontendu
4. Integracja z Docker build
5. Wdrozenie platformy przez `docker compose`
6. Integracja write-back do GitHub
7. Hardening, monitoring i backup

## Definicja sukcesu MVP

MVP uznajemy za gotowe, gdy:

- uzytkownik moze podac URL repozytorium
- system poprawnie wykrywa podstawowy stack
- generowane sa pliki `Dockerfile` i `.dockerignore`
- obraz moze zostac zbudowany testowo
- frontend pokazuje status, logi i wynik
- cala platforma dziala jako stack `docker compose` na Ubuntu
