# Architektur / Architecture

Vier eigenständige Oberflächen nutzen denselben fachlichen Stundenplan: Web (JavaScript), Windows (C#/WinForms), macOS (SwiftUI/AppKit), Linux (Python/Qt). Keine native App benötigt WebView. Die Implementierungen und Kalenderdateien liegen getrennt vor und müssen bei fachlichen Änderungen gemeinsam gepflegt werden.

Der PTB-Abgleich ist asynchron, mit Timeout und Rückfall. Die Anzeige verwendet Europe/Berlin inklusive Sommerzeit. Ferienabdeckung endet am21.08.2030; die Anwendung weist auf die Grenze hin.

GitHub Pages veröffentlicht eine zweisprachige Projektseite und die deutschsprachige Web-App unter `/demo/`. Der Build kopiert nur freigegebene statische Dateien. Native Downloads liegen als Release-Assets außerhalb des Git-Verlaufs. Versionsnummern der Plattformen bleiben unabhängig.

Four independent UIs share the same timetable specification. There is no common compiled core: changes require coordinated updates across JavaScript, C#, Swift and Python. Pages hosts only static assets; native binaries are release attachments. The production school site is independent.
