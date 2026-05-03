Plan do wykonania

Ważne!! : przy edycji frontend zachowaj styl

1. /dns/overview powinien w DNS Record wyświetlać skonfigurowane rekordy dla kontenerów. Routing Mode i Cloudflare Tunnel przenieś do setting i spraw żeby wyświetlały aktualne konfiguracje z backend. Jeżeli się nie da wyświetlać aktualnych informacj z backend usuń te sekcje.
2. W /dns/routes przycisk Publisz powinien wyświetłac okno z wstępną konfiguracją dla danego jobs. Po dodaniu powinien zaimplementować publiczny url i przyćiski które kierowały do lokalnego kontenera powinny teraz kierować do publicznego. Po udostępnieniu publicznym w /jobs/{id} powinien być wyświetlany status czy jest udostępniony publicznie czy prywatnie oraz url.
