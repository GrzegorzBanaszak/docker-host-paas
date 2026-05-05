Plan do wykonania

Wazne!! : przy edycji frontend zachowaj styl

# Propozycje rozbudowy Dockerizera

Projekt jest juz czyms wiecej niz prostym generatorem `Dockerfile`. To maly self-hosted PaaS: React frontend, ASP.NET API, worker, PostgreSQL, Redis, Docker runtime, historia obrazow, routing przez porty albo Traefik/Cloudflare Tunnel oraz Terraform dla hosta zdalnego.

Najbardziej sensowny kierunek rozwoju: z narzedzia do konteneryzacji repozytorium zrobic panel do budowania i utrzymywania wlasnych aplikacji z GitHuba na prywatnym hoscie Docker.

## Priorytet 1: Projekty / aplikacje jako byt nadrzedny - wykonane

Status: wykonane.

Wdrozone:

- `Project` jako glowny byt aplikacji,
- relacja `Project -> Jobs`,
- migracja istniejacych jobow do projektow,
- endpointy API dla projektow,
- tworzenie jobow z poziomu projektu,
- aktualizacja aktualnego joba, obrazu i deploymentu po udanym buildzie,
- widoki frontendu: lista projektow, tworzenie projektu i szczegoly projektu,
- przekierowanie ze szczegolow joba do projektu,
- przeniesienie `Resource Guard` do widoku projektu.

## Priorytet 2: Zmienne srodowiskowe i sekrety

Brakuje warstwy konfiguracji aplikacji. Przydatne funkcje:

- env vars per projekt,
- sekrety maskowane w UI i logach,
- rozdzielenie publicznych zmiennych od sekretow,
- przekazywanie `.env` tylko do runtime,
- walidacja wymaganych zmiennych przed uruchomieniem kontenera.

To jest naturalny nastepny krok po uruchamianiu kontenerow.

## Priorytet 3: Healthcheck aplikacji

Runtime sprawdza port TCP, ale warto dodac pelniejszy healthcheck:

- HTTP path, np. `/health`, `/`, `/api/health`,
- oczekiwany status code,
- timeout i liczba prob,
- ostatni wynik healthchecka w UI,
- status: healthy, unhealthy, unknown,
- opcjonalny automatyczny restart po niezdrowym stanie.

To zmieni informacje "kontener dziala" w "aplikacja realnie odpowiada".

## Priorytet 4: Lepszy pipeline diagnostyczny

Aktualnie sa logi i wygenerowane pliki. Mozna dodac:

- podzial logow na etapy: clone, detect, generate, build, run, healthcheck,
- czas trwania kazdego etapu,
- klasyfikacje bledow,
- sugestie naprawy, np. "brakuje npm start", "brakuje requirements.txt", "port nie odpowiada",
- filtr bledow i ostrzezen w log viewerze.

To bedzie szczegolnie przydatne przy cudzych repozytoriach.

## Priorytet 5: Rollback i promocja obrazow

Skoro jest historia obrazow, warto dodac:

- akcje "deploy this image" dla starszego obrazu,
- rollback do poprzedniego udanego builda,
- oznaczenia: current, previous, failed, candidate,
- porownanie buildow i commitow,
- informacje, ktory obraz jest aktualnie uruchomiony.

To jest bardzo praktyczne dla wlasnego mini-PaaS.

## Priorytet 6: GitHub write-back

W dokumentach byl plan opcjonalnego zapisania zmian do repozytorium. Mozna to rozbudowac do:

- generowania PR z `Dockerfile` i `.dockerignore`,
- komentarza z wynikiem builda,
- automatycznego commita na branch `dockerizer/...`,
- podgladu diffu przed utworzeniem PR,
- konfiguracji GitHub tokena lub GitHub App.

Dockerizer przestanie wtedy byc tylko runnerem, a stanie sie asystentem konteneryzacji repozytoriow.

## Priorytet 7: Autoryzacja i multi-user

API wyglada na administracyjne i lokalne. Warto dodac:

- logowanie uzytkownikow,
- role: admin, operator, viewer,
- wlasciciela projektu/joba,
- ograniczenia kto moze usuwac obrazy,
- ograniczenia kto moze publikowac DNS,
- ograniczenia kto moze restartowac i zatrzymywac kontenery.

