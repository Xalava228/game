// Original Time Thief score. No samples, recordings, soundfonts or borrowed songs.
// Run with Node.js; every oscillator, note and envelope is reproducible from this file.
const fs = require('fs'), path = require('path');
const output = path.resolve(__dirname, '../game/Assets/TimeThief/Resources/Audio');
fs.mkdirSync(output, { recursive: true });
const rate = 32000, tau = Math.PI * 2, report = [];
let seed = 20260909;
function noise() { seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0; return seed / 2147483648 - 1; }
const hz = note => 440 * 2 ** ((note - 69) / 12);
function track(seconds, loop = true) {
  const count = Math.round(seconds * rate), l = new Float32Array(count), r = new Float32Array(count);
  function note(midi, at, duration, velocity = .12, instrument = 'bell', pan = 0) {
    const frequency = hz(midi), begin = Math.round(at * rate), n = Math.ceil(duration * rate);
    const gl = Math.sqrt((1 - pan) / 2), gr = Math.sqrt((1 + pan) / 2);
    for (let i = 0; i < n; i++) {
      const t = i / rate, phase = tau * frequency * t, u = i / n;
      let wave, envelope;
      if (instrument === 'pad') {
        wave = .57 * Math.sin(phase) + .17 * Math.sin(phase * 1.002) + .12 * Math.sin(phase * 2) + .045 * Math.sin(phase * 3);
        envelope = Math.min(1, t / .25) * Math.min(1, (duration - t) / .55) * (.94 + .06 * Math.sin(tau * .6 * t));
      } else if (instrument === 'pluck') {
        wave = .65 * Math.sin(phase) + .21 * Math.sin(2 * phase) * Math.exp(-t * 5) + .12 * Math.sin(3 * phase) * Math.exp(-t * 9);
        envelope = (1 - Math.exp(-t * 240)) * Math.exp(-t * 3.1) * Math.min(1, (duration - t) / .1);
      } else if (instrument === 'bass') {
        wave = .85 * Math.sin(phase) + .12 * Math.sin(phase * 2);
        envelope = Math.min(1, t / .025) * Math.exp(-t * 1.8) * Math.min(1, (duration - t) / .1);
      } else if (instrument === 'tick') {
        wave = .25 * Math.sin(phase) + .1 * noise();
        envelope = Math.exp(-t * 90) * Math.min(1, t / .002) * (1 - u);
      } else {
        wave = .64 * Math.sin(phase) + .22 * Math.sin(phase * 2.003) * Math.exp(-t * 3.3) + .11 * Math.sin(phase * 3.99) * Math.exp(-t * 6);
        envelope = (1 - Math.exp(-t * 190)) * Math.exp(-t * 1.55) * Math.min(1, (duration - t) / .18);
      }
      const pos = loop ? (begin + i) % count : begin + i;
      if (pos >= count) break;
      l[pos] += wave * envelope * velocity * gl; r[pos] += wave * envelope * velocity * gr;
    }
  }
  function write(name, room = true) {
    if (room) {
      const a = l.slice(), b = r.slice();
      for (const [delay, gain] of [[.127,.16],[.271,.11],[.433,.075]]) {
        const d = Math.round(delay * rate);
        for (let i = 0; i < count; i++) {
          const source = loop ? (i - d + count) % count : i - d;
          if (source >= 0) { l[i] += b[source] * gain; r[i] += a[source] * gain; }
        }
      }
    }
    let peak = 0, energy = 0;
    for (let i = 0; i < count; i++) { peak = Math.max(peak, Math.abs(l[i]), Math.abs(r[i])); energy += l[i] ** 2 + r[i] ** 2; }
    const gain = Math.min(.72 / Math.max(.001, peak), .14 / Math.max(.001, Math.sqrt(energy / count / 2)));
    const dataSize = count * 4, file = Buffer.alloc(44 + dataSize);
    file.write('RIFF'); file.writeUInt32LE(36 + dataSize, 4); file.write('WAVEfmt ', 8); file.writeUInt32LE(16,16);
    file.writeUInt16LE(1,20); file.writeUInt16LE(2,22); file.writeUInt32LE(rate,24); file.writeUInt32LE(rate*4,28); file.writeUInt16LE(4,32); file.writeUInt16LE(16,34); file.write('data',36); file.writeUInt32LE(dataSize,40);
    for (let i = 0; i < count; i++) { file.writeInt16LE(Math.round(Math.max(-1,Math.min(1,l[i]*gain))*32767),44+i*4); file.writeInt16LE(Math.round(Math.max(-1,Math.min(1,r[i]*gain))*32767),46+i*4); }
    fs.writeFileSync(path.join(output,name+'.wav'),file);
    report.push({name,seconds:count/rate,peak:+(peak*gain).toFixed(4),rms:+(Math.sqrt(energy/count/2)*gain).toFixed(4),loop});
  }
  return {note,write};
}

