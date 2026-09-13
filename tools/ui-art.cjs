// Crisp authored UI marks; character artwork is generated separately as transparent PNG.
const fs=require('fs'),path=require('path'),{Resvg}=require('D:/Game/Work/tools/node_modules/@resvg/resvg-js');
const repo=path.resolve(__dirname,'..'),src=repo+'/assets/vector',out=repo+'/game/Assets/TimeThief/Resources/Art';
const mark='<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 256 256"><defs><linearGradient id="g" x2="0" y2="1"><stop stop-color="#ffe29a"/><stop offset="1" stop-color="#d8a447"/></linearGradient></defs><circle cx="128" cy="128" r="118" fill="#24283f"/><circle cx="128" cy="128" r="108" fill="none" stroke="#d8ae63" stroke-width="4"/><path d="M79 66 H177 C177 95 161 112 142 128 C161 144 177 161 177 190 H79 C79 161 95 144 114 128 C95 112 79 95 79 66Z" fill="#fff5de"/><path d="M94 78 H162 C160 97 144 113 128 123 C112 113 96 97 94 78Z" fill="#54536f"/><path d="M98 90 H158 C153 103 140 114 128 121 C116 114 103 103 98 90Z" fill="url(#g)"/><path d="M95 178 Q101 151 128 137 Q155 151 161 178Z" fill="url(#g)"/><path d="M128 128 V135" stroke="#d8a447" stroke-width="7" stroke-linecap="round"/><rect x="66" y="48" width="124" height="22" rx="10" fill="url(#g)"/><rect x="66" y="186" width="124" height="22" rx="10" fill="url(#g)"/><path d="M205 95 L212 111 L228 118 L212 125 L205 141 L198 125 L182 118 L198 111Z" fill="#89d5c0"/><circle cx="48" cy="153" r="8" fill="#89d5c0"/></svg>';
fs.writeFileSync(src+'/logo-mark.svg',mark);const png=new Resvg(mark).render().asPng();fs.writeFileSync(out+'/logo.png',png);fs.writeFileSync(repo+'/game/Assets/WebGLTemplates/TimeThief/icon.png',png);
const ticks=Array.from({length:60},(_,i)=>{let a=i*Math.PI/30,x=800+Math.sin(a)*338,y=440+Math.cos(a)*338,x2=800+Math.sin(a)*(i%5?334:326),y2=440+Math.cos(a)*(i%5?334:326);return '<path d="M'+x+' '+y+' L'+x2+' '+y2+'" stroke="#d9bb7f" stroke-opacity=".28" stroke-width="'+(i%5?1:3)+'"/>';}).join('');
const arena='<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="1000"><defs><radialGradient id="paper"><stop stop-color="#273946"/><stop offset="1" stop-color="#0e1423"/></radialGradient></defs><rect width="1600" height="1000" fill="url(#paper)"/><circle cx="800" cy="440" r="358" fill="none" stroke="#d6b56c" stroke-opacity=".16" stroke-width="2"/><circle cx="800" cy="440" r="315" fill="none" stroke="#d4c6a6" stroke-opacity=".12" stroke-width="2"/>'+ticks+'</svg>';
fs.writeFileSync(src+'/arena-clean.svg',arena);fs.writeFileSync(out+'/arena-clean.png',new Resvg(arena).render().asPng());console.log('Clean logo and backdrop generated.');

const frame='<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128"><path d="M14 2 H114 L126 14 V114 L114 126 H14 L2 114 V14Z" fill="none" stroke="#dcc18b" stroke-width="2"/><path d="M19 8 H109 L120 19 V109 L109 120 H19 L8 109 V19Z" fill="none" stroke="#967948" stroke-opacity=".42"/><path d="M3 24 V14 L14 3 H24 M104 3 H114 L125 14 V24 M125 104 V114 L114 125 H104 M24 125 H14 L3 114 V104" fill="none" stroke="#f2d89e" stroke-width="3"/></svg>';
fs.writeFileSync(src+'/frame-line.svg',frame);fs.writeFileSync(out+'/frame-line.png',new Resvg(frame).render().asPng());
const shape='<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128"><path d="M14 1 H114 L127 14 V114 L114 127 H14 L1 114 V14Z" fill="white"/></svg>';
fs.writeFileSync(out+'/panel.png',new Resvg(shape).render().asPng());fs.writeFileSync(src+'/panel.svg',shape);

