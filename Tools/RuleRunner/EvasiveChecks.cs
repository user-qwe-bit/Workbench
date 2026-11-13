using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using Workbench;

// A second, evasive test pilot. Controls only ordinary movement/aim/fire/shield input.
public static class EvasiveChecks {
 public static void Dodge(BattleModel b,int attempt){
  var p=b.robots[0];var e=b.robots[1];if(!p.Weapons.Any())return;
  Vector2 radial=(e.Center-p.Center).normalized,side=new Vector2(-radial.y,radial.x);float distance=Vector2.Distance(e.Center,p.Center);
  float range=p.Weapons.Any(m=>m.Type==5)?(attempt%3==0?24:31):p.Weapons.Any(m=>m.Type==4)?(attempt%3==0?12:23):p.Weapons.Any(m=>m.Type==6)?18:3;
  float rhythm=.45f+(attempt%5)*.2f,sign=((int)(b.time/rhythm)+attempt)%2==0?1:-1;
  Vector2 input=side*sign+radial*Mathf.Clamp((distance-range)*.5f,-1,1);
  if(b.LineClear(p.Center,e.Center)){
   if(b.ClearAt(p,p.position+input.normalized*.6f,true))p.move=input.normalized;
   else if(b.ClearAt(p,p.position+(input-side*sign*2).normalized*.6f,true))p.move=(input-side*sign*2).normalized;
  }
  var shot=b.projectiles.Where(s=>s.owner!=0&&Vector2.Dot(s.velocity,p.Center-s.position)>0).OrderBy(s=>Vector2.Distance(s.position,p.Center)).FirstOrDefault();
  if(shot!=null&&Vector2.Distance(shot.position,p.Center)<9){
   Vector2 tangent=new Vector2(-shot.velocity.y,shot.velocity.x).normalized;
   if(Vector2.Dot(tangent,p.velocity)<0)tangent=-tangent;
   if(b.ClearAt(p,p.position+tangent*.8f,true))p.move=tangent;
   else if(b.ClearAt(p,p.position-tangent*.8f,true))p.move=-tangent;
  }
 }
 public static void Run(GameData d,string directory){
  int total=0;var coverage=new List<object>();var witnesses=new List<object>();
  foreach(int index in new[]{4,6,8,9,3,5})for(int arena=0;arena<4;arena++){
   var l=d.levels[index];var flags=new bool[5];int tried=0;
   for(int attempt=0;attempt<48&&!flags.All(x=>x);attempt++){
    tried++;total++;var build=GoalChecks.Design(d,l,attempt,arena);if(Construction.Validate(build,d,l).Count>0||!build.Any(p=>d.modules[p.type].Weapon))continue;
    int seed=18017+index*1000+arena*97+attempt;var b=new BattleModel(d,l,build,arena,seed);var enemy=new BotController(b,l,1,32);var pilot=new BotController(b,new LevelSpec{reaction=.15f,aimError=0},0,71);
    for(int tick=0;tick<120*60&&!b.finished;tick++){enemy.Tick();pilot.Tick();GoalChecks.PilotAim(b,l,attempt);Dodge(b,attempt);b.Step(1f/60);}
    var earned=Stars.Evaluate(l,b.robots[0],b.robots[1],b.time,b.finished&&b.winner==0);bool gain=Enumerable.Range(0,5).Any(i=>earned[i]&&!flags[i]);Stars.Merge(flags,earned);
    if(gain){witnesses.Add(new{level=index+1,arena=arena+1,attempt,seed,seconds=b.time,earned,build,pilot="evasive",reflections=b.robots[0].reflectedHits});File.WriteAllText(Path.Combine(directory,"goal-witnesses-evasive.json"),JsonSerializer.Serialize(witnesses,new JsonSerializerOptions{IncludeFields=true,WriteIndented=true}));}
   }
   coverage.Add(new{level=index+1,arena=arena+1,attempts=tried,stars=flags});Console.WriteLine($"Evasive level {index+1}, arena {arena+1}: {flags.Count(x=>x)}/5 in {tried}");File.WriteAllText(Path.Combine(directory,"goal-coverage-evasive.json"),JsonSerializer.Serialize(new{total,coverage},new JsonSerializerOptions{WriteIndented=true}));
  }
 }
}
