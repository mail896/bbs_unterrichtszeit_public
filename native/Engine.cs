using System.Text.Json;
namespace Unterrichtszeiten;
public sealed record Slot(string Name, int Start, int End, bool Pause = false);
public sealed record Holiday(string Name, DateOnly Start, DateOnly End);
public sealed record ClockState(DateTime Time, Slot? Current, int Index, int? Remaining, double Progress, string Status, string Range, string Label, string Next, string Calendar);
public sealed class SchoolClock
{
    public static readonly Slot[] Slots = [new("1. Stunde",475,520),new("2. Stunde",520,565),new("1. Pause",565,585,true),new("3. Stunde",585,630),new("4. Stunde",630,675),new("2. Pause",675,705,true),new("5. Stunde",705,750),new("6. Stunde",750,795),new("3. Pause",795,805,true),new("7. Stunde",805,850),new("8. Stunde",850,895),new("4. Pause",895,905,true),new("9. Stunde",905,950),new("10. Stunde",950,995)];
    private readonly Holiday[] holidays;
    private readonly TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
    public SchoolClock(string json) => holidays = JsonSerializer.Deserialize<Holiday[]>(json)!;
    public static string Hm(int minute) => $"{minute / 60:00}:{minute % 60:00}";
    public static string? PublicHoliday(DateOnly date)
    {
        var fixedName=(date.Month,date.Day) switch {(1,1)=>"Neujahr",(5,1)=>"Tag der Arbeit",(10,3)=>"Tag der Deutschen Einheit",(10,31)=>"Reformationstag",(12,25)=>"1. Weihnachtstag",(12,26)=>"2. Weihnachtstag",_=>null};
        if(fixedName!=null)return fixedName;
        int y=date.Year,a=y%19,b=y/100,c=y%100,d=b/4,e=b%4,f=(b+8)/25,g=(b-f+1)/3,h=(19*a+b-d-g+15)%30,i=c/4,k=c%4,l=(32+2*e+2*i-h-k)%7,m=(a+11*h+22*l)/451;
        var easter=new DateOnly(y,(h+l-7*m+114)/31,(h+l-7*m+114)%31+1);
        return (date.DayNumber-easter.DayNumber) switch {-2=>"Karfreitag",1=>"Ostermontag",39=>"Christi Himmelfahrt",50=>"Pfingstmontag",_=>null};
    }
    public string NextSchoolStart(DateOnly date)
    {
        var end=new DateOnly(2030,8,21);
        if(date<new DateOnly(2026,1,1)||date>end)return "Nächsten Schultag bitte im Kalender prüfen.";
        for(var candidate=date.AddDays(1);candidate<=end;candidate=candidate.AddDays(1))
        {
            if(candidate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)continue;
            if(PublicHoliday(candidate)!=null||holidays.Any(h=>candidate>=h.Start&&candidate<=h.End))continue;
            string when=candidate==date.AddDays(1)?"Morgen":candidate.ToDateTime(TimeOnly.MinValue).ToString("ddd, dd.MM.",System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
            return $"Nächster Unterricht: {when} · 07:55 Uhr";
        }
        return "Nächsten Schultag bitte im Kalender prüfen.";
    }
    public ClockState Evaluate(DateTimeOffset utc)
    {
        var now=TimeZoneInfo.ConvertTime(utc,zone).DateTime;var date=DateOnly.FromDateTime(now);
        var holiday=holidays.FirstOrDefault(h=>date>=h.Start&&date<=h.End);
        var off=holiday?.Name??PublicHoliday(date)??(date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday?"Wochenende":null);
        bool shortened=holidays.Any(h=>h.Name=="Sommerferien"&&h.Start==date.AddDays(1));
        int seconds=(int)now.TimeOfDay.TotalSeconds;
        int index=off!=null?-1:Array.FindIndex(Slots,s=>seconds>=s.Start*60&&seconds<s.End*60&&(!shortened||s.End<=630));
        Slot? slot=index<0?null:Slots[index];bool before=off==null&&seconds<475*60;
        Slot? next=slot!=null&&index+1<Slots.Length?Slots[index+1]:null;if(shortened&&next?.End>630)next=null;
        int? remaining=slot!=null?slot.End*60-seconds:before?475*60-seconds:null;
        string label=before?"Die 1. Stunde beginnt in":slot==null?"Zeit zum Durchatmen":slot.Pause?"Bis zum Ende der Pause":"Bis zum Ende der Stunde";
        string note=before?"Start um 07:55 Uhr. Guten Morgen!":slot!=null?(next!=null?$"Danach: {next.Name}{(next.Start==675?" · Mensazeit":"")} · {Hm(next.Start)} Uhr":"Danach: Unterrichtsschluss. Schönen Feierabend!"):NextSchoolStart(date);
        var upcoming=holidays.FirstOrDefault(h=>h.Start>date);
        string calendar=date<new DateOnly(2026,1,1)||date>new DateOnly(2030,8,21)?"Ferienkalender nicht abgedeckt – bitte Termine prüfen.":holiday!=null?$"{holiday.Name}: {holiday.Start:dd.MM.yyyy} – {holiday.End:dd.MM.yyyy}":shortened?"Vor den Sommerferien: Schluss nach der 3. Stunde.":upcoming!=null?$"{upcoming.Name}: {upcoming.Start:dd.MM.yyyy} – {upcoming.End:dd.MM.yyyy}":"Ferientermine bis 21.08.2030.";
        return new(now,slot,index,remaining,slot!=null?(seconds-slot.Start*60)/(double)((slot.End-slot.Start)*60):0,off??slot?.Name??"Unterrichtsfreie Zeit",slot!=null?$"{Hm(slot.Start)} – {Hm(slot.End)} Uhr":off!=null?"Heute kein regulärer Unterricht":shortened?"Unterricht bis 10:30 Uhr":"07:55 – 16:35 Uhr",label,note,calendar);
    }
}
