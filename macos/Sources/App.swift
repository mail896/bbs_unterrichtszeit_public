import SwiftUI
import AppKit

@MainActor final class ClockModel: ObservableObject {
    let school: SchoolClock
    @Published var now = Date()
    @Published var source = "GERÄTEZEIT"
    private var anchor: Date?
    private var stamp: TimeInterval = 0
    private var fresh = false
    private var syncing = false
    private var ticker: Timer?
    private var syncTimer: Timer?
    private var wakeObserver: NSObjectProtocol?
    init(school: SchoolClock) {
        self.school = school
        ticker = Timer.scheduledTimer(withTimeInterval: 0.25,repeats: true) { [weak self] _ in
            Task { @MainActor in self?.update() }
        }
        syncTimer = Timer.scheduledTimer(withTimeInterval: 300,repeats: true) { [weak self] _ in
            Task { @MainActor in await self?.synchronize() }
        }
        wakeObserver=NSWorkspace.shared.notificationCenter.addObserver(forName:NSWorkspace.didWakeNotification,object:nil,queue:.main) { [weak self] _ in
            Task { @MainActor in
                // Re-anchor after sleep; stale uptime offsets must not appear as fresh PTB time.
                self?.anchor=nil; self?.fresh=false; self?.update()
                await self?.synchronize()
            }
        }
        Task { await synchronize() }
    }
    func update() {
        let elapsed=ProcessInfo.processInfo.systemUptime-stamp
        now=anchor.map { $0.addingTimeInterval(elapsed) } ?? Date()
        source=anchor == nil ? "GERÄTEZEIT" : fresh && elapsed < 360 ? "PTB-ZEIT" : "ZEIT FORTGEFÜHRT"
    }
    func synchronize() async {
        guard !syncing else { return }; syncing=true
        defer { syncing=false; update() }
        let session=URLSession(configuration: .ephemeral)
        let socket=session.webSocketTask(with: URL(string:"wss://uhr.ptb.de/time")!,protocols:["time"])
        socket.resume()
        let timeout=Task { try? await Task.sleep(nanoseconds:8_000_000_000); if !Task.isCancelled { socket.cancel(with:.goingAway,reason:nil) } }
        defer { timeout.cancel(); socket.cancel(with:.normalClosure,reason:nil); session.invalidateAndCancel() }
        do {
            let sent=ProcessInfo.processInfo.systemUptime
            try await socket.send(.string("{\"c\":1}"))
            let reply=try await socket.receive()
            guard case let .string(text)=reply, let data=text.data(using:.utf8), data.count < 4096,
                  let json=try JSONSerialization.jsonObject(with:data) as? [String:Any],
                  let ms=json["s"] as? Double, let token=json["c"] as? Int, token == 1,
                  ms.isFinite, ms > 1577836800000, ms < 4102444800000 else { fresh=false; return }
            let end=ProcessInfo.processInfo.systemUptime, rtt=end-sent
            guard rtt < 3 else { fresh=false; return }
            anchor=Date(timeIntervalSince1970:ms/1000+rtt/2); stamp=end; fresh=true
        } catch { fresh=false }
    }
}

struct WindowBridge: NSViewRepresentable {
    var full: Bool
    var pin: Bool
    var appearance: String
    func makeNSView(context: Context) -> NSView { NSView() }
    func updateNSView(_ view: NSView, context: Context) {
        DispatchQueue.main.async {
            guard let window=view.window else { return }
            let desired: NSAppearance? = appearance == "dark" ? NSAppearance(named:.darkAqua) : appearance == "light" ? NSAppearance(named:.aqua) : nil
            if window.appearance?.name != desired?.name { window.appearance=desired }
            let level: NSWindow.Level = pin ? .floating : .normal
            if window.level != level { window.level=level }
            window.title="Unterrichtszeit"
            window.isMovableByWindowBackground=true
            let target=NSSize(width:full ? 1080 : 680,height:full ? 880 : 362)
            // Constraints alone do not prove that the actual content area has the right size.
            let actual=window.contentRect(forFrameRect:window.frame).size
            let sizeChanged=abs(actual.width-target.width)>0.5 || abs(actual.height-target.height)>0.5
            if sizeChanged || window.contentMinSize != target || window.contentMaxSize != target {
                let topLeft=NSPoint(x:window.frame.minX,y:window.frame.maxY)
                window.contentMinSize=NSSize(width:0,height:0)
                window.contentMaxSize=NSSize(width:10000,height:10000)
                if sizeChanged {
                    window.setContentSize(target)
                    window.setFrameTopLeftPoint(topLeft)
                }
                window.contentMinSize=target; window.contentMaxSize=target
                window.standardWindowButton(.zoomButton)?.isEnabled=false

            }
        }
    }
}

