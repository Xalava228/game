/* Yandex Games integration. No third-party Unity plugin or native runtime library. */
(() => {
 'use strict';
 const key='TimeThiefSaveV1';
 const api=window.TimeThiefSDK={sdk:null,player:null,unity:null,saved:'',lang:'ru',adBusy:false,readyCalled:false,playing:false,wantsPlay:false,cloudReady:false};
 const pauses=new Set();let cloudTimer=null,pendingCloud=false;
 const send=(name,value)=>api.unity?.SendMessage('TimeThief',name,value);
 function sync(){const paused=pauses.size>0;send('OnPlatformPause',paused?'1':'0');const shouldPlay=api.wantsPlay&&!paused&&!api.adBusy;if(shouldPlay===api.playing)return;api.playing=shouldPlay;try{api.sdk?.features?.GameplayAPI?.[shouldPlay?'start':'stop']();}catch(e){console.warn('Gameplay reporting unavailable',e);}}
 function pause(reason,value){if(value)pauses.add(reason);else pauses.delete(reason);sync();}
 api.attach=instance=>{api.unity=instance;sync();};
 function valid(text){try{const s=JSON.parse(text);return s&&s.version===1&&Number.isFinite(s.updatedAt)?s:null;}catch{return null;}}
 async function cloudSave(){cloudTimer=null;if(!api.player||!api.cloudReady||!pendingCloud)return;pendingCloud=false;try{await api.player.setData({timeThief:JSON.parse(api.saved)});}catch(e){pendingCloud=true;console.warn('Cloud save deferred',e);}}
 api.init=async()=>{
  try{api.saved=localStorage.getItem(key)||'';}catch{}
  const local=location.hostname==='localhost'||location.hostname==='127.0.0.1';
  if(local){api.lang=new URLSearchParams(location.search).get('lang')==='en'?'en':'ru';return;}
  try{
   await new Promise((resolve,reject)=>{const s=document.createElement('script');s.src='/sdk.js';s.onload=resolve;s.onerror=reject;document.head.append(s);});
   api.sdk=await YaGames.init();api.lang=api.sdk.environment?.i18n?.lang==='ru'?'ru':'en';
   api.sdk.on('game_api_pause',()=>pause('platform',true));api.sdk.on('game_api_resume',()=>pause('platform',false));
   try{api.player=await api.sdk.getPlayer();const cloud=await api.player.getData(['timeThief']);const c=cloud?.timeThief,l=valid(api.saved);if(c?.version===1&&Number.isFinite(c.updatedAt)&&(!l||c.updatedAt>l.updatedAt))api.saved=JSON.stringify(c);api.cloudReady=true;}catch(e){console.warn('Cloud storage unavailable; local progress remains available',e);}
  }catch(e){console.warn('Yandex SDK unavailable; running with local saves',e);}
 };
 api.ready=()=>{if(api.readyCalled)return;api.readyCalled=true;document.getElementById('loading').hidden=true;try{api.sdk?.features?.LoadingAPI?.ready();}catch(e){console.warn(e);}pause('visibility',document.hidden);sync();};
 api.gameplay=value=>{api.wantsPlay=value;sync();};
 api.save=json=>{if(!valid(json))return;api.saved=json;try{localStorage.setItem(key,json);}catch(e){console.warn('Browser storage is unavailable',e);}pendingCloud=true;if(!cloudTimer)cloudTimer=setTimeout(cloudSave,12000);};
 api.ad=rewarded=>{
  if(!api.sdk||api.adBusy){send('OnAdDone','0');return;}api.adBusy=true;sync();let earned=false,closed=false,opened=false;
  const finish=()=>{if(closed)return;closed=true;clearTimeout(watchdog);api.adBusy=false;send('OnAdDone',earned?'1':'0');sync();};
  // Never resume a video that actually opened on a timer. The startup watchdog only handles a missing open/error callback.
  const watchdog=setTimeout(()=>{if(!opened)finish();},15000);
  const callbacks={onOpen:()=>{opened=true;clearTimeout(watchdog);if(closed)pause('lateAd',true);},onClose:()=>{pause('lateAd',false);finish();},onError:()=>{pause('lateAd',false);finish();}};
  if(rewarded)callbacks.onRewarded=()=>{if(!closed)earned=true;};
  try{api.sdk.adv[rewarded?'showRewardedVideo':'showFullscreenAdv']({callbacks});}catch(e){finish();}
 };
 document.addEventListener('visibilitychange',()=>pause('visibility',document.hidden));
 window.addEventListener('pagehide',()=>{pause('visibility',true);cloudSave();});
 window.addEventListener('pageshow',()=>pause('visibility',document.hidden));
 window.addEventListener('blur',()=>pause('focus',true));window.addEventListener('focus',()=>pause('focus',false));
})();