// The quiet shop: an original 16-bar, three-beat music-box miniature in D minor.
function minuteShop() {
  const beat=60/78, bars=16, t=track(bars*3*beat);
  const chords=[[50,57,65],[46,53,62],[53,60,69],[48,55,64],[43,50,58],[50,57,65],[46,53,62],[45,52,61]];
  const phrases=[[74,77,76,69],[70,74,77,72],[72,76,81,79],[76,72,67,69],[70,74,79,77],[77,76,74,69],[74,70,69,65],[73,76,69,73], [77,81,79,77],[74,77,76,70],[81,79,76,72],[79,76,72,67],[79,77,74,70],[81,77,76,74],[77,74,70,69],[76,73,69,73]];
  for(let bar=0;bar<bars;bar++) {
    const at=bar*3*beat,c=chords[bar%8];
    c.forEach((n,i)=>t.note(n,at,3.5*beat,.044,'pad',(i-1)*.45));
    t.note(c[0]-12,at,1.6*beat,.10,'bass');
    [0,1,2].forEach((i)=>t.note(c[i]+12,at+(i+.25)*beat,1.8*beat,.10,'pluck',(i-1)*.5));
    phrases[bar].forEach((n,i)=>t.note(n,at+[0,.75,1.5,2.25][i]*beat,2*beat,.17*(i===0?1:.83),'bell',.22));
  }
  t.write('minute-shop');
}

// Movement: layered plucked clocks and a restrained, newly composed eight-bar motif with a reply.
function secondThief() {
  const beat=60/96,bars=16,t=track(bars*4*beat);
  const chords=[[50,57,65],[46,53,62],[53,60,69],[48,55,64],[43,50,58],[46,53,62],[45,52,61],[50,57,65]];
  const phrases=[[74,77,69,72,76,74],[74,70,65,69,72,70],[77,81,76,72,79,77],[76,79,72,67,71,72],[74,79,77,70,74,72],[77,74,70,65,69,70],[73,76,81,79,76,73],[77,76,74,69,72,74]];
  for(let bar=0;bar<bars;bar++) {
    const at=bar*4*beat,c=chords[bar%8];
    c.forEach((n,i)=>t.note(n,at,4.5*beat,.044,'pad',(i-1)*.6));
    [0,2].forEach(b=>t.note(c[0]-12,at+b*beat,1.8*beat,.18,'bass'));
    for(let i=0;i<8;i++) { t.note(c[[0,1,2,1,0,2,1,2][i]]+12,at+i*.5*beat,.95*beat,.13,'pluck',i%2?.42:-.42); t.note(i%2?89:82,at+i*.5*beat,.07,.075,'tick',i%2?.7:-.7); }
    phrases[bar%8].forEach((n,i)=>t.note(n+(bar>=8&&i===4?12:0),at+[0,.75,1.5,2,2.75,3.5][i]*beat,1.8*beat,.22,'bell',-.12));
  }
  t.write('second-thief');
}

// Bosses: the same acoustic palette with lower harmony, ostinato and a distinct original theme.
function lastHour() {
  const beat=60/112,bars=16,t=track(bars*4*beat);
  const chords=[[50,57,62],[46,53,58],[43,50,58],[45,52,61],[50,57,65],[48,55,64],[46,53,62],[45,52,61]];
  const phrases=[[74,69,77,76,74],[70,65,74,72,70],[67,74,77,74,70],[73,69,76,79,76],[77,81,79,77,74],[76,79,84,79,76],[77,74,70,74,77],[76,73,69,73,76]];
  for(let bar=0;bar<bars;bar++) {
    const at=bar*4*beat,c=chords[bar%8];
    c.forEach((n,i)=>t.note(n,at,4.6*beat,.055,'pad',(i-1)*.6));
    for(let i=0;i<8;i++) {t.note(c[i%3]+(i%4===3?12:0),at+i*.5*beat,1.3*beat,.19,'pluck',i%2?.3:-.3);t.note(i%2?90:76,at+i*.5*beat,.09,.10,'tick',i%2?.6:-.6);}
    [0,1.5,2,3].forEach((b,i)=>t.note(c[0]-12+(i===3?7:0),at+b*beat,.9*beat,.22,'bass'));
    phrases[bar%8].forEach((n,i)=>t.note(n,at+[0,1,1.5,2.5,3.25][i]*beat,2.2*beat,.23,'bell',.1));
  }
  t.write('last-hour');
}
function cue(name,notes,step=.095,instrument='bell',duration=.85){const t=track(notes.length*step+duration,false);notes.forEach((n,i)=>t.note(n,i*step,duration,.22,instrument));t.write(name);}
minuteShop();secondThief();lastHour();
cue('hit',[74],.1,'pluck',.16);cue('critical',[77,86],.045,'bell',.4);
cue('magic-start',[69,74,77],.05,'bell',.45);
const magic=track(1.25);[74,81,86,81].forEach((n,i)=>magic.note(n,i*.3125,.7,.12,'bell',(i%2-.5)*.6));magic.write('magic-loop');
cue('enemy-hit',[43,38],.045,'pluck',.20);cue('enemy-magic',[70,66],.055,'bell',.25);
cue('victory',[74,77,81,86],.10,'bell',.85);cue('miniboss-intro',[50,57,62],.15,'pluck',.8);
cue('boss-intro',[38,45,50,61],.15,'bell',1.2);cue('upgrade',[77,81,86],.08,'bell',.65);
cue('shop',[74,81],.12,'bell',.5);cue('game-over',[69,65,62,50],.19,'bell',1.1);
const qa=path.resolve(__dirname,'../QA');fs.mkdirSync(qa,{recursive:true});fs.writeFileSync(path.join(qa,'audio-checks.json'),JSON.stringify(report,null,2)+'\n');
console.log(report);
