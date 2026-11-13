using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Workbench {
public sealed class ModuleState {
 public Placement placement; public ModuleSpec spec; public float hp,maxHp,nextShot=-100,lastShot=-100; public int shots; public bool powered=true,alive=true,detached; public Vector2 aim,aimTarget;public bool targeted,shotAllowed=true;
 public int Id=>placement.id; public int Type=>placement.type;
}
public sealed class RobotModel {
 public readonly GameData data; public readonly int team; public readonly List<ModuleState> modules; public readonly List<Placement> original;
 public Vector2 position,previousPosition,velocity,driveVelocity,move,aim; public bool firing,shieldRequested; public float wheelDistance;
 public float shieldUntil=-100,shieldReadyAt,shieldStarted; public int shieldUses,reflectedHits,revision;
 public readonly int[] destroyed=new int[9]; public event Action<ModuleState,string> Changed;
 readonly System.Random random; readonly Dictionary<int,float> groupNext=new Dictionary<int,float>(); readonly Dictionary<int,int> cursor=new Dictionary<int,int>();
 public RobotModel(GameData d,IEnumerable<Placement> build,int team,int seed=1,float durability=1){data=d;this.team=team;random=new System.Random(seed);original=Construction.Copy(build);modules=original.Select(p=>{float hp=d.modules[p.type].hp*durability;return new ModuleState{placement=p.Copy(),spec=d.modules[p.type],hp=hp,maxHp=hp};}).ToList();}
 public IEnumerable<ModuleState> Live=>modules.Where(m=>m.alive);
 public ModuleState Core=>modules.FirstOrDefault(m=>m.Type==0);
 public bool Dead=>Core==null||!Core.alive;
 public float Mass=>Live.Sum(m=>m.spec.mass);
 public float Speed=>data.Speed(Mass);
 public int Capacity=>Live.Count(m=>m.Type==3)*data.balance.batteryCapacity;
 public int Consumption=>Live.Where(m=>m.powered).Sum(m=>m.spec.Cost);
 public Rect LocalBounds {get{var l=Live.ToArray();if(l.Length==0)return new Rect(0,0,1,1);float x=l.Min(m=>m.placement.x),y=l.Min(m=>m.placement.y);return Rect.MinMaxRect(x,y,l.Max(m=>m.placement.x+m.spec.width),l.Max(m=>m.placement.y+m.spec.height));}}
 public Rect Bounds {get{var r=LocalBounds;r.position+=position;return r;}}
 public Vector2 Center=>Bounds.center;
 public Rect RectOf(ModuleState m)=>new Rect(position.x+m.placement.x,position.y+m.placement.y,m.spec.width,m.spec.height);
 public Vector2 CenterOf(ModuleState m)=>RectOf(m).center;
 public IEnumerable<ModuleState> Weapons=>Live.Where(m=>m.powered&&m.spec.Weapon);
 public ModuleState Shield=>Live.FirstOrDefault(m=>m.Type==8&&m.powered);
 public bool ShieldActive(float time)=>!Dead&&Shield!=null&&time<shieldUntil;
 public bool ActivateShield(float time){if(Shield==null||Dead||time<shieldReadyAt)return false;shieldStarted=time;shieldUntil=time+data.balance.shieldDuration;shieldReadyAt=time+data.balance.shieldCooldown;shieldUses++;Changed?.Invoke(Shield,"shield");return true;}
 public void Damage(ModuleState m,float amount){if(!m.alive||amount<=0||Dead)return;m.hp=Mathf.Max(0,m.hp-amount);Changed?.Invoke(m,"hit");if(m.hp<=0){m.alive=false;destroyed[m.Type]++;revision++;Changed?.Invoke(m,"destroy");}}
 public void ResolveDamage(){
  if(Dead)return;var reachable=Construction.Reachable(Live.Select(m=>m.placement).ToList(),data);
  foreach(var m in Live.ToArray())if(!reachable.Contains(m.Id)){m.alive=false;m.detached=true;destroyed[m.Type]++;revision++;Changed?.Invoke(m,"detach");}
  var candidates=Live.Where(m=>m.powered&&m.spec.Cost>0).ToList();int cost=Consumption,capacity=Capacity;
  while(cost>capacity&&candidates.Count>0){int i=random.Next(candidates.Count);var m=candidates[i];candidates.RemoveAt(i);m.powered=false;cost-=m.spec.Cost;revision++;Changed?.Invoke(m,"power_loss");}
  if(data.balance.restorePower)foreach(var m in Live.Where(m=>!m.powered).OrderBy(m=>m.Id))if(cost+m.spec.Cost<=capacity){m.powered=true;cost+=m.spec.Cost;revision++;Changed?.Invoke(m,"ready");}
  if(Shield==null)shieldUntil=-100;
 }
 public void Fire(float time,Action<ModuleState> emit){
  if(Dead||!firing)return;
  foreach(var group in Weapons.GroupBy(m=>m.Type)){
   var guns=group.OrderBy(m=>m.Id).ToArray();int type=group.Key;float interval=guns[0].spec.interval;
   if(!groupNext.ContainsKey(type)){groupNext[type]=time;cursor[type]=0;}
   if(time+.0001f<groupNext[type])continue;
   int start=cursor[type]%guns.Length;ModuleState gun=null;int picked=start;
   for(int i=0;i<guns.Length;i++){picked=(start+i)%guns.Length;if(guns[picked].shotAllowed&&time+.0001f>=guns[picked].nextShot){gun=guns[picked];break;}}
   if(gun==null)continue;gun.lastShot=time;gun.nextShot=time+interval;cursor[type]=(picked+1)%guns.Length;groupNext[type]=time+interval/guns.Length;emit(gun);gun.shots++;
  }
 }
}
public static class Stars {
 public static bool[] Evaluate(LevelSpec level,RobotModel player,RobotModel enemy,float seconds,bool victory){
  var result=new bool[5];if(!victory)return result;result[0]=true;result[1]=seconds<=level.targetSeconds;
  for(int i=0;i<3;i++){var g=level.goals[i];int n=(int)g.value;var p=player.original;var originalWeapons=p.Where(x=>player.data.modules[x.type].Weapon).ToArray();
   switch(g.kind){
   case "core_hp":result[i+2]=!player.Dead&&player.Core.hp/player.Core.maxHp>=g.value-.0001f;break;
   case "keep_type":result[i+2]=p.Any(x=>x.type==n)&&player.Live.Count(x=>x.Type==n)==p.Count(x=>x.type==n);break;
   case "no_type":result[i+2]=!p.Any(x=>x.type==n);break;
   case "modules":result[i+2]=p.Count<=n;break;
   case "cells":result[i+2]=p.Sum(x=>player.data.modules[x.type].width*player.data.modules[x.type].height)<=n;break;
   case "batteries":result[i+2]=p.Count(x=>x.type==3)<=n;break;
   case "keep_weapons":result[i+2]=originalWeapons.Length>0&&player.Live.Count(x=>x.spec.Weapon)==originalWeapons.Length;break;
   case "keep_weapon":result[i+2]=player.Live.Any(x=>x.spec.Weapon);break;
   case "destroy_rifles":result[i+2]=enemy.destroyed[5]>=n;break;
   case "destroy_armor":result[i+2]=enemy.destroyed[2]>=n;break;
   case "destroy_grenade":result[i+2]=enemy.destroyed[6]>=n;break;
   case "destroy_weapons":result[i+2]=enemy.destroyed.Skip(4).Take(4).Sum()>=n;break;
   case "destroy_all":result[i+2]=!enemy.Live.Any(x=>x.spec.Weapon);break;
   case "reflect":result[i+2]=player.reflectedHits>=n;break;
   case "no_shield_use":result[i+2]=player.shieldUses==0;break;
   }
  }return result;
 }
 public static void Merge(bool[] saved,bool[] earned){for(int i=0;i<5;i++)saved[i]|=earned[i];}
}
}
