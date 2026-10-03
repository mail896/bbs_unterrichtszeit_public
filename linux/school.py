"""Local timetable and Niedersachsen calendar, shared data with the Windows app."""
from dataclasses import dataclass
from datetime import date, datetime, timedelta
from zoneinfo import ZoneInfo
import json

BERLIN = ZoneInfo('Europe/Berlin')
@dataclass(frozen=True)
class Slot:
    name: str
    start: int
    end: int
    pause: bool = False
    @property
    def span(self): return f'{hm(self.start)} – {hm(self.end)}'

def hm(minutes): return f'{minutes//60:02}:{minutes%60:02}'
SLOTS = [Slot('1. Stunde',475,520),Slot('2. Stunde',520,565),Slot('1. Pause',565,585,True),Slot('3. Stunde',585,630),Slot('4. Stunde',630,675),Slot('2. Pause',675,705,True),Slot('5. Stunde',705,750),Slot('6. Stunde',750,795),Slot('3. Pause',795,805,True),Slot('7. Stunde',805,850),Slot('8. Stunde',850,895),Slot('4. Pause',895,905,True),Slot('9. Stunde',905,950),Slot('10. Stunde',950,995)]
FIRST, LAST = date(2026,1,1), date(2030,8,21)

def public_holiday(day):
    fixed={(1,1):'Neujahr',(5,1):'Tag der Arbeit',(10,3):'Tag der Deutschen Einheit',(10,31):'Reformationstag',(12,25):'1. Weihnachtstag',(12,26):'2. Weihnachtstag'}
    if (day.month,day.day) in fixed: return fixed[(day.month,day.day)]
    y=day.year; a=y%19; b=y//100; c=y%100; d=b//4; e=b%4; f=(b+8)//25; g=(b-f+1)//3
    h=(19*a+b-d-g+15)%30; i=c//4; k=c%4; l=(32+2*e+2*i-h-k)%7; m=(a+11*h+22*l)//451
    n=h+l-7*m+114; easter=date(y,n//31,n%31+1)
    return {-2:'Karfreitag',1:'Ostermontag',39:'Christi Himmelfahrt',50:'Pfingstmontag'}.get((day-easter).days)

class SchoolClock:
    def __init__(self, path):
        self.holidays=[(h['Name'],date.fromisoformat(h['Start']),date.fromisoformat(h['End'])) for h in json.loads(path.read_text())]
    def holiday(self,day): return next((h for h in self.holidays if h[1]<=day<=h[2]),None)
    def off(self,day):
        holiday=self.holiday(day)
        return holiday[0] if holiday else public_holiday(day) or ('Wochenende' if day.weekday()>4 else None)
    def next_start(self,day):
        if FIRST<=day<=LAST:
            candidate=day+timedelta(days=1)
            while candidate<=LAST:
                if not self.off(candidate):
                    when='Morgen' if candidate==day+timedelta(days=1) else ['Mo','Di','Mi','Do','Fr','Sa','So'][candidate.weekday()]+candidate.strftime(', %d.%m.')
                    return f'Nächster Unterricht: {when} · 07:55 Uhr'
                candidate+=timedelta(days=1)
        return 'Nächsten Schultag bitte im Kalender prüfen.'
    def evaluate(self,now):
        now=now.astimezone(BERLIN);day=now.date();sec=now.hour*3600+now.minute*60+now.second
        off=self.off(day);short=any(n=='Sommerferien' and a==day+timedelta(days=1) for n,a,b in self.holidays)
        index=next((i for i,s in enumerate(SLOTS) if not off and s.start*60<=sec<s.end*60 and (not short or s.end<=630)),None)
        slot=SLOTS[index] if index is not None else None
        before=not off and sec<475*60
        remaining=slot.end*60-sec if slot else 475*60-sec if before else None
        upcoming=SLOTS[index+1] if slot and index+1<len(SLOTS) else None
        if short and upcoming and upcoming.end>630:upcoming=None
        note=self.next_start(day) if remaining is None else 'Start um 07:55 Uhr. Guten Morgen!' if before else f'Danach: {upcoming.name}{" · Mensazeit" if upcoming.start==675 else ""} · {hm(upcoming.start)} Uhr' if upcoming else 'Danach: Unterrichtsschluss. Schönen Feierabend!'
        holiday=self.holiday(day) or next((h for h in self.holidays if h[1]>day),None)
        calendar='Ferienkalender nicht abgedeckt – bitte Termine prüfen.' if not FIRST<=day<=LAST else 'Vor den Sommerferien: Schluss nach der 3. Stunde.' if short and not off else f'{holiday[0]}: {holiday[1]:%d.%m.%Y} – {holiday[2]:%d.%m.%Y}' if holiday else 'Ferientermine bis 21.08.2030.'
        greeting='Schönen Feierabend!' if not off else 'Schönes Wochenende!' if off=='Wochenende' else 'Schöne Ferien!' if 'ferien' in off.lower() else 'Genieße deinen freien Tag!'
        return dict(now=now,status=off or (slot.name if slot else 'Guten Morgen' if before else 'Feierabend'),index=index,slot=slot,remaining=remaining,progress=(sec-slot.start*60)/((slot.end-slot.start)*60) if slot else 0,label='Die 1. Stunde beginnt in' if before else 'Bis zum Ende der Pause' if slot and slot.pause else 'Bis zum Ende der Stunde',note=note,calendar=calendar,greeting=greeting)