struct ClockView: View {
    @ObservedObject var model: ClockModel
    @AppStorage("appearance") private var appearance="auto"
    @AppStorage("pinned") private var pinned=false
    @State private var full=false
    @Environment(\.colorScheme) private var scheme
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    private let yellow=Color(red:245/255,green:214/255,blue:0)
    private let ink=Color(red:32/255,green:35/255,blue:34/255)
    private let cream=Color(red:1,green:0.977,blue:0.902)
    private var state: ClockState { model.school.evaluate(model.now) }
    var body: some View {
        VStack(spacing:16) {
            HStack {
                Toggle(isOn:$pinned) { Label("Anheften",systemImage:pinned ? "pin.fill" : "pin") }.help("Immer im Vordergrund")
                Button(full ? "Kompakt" : "Schultag") { full.toggle() }
                Picker("Ansicht",selection:$appearance) {
                    Text("Automatisch").tag("auto"); Text("Hell").tag("light"); Text("Dunkel").tag("dark")
                }.pickerStyle(.menu).frame(width:210).fixedSize(horizontal:true,vertical:false)
                Spacer()
            }
            if full {
                HStack {
                    Image(nsImage: NSImage(contentsOf:Bundle.main.url(forResource:scheme == .dark ? "bbs-logo-dark" : "bbs-logo",withExtension:"png")!)!).resizable().interpolation(.high).scaledToFit().frame(width:230,height:64)
                    Spacer()
                    Text(model.school.format(model.now,"EEEE, d. MMMM yyyy")).foregroundStyle(.secondary)
                }
                Text("Alles zu seiner Zeit.").font(.system(size:28,weight:.bold)).frame(maxWidth:.infinity,alignment:.leading)
            }
            HStack(alignment:.top,spacing:18) {
                VStack(spacing:10) { clockCard; countdown }
                if full { dayView.frame(width:355) }
            }
            if full {
                Spacer(minLength:0)
                VStack(alignment:.leading,spacing:4) {
                    Link("© Marc Schulz · marc.schulz@bbs-einbeck.de",destination:URL(string:"mailto:marc.schulz@bbs-einbeck.de")!)
                    Text("Mac-Version des Visual-Basic-Unterrichtsprojektes aus 2012")
                }.font(.caption).foregroundStyle(.secondary).frame(maxWidth:.infinity,alignment:.leading)
            }
        }
        .padding(16)
        .frame(maxWidth:.infinity,maxHeight:.infinity,alignment:.topLeading)
        .background(scheme == .dark ? Color(red:0.08,green:0.09,blue:0.085) : Color(red:0.965,green:0.965,blue:0.95))
        .background(WindowBridge(full:full,pin:pinned,appearance:appearance))
    }
    private var clockCard: some View {
        Button {
            NSWorkspace.shared.open(URL(string:"https://apps.bbs-einbeck.de/uzeit/")!)
        } label: {
        HStack(spacing:22) {
            HStack(spacing:5) {
                ForEach(Array(state.time.enumerated()),id:\.offset) { index,char in
                    if index == 2 {
                        Text(":").font(.system(size:43,weight:.bold)).foregroundStyle(yellow)
                            .opacity(reduceMotion || model.now.timeIntervalSince1970.truncatingRemainder(dividingBy:1) < 0.5 ? 1 : 0)
                    }
                    Text(String(char)).font(.system(size:55,weight:.bold,design:.rounded)).monospacedDigit()
                        .frame(width:47,height:88).background(Color(white:0.18),in:RoundedRectangle(cornerRadius:5))
                        .overlay(Rectangle().fill(Color.black.opacity(0.6)).frame(height:1))
                }
            }.foregroundStyle(cream).accessibilityElement(children:.ignore).accessibilityLabel("Uhrzeit \(model.school.format(model.now,"HH:mm"))")
            VStack(alignment:.leading,spacing:6) {
                Text(model.source).font(.system(size:10,weight:.medium)).foregroundStyle(.gray).frame(maxWidth:.infinity,alignment:.trailing)
                Text("aktuell:").font(.system(size:13)).foregroundStyle(.gray)
                Text(state.status).font(.system(size:24,weight:.bold)).foregroundStyle(yellow).lineLimit(2).minimumScaleFactor(0.7)
                if let idx=state.index {
                    Text(SchoolClock.slots[idx].range+" Uhr").font(.system(size:13)).foregroundStyle(.white.opacity(0.7))
                    if SchoolClock.slots[idx].start == 675 { Label("Mensazeit",systemImage:"fork.knife").font(.caption).foregroundStyle(yellow) }
                }
                Spacer(minLength:0)
            }
        }.padding(18).frame(maxWidth:.infinity).frame(height:142).background(ink,in:RoundedRectangle(cornerRadius:15))
            .contentShape(RoundedRectangle(cornerRadius:15))
        }.buttonStyle(.plain)
            .onContinuousHover { phase in
                switch phase {
                case .active(_): NSCursor.pointingHand.set()
                case .ended: NSCursor.arrow.set()
                }
            }
            .onDisappear { NSCursor.arrow.set() }
            .help("Web-Version / Downloads öffnen")
            .accessibilityLabel("Web-Version / Downloads öffnen")
    }
    private var countdown: some View {
        VStack(alignment:.leading,spacing:9) {
            if let seconds=state.remaining {
                Text(state.label.uppercased()).font(.system(size:11,weight:.semibold)).tracking(1)
                HStack(alignment:.firstTextBaseline,spacing:9) {
                    Text(String(format:"%02d",seconds/60)).font(.system(size:33,weight:.bold)).monospacedDigit()
                    Text("Minuten").font(.system(size:13))
                    Text(String(format:"%02d",seconds%60)).font(.system(size:33,weight:.bold)).monospacedDigit()
                    Text("Sekunden").font(.system(size:13))
                }
                HStack(spacing:3) { ForEach(0..<20) { i in Rectangle().fill(ink.opacity(Double(i+1)/20 <= state.progress ? 1 : 0.15)).frame(height:6) } }
                    .accessibilityLabel("\(Int(state.progress*100)) Prozent vergangen")
            } else { Text(state.greeting).font(.system(size:27,weight:.bold)) }
            HStack(alignment:.bottom,spacing:12) {
                Text(state.next).font(.system(size:13)).lineLimit(2)
                Spacer(minLength:0)
                Text("v" + (Bundle.main.object(forInfoDictionaryKey:"CFBundleShortVersionString") as? String ?? "–"))
                    .font(.system(size:9)).foregroundStyle(Color(white:0.40))
                    .fixedSize()
            }
        }.frame(maxWidth:.infinity,alignment:.leading).padding(16).foregroundStyle(ink).background(yellow,in:RoundedRectangle(cornerRadius:15))
    }
    private var dayView: some View {
        VStack(alignment:.leading,spacing:5) {
            Text("Dein Schultag").font(.title2.bold()).padding(.bottom,8)
            ForEach(SchoolClock.slots.indices,id:\.self) { idx in
                let slot=SchoolClock.slots[idx]
                HStack {
                    Text(slot.name+(slot.start == 675 ? " · Mensazeit" : ""))
                    Spacer(); Text(slot.range).monospacedDigit()
                }.font(.system(size:12,weight:state.index == idx ? .semibold : .regular)).padding(8)
                    .foregroundStyle(state.index == idx ? yellow : Color.primary)
                    .background(state.index == idx ? ink : slot.pause ? Color.gray.opacity(0.09) : Color.clear,in:RoundedRectangle(cornerRadius:6))
            }
            Text(state.calendar).font(.caption).foregroundStyle(.secondary).padding(.top,10)
            Link("Ferientermine Niedersachsen ↗",destination:URL(string:"https://www.mk.niedersachsen.de/startseite/service/ferientermine/")!).font(.caption)
        }.padding(16).background(scheme == .dark ? Color(white:0.14) : Color.white,in:RoundedRectangle(cornerRadius:15))
    }
}

