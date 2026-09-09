// Portable checks of the production C# rules. This file is OUTSIDE Assets.
// The adapter supplies only math and asset containers; no fake Unity build is produced.
using System;
using System.Linq;
using TimeThief;
namespace UnityEngine {
 public class Object {public static implicit operator bool(Object o)=>o!=null;}
 public class ScriptableObject:Object{} public class Sprite:Object{} public class AudioClip:Object{}
 public struct Color {public Color(float r,float g,float b){}}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
 public class CreateAssetMenuAttribute:Attribute {public string menuName;}
 public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){}}
 public class HeaderAttribute:Attribute {public HeaderAttribute(string s){}}
 public static class Random {static System.Random r=new System.Random(1234);public static float value=>(float)r.NextDouble();}
 public static class Mathf {
  public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);
  public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);
  public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);public static float Sqrt(float v)=>MathF.Sqrt(v);
  public static float Sin(float v)=>MathF.Sin(v);
  public static float Log(float v)=>MathF.Log(v);public static float Log(float v,float b)=>MathF.Log(v,b);
  public static int RoundToInt(float v)=>(int)MathF.Round(v);public static int FloorToInt(float v)=>(int)MathF.Floor(v);
 }
}
namespace TimeThief {
 public class GameManager {
  public GameConfig config;public PlayerStats player=new PlayerStats();public ShopManager shop=new ShopManager();public bool IsFighting=true;public bool won,lost;public TestUI ui=new TestUI();public TestAudio music=new TestAudio();
  public void Win(){won=true;IsFighting=false;}public void Lose(){lost=true;IsFighting=false;}
 }
 public class TestUI {public float taken;public bool magic,heavy,reflected;public void Hit(float x,float recovered,bool c){}public void EnemyHit(float t,bool m,bool h=false,bool r=false){taken=t;magic=m;heavy=h;reflected=r;}}
 public class TestAudio {public void Sfx(UnityEngine.AudioClip clip){}}
}
class ModelChecks {
 static int checks;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static GameConfig Config(){var c=new GameConfig();c.enemies=Enumerable.Range(0,16).Select(i=>new EnemyData{artKey="enemy"+i,attackType=i==6||i==2||i==4||i==9||i==10||i==12||i==13?AttackType.Magic:AttackType.Physical,enemySprite=new UnityEngine.Sprite()}).ToArray();c.bosses=Enumerable.Range(0,10).Select(i=>new BossData{artKey="boss-"+i,specialAbilityType=(Ability)(i+1),bossSprite=new UnityEngine.Sprite(),attackType=(AttackType)(i%2)}).ToArray();return c;}
 static void Main(){var c=Config();
  var p=new PlayerStats{CurrentTime=4.9f};Check(Math.Abs(p.AddTime(1)-.1f)<.0001,"receiver cap");Check(p.LoseTime(100)==5&&p.CurrentTime==0,"overkill cap");Check(PlayerStats.Reduced(2,100,10)>0,"defense nonnegative");
  for(int s=1;s<=4;s++)for(int l=1;l<=1100;l++){var e=EnemyGenerator.Generate(c,l,s);Check(e.maxTime>0&&float.IsFinite(e.maxTime)&&e.cooldown>=c.minimumCooldown,"finite difficulty");Check(!(e.physicalDefense>.35f&&e.magicDefense>.35f),"opposing defenses");Check(e.modifiers.Distinct().Count()==e.modifiers.Length,"unique modifiers");}
  Check(EnemyGenerator.TypeAt(50)==EncounterType.Boss&&EnemyGenerator.TypeAt(10)==EncounterType.MiniBoss&&EnemyGenerator.TypeAt(275)==EncounterType.Boss,"boss schedule");
  var game=new GameManager{config=c};var enemy=new EnemyController(game,EnemyGenerator.Generate(c,1,1));enemy.time=.2f;game.player.CurrentTime=1;enemy.Hit(false);Check(game.won&&Math.Abs(game.player.CurrentTime-1.12f)<.0001,"lethal hit recovers 60% of actual time");float after=game.player.CurrentTime;enemy.Hit(false);Check(game.player.CurrentTime==after,"hits after victory disabled");
  game=new GameManager{config=c};enemy=new EnemyController(game,EnemyGenerator.Generate(c,2,1));float original=enemy.time;for(int i=0;i<30;i++)enemy.Hit(true,1f/60);Check(Math.Abs(original-enemy.time-1.3f)<.001,"magic is frame-time based");
  game=new GameManager{config=c};enemy=new EnemyController(game,EnemyGenerator.Generate(c,20,1));enemy.time=enemy.data.maxTime;original=enemy.time;enemy.timer=.001f;enemy.Tick(.01f);Check(enemy.time>enemy.data.maxTime&&Math.Abs(enemy.time-original-(5-game.player.CurrentTime))<.0001,"enemy receives all time above initial capacity");
  Check(Math.Abs(game.ui.taken-(5-game.player.CurrentTime))<.0001,"damage notice uses actual removed time");
  game=new GameManager{config=c};enemy=new EnemyController(game,EnemyGenerator.Generate(c,125,1));enemy.data.ability=Ability.HeavyStrike;enemy.data.attackType=AttackType.Magic;enemy.attackCount=2;enemy.timer=0;enemy.Tick(.01f);Check(game.ui.heavy&&game.ui.magic&&game.ui.taken>0,"third heavy strike is identified with its damage type");
  game=new GameManager{config=c};game.player.Attack=.01f;enemy=new EnemyController(game,EnemyGenerator.Generate(c,175,1));enemy.data.ability=Ability.Thorns;for(int i=0;i<4;i++)enemy.Hit(false);Check(game.ui.reflected&&game.ui.taken>0&&!game.ui.heavy,"reflected fourth tap has distinct feedback");
  var shop=new ShopManager{shards=100};p=new PlayerStats();Check(shop.Buy(0,p)&&p.MaxTime==6,"shop max time");Check(shop.Buy(1,p)&&Math.Abs(p.Attack-1.15)<.001,"shop attack");Check(shop.Buy(2,p)&&!shop.Buy(2,p),"unique buffs");shop.EndBattle();shop.EndBattle();Check(shop.Has(BuffType.Armor),"buff after 2 battles");shop.EndBattle();Check(!shop.Has(BuffType.Armor),"buff expires at 3");Check(RewardManager.Choices(1,25,true).Select(r=>r.stat).Distinct().Count()==3,"three different gifts");

  for(int seed=1;seed<=20;seed++)for(int level=2;level<=1100;level++){
   var a=EnemyGenerator.Generate(c,level-1,seed);var b=EnemyGenerator.Generate(c,level,seed);
   Check(b.maxTime>a.maxTime && b.power>a.power && b.cooldown<=a.cooldown,"every level has stronger base stats, including after bosses");
   Check(a.artKey!=b.artKey,"no adjacent repeated silhouettes");
  }
  Check(Enumerable.Range(6,500).Select(l=>EnemyGenerator.Generate(c,l,3).condition).Distinct().Count()==8,"all eight battle conditions appear");
  Check(c.bosses.Select(b=>b.artKey).Distinct().Count()==10&&!c.bosses.Any(b=>c.enemies.Any(e=>e.artKey==b.artKey)),"independent boss catalogue");
  game=new GameManager{config=c};game.player.CritChance=0;game.player.CurrentTime=1;enemy=new EnemyController(game,EnemyGenerator.Generate(c,1,1));
  float before=enemy.time;enemy.Hit(true,.1f);Check(Math.Abs(before-enemy.time-.026f)<.0001,"90% own-type resistance");
  Check(Math.Abs(game.player.CurrentTime-1-.026f*.6f)<.0001,"recover only 60% of resisted damage");
  before=enemy.time;float taken=enemy.DrainPlayer(.25f);Check(Math.Abs(enemy.time-before-taken)<.0001,"passive time flow fully transfers");
  enemy.data.ability=Ability.Regenerate;enemy.time=enemy.data.maxTime+5;before=enemy.time;enemy.Tick(.01f);Check(enemy.time==before,"regeneration cannot erase absorbed time");
  enemy.data.condition=BattleCondition.FrayedTime;Check(enemy.RecoveryFraction==.45f,"frayed recovery");
  enemy.data.condition=BattleCondition.Abundance;Check(enemy.RecoveryFraction==.75f,"abundant recovery");
  enemy.data.condition=BattleCondition.QuickSand;Check(Math.Abs(enemy.FlowRate-1.2f)<.001,"quick time flow");
  enemy.data.condition=BattleCondition.SlowGlass;Check(Math.Abs(enemy.FlowRate-.85f)<.001,"slow time flow");
  enemy.data.ability=Ability.SwitchDefense;enemy.elapsed=0;Check(enemy.Defense(true)==.9f,"initial element resistance");enemy.elapsed=4;Check(enemy.Defense(false)==.9f&&enemy.Defense(true)<.9f,"resistance follows changed element");
  // Counter-element tutorial, with a human-sized response delay.
  for(int level=1;level<=5;level++){game=new GameManager{config=c};enemy=new EnemyController(game,EnemyGenerator.Generate(c,level,3));bool magic=!enemy.UsesMagic;for(int frame=0;frame<900&&game.IsFighting;frame++){enemy.DrainPlayer(1f/60);if(game.player.CurrentTime<=0){game.Lose();break;}enemy.Tick(1f/60);if(frame>=30){if(magic)enemy.Hit(true,1f/60);else if(frame%20==0)enemy.Hit(false);}}Check(game.won,"tutorial winnable using indicated counter: "+level);}
  Console.WriteLine(checks+" portable model checks passed. Production generation/combat/stats/shop/reward code; math/assets adapter only. Unity runtime integration is a separate check.");
 }
}
