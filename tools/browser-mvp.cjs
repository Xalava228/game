// Actual WebGL regression coverage for the redesigned battle and persisted boss phases.
const fs=require('fs'),assert=require('assert'),{chromium}=require('D:/Game/Work/tools/node_modules/playwright');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'});
 const ctx=await browser.newContext({viewport:{width:1600,height:900}}),page=await ctx.newPage();let checks=0;const errors=[];
 page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>{if(/Exception|RuntimeError|missing from.*font asset/.test(m.text()))errors.push(m.text());});
 const ready=async()=>{await page.waitForFunction(()=>window.TimeThiefSDK?.unity&&TimeThiefSDK.readyCalled,null,{timeout:120000});await page.waitForTimeout(800);};
 const click=async(x,y,ms=250)=>{const b=await page.locator('canvas').boundingBox();await page.mouse.click(b.x+b.width*x,b.y+b.height*y);await page.waitForTimeout(ms);};
 const save=async()=>{await page.evaluate(()=>TimeThiefSDK.unity.SendMessage('TimeThief','Persist'));await page.waitForTimeout(80);return page.evaluate(()=>JSON.parse(TimeThiefSDK.saved));};
 const check=(ok,label)=>{assert.ok(ok,label);checks++;console.log('PASS '+label);};
 await ctx.addInitScript(()=>{if(window.name.startsWith('TT_FIXTURE:')){localStorage.setItem('TimeThiefSaveV1',window.name.slice(11));window.name='';}});
 const snap=async name=>page.screenshot({path:__dirname+'/../QA/'+name+'.png'});
 try{
  await page.goto('http://127.0.0.1:8080');await ready();await click(.265,.652);let base=await save();
  const fixture={...base,seed:1,level:25,phase:'Intro',bossPhase2:false,enemyTime:7,enemyTimer:4,elapsed:0,enemyAttacks:0,physicalHits:0,player:{...base.player,CurrentTime:100,MaxTime:100,Attack:1,CritChance:0},updatedAt:Date.now()};
  await page.evaluate(f=>window.name='TT_FIXTURE:'+JSON.stringify(f),fixture);await page.reload();await ready();await click(.265,.652);await click(.5,.935);await click(.5,.855);
  const before=await save();await page.waitForTimeout(1100);let after=await save();
  check(before.player.CurrentTime===after.player.CurrentTime&&before.enemyTime===after.enemyTime&&before.elapsed===after.elapsed,'reading tactics freezes both actors and the attack timer');
  await snap('23-tactics-desktop');await page.setViewportSize({width:390,height:844});await page.waitForTimeout(400);await snap('24-tactics-mobile');await click(.5,.80);await page.waitForTimeout(250);
  after=await save();check(after.elapsed>before.elapsed&&after.player.CurrentTime<before.player.CurrentTime,'closing tactics resumes the active battle');
  await page.setViewportSize({width:1600,height:900});await page.waitForTimeout(350);
  const b=await page.locator('canvas').boundingBox();
  for(let i=0;i<14&&!after.bossPhase2;i++){
   const targetX=.5-.2*Math.min(b.width*.48,b.height*.62)/b.width;
   await page.mouse.move(b.x+b.width*targetX,b.y+b.height*.495);
   if(i===0)await page.mouse.down();await page.waitForTimeout(350);after=await save();
  }
  await page.mouse.up();
  check(after.bossPhase2&&after.phase==='Fighting','real boss enters its second phase during combat');await snap('25-boss-phase');
  await page.reload();await ready();await click(.265,.652);after=await save();
  check(after.phase==='Intro'&&after.bossPhase2,'reload preserves boss phase and requires an explicit fight start');
  await page.setViewportSize({width:390,height:844});await page.waitForTimeout(450);await snap('26-boss-phase-mobile');
  check(errors.length===0,'new GUI and title font produce no runtime or missing-glyph errors');
  fs.writeFileSync(__dirname+'/../QA/mvp-checks.txt',checks+' real Unity WebGL MVP checks passed: tactics pause, resume, live boss phase and phase persistence.\n');
 }finally{fs.writeFileSync(__dirname+'/../QA/mvp-console.log',errors.join('\n'));await browser.close();}
})();
