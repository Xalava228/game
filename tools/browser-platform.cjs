// Full Unity player against controlled SDK callbacks. This is not a live Yandex ad test.
const fs=require('fs'),assert=require('assert'),{chromium}=require('D:/Game/Work/tools/node_modules/playwright');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'});
 const ctx=await browser.newContext({viewport:{width:1600,height:900}}),page=await ctx.newPage();let checks=0;const logs=[];
 const mock="window.__platform={ready:0,start:0,stop:0,cloud:[],events:{}};window.YaGames={init:async()=>({environment:{i18n:{lang:'ru'}},deviceInfo:{isDesktop:()=>true},on:(n,f)=>__platform.events[n]=f,features:{LoadingAPI:{ready:()=>__platform.ready++},GameplayAPI:{start:()=>__platform.start++,stop:()=>__platform.stop++}},getPlayer:async()=>({getData:async()=>({}),setData:async d=>__platform.cloud.push(d)}),adv:{showRewardedVideo:o=>window.__ad=o.callbacks,showFullscreenAdv:o=>window.__ad=o.callbacks}})};";
 await ctx.route('**/*',async route=>{const u=new URL(route.request().url());if(u.pathname==='/sdk.js'){await route.fulfill({contentType:'text/javascript',body:mock});return;}const response=await ctx.request.fetch('http://127.0.0.1:8080'+u.pathname+u.search);await route.fulfill({response});});
 page.on('pageerror',e=>logs.push(String(e)));
 const check=(ok,label)=>{assert.ok(ok,label);checks++;console.log('PASS '+label);};
 const save=async()=>{await page.evaluate(()=>TimeThiefSDK.unity.SendMessage('TimeThief','Persist'));await page.waitForTimeout(100);return page.evaluate(()=>JSON.parse(TimeThiefSDK.saved));};
 const click=async(x,y,delay=250)=>{await page.mouse.click(x,y);await page.waitForTimeout(delay);};
 await page.addInitScript(()=>{if(window.name.startsWith('TT_FIXTURE:')){localStorage.setItem('TimeThiefSaveV1',window.name.slice(11));window.name='';}});
 try{
 await page.goto('http://platform.test:8080');await page.waitForFunction(()=>window.TimeThiefSDK?.unity&&TimeThiefSDK.readyCalled,null,{timeout:120000});await page.waitForTimeout(800);
 check(await page.evaluate(()=>__platform.ready===1&&__platform.start===0),'Game Ready once; menu does not report gameplay');
 await click(424,587);await click(800,842,60);let a=await save();await page.waitForTimeout(250);let b=await save();
 check(Math.abs((b.enemyTime-a.enemyTime)-(a.player.CurrentTime-b.player.CurrentTime))<.01,'real player transfers every elapsed second to enemy');
 await page.evaluate(()=>__platform.events.game_api_pause());a=await save();await page.waitForTimeout(450);b=await save();
 check(a.player.CurrentTime===b.player.CurrentTime&&await page.evaluate(()=>!TimeThiefSDK.playing),'platform event pauses Unity and Gameplay API');
 await page.evaluate(()=>__platform.events.game_api_resume());for(let i=0;i<7;i++)await click(800,430,120);await page.waitForTimeout(800);
 a=await save();check(a.phase==='Victory','counter-element wins under initialized SDK');
 const rewardFixture={...a,player:{...a.player,CurrentTime:2},updatedAt:Date.now()};
 await page.evaluate(f=>window.name='TT_FIXTURE:'+JSON.stringify(f),rewardFixture);await page.reload();await page.waitForFunction(()=>TimeThiefSDK?.unity&&TimeThiefSDK.readyCalled,null,{timeout:120000});await page.waitForTimeout(800);await click(424,587,800);a=await save();
 await click(400,792);await page.evaluate(()=>{__ad.onOpen();__ad.onClose();});await page.waitForTimeout(250);b=await save();check(b.player.CurrentTime===a.player.CurrentTime&&!b.rewardDoubled,'closing rewarded ad does not restore life');
 await click(400,792);await page.evaluate(()=>{__ad.onOpen();__ad.onRewarded();__ad.onRewarded();__ad.onClose();__ad.onClose();});await page.waitForTimeout(250);b=await save();check(b.rewardDoubled&&Math.abs(b.player.CurrentTime-a.player.CurrentTime-a.lastTimeReward)<.001,'reward granted exactly once through JS and C# bridge');
 await page.waitForTimeout(12500);check(await page.evaluate(()=>__platform.cloud.some(d=>d.timeThief.rewardDoubled)),'updated run reaches cloud setData');
 const fixture={...b,phase:'GameOver',reviveUsed:false,player:{...b.player,CurrentTime:0},updatedAt:Date.now()};
 await page.evaluate(f=>window.name='TT_FIXTURE:'+JSON.stringify(f),fixture);await page.reload();await page.waitForFunction(()=>TimeThiefSDK?.unity&&TimeThiefSDK.readyCalled,null,{timeout:120000});await page.waitForTimeout(800);await click(424,587,800);
 check((await save()).phase==='GameOver','defeat survives reload before a revive');
 await click(1056,635);await page.evaluate(()=>{__ad.onOpen();__ad.onClose();});await page.waitForTimeout(300);
 check(!(await save()).reviveUsed&&(await save()).phase==='GameOver','unrewarded close cannot revive the player');
 await click(1056,635);await page.evaluate(()=>{__ad.onOpen();__ad.onRewarded();__ad.onRewarded();__ad.onClose();});await page.waitForTimeout(300);b=await save();
 check(b.reviveUsed&&b.phase==='Intro'&&b.player.CurrentTime===b.player.MaxTime&&b.enemyTimer>=2,'rewarded revive restores full reserve once and waits for player');
 await page.reload();await page.waitForFunction(()=>TimeThiefSDK?.unity&&TimeThiefSDK.readyCalled,null,{timeout:120000});await page.waitForTimeout(800);await click(424,587);check((await save()).reviveUsed,'revive limit persists after reload');
 check(logs.length===0,'no JavaScript errors in platform integration');
 fs.writeFileSync(__dirname+'/../QA/platform-checks.txt',checks+' full WebGL / mocked Yandex integration checks passed. Live platform validation remains separate.\n');
 }finally{await browser.close();}
})();
