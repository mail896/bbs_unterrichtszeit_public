import Foundation

struct Holiday: Decodable {
    let Name: String
    let Start: String
    let End: String
}
struct Slot {
    let name: String
    let start: Int
    let end: Int
    let pause: Bool
    init(_ name: String, _ start: Int, _ end: Int, _ pause: Bool = false) {
        self.name = name; self.start = start; self.end = end; self.pause = pause
    }
    var range: String { "\(SchoolClock.hm(start)) – \(SchoolClock.hm(end))" }
}
struct ClockState {
    var time: String
    var status: String
    var index: Int?
    var remaining: Int?
    var progress: Double
    var label: String
    var next: String
    var calendar: String
    var greeting: String
}
struct SchoolClock {
    static let slots = [Slot("1. Stunde",475,520),Slot("2. Stunde",520,565),Slot("1. Pause",565,585,true),Slot("3. Stunde",585,630),Slot("4. Stunde",630,675),Slot("2. Pause",675,705,true),Slot("5. Stunde",705,750),Slot("6. Stunde",750,795),Slot("3. Pause",795,805,true),Slot("7. Stunde",805,850),Slot("8. Stunde",850,895),Slot("4. Pause",895,905,true),Slot("9. Stunde",905,950),Slot("10. Stunde",950,995)]
    let holidays: [Holiday]
    var cal: Calendar {
        var c = Calendar(identifier: .gregorian); c.timeZone = TimeZone(identifier: "Europe/Berlin")!; return c
    }
    static func hm(_ m: Int) -> String { String(format: "%02d:%02d",m/60,m%60) }
    func format(_ date: Date, _ pattern: String) -> String {
        let f = DateFormatter(); f.locale = Locale(identifier: "de_DE"); f.timeZone = cal.timeZone; f.dateFormat = pattern; return f.string(from: date)
    }
    func key(_ date: Date) -> String { format(date,"yyyy-MM-dd") }
    func add(_ date: Date, _ days: Int) -> Date { cal.date(byAdding: .day,value: days,to: date)! }
    func holiday(_ date: Date) -> Holiday? { let k = key(date); return holidays.first { $0.Start <= k && k <= $0.End } }
    func publicHoliday(_ date: Date) -> String? {
        let md = format(date,"MM-dd")
        if let name = ["01-01":"Neujahr","05-01":"Tag der Arbeit","10-03":"Tag der Deutschen Einheit","10-31":"Reformationstag","12-25":"1. Weihnachtstag","12-26":"2. Weihnachtstag"][md] { return name }
        let y = cal.component(.year,from: date)
        let a=y%19, b=y/100, c=y%100, d=b/4, e=b%4, f=(b+8)/25
        let g=(b-f+1)/3, h=(19*a+b-d-g+15)%30, i=c/4, k=c%4
        let l=(32+2*e+2*i-h-k)%7, m=(a+11*h+22*l)/451
        let n=h+l-7*m+114
        let easter=cal.date(from: DateComponents(year:y,month:n/31,day:n%31+1))!
        let delta=cal.dateComponents([.day],from:easter,to:cal.startOfDay(for:date)).day!
        return [-2:"Karfreitag",1:"Ostermontag",39:"Christi Himmelfahrt",50:"Pfingstmontag"][delta]
    }
    func off(_ date: Date) -> String? {
        if let h = holiday(date) { return h.Name }
        if let h = publicHoliday(date) { return h }
        return [1,7].contains(cal.component(.weekday,from:date)) ? "Wochenende" : nil
    }
    func nextStart(_ date: Date) -> String {
        guard key(date) >= "2026-01-01", key(date) <= "2030-08-21" else { return "Nächsten Schultag bitte im Kalender prüfen." }
        for day in 1...370 {
            let candidate=add(date,day)
            if key(candidate) > "2030-08-21" { break }
            if off(candidate) == nil { return "Nächster Unterricht: \(day == 1 ? "Morgen" : format(candidate,"EEE, dd.MM.")) · 07:55 Uhr" }
        }
        return "Nächsten Schultag bitte im Kalender prüfen."
    }
    func evaluate(_ date: Date) -> ClockState {
        let seconds=cal.component(.hour,from:date)*3600+cal.component(.minute,from:date)*60+cal.component(.second,from:date)
        let free=off(date), k=key(date)
        let shortened=holidays.contains { $0.Name == "Sommerferien" && $0.Start == key(add(date,1)) }
        let index=free == nil ? Self.slots.firstIndex { seconds >= $0.start*60 && seconds < $0.end*60 && (!shortened || $0.end <= 630) } : nil
        let slot=index.map { Self.slots[$0] }
        let before=free == nil && seconds < 475*60
        let remaining=slot.map { $0.end*60-seconds } ?? (before ? 475*60-seconds : nil)
        var next=remaining == nil ? nextStart(date) : ""
        if before { next="Start um 07:55 Uhr. Guten Morgen!" }
        if let idx=index {
            if idx+1 < Self.slots.count && (!shortened || Self.slots[idx+1].end <= 630) {
                let n=Self.slots[idx+1]; next="Danach: \(n.name)\(n.start == 675 ? " · Mensazeit" : "") · \(Self.hm(n.start)) Uhr"
            } else { next="Danach: Unterrichtsschluss. Schönen Feierabend!" }
        }
        let calendarText: String
        func display(_ s: String) -> String { s.split(separator:"-").reversed().joined(separator:".") }
        if k < "2026-01-01" || k > "2030-08-21" { calendarText="Ferienkalender nicht abgedeckt – bitte Termine prüfen." }
        else if let h=holiday(date) ?? holidays.first(where: { $0.Start > k }) { calendarText="\(h.Name): \(display(h.Start)) – \(display(h.End))" }
        else { calendarText="Ferientermine bis 21.08.2030." }
        let status=free ?? slot?.name ?? (before ? "Guten Morgen" : "Feierabend")
        let greeting=free == nil ? "Schönen Feierabend!" : free == "Wochenende" ? "Schönes Wochenende!" : status.lowercased().contains("ferien") ? "Schöne Ferien!" : "Genieße deinen freien Tag!"
        return ClockState(time:format(date,"HHmm"),status:status,index:index,remaining:remaining,progress:slot.map { Double(seconds-$0.start*60)/Double(($0.end-$0.start)*60) } ?? 0,label:before ? "Die 1. Stunde beginnt in" : slot?.pause == true ? "Bis zum Ende der Pause" : "Bis zum Ende der Stunde",next:next,calendar:shortened && free == nil ? "Vor den Sommerferien: Schluss nach der 3. Stunde." : calendarText,greeting:greeting)
    }
}
