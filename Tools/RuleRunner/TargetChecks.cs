using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Workbench;

public static class TargetChecks {
 static Placement P(int id,int type,int x,int y)=>new Placement{id=id,type=type,x=x,y=y};
 public static void Run(GameData d,string report){
  var walls=d.arenas[0].walls;d.arenas[0].walls=Array.Empty<WallData>();
  try{
   var enemy=new[]{P(0,0,0,0),P(1,3,0,1),P(2,4,1,0)};
   var b=new BattleModel(d,new LevelSpec{enemy=enemy},new[]{P(0,0,2,2),P(1,3,2,1),P(2,4,1,2)},0);
   b.robots[0].position=new Vector2(30,14);b.robots[1].position=new Vector2(20,16);var bot=new BotController(b,new LevelSpec{reaction=.2f});bot.Tick();
   if(bot.TargetModuleId!=2)throw new Exception("Unique exposed weapon was not prioritized: "+bot.TargetModuleId);
   string result="PASS Unique exposed active weapon takes priority over exposed battery when core is blocked\n";
   var bridge=new[]{P(0,0,2,2),P(1,3,2,1),P(2,3,2,0),P(3,2,1,0),P(4,2,1,1),P(5,2,1,2),P(6,1,2,3),P(7,4,3,3),P(8,4,3,4),P(9,4,4,3)};
   if(Construction.Validate(bridge,d,null).Count!=0)throw new Exception("Illegal bridge fixture");
   b=new BattleModel(d,new LevelSpec{enemy=enemy},bridge,0);b.robots[0].position=new Vector2(30,14);b.robots[1].position=new Vector2(20,17);bot=new BotController(b,new LevelSpec{reaction=.2f});bot.Tick();
   if(bot.TargetModuleId!=6)throw new Exception("Exposed connector carrying three weapons not selected: "+bot.TargetModuleId);
   result+="PASS Exposed connector supporting a three-gun branch selected over generic armour and individual guns\n";
   b.robots[0].Damage(b.robots[0].modules[6],999);b.robots[0].ResolveDamage();b.time=.3f;bot.Tick();
   if(b.robots[0].Weapons.Any()||bot.TargetModuleId==6)throw new Exception("Destroyed connector not reconsidered");
   result+="PASS After connector destruction the whole gun branch detaches and the bot selects a new target\n";File.WriteAllText(report,result);Console.WriteLine(result);
  }finally{d.arenas[0].walls=walls;}
 }
}
