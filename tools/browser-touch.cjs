const fs=require('fs'),path=require('path'),assert=require('assert'),{chromium}=require('D:/Game/Work/tools/node_modules/playwright');
const qa=path.resolve(__dirname,'../QA');
(async()=>{
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'});
const ctx=await browser.newContext({viewport:{width:390,height:844},isMobile:true,hasTouch:true,deviceScaleFactor:2});
const page=await ctx.newPage(),cdp=await ctx.newCDPSession(page);const logs=[];let checks=0;
const check=(v,label)=>{assert.ok(v,label);checks++;console.log('PASS '+label);};
page.on('pageerror',e=>logs.push(String(e)));
const tap=async(x,y,delay=300)=>{await page.touchscreen.tap(x,y);await page.waitForTimeout(delay);};
const save=async()=>{await page.evaluate(()=>TimeThiefSDK.unity.SendMessage('TimeThief','Persist'));await page.waitForTimeout(80);return page.evaluate(()=>JSON.parse(TimeThiefSDK.saved));};
const snap=async n=>page.screenshot({path:qa+'/'+n+'.png',scale:'css'});
const hold=async ms=>{await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:195,y:430}]});await page.waitForTimeout(ms);await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await page.waitForTimeout(800);};
try{
 await page.goto('http://127.0.0.1:8080');await page.waitForFunction(()=>window.TimeThiefSDK?.readyCalled&&TimeThiefSDK.unity,null,{timeout:120000});await page.waitForTimeout(800);await snap('17-mobile-menu');
 await tap(195,656);check((await save()).phase==='Intro','touch starts a run');await snap('18-mobile-intro');
 await tap(297,751,80);for(let i=0;i<6;i++)await tap(195,430,130);await page.waitForTimeout(750);let s=await save();check(s.phase==='Victory'&&s.defeated===1&&s.physicalHits>=3,'touch taps defeat magic keeper');
 await tap(287,767);check((await save()).level===2,'touch advances one level');await tap(297,751);await tap(195,430,150);s=await save();check(s.physicalHits===1,'short touch is one physical attack');
 await hold(3800);check((await save()).phase==='Victory','tap and hold can be combined on mobile');
 await page.setViewportSize({width:844,height:390});await page.waitForTimeout(600);await snap('19-phone-landscape');
 check(await page.evaluate(()=>document.documentElement.scrollHeight===innerHeight&&document.documentElement.scrollWidth===innerWidth),'phone layout has no page scrolling');
 check(logs.length===0,'touch input has no JavaScript errors');
 fs.writeFileSync(qa+'/touch-checks.txt',checks+' WebGL mobile-emulation checks passed at 390x844, DPR 2, touch events and 844x390 landscape. Physical-device testing is still separate.\n');
}finally{fs.writeFileSync(qa+'/touch-console.log',logs.join('\n'));await browser.close();}
})();
