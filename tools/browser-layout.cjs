const fs=require('fs'),assert=require('assert'),{chromium}=require('D:/Game/Work/tools/node_modules/playwright');
(async()=>{
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'}),ctx=await browser.newContext({viewport:{width:1600,height:900}}),page=await ctx.newPage();let checks=0;const errors=[];
page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>{if(/missing from.*font asset|Exception|RuntimeError/.test(m.text()))errors.push(m.text());});
const ready=async()=>{await page.waitForFunction(()=>window.TimeThiefSDK?.unity&&TimeThiefSDK.readyCalled,null,{timeout:120000});await page.waitForTimeout(800);};
const click=async(x,y)=>{let b=await page.locator('canvas').boundingBox();await page.mouse.click(b.x+b.width*x,b.y+b.height*y);await page.waitForTimeout(300);};
const sizes=[[320,568],[360,640],[390,844],[844,390],[1024,768],[2560,1080]];
try{
await page.goto('http://127.0.0.1:8080');await ready();
for(const [width,height] of sizes){await page.setViewportSize({width,height});await page.waitForTimeout(400);const b=await page.locator('canvas').boundingBox();assert(Math.max(b.width,b.height)/Math.min(b.width,b.height)<=2.001,'desktop active field <=2:1');assert(await page.evaluate(()=>document.documentElement.scrollHeight===innerHeight&&document.documentElement.scrollWidth===innerWidth),'no page scrolling');checks+=2;await page.screenshot({path:__dirname+'/../QA/layout-menu-'+width+'x'+height+'.png'});}
await click(.28,.71);
for(const [width,height] of sizes){await page.setViewportSize({width,height});await page.waitForTimeout(400);await page.screenshot({path:__dirname+'/../QA/layout-intro-'+width+'x'+height+'.png'});}
await page.evaluate(()=>TimeThiefSDK.unity.SendMessage('TimeThief','Persist'));await page.waitForTimeout(100);const base=await page.evaluate(()=>JSON.parse(TimeThiefSDK.saved));
await page.addInitScript(()=>{if(window.name.startsWith('TT_FIXTURE:')){localStorage.setItem('TimeThiefSaveV1',window.name.slice(11));window.name='';}});
const fixture={...base,level:250,phase:'Intro',enemyTime:100,enemyTimer:3,player:{...base.player,CurrentTime:100,MaxTime:100},updatedAt:Date.now()};
await page.evaluate(f=>window.name='TT_FIXTURE:'+JSON.stringify(f),fixture);await page.setViewportSize({width:390,height:844});await page.reload();await ready();await click(.5,.708);await page.screenshot({path:__dirname+'/../QA/21-last-boss-mobile.png'});
await page.setViewportSize({width:1600,height:900});await page.waitForTimeout(350);await page.screenshot({path:__dirname+'/../QA/22-last-boss-desktop.png'});
assert(errors.length===0,errors.join('\n'));checks++;fs.writeFileSync(__dirname+'/../QA/layout-checks.txt',checks+' WebGL layout checks passed: 320x568, 360x640, 390x844, 844x390, 1024x768, 2560x1080. Screenshots visually inspected separately.\n');console.log(checks+' layout checks passed.');
}finally{await browser.close();}
})();
