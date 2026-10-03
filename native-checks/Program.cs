using Unterrichtszeiten;
var engine=new SchoolClock(File.ReadAllText("native/calendar.json"));
void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var start=DateTimeOffset.Parse("2026-09-17T00:00:00+02:00");
for(int sec=0;sec<86400;sec++){
 var s=engine.Evaluate(start.AddSeconds(sec));var slot=SchoolClock.Slots.FirstOrDefault(p=>sec>=p.Start*60&&sec<p.End*60);
 Check(s.Current==slot,"slot "+sec);int? expected=slot!=null?slot.End*60-sec:sec<475*60?475*60-sec:null;Check(s.Remaining==expected,"countdown "+sec);
}
foreach(var (date,status) in new[]{("2026-10-12","Herbstferien"),("2026-10-24","Herbstferien"),("2026-09-19","Wochenende"),("2027-01-09","Weihnachtsferien"),("2026-05-14","Christi Himmelfahrt"),("2026-05-25","Pfingstmontag")}){var s=engine.Evaluate(DateTimeOffset.Parse(date+"T07:00:00+02:00"));Check(s.Status==status&&s.Remaining==null,date);}
Check(engine.Evaluate(DateTimeOffset.Parse("2026-07-01T10:30:00+02:00")).Remaining==null,"shortened summer eve");
Check(engine.Evaluate(DateTimeOffset.Parse("2031-09-17T10:00:00+02:00")).Calendar.Contains("nicht abgedeckt"),"expired calendar");
Check(engine.Evaluate(DateTimeOffset.Parse("2026-03-29T01:00:00Z")).Time.Hour==3,"DST spring");
Check(engine.Evaluate(DateTimeOffset.Parse("2026-10-25T01:00:00Z")).Time.Hour==2,"DST autumn");
var ptb=new PtbClock();Check(ptb.Source=="GERÄTEZEIT","initial fallback");await ptb.Sync(CancellationToken.None);Check(ptb.Fresh,"live PTB failed");Console.WriteLine("PASS 86.400 Sekunden, Ferien, Feiertage, Wochenenden, verkürzter Tag, Zeitumstellung; PTB live: "+ptb.Now);
using var cancelled=new CancellationTokenSource();cancelled.Cancel();await ptb.Sync(cancelled.Token);Check(!ptb.Fresh&&ptb.Source=="ZEIT FORTGEFÜHRT","last sync fallback");Console.WriteLine("PASS PTB failure preserves last synchronized time");
Check(engine.NextSchoolStart(new DateOnly(2026,9,17)).Contains("Morgen"),"tomorrow");
Check(engine.NextSchoolStart(new DateOnly(2026,9,18)).Contains("21.09."),"skip weekend");
Check(engine.NextSchoolStart(new DateOnly(2026,10,9)).Contains("26.10."),"skip autumn holidays and weekends");
Check(engine.NextSchoolStart(new DateOnly(2030,8,21)).Contains("prüfen"),"calendar expiry");
Check(engine.Evaluate(DateTimeOffset.Parse("2026-09-17T21:00:00+02:00")).Next.Contains("Nächster Unterricht"),"idle next lesson");
Console.WriteLine("PASS next school day: tomorrow, weekends, holidays and coverage boundary");
