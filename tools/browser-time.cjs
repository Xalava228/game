// Real WebGL interactions: life accounting, old-save migration, responsive shop, ten bosses.
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
  await page.goto('http://127.0.0.1:8080/?v=1.6.0');await ready();await click(.265,.652);base=await save();
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
  for(let n=1;n<=10;n++){
   await fixture({level:n*25,phase:'Intro',bossPhase2:false,bossStage:0,bossProgress:0,bossWindow:0,enemyTime:100,enemyTimer:n===7?.5:3,elapsed:0,enemyAttacks:0,physicalHits:0,player:{...base.player,CurrentTime:500,MaxTime:500,Attack:.2,CritChance:0}});
   await snap('boss-'+n+'-intro');await click(.5,.935,30);
   if(n===2){await hit(.28,.43);await hit(.72,.43);await hit(.5,.72);}
   else if(n===3){await hit(.28,.5);}
   else if(n===4){for(let i=0;i<3;i++)await hit(.5,.28);}
   else if(n===7){await hit(.73,.72,30);}
   else if(n===9){for(let i=0;i<10;i++)await hit(.5,.5,140);}
   else if(n===10){await hit(.5,.5,160);await hit(.5,.5,160);}
   else await hit(n===1?.30:.5,n===6?.74:.5);
   b=await save();check(b.phase==='Fighting'&&b.physicalHits>0,'boss '+n+' receives real pointer input');
   if(n===2||n===4)check(b.bossWindow>0,'boss '+n+' opens the correct weak point');
   if(n===3)check(b.bossStage===1,'twin clock changes the active side');
   if(n===7)check(b.enemyAttacks===0&&b.enemyTimer>1&&b.bossWindow>0,'stinger tap cancels the imminent attack');
   if(n===9)check(b.bossWindow>0,'phoenix overheats under rapid taps');
   if(n===10)check(b.bossStage===1,'bell cannot gain two seals in one beat');
   await snap('boss-'+n+'-battle');await page.setViewportSize({width:390,height:844});await page.waitForTimeout(250);await snap('boss-'+n+'-mobile');await page.setViewportSize({width:1600,height:900});
   if(n===2){await page.reload();await ready();await click(.265,.652);const r=await save();check(r.phase==='Intro'&&r.bossWindow>0,'boss mechanic progress persists and resumes paused');}
  }
  check(errors.length===0,'new economy and all boss screens have no runtime or font errors');
  fs.writeFileSync(__dirname+'/../QA/time-checks.txt',count+' real WebGL checks passed: time currency, migration, safe reserve, pointer targets, boss mechanics and reload.\n');
 }finally{fs.writeFileSync(__dirname+'/../QA/time-console.log',errors.join('\n'));await browser.close();}
})();