To jest wazne, jesli platforma ma wyjsc poza prywatne narzedzie na jednym serwerze.

## Priorytet 8: Webhooki i automatyczne rebuildy

Teraz job jest uruchamiany recznie. Kolejny etap:

- webhook z GitHuba na push,
- rebuild po zmianie wybranej galezi,
- reczne albo automatyczne deployowanie ostatniego udanego obrazu,
- historia commit SHA -> image -> deployment,
- ustawienie, czy projekt ma sie budowac automatycznie.

Model `JobImage.SourceCommitSha` juz sugeruje, ze projekt jest gotowy na taki kierunek.

## Priorytet 9: Registry zamiast tylko lokalnych obrazow

Obrazy sa budowane lokalnie na hoscie Docker. Dalsza rozbudowa:

- push do prywatnego registry,
- konfiguracja registry credentials,
- pull/deploy obrazu z registry,
- czyszczenie starych tagow,
- retencja obrazow per projekt.

Przy zdalnym hostingu i Terraformie to bedzie spojniejsze operacyjnie.

## Priorytet 10: Twardsza izolacja obcego kodu

Najwiekszym ryzykiem jest budowanie i uruchamianie kodu z repozytoriow. Mozna dodac:

- osobne kontenery build-runnerow,
- build bez dostepu do sieci po pobraniu zaleznosci, jesli mozliwe,
- allowlist hostow repozytoriow,
- limity czasu per etap,
- limity przestrzeni dyskowej workspace,
- skanowanie Dockerfile pod ryzykowne instrukcje,
- automatyczne czyszczenie starych workspace'ow, kontenerow i obrazow.

To powinno byc wysoko w priorytetach, jesli system ma budowac nie w pelni zaufane repozytoria.

## Priorytet 11: Obsluga docker-compose i aplikacji wieloserwisowych

Obecnie projekt skupia sie na pojedynczym kontenerze aplikacji. Mocne rozszerzenie:

- wykrywanie `docker-compose.yml`,
- generowanie compose dla aplikacji z baza,
- UI pokazujace kilka uslug,
- health status per service,
- wspolna siec i zaleznosci miedzy kontenerami.

To otworzy droge do deployowania realniejszych aplikacji.

## Priorytet 12: Lepsza obsluga stackow

Generator jest dobrym MVP, ale mozna go rozszerzyc o:

- pnpm, yarn i bun,
- monorepo Nx/Turborepo,
- Python Poetry/uv,
- FastAPI/Django/Flask jako osobne profile,
- Spring Boot z Maven/Gradle,
- .NET z wykrywaniem konkretnego `.csproj`,
- statyczne SPA z poprawnym fallbackiem `index.html` w Nginx.

Najwiekszy szybki zysk: Node package manager detection i lepszy monorepo flow.

## Priorytet 13: Monitoring i alerty

Dashboard ma zasoby kontenerow, ale mozna dodac:

- metryki jobow w czasie,
- liczbe failed buildow,
- zuzycie dysku przez obrazy/workspace,
- alert przy braku Redis/Postgres/Docker,
- webhook/Discord/Slack/email po sukcesie lub bledzie.

## Priorytet 14: Backup i disaster recovery

Dla self-hosted PaaS warto miec:

- backup PostgreSQL,
- eksport konfiguracji projektow,
- backup sekretow,
- procedure restore,
- UI/CLI do weryfikacji ostatniego backupu.

## Priorytet 15: CLI albo API tokeny

Dla wygody mozna dodac:

- `dockerizer deploy <repo>`,
- tworzenie joba z terminala,
- API tokens,
- integracje z CI/CD,
- endpoint do triggerowania rebuilda z pipeline'u.

## Proponowana kolejnosc prac

1. Dodac env vars i sekrety.
2. Dodac HTTP healthcheck i lepsze etapy pipeline'u.
3. Dodac rollback do poprzedniego obrazu.
4. Dodac GitHub write-back przez PR.
5. Dodac autoryzacje.
6. Dodac webhooki GitHub i automatyczne rebuildy.
7. Dodac registry i retencje obrazow.
8. Wzmocnic izolacje buildow i runtime.
9. Dodac obsluge compose / multi-service apps.
