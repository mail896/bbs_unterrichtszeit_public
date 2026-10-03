
'use strict';
const slots=[
{name:'1. Stunde',start:475,end:520},{name:'2. Stunde',start:520,end:565},
{name:'1. Pause',start:565,end:585,pause:true},{name:'3. Stunde',start:585,end:630},
{name:'4. Stunde',start:630,end:675},{name:'2. Pause',start:675,end:705,pause:true},
{name:'5. Stunde',start:705,end:750},{name:'6. Stunde',start:750,end:795},
{name:'3. Pause',start:795,end:805,pause:true},{name:'7. Stunde',start:805,end:850},
{name:'8. Stunde',start:850,end:895},{name:'4. Pause',start:895,end:905,pause:true},
{name:'9. Stunde',start:905,end:950},{name:'10. Stunde',start:950,end:995}];
const pad=value=>String(value).padStart(2,'0');
const formatTime=minute=>`${pad(Math.floor(minute/60))}:${pad(minute%60)}`;
const el=id=>document.getElementById(id);
const rows=slots.map(slot=>{const row=document.createElement('li');row.className=`slot${slot.pause?' pause':''}`;const name=document.createElement('span');name.textContent=slot.name+(slot.pause?` · ${slot.end-slot.start} Min.`:'');if(slot.start===675){const meal=document.createElement('span');meal.className='meal-label';meal.innerHTML='<svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"><circle cx="13" cy="12" r="6"/><path d="M3 3v6m3-6v6M3 7h3M4.5 9v12M22 3v18M22 3c-3 3-3 8 0 8"/></svg> Mensazeit';name.append(meal);}const time=document.createElement('span');time.className='slot-time';time.textContent=`${formatTime(slot.start)} – ${formatTime(slot.end)}`;row.append(name,time);el('schedule').append(row);return row;});
// Offizielle Ferienordnung des MK Niedersachsen, geprüft am 16.09.2026.
// Erster und letzter Tag jeweils inklusive. Kein automatischer externer Ferienabruf.
const holidays = [
['Weihnachtsferien','2025-12-22','2026-01-05'],
['Halbjahresferien','2026-02-02','2026-02-03'],['Osterferien','2026-03-23','2026-04-07'],['Brückentag','2026-05-15','2026-05-15'],['Pfingstferien','2026-05-26','2026-05-26'],['Sommerferien','2026-07-02','2026-08-12'],['Herbstferien','2026-10-12','2026-10-24'],['Weihnachtsferien','2026-12-23','2027-01-09'],
['Halbjahresferien','2027-02-01','2027-02-02'],['Osterferien','2027-03-22','2027-04-03'],['Brückentag','2027-05-07','2027-05-07'],['Pfingstferien','2027-05-18','2027-05-18'],['Sommerferien','2027-07-08','2027-08-18'],['Herbstferien','2027-10-16','2027-10-30'],['Weihnachtsferien','2027-12-23','2028-01-08'],
['Halbjahresferien','2028-01-31','2028-02-01'],['Osterferien','2028-04-10','2028-04-22'],['Brückentag','2028-05-26','2028-05-26'],['Pfingstferien','2028-06-06','2028-06-06'],['Sommerferien','2028-07-20','2028-08-30'],['Brückentag','2028-10-02','2028-10-02'],['Herbstferien','2028-10-23','2028-11-04'],['Weihnachtsferien','2028-12-27','2029-01-06'],
['Halbjahresferien','2029-02-01','2029-02-02'],['Osterferien','2029-03-19','2029-04-03'],['Brückentag','2029-04-30','2029-04-30'],['Brückentag','2029-05-11','2029-05-11'],['Pfingstferien','2029-05-22','2029-05-22'],['Sommerferien','2029-07-19','2029-08-29'],['Brückentage','2029-10-04','2029-10-05'],['Herbstferien','2029-10-22','2029-11-02'],['Weihnachtsferien','2029-12-21','2030-01-05'],
['Halbjahresferien','2030-01-31','2030-02-01'],['Osterferien','2030-04-08','2030-04-23'],['Brückentag','2030-05-31','2030-05-31'],['Pfingstferien','2030-06-11','2030-06-11'],['Sommerferien','2030-07-11','2030-08-21']
];
const dateFormatter=new Intl.DateTimeFormat('de-DE',{timeZone:'Europe/Berlin',weekday:'long',day:'numeric',month:'long',year:'numeric'});
const partsFormatter=new Intl.DateTimeFormat('en-GB',{timeZone:'Europe/Berlin',year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit',second:'2-digit',hourCycle:'h23'});
function berlinParts(date){return Object.fromEntries(partsFormatter.formatToParts(date).map(p=>[p.type,p.value]));}
function publicHoliday(key){
 const y=Number(key.slice(0,4)),md=key.slice(5);
 const fixed={'01-01':'Neujahr','05-01':'Tag der Arbeit','10-03':'Tag der Deutschen Einheit','10-31':'Reformationstag','12-25':'1. Weihnachtstag','12-26':'2. Weihnachtstag'};
 if(fixed[md])return fixed[md];
 // Gregorianischer Osteralgorithmus; bewegliche gesetzliche Feiertage Niedersachsen.
 const a=y%19,b=Math.floor(y/100),c=y%100,d=Math.floor(b/4),e=b%4,f=Math.floor((b+8)/25),g=Math.floor((b-f+1)/3),h=(19*a+b-d-g+15)%30,i=Math.floor(c/4),k=c%4,l=(32+2*e+2*i-h-k)%7,m=Math.floor((a+11*h+22*l)/451),month=Math.floor((h+l-7*m+114)/31),day=(h+l-7*m+114)%31+1;
 const easter=Date.UTC(y,month-1,day);
 return [[-2,'Karfreitag'],[1,'Ostermontag'],[39,'Christi Himmelfahrt'],[50,'Pfingstmontag']].find(([offset])=>new Date(easter+offset*86400000).toISOString().slice(0,10)===key)?.[1]||null;
}
function dayInfo(key){
 const holiday=holidays.find(([,start,end])=>key>=start&&key<=end);
 const weekday=new Date(key+'T12:00:00Z').getUTCDay();
 const off=holiday?.[0]||publicHoliday(key)||([0,6].includes(weekday)?'Wochenende':null);
 return {holiday,off,covered:key>='2026-01-01'&&key<='2030-08-21'};
}
let anchor=null,lastSync=0,syncFailed=false,syncing=false;
function clockNow(){return anchor?new Date(anchor.epoch+performance.now()-anchor.perf):new Date();}
function update(now=clockNow()){
 const p=berlinParts(now),key=`${p.year}-${p.month}-${p.day}`,seconds=Number(p.hour)*3600+Number(p.minute)*60+Number(p.second),day=dayInfo(key);
 // Am letzten Schultag vor den Sommerferien: Schluss nach der dritten Stunde.
 const tomorrow=new Date(Date.parse(key+'T12:00:00Z')+86400000).toISOString().slice(0,10);
 const shortDay=holidays.some(([name,start])=>name==='Sommerferien'&&start===tomorrow);
 const index=day.off?-1:slots.findIndex(slot=>seconds>=slot.start*60&&seconds<slot.end*60&&(!shortDay||slot.end<=630));
 const slot=slots[index];
 const beforeStart=!day.off&&seconds<slots[0].start*60;
 const following=slot?slots[index+1]:null;
 const nextSlot=following&&(!shortDay||following.end<=630)?following:null;
 el('clock').textContent=`${p.hour}:${p.minute}`;[...p.hour,...p.minute].forEach((digit,i)=>{if(el('digit-'+i).textContent!==digit)el('digit-'+i).textContent=digit;});el('meal-badge').hidden=!(slot&&slot.start===675);el('clock').dateTime=now.toISOString();el('date').textContent=dateFormatter.format(now);el('copyright-year').textContent=p.year;
 const setText=(id,text)=>{if(el(id).textContent!==text)el(id).textContent=text;};
 setText('status',day.off||(slot?slot.name:'Unterrichtsfreie Zeit'));
 setText('range',slot?`${formatTime(slot.start)} – ${formatTime(slot.end)} Uhr`:day.off?'Heute kein regulärer Unterricht':shortDay?'Unterricht bis 10:30 Uhr':'07:55 – 16:35 Uhr');
 setText('countdown-label',beforeStart?'Die 1. Stunde beginnt in':slot?(slot.pause?'Bis zum Ende der Pause':'Bis zum Ende der Stunde'):'Zeit zum Durchatmen');
 setText('note',beforeStart?'Start um 07:55 Uhr. Guten Morgen!':slot?(nextSlot?`Danach: ${nextSlot.name}${nextSlot.start===675?' · Mensazeit':''} · ${formatTime(nextSlot.start)} Uhr`:'Danach: Unterrichtsschluss. Schönen Feierabend!'):day.off?'Der Countdown pausiert. Genießen Sie Ihre freie Zeit.':'Für heute ist der Unterricht vorbei. Schönen Feierabend!');
 rows.forEach((row,i)=>{row.classList.toggle('active',i===index);if(i===index)row.setAttribute('aria-current','true');else row.removeAttribute('aria-current');});
 const remaining=slot?slot.end*60-seconds:beforeStart?slots[0].start*60-seconds:null;
 el('minutes').textContent=remaining===null?'--':pad(Math.floor(remaining/60));el('seconds').textContent=remaining===null?'--':pad(remaining%60);
 const progress=slot?(seconds-slot.start*60)/((slot.end-slot.start)*60)*100:0;
 el('progress-fill').style.width=`${progress}%`;el('progress-caption').textContent=beforeStart?'START UM 07:55':slot?`${Math.floor(progress)} % VERGANGEN`:'PAUSIERT';el('progress').setAttribute('aria-valuenow',String(Math.round(progress)));
 const next=holidays.find(([,start])=>start>key),pretty=value=>value.split('-').reverse().join('.');
 setText('calendar-note',!day.covered?'Ferienkalender nicht abgedeckt – bitte Termine prüfen.':day.holiday?`${day.holiday[0]} · ${pretty(day.holiday[1])} – ${pretty(day.holiday[2])}`:shortDay?'Vor den Sommerferien: Unterrichtsschluss nach der 3. Stunde.':next?`Als Nächstes: ${next[0]} · ${pretty(next[1])} – ${pretty(next[2])}`:'Ferientermine bis 21.08.2030 hinterlegt.');
 const fresh=anchor&&!syncFailed&&performance.now()-lastSync<360000;
 setText('time-source',fresh?'PTB-ZEIT':anchor?'ZEIT FORTGEFÜHRT':'GERÄTEZEIT');
 setText('sync-note',fresh?'Mit PTB-Zeit abgeglichen':anchor?'PTB nicht erreichbar · letzter Abgleich fortgeführt':'Gerätezeit · PTB-Abgleich ausstehend');
}
function syncTime(){
 if(syncing||document.hidden)return;
 syncing=true;let ws,finished=false,sent=0;
 const finish=(ok)=>{if(finished)return;finished=true;clearTimeout(timeout);syncing=false;syncFailed=!ok;if(ws)ws.close();update();};
 const timeout=setTimeout(()=>finish(false),8000);
 try{
 ws=new WebSocket('wss://uhr.ptb.de/time','time');
 ws.onopen=()=>{sent=performance.now();ws.send(JSON.stringify({c:sent}));};
 ws.onmessage=event=>{try{const data=JSON.parse(event.data),received=performance.now(),rtt=received-sent;if(data.c!==sent||!Number.isFinite(data.s)||data.s<1577836800000||data.s>4102444800000||rtt>3000)throw Error('Invalid time sample');anchor={epoch:data.s+rtt/2,perf:received};lastSync=received;finish(true);}catch{finish(false);}};
 ws.onerror=()=>finish(false);ws.onclose=()=>finish(false);
 }catch{finish(false);}
}
let presenting=false;
function setPresentation(enabled){
 presenting=enabled;document.body.classList.toggle('presentation',enabled);
 el('presentation-toggle').setAttribute('aria-pressed',String(enabled));
 el('presentation-toggle').textContent=enabled?'Ansicht schließen · Esc':'Präsentationsmodus';
}
el('presentation-toggle').addEventListener('click',()=>setPresentation(!presenting));
document.addEventListener('keydown',event=>{if(event.key==='Escape')setPresentation(false);});
update();syncTime();setInterval(update,1000);setInterval(syncTime,300000);
document.addEventListener('visibilitychange',()=>{if(!document.hidden){update();syncTime();}});
window.addEventListener('online',syncTime);
