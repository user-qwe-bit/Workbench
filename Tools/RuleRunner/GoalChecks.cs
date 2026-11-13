using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using Workbench;
using UnityEngine;

// Search for playable witnesses using only legal construction and ordinary movement/aim/fire/shield input.
// No damage, HP, positions, cooldowns or star flags are injected into a match.
public static class GoalChecks {
 public static List<Placement> Design(GameData d,LevelSpec level,int attempt,int arena){
  var random=new System.Random(9301+attempt*79+arena*1231+level.id*104729);
  var parts=new List<Placement>{new Placement{id=0,type=0,x=2,y=2}};
  if(level.inventory[8]>0&&attempt%6==0){parts.AddRange(new[]{new Placement{id=1,type=8,x=3,y=1},new Placement{id=2,type=3,x=2,y=1},new Placement{id=3,type=3,x=2,y=3},new Placement{id=4,type=4,x=1,y=2},new Placement{id=5,type=4,x=1,y=1}});if(level.inventory[5]>0)parts.Add(new Placement{id=6,type=5,x=0,y=1});return parts;}
  int maxCells=new[]{8,10,12,18,25}[attempt%5],next=1;
  bool Add(int type){if(parts.Count(p=>p.type==type)>=level.inventory[type])return false;
   var options=new List<Placement>();for(int y=0;y<5;y++)for(int x=0;x<5;x++){
    var p=new Placement{id=next,type=type,x=x,y=y};if(!Construction.Fits(p,parts,d))continue;
    if(parts.Sum(q=>d.modules[q.type].width*d.modules[q.type].height)+d.modules[type].width*d.modules[type].height>maxCells)continue;
    var copy=Construction.Copy(parts);copy.Add(p);if(Construction.Validate(copy,d,level).Count==0)options.Add(p);
   }
   if(options.Count==0)return false;parts.Add(options[random.Next(options.Count)]);next++;return true;
  }
  Add(3);int[] order=(attempt%8) switch {0=>new[]{4,4,4,3,8,5,1},1=>new[]{4,3,8,4,5,1},2=>new[]{5,5,3,4,4,2,1},3=>new[]{4,4,3,6,7,1},4=>new[]{7,7,3,4,2,1},5=>new[]{3,8,5,4,2,1},6=>new[]{5,4,4,3,8,2,1},_=>new[]{4,4,3,2,2,1}};
  foreach(int type in order)Add(type);
  if(attempt%5>=3)for(int i=0;i<8;i++)Add(new[]{1,2,4,7}[random.Next(4)]);
  return parts;
 }
 public static void PilotAim(BattleModel b,LevelSpec level,int attempt){
  var player=b.robots[0];var enemy=b.robots[1];var weapons=player.Weapons.ToArray();foreach(var gun in player.modules){gun.targeted=false;gun.shotAllowed=true;}if(weapons.Length==0)return;
  if(level.goals.Any(g=>g.kind=="reflect")&&player.reflectedHits==0&&player.Shield!=null&&enemy.Weapons.Any()){
   bool window=enemy.Shield==null||!enemy.ShieldActive(b.time)&&enemy.shieldReadyAt>b.time+2;
   player.firing=!window&&!enemy.ShieldActive(b.time);player.shieldRequested=false;Vector2 center=player.CenterOf(player.Shield);var incoming=b.projectiles.Where(p=>p.owner!=0&&Vector2.Dot(p.velocity,center-p.position)>0).OrderBy(p=>Vector2.Distance(p.position,center)).FirstOrDefault();
   float distance=Vector2.Distance(player.Center,enemy.Center);Vector2 approach=(enemy.Center-player.Center).normalized;
   if(distance>9&&b.ClearAt(player,player.position+approach*.5f,true))player.move=approach;else if(distance<=9)player.move=Vector2.zero;
   player.aim=enemy.Shield!=null&&!window?enemy.CenterOf(enemy.Shield):enemy.Center;
   if(window&&incoming!=null&&Vector2.Distance(incoming.position,center)<5){player.aim=incoming.position;player.move=Vector2.zero;if(Vector2.Distance(incoming.position,center)<3.5f)player.shieldRequested=true;}
   return;
  }
  var candidates=enemy.Live.ToArray();bool armor=level.goals.Any(g=>g.kind=="destroy_armor")&&enemy.destroyed[2]<3;
  bool special=level.goals.Any(g=>g.kind.StartsWith("destroy"));
  ModuleState target=null;float best=float.MinValue;
  foreach(var m in candidates){float score=m.Type==0?10:2;if(special&&m.spec.Weapon)score+=60;if(armor&&m.Type==2)score+=90;
   Vector2 pos=enemy.CenterOf(m);if(!b.LineClear(player.Center,pos))score-=100;
   foreach(var obstacle in candidates)if(obstacle!=m&&BattleModel.SegmentRect(player.Center,pos,enemy.RectOf(obstacle))<.97f)score-=25;
   score+=(1-m.hp/m.maxHp)*5;if(score>best){best=score;target=m;}
  }
  if(target==null)return;float speed=weapons.Average(m=>m.spec.projectileSpeed);Vector2 aim=enemy.CenterOf(target);
  if(attempt%3!=0)aim+=enemy.velocity*Mathf.Min(1.5f,Vector2.Distance(player.Center,aim)/speed);
  player.aim=aim;player.firing=b.LineClear(player.Center,aim)&&!enemy.ShieldActive(b.time);
  if(attempt%4==1&&special&&target.Type!=0&&BattleModel.SegmentRect(player.Center,enemy.CenterOf(target),enemy.RectOf(enemy.Core))<.99f){
   player.firing=false;Vector2 radial=(player.Center-enemy.Center).normalized;Vector2 side=new Vector2(-radial.y,radial.x)*(attempt%2==0?1:-1);
   if(b.ClearAt(player,player.position+side*.5f,true))player.move=side;else if(b.ClearAt(player,player.position-side*.5f,true))player.move=-side;
  }
  if(level.goals.Any(g=>g.kind=="reflect")&&player.Shield!=null){Vector2 center=player.CenterOf(player.Shield);bool window=enemy.Shield==null||!enemy.ShieldActive(b.time)&&enemy.shieldReadyAt>b.time+2;player.shieldRequested=window&&b.projectiles.Any(p=>p.owner!=0&&Vector2.Distance(p.position,center)<3.5f&&Vector2.Dot(p.velocity,center-p.position)>0);}
  if(level.id==8)player.shieldRequested=false;
 }
 public static void Run(GameData data,string directory,bool focused=false,bool finalOnly=false){
  var witnesses=new List<object>();var coverage=new List<object>();int total=0,missing=0;
  for(int levelIndex=0;levelIndex<10;levelIndex++)for(int arena=0;arena<4;arena++){
   if(focused&&levelIndex!=5&&levelIndex!=9)continue;
   if(finalOnly&&levelIndex!=9)continue;
   var level=data.levels[levelIndex];var collected=new bool[5];int attempts=0;
   for(int attempt=0;attempt<240&&!collected.All(x=>x);attempt++){
    attempts++;total++;var build=Design(data,level,attempt,arena);if(!build.Any(p=>data.modules[p.type].Weapon))continue;
    int seed=873+level.id*10000+arena*121+attempt;var battle=new BattleModel(data,level,build,arena,seed);
    var enemy=new BotController(battle,level,1,32);var pilot=new BotController(battle,new LevelSpec{reaction=.12f,aimError=.08f},0,71);
    for(int tick=0;tick<60*240&&!battle.finished;tick++){enemy.Tick();pilot.Tick();PilotAim(battle,level,attempt);battle.Step(1f/60);}
    var earned=Stars.Evaluate(level,battle.robots[0],battle.robots[1],battle.time,battle.winner==0&&battle.finished);
    bool gain=Enumerable.Range(0,5).Any(i=>earned[i]&&!collected[i]);Stars.Merge(collected,earned);
    if(gain){witnesses.Add(new {level=levelIndex+1,arena=arena+1,attempt,seed,seconds=battle.time,earned,build,reflections=battle.robots[0].reflectedHits});File.WriteAllText(Path.Combine(directory,finalOnly?"goal-witnesses-final.json":focused?"goal-witnesses-focused.json":"goal-witnesses.json"),JsonSerializer.Serialize(witnesses,new JsonSerializerOptions{IncludeFields=true,WriteIndented=true}));}
   }
   int left=collected.Count(x=>!x);missing+=left;coverage.Add(new {level=levelIndex+1,arena=arena+1,attempts,stars=collected});Console.WriteLine($"Level {levelIndex+1}, arena {arena+1}: {5-left}/5 stars in {attempts} battles");
   File.WriteAllText(Path.Combine(directory,finalOnly?"goal-coverage-final.json":focused?"goal-coverage-focused.json":"goal-coverage.json"),JsonSerializer.Serialize(new{total,missing,coverage},new JsonSerializerOptions{WriteIndented=true}));
  }
  Console.WriteLine($"Goal sweep completed: {total} legal battles, {missing} missing arena/star combinations.");
 }
}
