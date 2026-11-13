using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using Workbench;

// Disposable data variants, never saved back to the game. Damage/volley variants are applied
// at the actual muzzle event before the projectile advances; this is a balancing experiment.
public static class BalanceProbe {
 public static void Run(GameData d,string directory){
  var variants=new[]{new{key="hp50_tuned",hp=5f,error=1.4f,damage=.4f,on=2f,off=.7f},new{key="hp25_tuned",hp=2.5f,error=1.4f,damage=.7f,on=2f,off=.6f}};
  var results=new List<object>();var originalErrors=d.levels.Select(l=>l.aimError).ToArray();
  foreach(var variant in variants){d.balance.enemyDurabilityLast=variant.hp;foreach(var l in d.levels){l.aimError=Mathf.Max(.8f,originalErrors[l.id]*variant.error);l.reaction=.25f-.03f*l.id/9;}
   for(int index=0;index<10;index++){
    int compact=0,heavy=0,finished=0;float seconds=0;
    for(int arena=0;arena<4;arena++)foreach(bool big in new[]{false,true}){
     var l=d.levels[index];var build=CampaignChecks.Build(d,l,big);var b=new BattleModel(d,l,build,arena,271+l.id*8+arena);var enemy=new BotController(b,l,1,32);var pilot=new BotController(b,new LevelSpec{reaction=.2f,aimError=.3f},0,71);int lastShot=-1;
     float outgoing=index<3?.9f:index==7?.85f:variant.damage;
     b.Effect+=(kind,point,team)=>{if(!kind.StartsWith("muzzle_"))return;foreach(var p in b.projectiles)if(p.id>lastShot&&p.owner==1)p.damage*=outgoing;if(b.projectiles.Count>0)lastShot=Mathf.Max(lastShot,b.projectiles.Max(p=>p.id));};
     for(int tick=0;tick<180*60&&!b.finished;tick++){enemy.Tick();if(b.time%(variant.on+variant.off)>=variant.on)b.robots[1].firing=false;pilot.Tick();b.Step(1f/60);}
     if(b.finished)finished++;if(b.winner==0&&b.finished){if(big)heavy++;else compact++;}seconds+=b.time;
    }
    results.Add(new{variant=variant.key,level=index+1,compact_wins=compact,heavy_wins=heavy,finished,mean_seconds=seconds/8});Console.WriteLine($"{variant.key} level {index+1}: compact {compact}/4, heavy {heavy}/4, done {finished}/8, mean {seconds/8:F1}s");File.WriteAllText(Path.Combine(directory,"balance-probe.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
   }
  }
 }
}
