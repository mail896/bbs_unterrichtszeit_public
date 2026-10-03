from datetime import datetime,timedelta
from pathlib import Path
from school import SchoolClock,SLOTS,public_holiday
s=SchoolClock(Path(__file__).parent/'assets/calendar.json')
start=datetime.fromisoformat('2026-09-17T00:00:00+02:00')
for second in range(86400):
 state=s.evaluate(start+timedelta(seconds=second));slot=next((slot for slot in SLOTS if slot.start*60<=second<slot.end*60),None)
 assert state['slot']==slot
 assert state['remaining']==(slot.end*60-second if slot else 475*60-second if second<475*60 else None)
for day,status in [('2026-10-12','Herbstferien'),('2026-10-24','Herbstferien'),('2026-09-19','Wochenende'),('2026-05-14','Christi Himmelfahrt'),('2026-05-25','Pfingstmontag')]:
 state=s.evaluate(datetime.fromisoformat(day+'T10:00:00+02:00'));assert state['status']==status and state['remaining'] is None
assert s.evaluate(datetime.fromisoformat('2026-07-01T10:30:00+02:00'))['remaining'] is None
assert s.evaluate(datetime.fromisoformat('2026-03-29T01:00:00+00:00'))['now'].hour==3
assert s.evaluate(datetime.fromisoformat('2026-10-25T01:00:00+00:00'))['now'].hour==2
assert 'prüfen' in s.evaluate(datetime.fromisoformat('2031-09-17T10:00:00+02:00'))['calendar']
assert '21.09.' in s.next_start(datetime(2026,9,18).date())
assert '26.10.' in s.next_start(datetime(2026,10,9).date())
print('PASS: 86400 Sekunden, Ferien, Feiertage, Wochenende, verkürzter Tag, DST und Kalendergrenze')
