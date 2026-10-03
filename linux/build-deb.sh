#!/bin/sh
set -eu
cd -- "$(dirname -- "$0")"
staging=$(mktemp -d)
trap 'rm -rf "$staging"' EXIT HUP INT TERM
install -d "$staging/DEBIAN" "$staging/usr/bin" "$staging/usr/share/unterrichtszeit/assets" "$staging/usr/share/applications" "$staging/usr/share/icons/hicolor/1024x1024/apps" "$staging/usr/share/doc/unterrichtszeit"
install -m 644 app.py school.py "$staging/usr/share/unterrichtszeit/"
install -m 644 assets/* "$staging/usr/share/unterrichtszeit/assets/"
install -m 755 packaging/unterrichtszeit "$staging/usr/bin/"
install -m 644 packaging/*.desktop "$staging/usr/share/applications/"
install -d "$staging/usr/share/unterrichtszeit/icons"
install -m 644 icons/*.png "$staging/usr/share/unterrichtszeit/icons/"
for size in 16 24 32 48 64 128 256 512; do
    install -d "$staging/usr/share/icons/hicolor/${size}x${size}/apps"
    install -m 644 "icons/$size.png" "$staging/usr/share/icons/hicolor/${size}x${size}/apps/de.bbs-einbeck.unterrichtszeit.png"
done
install -m 644 assets/icon.png "$staging/usr/share/icons/hicolor/1024x1024/apps/de.bbs-einbeck.unterrichtszeit.png"
install -m 644 README.md "$staging/usr/share/doc/unterrichtszeit/"
cat > "$staging/DEBIAN/control" <<CONTROL
Package: unterrichtszeit
Version: 1.0.3
Section: education
Priority: optional
Architecture: all
Maintainer: Marc Schulz <marc.schulz@bbs-einbeck.de>
Depends: python3 (>= 3.9), python3-pyqt5, python3-pyqt5.qtwebsockets, qtwayland5, tzdata, ca-certificates
Installed-Size: $(du -sk "$staging/usr" | cut -f1)
Description: Native Unterrichtsuhr der BBS Einbeck
 Kompakte Uhr, Unterrichtszeiten, Pausen und Ferienkalender.
 Mit Darkmode, Tagesuebersicht, Offlinebetrieb und PTB-Zeitabgleich.
CONTROL
mkdir -p ../delivery
dpkg-deb --root-owner-group --build "$staging" ../delivery/unterrichtszeit_1.0.3_all.deb
