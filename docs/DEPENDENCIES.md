# Abhängigkeiten / Dependencies

- Web-Laufzeit: Vanilla HTML/CSS/JavaScript, Systemschriften; keine CDN-Bibliotheken.
- Windows: .NET 10/WinForms (Microsoft, MIT; mitgelieferte Runtime behält ihre Hinweise).
- macOS: SwiftUI/AppKit/Foundation aus dem Betriebssystem; Xcode nur für Builds.
- Linux: Python 3 (PSF), PyQt5/QtWebSockets (GPLv3 oder kommerziell), Qt (je Modul LGPL/GPL/kommerziell). apt liefert Plattformpakete separat. Bei eigener Weiterverteilung gelten die jeweiligen Lizenzpflichten; die MIT-Lizenz dieses Quellcodes ersetzt sie nicht.
- Entwicklung: Playwright (Apache-2.0), Chromium mit eigenen Lizenzhinweisen, Python, Node.js, GitHub Actions.
- Zeitabgleich: externer PTB-Dienst, keine eingebundene Bibliothek.

Third-party components retain their own licenses. Python/PyQt/Qt are system dependencies of the DEB package, not relicensed under MIT. Windows self-contained distributions include the .NET runtime.
