// Capture actual shipping WebGL frames and native MP4/AAC gameplay. No production debug controls.
const fs=require('fs'),path=require('path'),assert=require('assert'),cp=require('child_process'),{chromium}=require('D:/Game/Work/tools/node_modules/playwright');
const out='D:/Game/Release/Store-1.6.0';fs.mkdirSync(out,{recursive:true});
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--autoplay-policy=no-user-gesture-required']});
 const media=[];
 try{for(const lang of ['ru','en'])for(const portrait of [false,true]){
  const width=portrait?1080:1920,height=portrait?1920:1080;
  const ctx=await browser.newContext({viewport:{width,height},deviceScaleFactor:1}),page=await ctx.newPage();
  const prefix=lang+'-'+(portrait?'mobile':'desktop');let held=false,cdp=null,frameBusy=false,recordingFrames=0;
  await ctx.addInitScript(()=>{
   const connect=AudioNode.prototype.connect;
   AudioNode.prototype.connect=function(dest,...args){if(dest===this.context.destination){if(!window.__recordAudio)window.__recordAudio=this.context.createMediaStreamDestination();connect.call(this,window.__recordAudio);}return connect.call(this,dest,...args);};
  });
  const ready=async()=>{await page.waitForFunction(()=>window.TimeThiefSDK?.unity&&TimeThiefSDK.readyCalled,null,{timeout:120000});await page.waitForTimeout(800);};
  const click=async(x,y,ms=250)=>{const b=await page.locator('canvas').boundingBox();await page.mouse.click(b.x+b.width*x,b.y+b.height*y);await page.waitForTimeout(ms);};
  const save=async()=>{await page.evaluate(()=>TimeThiefSDK.unity.SendMessage('TimeThief','Persist'));await page.waitForTimeout(50);return page.evaluate(()=>JSON.parse(TimeThiefSDK.saved));};
  const snap=async(name)=>{const file=prefix+'-'+name+'.jpg';await page.locator('canvas').screenshot({path:path.join(out,file),type:'jpeg',quality:100});media.push({file,width,height,type:'screenshot'});};
  await page.goto('http://127.0.0.1:8080/?v=1.6.0&lang='+lang);await ready();await snap('menu');
  await click(portrait?.5:.265,portrait?.777:.652);await snap('encounter');
  // Record Russian desktop and mobile clips; screenshots cover both localizations.
  if(lang==='ru'){
   await page.evaluate(()=>{
    const source=document.querySelector('canvas');window.__videoCanvas=document.createElement('canvas');__videoCanvas.width=source.width;__videoCanvas.height=source.height;window.__videoCtx=__videoCanvas.getContext('2d');__videoCtx.drawImage(source,0,0);const stream=__videoCanvas.captureStream(0);window.__videoTrack=stream.getVideoTracks()[0];
    if(window.__recordAudio)for(const track of __recordAudio.stream.getAudioTracks())stream.addTrack(track);
    const mimeType='video/mp4;codecs=avc1.42001E,mp4a.40.2';
    if(!MediaRecorder.isTypeSupported(mimeType))throw Error('Native H264/AAC recorder unavailable');
    window.__chunks=[];window.__recorder=new MediaRecorder(stream,{mimeType,videoBitsPerSecond:10000000,audioBitsPerSecond:192000});
    __recorder.ondataavailable=e=>{if(e.data.size)__chunks.push(e.data);};
    window.__recordDone=new Promise(resolve=>__recorder.onstop=async()=>{const blob=new Blob(__chunks,{type:mimeType});const reader=new FileReader();reader.onload=()=>resolve(reader.result.slice(reader.result.indexOf(';base64,')+8));reader.readAsDataURL(blob);});
    __recorder.start(1000);setTimeout(()=>__recorder.stop(),26500);
   });
   cdp=await ctx.newCDPSession(page);
   cdp.on('Page.screencastFrame',async frame=>{
    if(frameBusy){await cdp.send('Page.screencastFrameAck',{sessionId:frame.sessionId}).catch(()=>{});return;}
    frameBusy=true;try{await page.evaluate(async data=>{const img=new Image();const loaded=new Promise((resolve,reject)=>{img.onload=resolve;img.onerror=reject;});img.src='data:image/jpeg;base64,'+data;await loaded;__videoCtx.drawImage(img,0,0,__videoCanvas.width,__videoCanvas.height);__videoTrack.requestFrame();},frame.data);recordingFrames++;}finally{frameBusy=false;await cdp.send('Page.screencastFrameAck',{sessionId:frame.sessionId}).catch(()=>{});}
   });
   await cdp.send('Page.startScreencast',{format:'jpeg',quality:95,maxWidth:width,maxHeight:height,everyNthFrame:2});
  }
  const start=Date.now();let capturedBattle=false,capturedResults=false;const phases=[];
  while(Date.now()-start<(lang==='ru'?26700:13000)){
   const s=await save();phases.push({at:Date.now()-start,phase:s.phase,level:s.level});
   if(s.phase!=='Fighting'&&held){await page.mouse.up();held=false;}
   if(s.phase==='Intro'){await click(portrait?.763:.5,portrait?.89:.935,0);}
   else if(s.phase==='Fighting'){
    const b=await page.locator('canvas').boundingBox();await page.mouse.move(b.x+b.width*.5,b.y+b.height*.49);
    const tap=s.level===1||s.level===5;
    if(tap){if(held){await page.mouse.up();held=false;}await page.mouse.click(b.x+b.width*.5,b.y+b.height*.49);}
    else if(!held){await page.mouse.down();held=true;}
    if(!capturedBattle&&s.level>=2){await snap('battle');capturedBattle=true;}
    await page.waitForTimeout(150);
   }
   else if(s.phase==='Victory'){
    if(!capturedResults){await snap('victory');capturedResults=true;}
    await page.waitForTimeout(650);await click(portrait?.5:.69,.945,0);
   }
   else if(s.phase==='RewardSelection'){await page.waitForTimeout(750);await click(portrait?.698:.19,portrait?.494:.699);}
   else if(s.phase==='GameOver')break;
  }
  if(held)await page.mouse.up();
  if(lang==='ru'){
   await cdp.send('Page.stopScreencast');while(frameBusy)await page.waitForTimeout(20);
   assert(recordingFrames>100,'Record actual browser frames');
   let bytes=Buffer.from(await page.evaluate(()=>__recordDone),'base64');const file=prefix+'-gameplay.mp4';const raw='D:/Game/Work/Temp/'+prefix+'-raw.mp4';fs.writeFileSync(raw,bytes);
   cp.execFileSync('D:/Game/Work/tools/ffmpeg/package/ffmpeg.exe',['-hide_banner','-loglevel','error','-i',raw,'-c:v','libx264','-preset','veryfast','-crf','18','-pix_fmt','yuv420p','-r','30','-c:a','aac','-b:a','160k','-max_muxing_queue_size','4096','-movflags','+faststart','-y',path.join(out,file)],{stdio:'pipe'});bytes=fs.readFileSync(path.join(out,file));
   const meta=await page.evaluate(async encoded=>{const v=document.createElement('video');v.muted=true;v.src='data:video/mp4;base64,'+encoded;document.body.append(v);await new Promise((resolve,reject)=>{v.onloadedmetadata=resolve;v.onerror=reject;});return {width:v.videoWidth,height:v.videoHeight,duration:v.duration};},bytes.toString('base64'));
   assert(meta.width===width&&meta.height===height&&meta.duration<=28&&meta.duration>=20&&bytes.length>500000&&bytes.length<100*1024*1024);
   let fighting=0,total=0;for(let i=1;i<phases.length;i++){const dt=phases[i].at-phases[i-1].at;total+=dt;if(phases[i-1].phase==='Fighting')fighting+=dt;}
   assert(fighting/total>=.7,'At least 70% of the clip must show active combat');
   media.push({file,...meta,bytes:bytes.length,type:'video',recordingFrames,combatFraction:fighting/total});
   fs.writeFileSync(path.join(out,prefix+'-capture-log.json'),JSON.stringify(phases,null,2));
  }
  await ctx.close();console.log('Captured '+prefix);
 }}finally{await browser.close();}
 fs.writeFileSync(path.join(out,'media-manifest.json'),JSON.stringify(media,null,2));
})();
