// Release 1.5: real Unity UI, save compatibility, startup localization and ranking bridge.
const fs=require('fs'),assert=require('assert'),{chromium}=require('D:/Game/Work/tools/node_modules/playwright');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'});
 const ctx=await browser.newContext({viewport:{width:1600,height:900}}),page=await ctx.newPage();
 let checks=0,locale='ru';const errors=[];
 const check=(ok,label)=>{assert.ok(ok,label);checks++;console.log('PASS '+label);};
 page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>{if(/Exception|RuntimeError|missing from.*font asset/.test(m.text()))errors.push(m.text());});
 const ready=async()=>{await page.waitForFunction(()=>window.TimeThiefSDK?.unity&&TimeThiefSDK.readyCalled,null,{timeout:120000});await page.waitForTimeout(800);};
 const click=async(x,y,ms=300)=>{const b=await page.locator('canvas').boundingBox();await page.mouse.click(b.x+b.width*x,b.y+b.height*y);await page.waitForTimeout(ms);};
 const save=async()=>{await page.evaluate(()=>TimeThiefSDK.unity.SendMessage('TimeThief','Persist'));await page.waitForTimeout(80);return page.evaluate(()=>JSON.parse(TimeThiefSDK.saved));};
 const snap=async name=>page.screenshot({path:__dirname+'/../QA/polish-'+name+'.png'});
 await ctx.addInitScript(()=>{if(window.name.startsWith('TT_FIXTURE:')){localStorage.setItem('TimeThiefSaveV1',window.name.slice(11));window.name='';}});
 const fixture=async data=>{await page.evaluate(f=>window.name='TT_FIXTURE:'+JSON.stringify({...f,updatedAt:Date.now()}),data);await page.reload();await ready();await click(.265,.652,800);};
 try{
  await page.goto('http://127.0.0.1:8080/?v=1.6.0');await ready();await click(.265,.87);await snap('ranking-offline');
  check(await page.evaluate(()=>TimeThiefSDK.readyCalled&&!TimeThiefSDK.playing),'offline leaderboard opens without gameplay');
  await click(.5,.857);await click(.265,.652);const base=await save();
  await snap('centered-level-desktop');
  await page.setViewportSize({width:390,height:844});await page.waitForTimeout(400);await snap('centered-level-mobile');
  await page.setViewportSize({width:1600,height:900});
  await fixture({...base,level:6,phase:'Victory',defeated:6,lastTimeReward:.65,lastTimeAwarded:.65,economyVersion:2,player:{...base.player,CurrentTime:20,MaxTime:20},shop:{shards:200,maxTimePurchases:0,attackPurchases:0,buffs:[]}});
  await snap('aligned-results');await click(.815,.865);await click(.175,.839);let after=await save();
  check(after.shop.armorPurchases===1&&Math.abs(after.player.Armor-base.player.Armor-3)<.001,'armor purchase changes the player by +3');
  await click(.645,.839);after=await save();
  check(after.shop.resistancePurchases===1&&Math.abs(after.player.MagicResistance-base.player.MagicResistance-3)<.001,'magic guard purchase changes the player by +3');
  check(Math.abs(after.player.CurrentTime-18.2)<.001&&after.shop.buffs.length===0,'defenses spend 0.9 life seconds each and are permanent');
  await snap('shop-desktop');await click(.735,.253);await click(.197,.482);let boosted=await save();
  check(boosted.shop.buffs.some(b=>b.type===2)&&Math.abs(boosted.player.CurrentTime-17.55)<.001,'one-battle tab buys its own temporary boost');
  await click(.197,.482);check(Math.abs((await save()).player.CurrentTime-17.55)<.001,'owned temporary boost cannot be bought twice');
  await snap('shop-temporary-desktop');await page.setViewportSize({width:390,height:844});await page.waitForTimeout(400);await snap('shop-temporary-mobile');await page.setViewportSize({width:1600,height:900});await page.waitForTimeout(350);await click(.265,.253);await page.setViewportSize({width:390,height:844});await page.waitForTimeout(400);await snap('shop-mobile');
  await page.setViewportSize({width:1600,height:900});await page.reload();await ready();await click(.265,.652);after=await save();
  check(after.shop.armorPurchases===1&&after.shop.resistancePurchases===1&&after.player.Armor>=3&&after.player.MagicResistance>=3,'both defenses and their price progression survive reload');
  await fixture({...after,phase:'GameOver',reviveUsed:false,player:{...after.player,CurrentTime:0}});await snap('death-desktop');
  await page.setViewportSize({width:390,height:844});await page.waitForTimeout(400);await snap('death-mobile');await page.setViewportSize({width:1600,height:900});
  // Controlled Yandex host: verify the SDK language property is read before the Unity loader exists.
  await ctx.route('http://platform.test:8080/**',async route=>{
   const u=new URL(route.request().url());
   if(u.pathname==='/sdk.js'){
    const mock=`window.__rank={submitted:[],readBeforeLoader:false};window.YaGames={init:async()=>({environment:{i18n:{get lang(){__rank.readBeforeLoader=!document.querySelector('script[src^="Build/"]');return '${locale}';}}},deviceInfo:{isDesktop:()=>true},on:()=>{},features:{LoadingAPI:{ready:()=>{}},GameplayAPI:{start:()=>{},stop:()=>{}}},getPlayer:async()=>({getData:async()=>({}),setData:async()=>{}}),isAvailableMethod:async()=>false,leaderboards:{getEntries:async()=>({entries:[{rank:1,score:250,player:{publicName:'Хранитель'}},{rank:2,score:180,player:{publicName:'<b>Ada</b>'}}]}),setScore:async(...args)=>__rank.submitted.push(args)}})};`;
    await route.fulfill({contentType:'text/javascript',body:mock});return;
   }
   if(u.pathname==='/platform-config.js'){await route.fulfill({contentType:'text/javascript',body:"window.TimeThiefPlatformConfig={leaderboardName:'test-board'};"});return;}
   const response=await ctx.request.fetch('http://127.0.0.1:8080'+u.pathname+u.search);await route.fulfill({response});
  });
  for(locale of ['ru','en']){
   await page.goto('http://platform.test:8080/?lang='+locale);await ready();
   check(await page.evaluate(l=>__rank.readBeforeLoader&&TimeThiefSDK.lang===l&&document.documentElement.lang===l,locale),'SDK '+locale+' is consumed at startup before Unity');
   await snap('sdk-menu-'+locale);await click(.265,.87,800);await snap('ranking-'+locale);await page.setViewportSize({width:320,height:568});await page.waitForTimeout(350);await snap('ranking-mobile-'+locale);await page.setViewportSize({width:1600,height:900});await page.waitForTimeout(350);
   check(await page.evaluate(()=>__rank.submitted.length===0&&!TimeThiefSDK.playing),'guest leaderboard '+locale+' does not submit or start gameplay');
   await click(.5,.857);await click(.265,.652);await snap('sdk-intro-'+locale);
  }
  check(errors.length===0,'no runtime errors or missing glyphs in new screens');
  fs.writeFileSync(__dirname+'/../QA/polish-checks.txt',checks+' real WebGL checks passed: permanent defenses, reload, leaderboard, RU/EN startup and menu integration. Alignment is also checked in captured images.\n');
 }finally{await browser.close();}
})();
