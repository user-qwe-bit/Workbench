using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Workbench {
[Serializable] public class ModuleSpec {
 public int id,width,height,pellets; public string key,title,description;
 public float hp,mass,damage,interval,projectileSpeed,range,spread,blastRadius;
 public int Cost => id==0||id==3?0:width*height;
 public bool Weapon=>id>=4&&id<=7;
}
[Serializable] public class Placement { public int id,type,x,y; public Placement Copy()=>new Placement{id=id,type=type,x=x,y=y}; }
[Serializable] public class GoalSpec { public string kind,text; public float value; }
[Serializable] public class LevelSpec { public int id,defaultArena; public string title; public int[] inventory; public Placement[] enemy; public float targetSeconds,reaction,aimError,enemyDamageScale=1,burstOn=2,burstOff=.6f; public GoalSpec[] goals; }
[Serializable] public class PointData { public float x,y; public Vector2 V=>new Vector2(x,y); }
[Serializable] public class WallData { public int x,y,mask; }
[Serializable] public class ArenaSpec { public int id; public string title; public WallData[] walls; public PointData[] spawns; }
[Serializable] public class DirectionData { public int index; public PointData[] muzzles; }
[Serializable] public class TurretData { public int type; public int[] recoil; public DirectionData[] directions; }
[Serializable] public class PropData { public string id,name,path,parent_id; public int x,y,width,height,layer; public float scale; }
[Serializable] public class MarkingData { public string name; public int x,y,rotation; }
[Serializable] public class GridRow { public int[] values; }
[Serializable] public class ClipData { public string name; public string[] frames; public float fps; public bool loop; public string mode,end; }
[Serializable] public class Balance {
 public int gridSize=5,batteryCapacity=5;
 public float speedBase=10,speedMax=9,speedMin=3.5f,massDrag=.02f,massInfluence=1.25f;
 public float lightMass=20,heavyMass=80,lightAccelerationTime=.22f,heavyAccelerationTime=.32f,lightStopTime=.15f,heavyStopTime=.23f,reverseBraking=2,stopEpsilon=.015f;
 public float impactDamage=15,impactMinSpeed=.75f,wallDamageFactor=.5f,playerDamageScale=1/1.1f,enemyDurabilityFirst=1,enemyDurabilityLast=2.5f;
 public float shieldDuration=3,shieldCooldown=30,shieldRadius=1.8f,shieldArc=120,cameraMinSize=8.4375f,cameraMargin=3;
 public bool restorePower;
}
[Serializable] public class GameData {
 public ModuleSpec[] modules; public LevelSpec[] levels; public ArenaSpec[] arenas; public TurretData[] turrets; public PropData[] props; public MarkingData[] markings; public GridRow[] floor,surface; public ClipData[] clips; public Balance balance;
 public static GameData Load()=>JsonUtility.FromJson<GameData>(Resources.Load<TextAsset>("GameData").text);
 public float Speed(float mass)=>Mathf.Clamp(balance.speedBase/(1+Mathf.Max(0,mass)*balance.massDrag*balance.massInfluence),balance.speedMin,balance.speedMax);
 public float MassBlend(float mass)=>Mathf.InverseLerp(balance.lightMass,balance.heavyMass,mass);
 public float AccelerationTime(float mass)=>Mathf.Lerp(balance.lightAccelerationTime,balance.heavyAccelerationTime,MassBlend(mass));
 public float StopTime(float mass)=>Mathf.Lerp(balance.lightStopTime,balance.heavyStopTime,MassBlend(mass));
 public float EnemyDurability(int level)=>Mathf.Lerp(balance.enemyDurabilityFirst,balance.enemyDurabilityLast,Mathf.Clamp01(level/9f));
 public Vector2 Muzzle(int type,int direction,int shot) { var t=turrets.First(x=>x.type==type).directions[direction]; return t.muzzles[shot%t.muzzles.Length].V; }
 public static int Direction(Vector2 v)=>((Mathf.RoundToInt(Mathf.Atan2(-v.y,v.x)*Mathf.Rad2Deg/11.25f)%32)+32)%32;
}
public static class Construction {
 // The dragged part is absent from parts. Commit only after the entire exchange fits.
 public static bool TryPlace(IList<Placement> parts,Placement moved,Placement original,GameData d,out List<Placement> result){
  result=Copy(parts);var hits=result.Where(p=>Rect(p,d).Overlaps(Rect(moved,d))).ToArray();
  foreach(var hit in hits)result.Remove(hit);
  if(original!=null&&hits.Length>0){
   int minX=hits.Min(p=>p.x),minY=hits.Min(p=>p.y);
   foreach(var hit in hits){hit.x=original.x+hit.x-minX;hit.y=original.y+hit.y-minY;result.Add(hit);}
  }
  result.Add(moved.Copy());
  var candidate=result;if(candidate.Any(p=>!Fits(p,candidate,d))||candidate.Select(p=>p.id).Distinct().Count()!=candidate.Count){result=null;return false;}
  return true;
 }
 public struct Link {public int a,b;public Vector2 center;public bool horizontal;}
 public static IEnumerable<Link> Links(IList<Placement> parts,GameData d){
  for(int i=0;i<parts.Count;i++)for(int j=i+1;j<parts.Count;j++){
   var a=Rect(parts[i],d);var b=Rect(parts[j],d);if(!Adjacent(a,b))continue;
   bool horizontal=Mathf.Abs(a.xMax-b.xMin)<.001f||Mathf.Abs(b.xMax-a.xMin)<.001f;
   float lo=horizontal?Mathf.Max(a.yMin,b.yMin):Mathf.Max(a.xMin,b.xMin),hi=horizontal?Mathf.Min(a.yMax,b.yMax):Mathf.Min(a.xMax,b.xMax);
   for(float c=lo+.5f;c<hi;c++)yield return new Link{a=parts[i].id,b=parts[j].id,horizontal=horizontal,center=horizontal?new Vector2(Mathf.Max(a.xMin,b.xMin),c):new Vector2(c,Mathf.Max(a.yMin,b.yMin))};
  }
 }
 public static Rect Rect(Placement p,GameData d)=>new Rect(p.x,p.y,d.modules[p.type].width,d.modules[p.type].height);
 public static bool Adjacent(Rect a,Rect b)=>((Mathf.Abs(a.xMax-b.xMin)<.001f||Mathf.Abs(b.xMax-a.xMin)<.001f)&&Mathf.Max(a.yMin,b.yMin)<Mathf.Min(a.yMax,b.yMax))||((Mathf.Abs(a.yMax-b.yMin)<.001f||Mathf.Abs(b.yMax-a.yMin)<.001f)&&Mathf.Max(a.xMin,b.xMin)<Mathf.Min(a.xMax,b.xMax));
 public static HashSet<int> Reachable(IList<Placement> parts,GameData d) {
  var seen=new HashSet<int>();var core=parts.FirstOrDefault(p=>p.type==0); if(core==null)return seen;
  var q=new Queue<Placement>();q.Enqueue(core);seen.Add(core.id);
  while(q.Count>0){var p=q.Dequeue();foreach(var n in parts)if(!seen.Contains(n.id)&&Adjacent(Rect(p,d),Rect(n,d))){seen.Add(n.id);q.Enqueue(n);}}
  return seen;
 }
 public static bool Fits(Placement p,IList<Placement> parts,GameData d) {var r=Rect(p,d);return r.xMin>=0&&r.yMin>=0&&r.xMax<=d.balance.gridSize&&r.yMax<=d.balance.gridSize&&!parts.Any(o=>o.id!=p.id&&Rect(o,d).Overlaps(r));}
 public static List<string> Validate(IList<Placement> parts,GameData d,LevelSpec level) {
  var errors=new List<string>(); if(parts.Any(p=>p.type<0||p.type>=d.modules.Length)){errors.Add("Неизвестная деталь");return errors;}
  if(parts.Select(p=>p.id).Distinct().Count()!=parts.Count)errors.Add("Повтор идентификатора детали");
  if(parts.Count(p=>p.type==0)!=1)errors.Add("Нужно ровно одно ядро");
  if(parts.Any(p=>!Fits(p,parts,d)))errors.Add("Детали пересекаются или вне поля");
  if(Reachable(parts,d).Count!=parts.Count)errors.Add("Есть несоединённые детали");
  if(parts.Sum(p=>d.modules[p.type].Cost)>parts.Count(p=>p.type==3)*d.balance.batteryCapacity)errors.Add("Не хватает энергии");
  if(parts.Count(p=>p.type==8)>1)errors.Add("Можно установить только один щит");
  if(level!=null&&Enumerable.Range(0,9).Any(t=>parts.Count(p=>p.type==t)>level.inventory[t]))errors.Add("Превышен запас уровня");
  return errors;
 }
 public static List<Placement> Copy(IEnumerable<Placement> p)=>p.Select(x=>x.Copy()).ToList();
}
}
