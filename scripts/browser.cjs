const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../dist/pages');
const types={'.html':'text/html','.js':'text/javascript','.css':'text/css','.png':'image/png','.svg':'image/svg+xml','.webmanifest':'application/manifest+json'};
(async()=>{
const server=http.createServer((req,res)=>{const url=new URL(req.url,'http://localhost');let f=path.join(root,decodeURIComponent(url.pathname));if(!f.startsWith(root+path.sep)&&f!==root){res.writeHead(403).end();return;}if(fs.existsSync(f)&&fs.statSync(f).isDirectory())f=path.join(f,'index.html');if(!fs.existsSync(f)){res.writeHead(404).end();return;}res.setHeader('Content-Type',types[path.extname(f)]||'application/octet-stream');res.end(fs.readFileSync(f));});
await new Promise(r=>server.listen(0,'127.0.0.1',r));const base=`http://127.0.0.1:${server.address().port}`;
let browser;
try{browser=await chromium.launch({headless:true});const errors=[];
for(const [name,width,theme] of [['desktop',1440,'light'],['dunkel',1440,'dark'],['mobil',390,'light']]){
const page=await browser.newPage({viewport:{width,height:1000},colorScheme:theme});page.on('pageerror',e=>errors.push(e.message));
await page.clock.install({time:new Date('2026-09-16T09:20:00Z')});
await page.routeWebSocket('wss://uhr.ptb.de/time',ws=>ws.close());
await page.goto(base+'/demo/');await page.locator('#theme-select').selectOption(theme);await page.clock.runFor(1000);
assert.equal(await page.locator('#status').innerText(),'2. Pause');assert.equal(await page.locator('#clock').innerText(),'11:20');
assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
assert.equal(await page.locator('.desktop-downloads .download-card').count(),3);
if(process.argv.includes('--screenshots'))await page.screenshot({path:path.join(__dirname,'../images',name+'.png'),fullPage:true});
await page.locator('#theme-select').selectOption('auto');await page.emulateMedia({colorScheme:'dark'});await page.waitForFunction(()=>document.documentElement.dataset.theme==='dark');assert.equal(await page.locator('html').getAttribute('data-theme'),'dark');
await page.locator('#presentation-toggle').click();assert.equal(await page.locator('.desktop-downloads').isVisible(),false);await page.close();console.log('PASS demo',name);
}
if(process.argv.includes('--screenshots'))for(const file of ['desktop.png','dunkel.png','mobil.png'])fs.copyFileSync(path.join(__dirname,'../images',file),path.join(root,'images',file));
for(const pagePath of ['/','/en.html']){const page=await browser.newPage({viewport:{width:390,height:850}});await page.goto(base+pagePath);assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));assert(await page.locator('a[href="demo/"]').count()>0);await page.locator('footer').scrollIntoViewIfNeeded();await page.waitForTimeout(300);const broken=await page.evaluate(()=>Array.from(document.images).filter(i=>!i.complete||i.naturalWidth===0).map(i=>i.src));assert.deepEqual(broken,[]);await page.close();console.log('PASS landing',pagePath);}
assert.deepEqual(errors,[]);
}finally{if(browser)await browser.close();server.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
