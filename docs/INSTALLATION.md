# Installation und Entwicklung

[English](INSTALLATION.en.md)

## Web und GitHub Pages

Die Laufzeit benötigt nur statische Dateien aus `public/` und einen Webserver. JavaScript und Bilder liegen lokal, der Zeitabgleich verbindet zu `wss://uhr.ptb.de/time`. Node.js 24 und Python 3 werden für Prüfungen und den Pages-Build verwendet.

```sh
npm ci --ignore-scripts
npm run check
npm run pages:build
npx playwright install --with-deps chromium
npm run test:browser
npm run screenshots
npm run pages:build
```

Der letzte Build übernimmt neue Screenshots. `dist/pages/` enthält die Projektseite, `/en.html` und `/demo/`. GitHub Pages veröffentlicht ausschließlich dieses Verzeichnis über den Pages-Workflow. Auf eigener Infrastruktur passende HTTPS-/CSP-Header setzen und Verzeichnisauflistungen deaktivieren. Ein Serverbeispiel für interne Schulnetze wird bewusst nicht mitgeliefert.

## Windows

.NET SDK 10 mit Windows-Targeting:

```sh
sh native/build.sh
# aus dem Repository-Stamm, Kalenderprüfungen und optionaler PTB-Livetest:
dotnet run --project native-checks/Test.csproj
```

`native/dist/` enthält die selbständige EXE; `native/dist-mini/` die Variante mit externer .NET-Desktop-Laufzeit. Cross-Builds unter Linux sind möglich, die Fenstertests benötigen Windows.

## macOS

Auf einem Mac mit vollständigem Xcode:

```sh
cd macos
./build.command
```

Das Skript prüft die Kalenderlogik, baut arm64+x86_64, erstellt das Icon und signiert lokal. Ergebnis: `macos/build/Unterrichtszeit.app`. Es installiert keine zusätzliche Software. Notarisierung und Developer-ID-Signierung sind nicht enthalten. Fensterverhalten, Cursor und Ruhezustand müssen auf macOS geprüft werden.

## Debian/Linux

```sh
sudo apt install python3-pyqt5 python3-pyqt5.qtwebsockets qtwayland5 tzdata ca-certificates
python3 linux/test_school.py
QT_QPA_PLATFORM=offscreen python3 linux/test_gui.py
sh linux/build-deb.sh
sudo apt install ./delivery/unterrichtszeit_1.0.3_all.deb
```

Der GUI-Test enthält einen PTB-Livetest und benötigt Internet. Browserprüfungen blockieren PTB bewusst und testen den Gerätezeit-Fallback; die Sekundentests laufen ohne Netzwerk. Paketarchitektur `all` bezeichnet Python-Quellen, nicht enthaltene Binärbibliotheken für jede CPU.

## Anpassen

Stundenplan und Kalender sind zurzeit pro Plattform enthalten: `public/app.js`, `native/Engine.cs`/`calendar.json`, `macos/Sources/SchoolClock.swift`/`Resources/calendar.json`, `linux/school.py`/`assets/calendar.json`. Änderungen überall abgleichen und Tests aktualisieren. Logos, Schulname, Kontaktadresse und native Website-Links bei eigener Veröffentlichung ersetzen. Native Oberflächen können noch ältere Anreden enthalten; die aktuelle Schul-Webversion verwendet die förmliche Anrede.
