/* Projekteigene Darstellungseinstellung; auch ohne verfügbaren Speicher nutzbar. */
(()=>{
 const key='uzeit.theme',allowed=['auto','light','dark'];
 const media=window.matchMedia('(prefers-color-scheme: dark)');
 let mode='auto';
 try{const saved=localStorage.getItem(key);if(allowed.includes(saved))mode=saved;}catch{}
 function apply(){
  const dark=mode==='dark'||(mode==='auto'&&media.matches);
  document.documentElement.dataset.theme=dark?'dark':'light';
  document.querySelector('meta[name="theme-color"]').content=dark?'#141615':'#f6f6f3';
 }
 apply();
 media.addEventListener('change',apply);
 document.addEventListener('DOMContentLoaded',()=>{
  const select=document.getElementById('theme-select');select.value=mode;
  select.addEventListener('change',()=>{mode=select.value;try{localStorage.setItem(key,mode);}catch{}apply();});
 });
 window.addEventListener('storage',event=>{if(event.key===key||event.key===null){mode=allowed.includes(event.newValue)?event.newValue:'auto';apply();const select=document.getElementById('theme-select');if(select)select.value=mode;}});
})();
