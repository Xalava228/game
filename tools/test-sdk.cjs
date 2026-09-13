// Exercises the shipped SDK bridge with deterministic browser/platform substitutes.
const fs=require('fs'),vm=require('vm'),assert=require('assert');
const code=fs.readFileSync(__dirname+'/../game/Assets/WebGLTemplates/TimeThief/sdk-bridge.js','utf8');
let passed=0;
function harnessWithCode(bridgeCode=code){let timers=new Map(),events={},messages=[],storage={},callbacks,starts=0,stops=0,readies=0,id=0;
const doc={hidden:false,addEventListener:(n,f)=>events[n]=f,getElementById:()=>({hidden:false}),createElement:()=>({}),head:{append:s=>s.onload()}};
const sdk={environment:{i18n:{lang:'ru'}},features:{LoadingAPI:{ready:()=>readies++},GameplayAPI:{start:()=>starts++,stop:()=>stops++}},on:(n,f)=>events[n]=f,getPlayer:async()=>({getData:async()=>({}),setData:async()=>{}}),adv:{showRewardedVideo:o=>callbacks=o.callbacks,showFullscreenAdv:o=>callbacks=o.callbacks}};
const win={addEventListener:(n,f)=>events[n]=f};const context={window:win,document:doc,location:{hostname:'example.yandex.net'},localStorage:{getItem:k=>storage[k],setItem:(k,v)=>storage[k]=v},console,YaGames:{init:async()=>sdk},URLSearchParams,setTimeout:f=>{timers.set(++id,f);return id},clearTimeout:i=>timers.delete(i)};vm.runInNewContext(bridgeCode,context);const api=win.TimeThiefSDK;api.unity={SendMessage:(...m)=>messages.push(m)};return {api,sdk,doc,events,messages,timers,get callbacks(){return callbacks},get starts(){return starts},get stops(){return stops},get readies(){return readies}};}
const harness=()=>harnessWithCode();
(async()=>{
const h=harness();await h.api.init();h.api.ready();h.api.ready();assert.equal(h.readies,1);passed++;
h.api.gameplay(true);assert.equal(h.starts,1);h.doc.hidden=true;h.events.visibilitychange();assert.equal(h.stops,1);h.events.game_api_pause();h.doc.hidden=false;h.events.visibilitychange();assert.equal(h.starts,1);h.events.game_api_resume();assert.equal(h.starts,2);passed++;
h.api.ad(true);h.callbacks.onOpen();h.callbacks.onClose();assert.equal(h.messages.at(-2)?.[1]==='OnAdDone'?h.messages.at(-2)[2]:h.messages.findLast(m=>m[1]==='OnAdDone')[2],'0');passed++;
h.api.ad(true);h.callbacks.onOpen();h.callbacks.onRewarded();h.callbacks.onClose();h.callbacks.onClose();assert.equal(h.messages.filter(m=>m[1]==='OnAdDone'&&m[2]==='1').length,1);passed++;
h.api.ad(true);h.callbacks.onError();h.callbacks.onRewarded();h.callbacks.onClose();assert.equal(h.messages.filter(m=>m[1]==='OnAdDone'&&m[2]==='1').length,1);passed++;
h.api.ad(false);h.callbacks.onOpen();for(const f of [...h.timers.values()])f();assert.equal(h.api.adBusy,true);h.callbacks.onClose();assert.equal(h.api.adBusy,false);passed++;
h.api.save(JSON.stringify({version:1,updatedAt:1,level:5}));assert.equal(JSON.parse(h.api.saved).level,5);h.api.save('invalid json');assert.equal(JSON.parse(h.api.saved).level,5);passed++;
h.api.gameplay(false);h.events.game_api_pause();h.events.game_api_resume();assert.equal(h.api.playing,false);passed++;
const newer=harness();newer.sdk.getPlayer=async()=>({getData:async()=>({timeThief:{version:1,updatedAt:200,level:12}}),setData:async()=>{}});await newer.api.init();assert.equal(JSON.parse(newer.api.saved).level,12);passed++;
const stalled=harness();let wrote=false;stalled.sdk.getPlayer=async()=>({getData:()=>new Promise(()=>{}),setData:async()=>{wrote=true;}});const init=stalled.api.init();for(let i=0;i<30;i++)await Promise.resolve();for(const f of [...stalled.timers.values()])f();await init;assert.equal(stalled.api.cloudReady,false);stalled.api.save(JSON.stringify({version:1,updatedAt:300,level:2}));for(const f of [...stalled.timers.values()])f();assert.equal(wrote,false);passed++;
const failed=harness();failed.sdk.getPlayer=async()=>{throw new Error('offline guest')};await failed.api.init();failed.api.ready();failed.api.gameplay(true);assert.equal(failed.readies,1);assert.equal(failed.starts,1);passed++;
// Startup language must be consumed before Unity attaches, including a fallback locale.
for(const [locale,expected] of [['ru','ru'],['en','en'],['de','en']]){
 const h=harness();let reads=0;
 Object.defineProperty(h.sdk.environment.i18n,'lang',{get(){reads++;return locale;}});
 h.api.unity=null;await h.api.init();assert.ok(reads>0);assert.equal(h.api.lang,expected);passed++;
}
const ranking=harness();await ranking.api.init();await ranking.api.leaderboard();assert.equal(JSON.parse(ranking.messages.at(-1)[2]).status,'unavailable');passed++;
// The public list works for guests; only authenticated users can publish their best.
const rankCode=code.replace("const name=window.TimeThiefPlatformConfig?.leaderboardName;","const name='test-board';");
const publicBoard=harnessWithCode(rankCode);let submitted=0;
publicBoard.sdk.isAvailableMethod=async()=>false;
publicBoard.sdk.leaderboards={getEntries:async(name,opts)=>{assert.equal(name,'test-board');assert.equal(opts.quantityTop,5);return {entries:[{rank:1,score:80,player:{publicName:'<b>Player</b>'}}]};},setScore:async()=>submitted++};
await publicBoard.api.init();await publicBoard.api.leaderboard();assert.equal(submitted,0);assert.equal(JSON.parse(publicBoard.messages.at(-1)[2]).entries[0].score,80);passed++;
const lower=harnessWithCode(rankCode);let calls=0;
lower.sdk.isAvailableMethod=async()=>true;lower.sdk.leaderboards={getPlayerEntry:async()=>({rank:8,score:90,player:{publicName:'Me'}}),getEntries:async()=>({entries:[]}),setScore:async()=>calls++};
await lower.api.init();lower.api.save(JSON.stringify({version:1,updatedAt:1,bestLevel:20}));await lower.api.leaderboard();assert.equal(calls,0);assert.equal(JSON.parse(lower.messages.at(-1)[2]).entries[0].score,90);passed++;
const higher=harnessWithCode(rankCode);let serverBest=0;
higher.sdk.isAvailableMethod=async()=>true;higher.sdk.leaderboards={getPlayerEntry:async()=>{if(!serverBest)throw {code:'LEADERBOARD_PLAYER_NOT_PRESENT'};return {rank:1,score:serverBest,player:{publicName:'Me'}};},getEntries:async()=>({entries:[]}),setScore:async(name,score)=>serverBest=score};
await higher.api.init();higher.api.save(JSON.stringify({version:1,updatedAt:1,bestLevel:42}));await higher.api.leaderboard();assert.equal(serverBest,42);assert.equal(JSON.parse(higher.messages.at(-1)[2]).status,'ok');passed++;
const result=`${passed} SDK tests passed: ready idempotency, independent pause causes, no reward on close/error, reward once on callback, no open-ad timeout resume, save validation, no gameplay from menus.\n`;fs.mkdirSync(__dirname+'/../QA',{recursive:true});fs.writeFileSync(__dirname+'/../QA/sdk-checks.txt',result);console.log(result);
})();