const paintedLogo=repo+'/assets/raster/logo.png';if(fs.existsSync(paintedLogo)){const logoSvg='<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512"><image href="data:image/png;base64,'+fs.readFileSync(paintedLogo).toString('base64')+'" width="512" height="512"/></svg>';const webIcon=new Resvg(logoSvg).render().asPng();fs.writeFileSync(out+'/logo.png',webIcon);fs.writeFileSync(repo+'/game/Assets/WebGLTemplates/TimeThief/icon.png',webIcon);}

// Gesture marks: a fingertip impact for taps, a held fingertip inside a magic ring.
const gestures = {
 tap: '<path d="M30 49V23a6 6 0 0 1 12 0v19l5-3 10 7v15c0 8-5 13-13 13H33l-16-21a5 5 0 0 1 8-6l5 5"/><path d="M36 4v6M16 13l5 5M55 13l-5 5M9 30h8"/>',
 hold: '<path d="M31 52V32a5 5 0 0 1 10 0v13l5-2 11 8v12c0 7-5 11-12 11H34L20 56a5 5 0 0 1 7-6l4 5"/><path d="M19 37a22 22 0 1 1 42 0"/><path d="M36 6v8M14 21l7 3M58 21l-7 3"/>',
 ranking: '<path d="M22 12h36v15c0 15-7 22-18 22s-18-7-18-22V12Z M22 18H10v10c0 9 7 14 16 14M58 18h12v10c0 9-7 14-16 14M40 49v13M26 70h28M30 62h20v8"/><path d="m40 22 3 6 7 1-5 5 1 7-6-3-6 3 1-7-5-5 7-1Z" fill="white" stroke="none"/>'
};
for(const [name,paths] of Object.entries(gestures)){const svg='<svg xmlns="http://www.w3.org/2000/svg" width="240" height="240" viewBox="0 0 80 80"><g fill="none" stroke="white" stroke-width="4" stroke-linecap="round" stroke-linejoin="round">'+paths+'</g></svg>';fs.writeFileSync(src+'/icon-'+name+'.svg',svg);fs.writeFileSync(out+'/icon-'+name+'.png',new Resvg(svg).render().asPng());}

// Solid gesture silhouettes stay legible at phone size; the hold mark includes a clock.
const hand='<path d="M29 47V28a6 6 0 0 1 12 0v14l5-3 16 8v15c0 8-5 13-14 13H35L16 55c-4-6 3-13 8-7l5 5Z" fill="white" stroke="#142033" stroke-width="2.5" stroke-linejoin="round"/>';
const revisedMarks={
 tap:'<g fill="none" stroke="white" stroke-width="5" stroke-linecap="round"><circle cx="35" cy="27" r="15"/><path d="M35 2v4M9 13l5 3M61 13l-5 3M5 33h6"/></g>'+hand,
 hold:'<circle cx="54" cy="22" r="19" fill="white"/><path d="M54 10v12l9 5" fill="none" stroke="#142033" stroke-width="5" stroke-linecap="round"/>'+hand,
 attack:'<g transform="rotate(42 40 40)" fill="white" stroke="#142033" stroke-width="2" stroke-linejoin="round"><path d="M40 5 48 18 46 51H34L32 18Z"/><path d="M40 12v37" fill="none" stroke="#142033" stroke-width="2"/><path d="M24 50h32v7H24Z"/><path d="M35 57h10v13H35Z"/><circle cx="40" cy="74" r="5"/></g>'
};
for(const [name,paths] of Object.entries(revisedMarks)){const svg='<svg xmlns="http://www.w3.org/2000/svg" width="320" height="320" viewBox="0 0 80 80">'+paths+'</svg>';fs.writeFileSync(src+'/icon-'+name+'.svg',svg);fs.writeFileSync(out+'/icon-'+name+'.png',new Resvg(svg).render().asPng());}
// The boss target is authored as a vector alongside the other UI marks.
if(fs.existsSync(src+'/weakpoint.svg'))fs.writeFileSync(out+'/weakpoint.png',new Resvg(fs.readFileSync(src+'/weakpoint.svg','utf8')).render().asPng());
