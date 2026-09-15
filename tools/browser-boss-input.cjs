// Production WebGL, real mouse/touch events, HP deltas and persisted mechanic state.
const fs=require('fs'),assert=require('assert'),{chromium}=require('D:/Game/Work/tools/node_modules/playwright');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'});
 let checks=0;const errors=[],report=[];
 const check=(ok,label,detail)=>{assert.ok(ok,label+' '+JSON.stringify(detail||''));checks++;console.log('PASS '+label);report.push({label,detail});};
 try{
 for(const mobile of [false,true]){
  const ctx=await browser.newContext({viewport:mobile?{width:390,height:844}:{width:1600,height:900},hasTouch:mobile,isMobile:mobile,deviceScaleFactor:mobile?2:1}),page=await ctx.newPage(),cdp=await ctx.newCDPSession(page);
  page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>{if(/Exception|RuntimeError|missing from.*font asset/.test(m.text()))errors.push(m.text());});
  await ctx.addInitScript(()=>{if(window.name.startsWith('TT_FIXTURE:')){localStorage.setItem('TimeThiefSaveV1',window.name.slice(11));window.name='';}});
  const ready=async()=>{await page.waitForFunction(()=>window.TimeThiefSDK?.readyCalled&&TimeThiefSDK.unity,null,{timeout:120000});await page.waitForTimeout(650);};
  const save=async()=>{await page.evaluate(()=>TimeThiefSDK.unity.SendMessage('TimeThief','Persist'));await page.waitForTimeout(70);return page.evaluate(()=>JSON.parse(TimeThiefSDK.saved));};
  const click=async(x,y,ms=260)=>{const b=await page.locator('canvas').boundingBox();if(mobile)await page.touchscreen.tap(b.x+b.width*x,b.y+b.height*y);else await page.mouse.click(b.x+b.width*x,b.y+b.height*y);await page.waitForTimeout(ms);};
  let pressed=false,px,py;
  const move=async(x,y)=>{const b=await page.locator('canvas').boundingBox(),portrait=b.height>b.width,side=Math.min(b.width*(portrait?.94:.48),b.height*(portrait?.47:.62));px=b.x+b.width*.5+(x-.5)*side;py=b.y+b.height*(1-(portrait?.489:.505))-(y-.5)*side;if(mobile){if(pressed)await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:px,y:py,id:1}]});}else await page.mouse.move(px,py);};
  const down=async()=>{pressed=true;if(mobile)await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:px,y:py,id:1}]});else await page.mouse.down();};
  const up=async(cancel=false)=>{if(mobile)await cdp.send('Input.dispatchTouchEvent',{type:cancel?'touchCancel':'touchEnd',touchPoints:[]});else await page.mouse.up();pressed=false;await page.waitForTimeout(30);};
  const snap=async(name)=>page.screenshot({path:__dirname+'/../QA/boss-input-'+(mobile?'touch-':'mouse-')+name+'.png',scale:'css'});
  await page.goto('http://127.0.0.1:8080/?v=1.7.0');await ready();await click(mobile?.5:.265,mobile?.777:.652);const base=await save();
  const fixture=async(n,patch={})=>{
   const f={...base,seed:1,level:n*25,phase:'Intro',bossRulesVersion:2,bossStage:0,bossProgress:n===3?.5:0,bossWindow:0,bossIdle:10,bossStoredDamage:0,bossPhase2:n%2===0,enemyTime:300,enemyTimer:10,elapsed:0,enemyAttacks:0,physicalHits:0,player:{...base.player,MaxTime:500,CurrentTime:400,Attack:10,CritChance:0,Armor:1000,MagicResistance:1000},...patch,updatedAt:Date.now()};
   await page.evaluate(f=>window.name='TT_FIXTURE:'+JSON.stringify(f),f);await page.reload();await ready();await click(mobile?.5:.265,mobile?.777:.652,450);await click(mobile?.763:.5,mobile?.89:.935,30);return save();
  };
  const target=(n,s)=>n===1?[.3,.5]:n===2?[s.bossStage===0?.28:s.bossStage===1?.72:.5,s.bossStage===2?.72:s.bossStage===3?.5:.43]:n===3?[s.bossProgress<=.5?.28:.72,.5]:n===4?[.5,.28]:n===6?[.5+Math.sin(s.elapsed*.75)*.23,.5+Math.cos(s.elapsed*.75)*.23]:n===7?[.68,.69]:n===8?[.5,.4]:[.5,.5];
  for(let n=1;n<=10;n++){
   let start=await fixture(n,{enemyTime:1000,...(n===7?{enemyTimer:.65}:{})}),s=start;await move(...target(n,s));await down();
   for(let i=0;i<(n===2||n===10?32:9);i++){await page.waitForTimeout(95);s=await save();await move(...target(n,s));}
   await up();s=await save();
   check(start.enemyTime-s.enemyTime>5,(mobile?'touch':'mouse')+' hold deals counter damage to boss '+n,{damage:start.enemyTime-s.enemyTime,stage:s.bossStage,progress:s.bossProgress});
   check(s.physicalHits===0,'releasing hold never adds a physical tap '+n+' '+mobile);
   if(n===1)check(s.bossStage>=1,'holding destroys a train armor plate '+mobile);
   if(n===2)check(s.bossStage===3,'one dragging hold opens all three portal seals '+mobile,s);
   if(n===3)check(s.bossProgress>.25&&s.bossProgress<.75,'dragging a held finger balances both faces '+mobile);
   if(n===4)check(s.bossWindow>6,'holding roots disables healing '+mobile);
   if(n===5){const hp=s.enemyTime;await page.waitForTimeout(650);s=await save();check(s.bossProgress===0&&s.enemyTime<hp-1,'releasing a charged hold detonates the forge '+mobile);}
   if(n===6)check(s.bossProgress>.85,'dragging continuously follows the orbit '+mobile);
   if(n===7)check(s.enemyAttacks===0&&s.enemyTimer>1,'held magic interrupts the live stinger strike '+mobile);
   if(n===10)check(s.bossStage===1,'continuous hold counts as only one rhythm gesture '+mobile);
   await snap('boss-'+n);
   if(n===2){await page.reload();await ready();await click(mobile?.5:.265,mobile?.777:.652);let resumed=await save();check(resumed.phase==='Intro'&&resumed.bossStage===3&&resumed.bossWindow>0,'portal progress persists and resumes paused '+mobile);}
  }
  // Edge hit, pointer capture outside the stage, and returning without release.
  let start=await fixture(1);await move(.3+.145,.5);await down();await page.waitForTimeout(900);let a=await save();
  check(start.enemyTime-a.enemyTime>8,'visible weak-point edge registers during hold '+mobile);
  await move(-.65,.5);await page.waitForTimeout(100);const stopped=await save();await page.waitForTimeout(350);let outside=await save();
  check(outside.enemyTime>=stopped.enemyTime,'moving outside the stage pauses damage '+mobile,{before:stopped.enemyTime,after:outside.enemyTime});
  await move(.3,.5);await page.waitForTimeout(650);let returned=await save();await up();
  check(returned.enemyTime<outside.enemyTime-8&&returned.physicalHits===0,'returning inside resumes the same held pointer '+mobile);
  // Short gestures retain physical damage and do not double count.
  await fixture(1,{bossPhase2:true});await move(.3,.5);await down();await page.waitForTimeout(50);await up();
  check((await save()).physicalHits===1,'short gesture produces exactly one physical hit '+mobile);
  if(mobile){await fixture(1,{bossPhase2:true});await move(.3,.5);await down();await page.waitForTimeout(40);await up(true);check((await save()).physicalHits===0,'canceled touch cannot create a phantom tap');}
  // Old puzzle state must not corrupt the new mechanics or reset the player's run.
  let migrated=await fixture(3,{bossRulesVersion:0,bossStage:1,bossProgress:0,bossWindow:3});
  check(migrated.bossRulesVersion===2&&migrated.bossProgress===.5&&migrated.level===75&&migrated.player.Attack===10,'old save keeps run and resets only incompatible puzzle '+mobile);
  await ctx.close();
 }
 check(errors.length===0,'boss input has no runtime or missing-font errors',errors);
 fs.writeFileSync(__dirname+'/../QA/boss-input-checks.json',JSON.stringify({checks,report,errors},null,2)+'\n');
 }finally{fs.writeFileSync(__dirname+'/../QA/boss-input-console.log',errors.join('\n'));await browser.close();}
})();
