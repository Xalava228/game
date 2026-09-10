const fs=require('fs'),path=require('path'),assert=require('assert'),{chromium}=require('D:/Game/Work/tools/node_modules/playwright');
const qa=path.resolve(__dirname,'../QA');fs.mkdirSync(qa,{recursive:true});
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--autoplay-policy=no-user-gesture-required']});
 const context=await browser.newContext({viewport:{width:1600,height:900}}),page=await context.newPage();const messages=[];let checks=0;
 page.on('console',m=>messages.push(m.type()+': '+m.text()));page.on('pageerror',e=>messages.push('PAGEERROR '+e));
 const ready=async()=>{await page.waitForFunction(()=>window.TimeThiefSDK?.readyCalled&&window.TimeThiefSDK?.unity,null,{timeout:120000});await page.waitForTimeout(700);};
 const snap=async n=>page.screenshot({path:qa+'/'+n+'.png'});
 const click=async(x,y,delay=220)=>{const v=page.viewportSize(),b=await page.locator("canvas").boundingBox();await page.mouse.click(b.x+x/v.width*b.width,b.y+y/v.height*b.height);await page.waitForTimeout(delay);};
 const save=async()=>{await page.evaluate(()=>TimeThiefSDK.unity.SendMessage('TimeThief','Persist'));await page.waitForTimeout(80);return page.evaluate(()=>JSON.parse(TimeThiefSDK.saved));};
 const check=(v,label)=>{assert.ok(v,label);checks++;console.log('PASS '+label);};
 await page.addInitScript(()=>{if(window.name.startsWith('TT_FIXTURE:')){localStorage.setItem('TimeThiefSaveV1',window.name.slice(11));window.name='';}});
 try {
  await page.goto('http://127.0.0.1:8080');await ready();await snap('01-menu');
  await click(424,587);await snap('02-intro');let s=await save();check(s.level===1&&s.phase==='Intro','new run opens paused tutorial');
  await click(1046,801,30);for(let i=0;i<6;i++)await click(800,470,140);await page.waitForTimeout(700);s=await save();check(s.phase==='Victory'&&s.defeated===1,'taps defeat the magic keeper');await snap('03-victory');
  check(s.lastGrowth?.length===6&&Math.abs(s.lastGrowth[0]-.04)<.0001,'results store actual growth of all six stats');
  const time=s.player.CurrentTime;await page.waitForTimeout(700);s=await save();check(Math.abs(time-s.player.CurrentTime)<.001,'time freezes on results');
  await click(864,797);await snap('04-stats');await click(1200,824);await click(1112,797);await snap('05-shop');
  await click(1200,833);await click(1400,797);s=await save();check(s.level===2&&s.phase==='Intro','continue advances exactly one level');
  await click(1046,801,220);await click(1504,48,30);const paused=await save();await page.waitForTimeout(600);check(Math.abs((await save()).player.CurrentTime-paused.player.CurrentTime)<.001,'manual pause freezes time');await snap('06-pause');await click(800,576,25);
  await page.mouse.move(800,470);await page.mouse.down();await page.waitForTimeout(3800);await page.mouse.up();await page.waitForTimeout(700);s=await save();check(s.phase==='Victory'&&s.defeated===2,'resume and magic work');
  await click(1112,797);const before=await save();await click(447,369);const purchased=await save();check(Math.abs(purchased.player.MaxTime-before.player.MaxTime-1)<.0001&&purchased.shop.shards<before.shop.shards,'shop spends shards and increases capacity');
  await page.reload();await ready();await click(424,587);s=await save();check(s.level===2&&s.phase==='Victory'&&s.shop.maxTimePurchases===1,'reload restores run and shop state');
  await snap('20-resumed-results');
  // Fixture states exist only in the isolated test browser; the released game contains no debug controls.
  const fixture={...s,level:25,phase:'Intro',bossPhase2:true,enemyTime:3,enemyTimer:4,elapsed:0,enemyAttacks:0,physicalHits:0,rewardDoubled:false,player:{...s.player,CurrentTime:30,MaxTime:30,Attack:10}};
  await page.evaluate(f=>window.name='TT_FIXTURE:'+JSON.stringify({...f,updatedAt:Date.now()}),fixture);await page.reload();await ready();await click(424,587);await snap('07-boss-intro');await click(1046,801,220);await click(800,470,200);await page.waitForTimeout(750);s=await save();check(s.phase==='RewardSelection','boss defeat opens three rewards');await snap('08-rewards');await click(304,629,750);s=await save();check(s.phase==='Victory'&&s.totalBossesDefeated===1&&s.lastGift,'exactly one boss reward is committed and described');
  await page.setViewportSize({width:390,height:844});await page.waitForTimeout(450);await snap('09-mobile-results');await click(103,671);await snap('10-mobile-stats');await click(292,773,800);await click(287,671);await snap('11-mobile-shop');const mobileBefore=await save();await click(155,302);check((await save()).shop.maxTimePurchases===mobileBefore.shop.maxTimePurchases+1,'portrait shop button buys an upgrade');
  await page.setViewportSize({width:1024,height:768});await page.waitForTimeout(400);await snap('12-tablet-shop');
  check(!messages.some(m=>/PAGEERROR|NullReferenceException|RuntimeError|InvalidOperationException|missing from.*font asset/i.test(m)),'no runtime or missing-glyph errors');
  fs.writeFileSync(qa+'/browser-checks.txt',checks+' real Unity WebGL browser checks passed.\nDesktop 1600x900, portrait 390x844, tablet 1024x768.\n');
 }finally{fs.writeFileSync(qa+'/browser-console.log',messages.join('\n'));await browser.close();}
})();
