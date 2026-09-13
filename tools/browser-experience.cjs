// Experience checks against the release WebGL. Fixtures are isolated browser saves, never game cheats.
const fs=require('fs'),path=require('path'),assert=require('assert');
const {chromium}=require('D:/Game/Work/tools/node_modules/playwright');
const qa=path.resolve(__dirname,'../QA');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'});
 const ctx=await browser.newContext({viewport:{width:1600,height:900}}),page=await ctx.newPage();
 let checks=0;const logs=[];
 const check=(v,label)=>{assert.ok(v,label);checks++;console.log('PASS '+label);};
 page.on('console',m=>logs.push(m.type()+': '+m.text()));page.on('pageerror',e=>logs.push('PAGEERROR '+e));
 await ctx.addInitScript(()=>{
   // Observe actual Web Audio output, without altering the game's gain or playback behavior.
   const original=AudioNode.prototype.connect;
   AudioNode.prototype.connect=function(destination,...rest){
     if(destination===this.context.destination){
       if(!window.__musicAnalyser){window.__musicAnalyser=this.context.createAnalyser();window.__musicAnalyser.fftSize=2048;}
       original.call(this,window.__musicAnalyser);
     }
     return original.call(this,destination,...rest);
   };
 });
 const ready=async()=>{await page.waitForFunction(()=>window.TimeThiefSDK?.readyCalled&&TimeThiefSDK.unity,null,{timeout:120000});await page.waitForTimeout(800);};
 const click=async(x,y,delay=240)=>{await page.mouse.click(x,y);await page.waitForTimeout(delay);};
 const save=async()=>{await page.evaluate(()=>TimeThiefSDK.unity.SendMessage('TimeThief','Persist'));await page.waitForTimeout(90);return page.evaluate(()=>JSON.parse(TimeThiefSDK.saved));};
 const snap=async n=>page.screenshot({path:qa+'/'+n+'.png'});
 const energy=async()=>page.evaluate(()=>{const a=window.__musicAnalyser;if(!a)return -1;const b=new Float32Array(a.fftSize);a.getFloatTimeDomainData(b);return Math.sqrt(b.reduce((s,v)=>s+v*v,0)/b.length);});
 await page.addInitScript(()=>{if(window.name.startsWith('TT_FIXTURE:')){localStorage.setItem('TimeThiefSaveV1',window.name.slice(11));window.name='';}});
 let base;
 const fixture=async overrides=>{const s={...base,...overrides,updatedAt:Date.now()};await page.evaluate(f=>window.name='TT_FIXTURE:'+JSON.stringify(f),s);await page.reload();await ready();await click(424,587);};
 try {
   await page.goto('http://127.0.0.1:8080');await ready();
   await click(424,587);base=await save();
   await page.waitForTimeout(1600);check(await energy()>.0001,'music produces real audio after user gesture');
   await click(1504,48);await page.waitForTimeout(200);check(await energy()<.00001,'sound switch mutes all output');
   await click(1504,48);await page.waitForTimeout(600);check(await energy()>.0001,'sound switch restores music');
   await page.reload();await ready();await click(424,587);check((await save()).level===1,'resume remains available after audio toggle');
   const player={...base.player,CurrentTime:100,MaxTime:100,Attack:.5,CritChance:.75};
   await fixture({level:125,phase:'Intro',enemyTime:10,enemyTimer:.35,enemyAttacks:2,elapsed:0,physicalHits:0,player});
   await click(800,842,900);await snap('13-heavy-boss-strike');
   let s=await save();check(s.enemyAttacks===3&&s.player.CurrentTime<99,'third boss strike removes time');
   await click(1411,48);const paused=await save();await page.waitForTimeout(500);check(Math.abs((await save()).player.CurrentTime-paused.player.CurrentTime)<.001,'pause stops combat after heavy strike');check(await energy()<.00001,'pause silences music');
   // Visibility/focus pause must not clear a manual pause.
   await page.evaluate(()=>window.dispatchEvent(new Event('blur')));await page.evaluate(()=>window.dispatchEvent(new Event('focus')));await page.waitForTimeout(400);
   check(Math.abs((await save()).player.CurrentTime-paused.player.CurrentTime)<.001,'focus restoration preserves manual pause');
   await click(800,576);await page.waitForTimeout(300);check(await energy()>.0001,'resume restores music');
   await page.evaluate(()=>window.dispatchEvent(new Event('blur')));const hidden=await save();await page.waitForTimeout(500);check(Math.abs((await save()).player.CurrentTime-hidden.player.CurrentTime)<.001,'focus loss pauses time');check(await energy()<.00001,'focus loss silences music');
   await page.evaluate(()=>window.dispatchEvent(new Event('focus')));await page.waitForTimeout(250);check((await save()).player.CurrentTime<hidden.player.CurrentTime,'focus restoration resumes active combat');
   await fixture({level:1,phase:'Intro',enemyTime:4,enemyTimer:4,enemyAttacks:0,elapsed:0,physicalHits:0,player});
   await click(800,842);await click(800,430,120);await snap('14-player-hit');s=await save();check(s.physicalHits===1&&s.enemyTime<4+s.elapsed-.4,'tap creates exactly one physical attack while passive time still transfers');
   const physical=s.physicalHits;await page.mouse.move(800,430);await page.mouse.down();await page.waitForTimeout(550);await page.mouse.move(40,40);await page.waitForTimeout(60);const cancelled=await save();await page.waitForTimeout(250);await page.mouse.up();s=await save();check(s.physicalHits===physical&&Math.abs((s.enemyTime-cancelled.enemyTime)-(cancelled.player.CurrentTime-s.player.CurrentTime))<.015,'leaving target cancels magic without a release hit');
   await fixture({level:3,phase:'Intro',enemyTime:4,enemyTimer:4,enemyAttacks:0,elapsed:0,physicalHits:0,player:{...player,CurrentTime:.6}});
   await click(800,842,1500);s=await save();check(s.phase==='GameOver'&&s.player.CurrentTime===0,'time expiry opens game over');await snap('15-game-over');
   await page.goto('http://127.0.0.1:8080/?lang=en');await ready();await snap('16-english-menu');check(await page.evaluate(()=>TimeThiefSDK.lang==='en'),'English environment selected');
   check(!logs.some(m=>/PAGEERROR|NullReferenceException|RuntimeError|InvalidOperationException|missing from.*font asset/i.test(m)),'experience flows have no runtime errors');
   fs.writeFileSync(qa+'/experience-checks.txt',checks+' real WebGL checks passed: audio playback, mute, pause, focus, combat feedback, tap, pointer exit, defeat and English.\n');
 } finally {fs.writeFileSync(qa+'/experience-console.log',logs.join('\n'));await browser.close();}
})();
