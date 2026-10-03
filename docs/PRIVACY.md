# Daten und Datenschutz / Privacy

Kein Benutzerkonto, keine Klassenlisten, keine Telemetrie. Die Web-App speichert die Darstellungswahl lokal; native Apps speichern Einstellungen lokal. Die Apps stellen die Systemuhr nicht um.

Der PTB-Abgleich verbindet zum Dienst `wss://uhr.ptb.de/time`; dabei ist die IP-Adresse gegenüber dem Dienst sichtbar. Ohne Verbindung wird Gerätezeit bzw. die letzte synchronisierte Zeit fortgeführt. Ferien und Stundenplan sind eingebettet, keine Kalender-API und keine automatische Aktualisierung.

GitHub Pages und GitHub-Downloads verarbeiten normale HTTP-Anfragen nach den Bedingungen von GitHub. Auf der separaten Schul-Website existieren aggregierte Downloadzähler. Diese Zähler und serverinternen Logs sind weder Teil der Pages-Demo noch dieses Repositorys.

No accounts, student lists or telemetry. Appearance preferences are stored locally. PTB requests expose the connecting IP to that service. Calendar data is bundled; there is no holiday API. GitHub hosting has its own request processing. The school deployment’s log-based counters are not included in the public demo.
