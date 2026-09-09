using System;
using System.Linq;
using TimeThief;
class BalanceChecks {
 static bool Fight(GameManager g,EnemyController e,int clicks){
  bool mode=!e.UsesMagic;float actionAfter=.35f,nextTap=.35f;
  for(int frame=0;frame<60*90&&g.IsFighting;frame++){
   float dt=1f/60;
   if(!(g.shop.Has(BuffType.Freeze)&&e.elapsed<2))e.DrainPlayer(dt*e.FlowRate);
   if(g.player.CurrentTime<=0){g.Lose();break;}
   e.Tick(dt);bool nextMode=!e.UsesMagic;
   if(nextMode!=mode){mode=nextMode;actionAfter=e.elapsed+.35f;e.holdSeconds=0;}
   if(e.elapsed<actionAfter)continue;
   if(mode){e.holdSeconds+=dt;e.Hit(true,dt);}
   else if(e.elapsed>=nextTap){e.Hit(false);nextTap=e.elapsed+1f/clicks;}
  }
  return g.won;
 }
 static void Gift(GameManager g,Encounter e,int seed){
  if(e.type==EncounterType.Normal)return;
  var options=RewardManager.Choices(seed,e.level,e.type==EncounterType.Boss);
  var reward=options.OrderByDescending(r=>r.stat==Stat.Attack?5:r.stat==Stat.MaxTime?4:r.stat==Stat.CritChance?3:r.stat==Stat.CritMultiplier?2:1).First();
  reward.Apply(g.player);
 }
 static int Run(GameConfig c,int seed,bool shopping,int clicks){
  var g=new GameManager{config=c};
  for(int level=1;level<=300;level++){
   var e=EnemyGenerator.Generate(c,level,seed);
   g.IsFighting=true;g.won=g.lost=false;
   if(!Fight(g,new EnemyController(g,e),clicks))return level-1;
   g.shop.shards+=RewardManager.Shards(e)*(g.shop.Has(BuffType.DoubleShards)?2:1);
   g.shop.EndBattle();g.player.Grow(c.growth);g.player.AddTime(.35f);Gift(g,e,seed);
   if(shopping){
    var next=EnemyGenerator.Generate(c,level+1,seed);
    for(int buy=0;buy<12;buy++){
     float damage=PlayerStats.Reduced(next.power*1.8f,Math.Min(g.player.Armor,g.player.MagicResistance),10);
     int item=g.player.MaxTime<Math.Max(6,damage*1.7f+2)||g.player.CurrentTime<g.player.MaxTime*.45f?0:1;
     if(!g.shop.Buy(item,g.player))break;
    }
    if(next.type==EncounterType.Boss){g.shop.Buy(2,g.player);g.shop.Buy(3,g.player);g.shop.Buy(next.attackType==AttackType.Physical?5:4,g.player);}
   }
  }
  return 300;
 }
 static void Main(string[] args){
  System.Threading.Thread.CurrentThread.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
  var c=ModelChecks.Config();if(args.Length>0)c.powerCurve=float.Parse(args[0],System.Globalization.CultureInfo.InvariantCulture);
  Console.WriteLine("mode,clicks,seed,lastWin");
  foreach(bool shopping in new[]{false,true})foreach(int clicks in new[]{3,5})for(int seed=1;seed<=12;seed++)Console.WriteLine((shopping?"shop":"no-shop")+","+clicks+","+seed+","+Run(c,seed,shopping,clicks));
  Console.WriteLine("level,time,power,cooldown");
  foreach(int level in new[]{1,5,10,25,50,100,250,500,1000}){var e=EnemyGenerator.Generate(c,level,1);Console.WriteLine(level+","+e.maxTime+","+e.power+","+e.cooldown);}
 }
}
