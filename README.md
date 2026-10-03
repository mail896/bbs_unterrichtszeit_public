# Unterrichtszeit · BBS Einbeck

[English version](README.en.md) · [Projektseite & Demo](https://mail896.github.io/bbs_unterrichtszeit_public/) · [Desktop-Downloads](https://github.com/mail896/bbs_unterrichtszeit_public/releases/latest)

**Alles zu seiner Zeit.** Unterricht, nächste Pause und verbleibende Zeit auf einen Blick: als responsive Web-App und als native Anwendung für Windows, macOS und Linux. Entstanden aus Marc Schulz’ Visual-Basic-Unterrichtsprojekt von 2012.

![Unterrichtszeit im Browser](images/desktop.png)

## Ausprobieren

**[Web-Demo öffnen →](https://mail896.github.io/bbs_unterrichtszeit_public/demo/)** · [Schul-Webversion](https://apps.bbs-einbeck.de/uzeit/)

Die Demo ist die echte browserbasierte Uhr mit dem festen BBS-Stundenplan und deutscher Ortszeit. Keine Anmeldung, kein Backend. Oberfläche und Schulkalender sind deutsch; Projektseite und Dokumentation stehen auch auf Englisch zur Verfügung. Screenshots zeigen einen festgelegten Beispielzeitpunkt mit Gerätezeit.

## Funktionen

- Große Retro-Uhr mit blinkendem Doppelpunkt und segmentiertem Fortschritt.
- Aktuelle Unterrichtsstunde oder Pause, Countdown und Vorschau auf den nächsten Abschnitt.
- Mensazeit in der zweiten Pause, Tagesübersicht von 07:55 bis 16:35 Uhr.
- Ferien für Niedersachsen, Feiertage und Wochenenden; verkürzter Tag vor den Sommerferien.
- Hell, dunkel oder automatisch; responsive Darstellung für Desktop, iPad und iPhone.
- Web-Präsentationsmodus und native kompakte Fenster mit Anheften-Funktion.
- PTB-Zeitabgleich mit klarer Rückfallanzeige bei fehlender Verbindung. Die Systemuhr wird nicht verändert.
- Native Apps: Klick auf das dunkle Uhrfeld öffnet Webversion und Downloads.

<details><summary>Dunkle und mobile Ansicht</summary>

![Dunkle Darstellung](images/dunkel.png)

<img src="images/mobil.png" width="320" alt="Unterrichtszeit auf schmalem Bildschirm">

</details>

## Desktop-Apps

| Plattform | Stand | Installation |
| --- | --- | --- |
| Windows x64 | 2.10 | ZIP entpacken, `Unterrichtszeiten.exe` starten. MINI benötigt .NET 10 Desktop Runtime x64. |
| macOS 13+ · Apple Silicon & Intel | 1.0.8 | ZIP entpacken, `Unterrichtszeit.app` nach Programme kopieren. |
| Debian 12/13 · KDE & GNOME | 1.0.3 | `sudo apt install ./unterrichtszeit_1.0.3_all.deb` und im Appmenü starten. Auch auf Raspberry Pi 5 getestet. |

**[Alle Downloads →](https://github.com/mail896/bbs_unterrichtszeit_public/releases/latest)**

Die nativen Apps enthalten Kalender und Oberfläche für den Offlinebetrieb. Die erste Linux-Installation kann zum Nachladen von Abhängigkeiten Internet benötigen. Die Web-App ist kein garantierter Offline-PWA-Cache. Windows ist nicht signiert; die Mac-App ist lokal/ad-hoc signiert, nicht notarisiert. Keine Sicherheitseinstellungen global deaktivieren.

## Geltungsbereich

Der Stundenplan ist für die BBS Einbeck fest hinterlegt, nicht für persönliche Klassenpläne. Kalenderabdeckung: **2026 bis 21.08.2030**; keine automatische Ferienaktualisierung. Schulinterne Sondertage können abweichen. Nach Ablauf erscheint ein Prüfhinweis.

## Selbst bauen und testen

```sh
npm ci --ignore-scripts
npm run check
npm run pages:build
python3 -m http.server --bind 127.0.0.1 --directory dist/pages 8080
```

[Installation und Plattform-Builds](docs/INSTALLATION.md) · [Architektur](docs/ARCHITEKTUR.md) · [Daten und Datenschutz](docs/PRIVACY.md)

## Aufbau

- `public/`: statische Web-App, lokale Logos und Icons.
- `native/`: Windows mit C#/.NET und WinForms, ohne WebView.
- `macos/`: SwiftUI/AppKit, Xcode-Bauskript für Universal-App.
- `linux/`: Python/PyQt5 mit Debian-Paketbau.
- `site/`: deutsche und englische GitHub-Projektseite.
- `scripts/`, `tests/`: Pages-Build, reproduzierbare Screenshots, Browser- und Kalenderprüfungen.
- `images/`: tatsächliche App-Screenshots ohne persönliche Desktop-Inhalte.

## Stand und Lizenz

Öffentlicher Quellstand vom **3. Oktober 2026**, mit eigener Historie. Native Versionsnummern werden unabhängig gepflegt. Historische Entwürfe, interne Serverdokumentation und lokale Buildverzeichnisse sind nicht enthalten.

© 2026 Marc Schulz · BBS Einbeck. [MIT-Lizenz](LICENSE) · [Bilder/Schulzeichen](docs/ASSETS.md) · [Abhängigkeiten](docs/DEPENDENCIES.md) · [Sicherheitsmeldungen](SECURITY.md)

Der bestätigte Mac-Download ist 1.0.8. Der Quellstand enthält bereits die Fenstergrößenkorrekturen bis 1.0.10; deren Langzeitverhalten ist noch nicht bestätigt.
