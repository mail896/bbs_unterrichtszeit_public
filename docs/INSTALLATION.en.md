# Installation and development

[Deutsch](INSTALLATION.md)

## Web / Pages

Serve `public/` as static files, or use Node.js 24 and Python 3 to build the bilingual project website:

```sh
npm ci --ignore-scripts
npm run check
npm run pages:build
npx playwright install --with-deps chromium
npm run test:browser
npm run screenshots
npm run pages:build
python3 -m http.server --bind 127.0.0.1 --directory dist/pages 8080
```

Only `dist/pages/` is deployed to GitHub Pages. No download counters or school-server configuration are included. Self-hosters should configure HTTPS, appropriate CSP headers and disabled directory listings. PTB uses `wss://uhr.ptb.de/time`.

## Windows

Install .NET SDK 10 and run `sh native/build.sh`. The self-contained executable is in `native/dist/`; the smaller framework-dependent executable is in `native/dist-mini/`. The latter needs .NET 10 Desktop Runtime x64. Run `dotnet run --project native-checks/Test.csproj` from the repository root for calendar and live PTB checks. Linux can cross-compile; actual window tests need Windows.

## macOS

On a Mac with full Xcode, run `cd macos && ./build.command`. It checks calendar logic, builds Apple Silicon and Intel, generates the icon and signs locally. Output: `macos/build/Unterrichtszeit.app`. No notarization or Developer ID signing is included. Validate window geometry, hover cursor and sleep/wake on a real Mac.

## Linux

```sh
sudo apt install python3-pyqt5 python3-pyqt5.qtwebsockets qtwayland5 tzdata ca-certificates
python3 linux/test_school.py
QT_QPA_PLATFORM=offscreen python3 linux/test_gui.py
sh linux/build-deb.sh
sudo apt install ./delivery/unterrichtszeit_1.0.3_all.deb
```

The GUI test needs internet for its PTB check. Browser tests deliberately block PTB and use a fixed device clock. Debian architecture `all` packages Python source; native dependencies are resolved by apt for the machine’s architecture. Pinning behavior depends on the desktop/compositor.

## Customization

Timetable and holiday data are maintained per platform: web `public/app.js`, Windows `native/`, macOS `macos/`, Linux `linux/`. Keep changes consistent and update tests. Replace school identity, logos, contact address and website links for your own deployment. Some native UI wording predates the formal-address changes in the school web version.