@MainActor final class AppDelegate: NSObject, NSApplicationDelegate {
    private var window: NSWindow?
    private var model: ClockModel?
    func applicationDidFinishLaunching(_ notification: Notification) {
        do {
            guard let url=Bundle.main.url(forResource:"calendar",withExtension:"json") else {
                throw NSError(domain:"Unterrichtszeit",code:1,userInfo:[NSLocalizedDescriptionKey:"Ferienkalender fehlt im App-Paket."])
            }
            let holidays=try JSONDecoder().decode([Holiday].self,from:Data(contentsOf:url))
            let clock=ClockModel(school:SchoolClock(holidays:holidays))
            model=clock
            let controller=NSHostingController(rootView:ClockView(model:clock))
            // WindowBridge owns window geometry. Content changes (lesson/pause/free time)
            // must not rewrite NSWindow limits via the hosting controller.
            controller.sizingOptions=[]
            let main=NSWindow(contentRect:NSRect(x:0,y:0,width:680,height:362),styleMask:[.titled,.closable,.miniaturizable],backing:.buffered,defer:false)
            main.title="Unterrichtszeit"
            main.isReleasedWhenClosed=false
            main.isRestorable=false
            main.contentViewController=controller
            main.center()
            window=main
            let menu=NSMenu()
            let item=NSMenuItem();menu.addItem(item)
            let appMenu=NSMenu();item.submenu=appMenu
            let quit=NSMenuItem(title:"Unterrichtszeit beenden",action:#selector(NSApplication.terminate(_:)),keyEquivalent:"q")
            appMenu.addItem(quit);NSApp.mainMenu=menu
            showWindow()
        } catch {
            let alert=NSAlert();alert.messageText="Unterrichtszeit konnte nicht gestartet werden"
            alert.informativeText=error.localizedDescription;alert.runModal();NSApp.terminate(nil)
        }
    }
    private func showWindow() {
        guard let window=window else { return }
        if window.isMiniaturized { window.deminiaturize(nil) }
        if !NSScreen.screens.contains(where: { $0.visibleFrame.intersects(window.frame) }) { window.center() }
        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps:true)
    }
    func applicationShouldHandleReopen(_ sender:NSApplication,hasVisibleWindows flag:Bool) -> Bool {
        showWindow();return true
    }
    func applicationShouldTerminateAfterLastWindowClosed(_ sender:NSApplication) -> Bool { true }
}
@main struct UnterrichtszeitMain {
    @MainActor static func main() {
        let app=NSApplication.shared
        let delegate=AppDelegate()
        app.setActivationPolicy(.regular)
        app.delegate=delegate
        withExtendedLifetime(delegate) { app.run() }
    }
}
