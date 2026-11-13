using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Cryptography;
using UnityEngine;
using Workbench;
public static class BalancedGoals {
 public class Witness {public int level,arena,attempt,seed;public Placement[] build;}
 public class Coverage {public int total,missing;public Row[] coverage;}
 public class Row {public int level,arena,attempts;public bool[] stars;}
 public static void Run(GameData d,string directory,bool resume=false){
  var options=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};var previous=new List<Witness>();string gameHash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory,"../Assets/Workbench/Resources/GameData.json")))).ToLowerInvariant();
  foreach(string name in new[]{"PreviousBalance-20261113/goal-witnesses.json","HardBalance-20261113/goal-witnesses.json"})previous.AddRange(JsonSerializer.Deserialize<List<Witness>>(File.ReadAllText(Path.Combine(directory,name)),options));
  var saved=resume?JsonSerializer.Deserialize<Coverage>(File.ReadAllText(Path.Combine(directory,"goal-coverage.json")),options):null;
  var witnesses=resume?JsonSerializer.Deserialize<List<object>>(File.ReadAllText(Path.Combine(directory,"goal-witnesses.json")),options):new List<object>();var coverage=new List<object>();int total=saved?.total??0,missing=0;
  for(int index=0;index<10;index++)for(int arena=0;arena<4;arena++){
   var l=d.levels[index];var row=saved?.coverage.FirstOrDefault(r=>r.level==index+1&&r.arena==arena+1);var flags=row?.stars??new bool[5];var known=previous.Where(w=>w.level==index+1&&w.arena==arena+1).ToArray();int tries=row?.attempts??0;
   for(int attempt=resume?64:0;attempt<(resume?240:64)&&!flags.All(v=>v);attempt++){
    var w=attempt<known.Length?known[attempt]:null;int design=w?.attempt??attempt-known.Length;var build=w?.build?.ToList()??GoalChecks.Design(d,l,design,arena);
    if(Construction.Validate(build,d,l).Count>0||!build.Any(p=>d.modules[p.type].Weapon))continue;
    total++;tries++;int seed=w?.seed??29017+index*1000+arena*97+attempt;var b=new BattleModel(d,l,build,arena,seed);var enemy=new BotController(b,l,1,32);var pilot=new BotController(b,new LevelSpec{reaction=.15f,aimError=.08f},0,71);bool evasive=attempt%3==2;
    var pilotGoals=new LevelSpec{id=l.id,goals=l.goals.Where((g,i)=>!flags[i+2]).ToArray()};
    for(int tick=0;tick<240*60&&!b.finished;tick++){enemy.Tick();pilot.Tick();GoalChecks.PilotAim(b,pilotGoals,design);if(evasive&&!(pilotGoals.goals.Any(g=>g.kind=="reflect")&&b.robots[0].Shield!=null&&b.robots[0].reflectedHits==0))EvasiveChecks.Dodge(b,attempt);b.Step(1f/60);}
    var earned=Stars.Evaluate(l,b.robots[0],b.robots[1],b.time,b.finished&&b.winner==0);bool gain=Enumerable.Range(0,5).Any(i=>earned[i]&&!flags[i]);Stars.Merge(flags,earned);
    if(gain){witnesses.Add(new{level=index+1,arena=arena+1,attempt=design,seed,seconds=b.time,earned,build,pilot=evasive?"evasive":"target",reflections=b.robots[0].reflectedHits});File.WriteAllText(Path.Combine(directory,"goal-witnesses.json"),JsonSerializer.Serialize(witnesses,options));}
   }
   int left=flags.Count(v=>!v);missing+=left;coverage.Add(new{level=index+1,arena=arena+1,attempts=tries,stars=flags});Console.WriteLine($"Balanced level {index+1}, arena {arena+1}: {5-left}/5 in {tries}");
   File.WriteAllText(Path.Combine(directory,"goal-coverage.json"),JsonSerializer.Serialize(new{total,missing,coverage,game_data_sha256=gameHash,searches=new[]{"Prior legal builds plus generated alternatives; target and evasive pilots; initial 64 attempts per layout, missing conditions up to 240; 240 s per match; achieved goals need not be repeated"}},options));
  }
  Console.WriteLine($"Balanced sweep: {total} legal battles, {200-missing}/200 conditions.");
 }
}
