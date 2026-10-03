import Foundation
let holidays=try JSONDecoder().decode([Holiday].self,from:Data(contentsOf:URL(fileURLWithPath:CommandLine.arguments[1])))
let clock=SchoolClock(holidays:holidays)
let f=ISO8601DateFormatter()
func state(_ value:String) -> ClockState { clock.evaluate(f.date(from:value)!) }
func check(_ condition: @autoclosure () -> Bool,_ message:String) { if !condition() { fatalError(message) } }
check(state("2026-09-17T05:54:59Z").remaining == 1,"Before school")
check(state("2026-09-17T05:55:00Z").status == "1. Stunde","First lesson")
check(state("2026-09-17T09:15:00Z").status == "2. Pause","Lunch break")
check(state("2026-09-17T09:44:59Z").remaining == 1,"Break boundary")
check(state("2026-09-17T09:45:00Z").status == "5. Stunde","After break")
check(state("2026-09-17T14:35:00Z").remaining == nil,"End of school")
check(state("2026-09-19T10:00:00Z").status == "Wochenende","Weekend")
check(state("2026-10-12T08:00:00Z").status == "Herbstferien","Holiday")
check(state("2026-07-01T08:30:00Z").remaining == nil,"Short day")
check(clock.publicHoliday(f.date(from:"2026-04-03T10:00:00Z")!) == "Karfreitag","Easter calculation")
check(state("2026-09-18T18:00:00Z").next.contains("21.09."),"Skip weekend")
check(state("2031-01-07T10:00:00Z").calendar.contains("nicht abgedeckt"),"Calendar limit")
check(state("2026-01-12T06:55:00Z").status == "1. Stunde","Winter time")
let base=f.date(from:"2026-09-17T00:00:00+02:00")!
for second in stride(from:0,to:86400,by:60) {
    let s=clock.evaluate(base.addingTimeInterval(Double(second)))
    if let idx=s.index { let slot=SchoolClock.slots[idx]; check(second >= slot.start*60 && second < slot.end*60,"Slot range"); check(s.remaining! > 0,"Positive countdown"); check(s.progress >= 0 && s.progress < 1,"Progress") }
}
print("Kalender-, Zeitgrenzen- und Schultag-Test im Minutenraster erfolgreich.")
