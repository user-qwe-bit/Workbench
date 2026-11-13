using System;
using System.Collections.Generic;
using System.Linq;
using Workbench;
using UnityEngine;
public static class CampaignChecks {
 public static List<Placement> Build(GameData d,LevelSpec l,bool heavy){var p=new List<Placement>{new Placement{id=0,type=0,x=2,y=2}};int next=1;
  Action<int> add=t=>{if(p.Count(x=>x.type==t)>=l.inventory[t])return;for(int ring=0;ring<8;ring++)for(int y=0;y<5;y++)for(int x=0;x<5;x++){if(Math.Abs(x-2)+Math.Abs(y-2)!=ring)continue;var candidate=new Placement{id=next,type=t,x=x,y=y};if(!Construction.Fits(candidate,p,d))continue;var copy=Construction.Copy(p);copy.Add(candidate);if(Construction.Validate(copy,d,l).Count==0){p.Add(candidate);next++;return;}}};
  add(3);add(4);if(!heavy){add(4);if(l.id>=3)add(5);return p;}
  add(3);add(3);add(8);add(6);add(5);add(5);add(4);add(4);add(7);add(7);for(int i=0;i<5;i++){add(2);add(1);}return p;
 }
 public static void Run(GameData d,string report){var rows=new List<string>{"level,arena,design,cells,mass,winner,seconds,player_core,enemy_core,bot_recoveries,player_recoveries,impacts"};
  foreach(var l in d.levels)for(int arena=0;arena<4;arena++)foreach(bool heavy in new[]{false,true}){
   var build=Build(d,l,heavy);var b=new BattleModel(d,l,build,arena,271+l.id*8+arena);var enemy=new BotController(b,l,1,32);var pilot=new BotController(b,new LevelSpec{reaction=.2f,aimError=.3f},0,71);
   for(int tick=0;tick<60*180&&!b.finished;tick++){enemy.Tick();pilot.Tick();b.Step(1f/60);}
   string row=$"{l.id+1},{arena+1},{(heavy?"heavy":"compact")},{build.Sum(p=>d.modules[p.type].width*d.modules[p.type].height)},{b.robots[0].original.Sum(p=>d.modules[p.type].mass)},{(b.finished?b.winner:-2)},{b.time:F1},{b.robots[0].Core.hp:F1},{b.robots[1].Core.hp:F1},{enemy.Recoveries},{pilot.Recoveries},{b.impactCount}";
   rows.Add(row);Console.WriteLine(row);System.IO.File.WriteAllLines(report,rows);
  }
 }
}
