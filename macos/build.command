#!/bin/bash
set -euo pipefail
cd -- "$(dirname -- "$0")"
printf "\nUnterrichtszeit – Xcode-Build 1.0.10\nOrdner: %s\n" "$PWD"
if [[ "$(uname -s)" != Darwin ]]; then
  echo 'Dieses Skript bitte auf deinem Mac ausführen. Hier wird nichts installiert.'
  exit 1
fi
# Use the full Xcode installation for SDK/compiler/plugin consistency.
# Set this only for this script; do not change the machine's xcode-select setting.
if ! xcodebuild -version >/dev/null 2>&1; then
  if [[ -d /Applications/Xcode.app/Contents/Developer ]]; then
    export DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer
  else
    echo 'Vollständiges Xcode nicht ausgewählt. In Xcode → Settings → Locations → Command Line Tools auswählen.'
    exit 1
  fi
fi
xcodebuild -version
xcrun swiftc --version
mkdir -p build
printf '\nKalender und Zeitlogik prüfen …\n'
xcrun swiftc -swift-version 5 -O Sources/SchoolClock.swift Tests/main.swift -o build/calendar-tests
./build/calendar-tests Resources/calendar.json
ICONSET="$PWD/build/AppIcon.iconset"
mkdir -p "$ICONSET"
for SIZE in 16 32 128 256 512; do
  sips -z "$SIZE" "$SIZE" Resources/icon.png --out "$ICONSET/icon_${SIZE}x${SIZE}.png" >/dev/null
  DOUBLE=$((SIZE * 2))
  sips -z "$DOUBLE" "$DOUBLE" Resources/icon.png --out "$ICONSET/icon_${SIZE}x${SIZE}@2x.png" >/dev/null
done
iconutil -c icns "$ICONSET" -o "$PWD/build/AppIcon.icns"
printf '\nApple Silicon und Intel mit Xcode bauen …\n'
xcodebuild -project Unterrichtszeit.xcodeproj -target Unterrichtszeit \
  -configuration Release -sdk macosx \
  ARCHS="arm64 x86_64" ONLY_ACTIVE_ARCH=NO CODE_SIGNING_ALLOWED=NO \
  CONFIGURATION_BUILD_DIR="$PWD/build/products" build
BUILT_APP="$PWD/build/products/Unterrichtszeit.app"
plutil -lint "$BUILT_APP/Contents/Info.plist"
codesign --force --sign - "$BUILT_APP"
codesign --verify --strict --verbose=2 "$BUILT_APP"
BUILT_ARCHS="$(xcrun lipo -archs "$BUILT_APP/Contents/MacOS/Unterrichtszeit")"
echo "Architekturen: $BUILT_ARCHS"
for REQUIRED_ARCH in arm64 x86_64; do
  case " $BUILT_ARCHS " in
    *" $REQUIRED_ARCH "*) ;;
    *) echo "Fehler: Architektur $REQUIRED_ARCH fehlt."; exit 1 ;;
  esac
done
APP="$PWD/build/Unterrichtszeit.app"
# Replace only this script's own previous output, after a successful build.
if [[ -e "$APP" ]]; then
  mv "$APP" "$PWD/build/Unterrichtszeit-vorher-$(date +%Y%m%d-%H%M%S)-$$.app"
fi
ditto "$BUILT_APP" "$APP"
printf '\nFertig: %s\n' "$APP"
du -sh "$APP"
echo 'Die App kann jetzt in Programme kopiert werden. Xcode wird zum Ausführen nicht gebraucht.'
open -R "$APP"
