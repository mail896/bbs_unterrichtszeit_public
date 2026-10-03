const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const html=fs.readFileSync(__dirname+'/../public/index.html','utf8');
const originalSlots=require("./fixtures/timetable.json");
function element(){return{addEventListener(){},textContent:'',style:{},attributes:{},children:[],classList:{toggle(){}},append(...children){this.children.push(...children)},setAttribute(k,v){this.attributes[k]=v},removeAttribute(k){delete this.attributes[k]}}}
const elements=new Map([...html.matchAll(/id="([^"]+)"/g)].map(m=>[m[1],element()]));
const context=vm.createContext({document:{getElementById:id=>{assert(elements.has(id),id);return elements.get(id)},createElement:element,addEventListener(){}},setInterval(){},setTimeout(){},clearTimeout(){},performance:{now:()=>1000},window:{addEventListener(){}},WebSocket:class {close(){}},Date,Intl});
vm.runInContext(fs.readFileSync(__dirname+'/../public/app.js','utf8'),context);
const slots=vm.runInContext('slots',context);
assert.deepEqual(Array.from(slots,s=>[s.start,s.end]),originalSlots.map(s=>[s.start,s.end]));
assert.equal(elements.get('schedule').children.length,14);
for(let second=0;second<86400;second++){
context.now=new Date(Date.UTC(2026,8,15,22,0,second));vm.runInContext('update(now)',context);
const slot=originalSlots.find(s=>second>=s.start*60&&second<s.end*60);
if(slot){const remaining=slot.end*60-second;assert.equal(elements.get('minutes').textContent,String(Math.floor(remaining/60)).padStart(2,'0'));assert.equal(elements.get('seconds').textContent,String(remaining%60).padStart(2,'0'));}
else if(second<475*60){const left=475*60-second;assert.equal(elements.get('minutes').textContent,String(Math.floor(left/60)).padStart(2,'0'));assert.equal(elements.get('seconds').textContent,String(left%60).padStart(2,'0'));}
else{assert.equal(elements.get('minutes').textContent,'--');assert.equal(elements.get('status').textContent,'Unterrichtsfreie Zeit');}
}
console.log('PASS: Alle 14 Original-Zeitabschnitte identisch; 86.400 Sekunden inklusive aller Stunden-/Pausenwechsel geprüft.');

for (const [date,expected] of [['2026-10-12','Herbstferien'],['2026-10-24','Herbstferien'],['2026-10-25','Wochenende'],['2027-01-09','Weihnachtsferien'],['2026-09-19','Wochenende'],['2026-05-14','Christi Himmelfahrt'],['2026-05-25','Pfingstmontag'],['2026-05-01','Tag der Arbeit']]) {
 context.now=new Date(date+'T10:00:00Z');vm.runInContext('update(now)',context);assert.equal(elements.get('status').textContent,expected);assert.equal(elements.get('minutes').textContent,'--');
}
context.now=new Date('2026-07-01T08:30:00Z');vm.runInContext('update(now)',context);assert.equal(elements.get('minutes').textContent,'--');
context.now=new Date('2031-09-16T08:00:00Z');vm.runInContext('update(now)',context);assert.match(elements.get('calendar-note').textContent,/nicht abgedeckt/);
for (const instant of ['2026-03-29T00:59:59Z','2026-03-29T01:00:00Z','2026-10-25T00:59:59Z','2026-10-25T01:00:00Z']) {context.now=new Date(instant);vm.runInContext('update(now)',context);assert.equal(elements.get('clock').textContent,new Intl.DateTimeFormat('de-DE',{timeZone:'Europe/Berlin',hour:'2-digit',minute:'2-digit'}).format(context.now));}
console.log('PASS: Feriengrenzen, Jahreswechsel, Wochenende, Feiertage, Sommerferien-Vortag, Kalenderablauf und Zeitumstellungen.');

for(const [instant,expected] of [['2026-09-16T09:50:00Z','Danach: 6. Stunde · 12:30 Uhr'],['2026-09-16T09:00:00Z','Danach: 2. Pause · Mensazeit · 11:15 Uhr'],['2026-07-01T08:00:00Z','Danach: Unterrichtsschluss. Schönen Feierabend!']]){context.now=new Date(instant);vm.runInContext('update(now)',context);assert.equal(elements.get('note').textContent,expected);}
context.now=new Date('2026-10-12T05:00:00Z');vm.runInContext('update(now)',context);assert.equal(elements.get('minutes').textContent,'--');
console.log('PASS: Danach-Hinweise, verkürzter Schultag und kein Startcountdown in Ferien.');
