# Linux · Unterrichtszeit 1.0.3

Debian 12/13, KDE/GNOME; auch auf Raspberry Pi 5 mit Debian 12 ARM64 vom Autor getestet.

```sh
sudo apt install ./unterrichtszeit_1.0.3_all.deb
unterrichtszeit
```

Build: `./build-deb.sh`. Paketarchitektur `all`: Python-Code; die Paketverwaltung liefert die passenden Qt-Bibliotheken. Ersteinrichtung kann Internet benötigen. Danach funktioniert die Uhr offline. `dpkg -i` allein löst fehlende Abhängigkeiten nicht auf.

Die Anheften-Funktion hängt vom Fenstermanager ab. Einstellungen werden lokal mit QSettings gespeichert. [Installation / English](../docs/INSTALLATION.en.md).
