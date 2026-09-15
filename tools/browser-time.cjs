// Real WebGL life accounting and responsive shop. Boss interaction coverage: browser-boss-input.cjs.
const fs=require('fs'),assert=require('assert'),{chromium}=require('D:/Game/Work/tools/node_modules/playwright');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'}),ctx=await browser.newContext({viewport:{width:1600,height:900}}),page=await ctx.newPage();let count=0;const errors=[];
 page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>{if(/Exception|RuntimeError|missing from.*font asset/.test(m.text()))errors.push(m.text());});
 await ctx.addInitScript(()=>{if(window.name.startsWith('TT_FIXTURE:')){localStorage.setItem('TimeThiefSaveV1',window.name.slice(11));window.name='';}});
 const ready=async()=>{await page.waitForFunction(()=>window.TimeThiefSDK?.readyCalled&&TimeThiefSDK.unity,null,{timeout:120000});await page.waitForTimeout(700);};
 const click=async(x,y,ms=240)=>{const b=await page.locator('canvas').boundingBox();await page.mouse.click(b.x+b.width*x,b.y+b.height*y);await page.waitForTimeout(ms);};
 const save=async()=>{await page.evaluate(()=>TimeThiefSDK.unity.SendMessage('TimeThief','Persist'));await page.waitForTimeout(90);return page.evaluate(()=>JSON.parse(TimeThiefSDK.saved));};
 const check=(v,label)=>{assert(v,label);console.log('PASS '+label);count++;};
 const snap=async n=>page.screenshot({path:__dirname+'/../QA/time-'+n+'.png'});
 let base;
 const fixture=async f=>{await page.evaluate(f=>window.name='TT_FIXTURE:'+JSON.stringify({...f,updatedAt:Date.now()}),{...base,...f});await page.reload();await ready();await click(.265,.652,750);};
 const hit=async(x,y,ms=160)=>click(.5+(x-.5)*.48,1-(.505+(y-.5)*.62),ms);
 try{
  await page.goto('http://127.0.0.1:8080/?v=1.7.0');await ready();await click(.265,.652);base=await save();
  await fixture({economyVersion:0,phase:'Victory',lastReward:5,player:{...base.player,CurrentTime:2,MaxTime:10},shop:{...base.shop,shards:30,armorPurchases:1}});
  let a=await save();check(a.economyVersion===2&&a.shop.shards===0&&a.player.CurrentTime===5&&a.shop.armorPurchases===1,'old shards convert once with existing purchases preserved');
  await page.reload();await ready();await click(.265,.652,750);check((await save()).player.CurrentTime===5,'reloading migrated save cannot mint more life');
  await click(.815,.865);a=await save();await click(.175,.539);let b=await save();
  check(Math.abs(a.player.CurrentTime-b.player.CurrentTime-.85)<.001&&b.player.MaxTime===a.player.MaxTime+1,'capacity spends exactly 0.85 life seconds without healing');
  await fixture({economyVersion:2,phase:'Victory',player:{...base.player,CurrentTime:1.60},shop:{...base.shop,shards:99999}});await click(.815,.865);a=await save();await click(.645,.539);b=await save();
  check(a.player.CurrentTime===b.player.CurrentTime&&a.player.Attack===b.player.Attack,'cannot buy with last life second even with legacy shards');
  await fixture({phase:'Victory',economyVersion:2,lastTimeReward:.65,lastTimeAwarded:.65,player:{...base.player,CurrentTime:7,MaxTime:8}});
  for(const [w,h] of [[1000,576],[844,390],[390,844],[320,568]]){
   await page.setViewportSize({width:w,height:h});await page.waitForTimeout(400);await snap('results-'+w+'x'+h);await click(w<h?.735:.815,w<h?.81:.865);
   await snap('shop-'+w+'x'+h);await click(.735,.253);await snap('buffs-'+w+'x'+h);await click(.75,.955);await page.waitForTimeout(400);
  }
  await page.setViewportSize({width:1600,height:900});
  check(errors.length===0,'economy screens have no runtime or font errors');
  fs.writeFileSync(__dirname+'/../QA/time-checks.txt',count+' real WebGL checks passed: time currency, migration, safe reserve, responsive shop and reload.\n');
 }finally{fs.writeFileSync(__dirname+'/../QA/time-console.log',errors.join('\n'));await browser.close();}
})();
