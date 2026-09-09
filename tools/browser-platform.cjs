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
 try{
 await page.goto('http://platform.test:8080');await page.waitForFunction(()=>window.TimeThiefSDK?.unity&&TimeThiefSDK.readyCalled,null,{timeout:120000});await page.waitForTimeout(800);
 check(await page.evaluate(()=>__platform.ready===1&&__platform.start===0),'Game Ready once; menu does not report gameplay');
 await click(448,639);await click(1046,801,60);let a=await save();await page.waitForTimeout(250);let b=await save();
 check(Math.abs((b.enemyTime-a.enemyTime)-(a.player.CurrentTime-b.player.CurrentTime))<.01,'real player transfers every elapsed second to enemy');
 await page.evaluate(()=>__platform.events.game_api_pause());a=await save();await page.waitForTimeout(450);b=await save();
 check(a.player.CurrentTime===b.player.CurrentTime&&await page.evaluate(()=>!TimeThiefSDK.playing),'platform event pauses Unity and Gameplay API');
 await page.evaluate(()=>__platform.events.game_api_resume());for(let i=0;i<7;i++)await click(800,430,120);await page.waitForTimeout(800);
 a=await save();check(a.phase==='Victory','counter-element wins under initialized SDK');
 await click(1088,693);await page.evaluate(()=>{__ad.onOpen();__ad.onClose();});await page.waitForTimeout(250);b=await save();check(b.shop.shards===a.shop.shards&&!b.rewardDoubled,'closing rewarded ad does not grant shards');
 await click(1088,693);await page.evaluate(()=>{__ad.onOpen();__ad.onRewarded();__ad.onRewarded();__ad.onClose();__ad.onClose();});await page.waitForTimeout(250);b=await save();check(b.rewardDoubled&&b.shop.shards===a.shop.shards+a.lastReward,'reward granted exactly once through JS and C# bridge');
 await page.waitForTimeout(12500);check(await page.evaluate(()=>__platform.cloud.some(d=>d.timeThief.rewardDoubled)),'updated run reaches cloud setData');
 check(logs.length===0,'no JavaScript errors in platform integration');
 fs.writeFileSync(__dirname+'/../QA/platform-checks.txt',checks+' full WebGL / mocked Yandex integration checks passed. Live platform validation remains separate.\n');
 }finally{await browser.close();}
})();
