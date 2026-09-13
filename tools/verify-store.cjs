const fs=require('fs'),assert=require('assert'),cp=require('child_process'),{chromium}=require('D:/Game/Work/tools/node_modules/playwright');
const root='D:/Game/Release/Store-1.6.0';
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--autoplay-policy=no-user-gesture-required','--disable-accelerated-video-decode']});
 const ctx=await browser.newContext({viewport:{width:1280,height:720}}),page=await ctx.newPage(),report=[];
 await ctx.route('http://media.test/**',async route=>{const file=decodeURIComponent(new URL(route.request().url()).pathname.slice(1));if(!file){await route.fulfill({contentType:'text/html',body:'<style>body{margin:0;background:#111827}video{width:100vw;height:100vh;object-fit:contain}</style><video></video>'});return;}assert(!file.includes('/')&&!file.includes('..'));const body=fs.readFileSync(root+'/'+file),range=/bytes=(\d+)-(\d*)/.exec(route.request().headers().range||'');if(range){const from=Number(range[1]),to=range[2]?Math.min(body.length-1,Number(range[2])):body.length-1;await route.fulfill({status:206,contentType:'video/mp4',headers:{'Accept-Ranges':'bytes','Content-Range':'bytes '+from+'-'+to+'/'+body.length},body:body.subarray(from,to+1)});}else await route.fulfill({contentType:'video/mp4',headers:{'Accept-Ranges':'bytes'},body});});
 try{
  const manifest=JSON.parse(fs.readFileSync(root+'/media-manifest.json','utf8'));
  for(const item of manifest){
   const bytes=fs.readFileSync(root+'/'+item.file);
   if(item.type==='screenshot'){assert(bytes[0]===255&&bytes[1]===216);assert(item.width*9===item.height*16||item.width*16===item.height*9);continue;}
   assert(bytes.length===item.bytes&&bytes.length>500000&&item.combatFraction>=.70);
   assert(bytes.includes(Buffer.from('avc1'))&&bytes.includes(Buffer.from('mp4a'))&&bytes.includes(Buffer.from('soun')));
   await page.goto('http://media.test/');
   const metadata=await page.evaluate(async file=>{const v=document.querySelector('video');v.src='/'+file;await new Promise((resolve,reject)=>{v.onloadedmetadata=resolve;v.onerror=()=>reject(Error('video decode failed'));});return {width:v.videoWidth,height:v.videoHeight,duration:v.duration};},item.file);
   assert(metadata.width===item.width&&metadata.height===item.height&&metadata.duration<=28);item.duration=metadata.duration;
   for(const second of [3,12,22]){
    await page.evaluate(async second=>{const v=document.querySelector('video');v.pause();v.currentTime=second;await new Promise(r=>v.onseeked=r);await v.play();await new Promise(r=>setTimeout(r,180));v.pause();},second);
    const brightnessInBrowser=await page.evaluate(()=>{const c=document.createElement('canvas');c.width=c.height=32;const x=c.getContext('2d');x.drawImage(document.querySelector('video'),0,0,32,32);const pixels=x.getImageData(0,0,32,32).data;let sum=0;for(let i=0;i<pixels.length;i+=4)sum+=pixels[i]+pixels[i+1]+pixels[i+2];return sum/(32*32*3);});assert(brightnessInBrowser>16,'Browser-decoded frame must contain the game');
    const frameFile=__dirname+'/../QA/media-'+(item.width>item.height?'desktop':'mobile')+'-'+second+'.png';
    cp.execFileSync('D:/Game/Work/tools/ffmpeg/package/ffmpeg.exe',['-hide_banner','-loglevel','error','-ss',String(second),'-i',root+'/'+item.file,'-frames:v','1','-y',frameFile],{stdio:'pipe'});
    const analysis=cp.spawnSync('D:/Game/Work/tools/ffmpeg/package/ffmpeg.exe',['-hide_banner','-ss',String(second),'-i',root+'/'+item.file,'-frames:v','1','-vf','signalstats,metadata=print','-f','null','-'],{encoding:'utf8'});
    const brightness=Number(/lavfi.signalstats.YAVG=([0-9.]+)/.exec(analysis.stderr)?.[1]);assert(brightness>24,'Native decoded gameplay frame must not be black');
   }
   const energy=await page.evaluate(async()=>{const v=document.querySelector('video'),a=new AudioContext(),an=a.createAnalyser(),gain=a.createGain();gain.gain.value=0;a.createMediaElementSource(v).connect(an);an.connect(gain).connect(a.destination);await a.resume();await v.play();await new Promise(r=>setTimeout(r,600));const values=new Float32Array(an.fftSize);an.getFloatTimeDomainData(values);v.pause();return Math.sqrt(values.reduce((s,x)=>s+x*x,0)/values.length);});
   assert(energy>.00001,'recorded audio must not be silent');report.push({...item,decoded:true,audioRms:energy});
  }
  await page.goto('file:///D:/Game/Release/Store-1.6.0/START-HERE.html');await page.screenshot({path:__dirname+'/../QA/store-guide.png'});
  assert(await page.evaluate(()=>document.querySelectorAll('article').length===12));
  fs.writeFileSync(root+'/media-manifest.json',JSON.stringify(manifest,null,2));
  fs.writeFileSync(__dirname+'/../QA/store-checks.json',JSON.stringify({screenshots:manifest.filter(x=>x.type==='screenshot').length,videos:report,guideFields:12},null,2));console.log('Store files validated: '+report.length+' decodable MP4/AAC clips, non-silent audio, screenshots and guide.');
 }finally{await browser.close();}
})();
