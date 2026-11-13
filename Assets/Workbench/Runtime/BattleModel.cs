using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Workbench {
public sealed class ProjectileState { public int id,owner,type;public Vector2 position,velocity;public float damage,remaining,distance,range,blast;public bool alive=true,reflected; public readonly Queue<Vector2> trail=new Queue<Vector2>(); }
public sealed partial class BattleModel {
 public readonly GameData data;public readonly ArenaSpec arena;public readonly RobotModel[] robots; public readonly List<ProjectileState> projectiles=new List<ProjectileState>();public readonly LevelSpec level;
 public float time;public bool paused,finished;public int winner=-1; public int impactCount; public event Action<string,Vector2,int> Effect;
 public readonly bool[,] walls=new bool[60,32]; int nextProjectile;bool contacting;float separatedTime;readonly System.Random random;
 readonly bool[] wallContact=new bool[2];readonly float[] wallSeparated=new float[2];public int wallImpactCount;
 public BattleModel(GameData data,LevelSpec level,IEnumerable<Placement> player,int scheme,int seed=1){
  this.data=data;this.level=level;arena=data.arenas[scheme];random=new System.Random(seed);robots=new[]{new RobotModel(data,player,0,seed),new RobotModel(data,level.enemy,1,seed+1,data.EnemyDurability(level.id))};
  for(int i=0;i<2;i++){robots[i].position=arena.spawns[i].V;robots[i].aim=arena.spawns[1-i].V;var r=robots[i];r.Changed+=(m,kind)=>{string fx=kind;if(kind=="destroy"||kind=="detach")fx=m.Type==0?"break_core":m.Type==3?"break_battery":m.spec.width==2?"break_module_large":m.spec.height==2?"break_module_tall":"break_module";if(kind=="hit")fx="hit_module";Effect?.Invoke(fx,r.CenterOf(m),r.team);};}
  foreach(var w in arena.walls)walls[w.x,w.y]=true;foreach(var r in robots)r.previousPosition=r.position;
 }
 public bool RectClear(Rect r){if(r.xMin<0||r.yMin<0||r.xMax>60||r.yMax>32)return false;for(int y=Mathf.FloorToInt(r.yMin+.001f);y<=Mathf.FloorToInt(r.yMax-.001f);y++)for(int x=Mathf.FloorToInt(r.xMin+.001f);x<=Mathf.FloorToInt(r.xMax-.001f);x++)if(walls[x,y])return false;return true;}
 public bool ClearAt(RobotModel robot,Vector2 position,bool conservative=false){if(conservative){var r=robot.LocalBounds;r.position+=position;return RectClear(r);}foreach(var m in robot.Live){var r=Construction.Rect(m.placement,data);r.position+=position;if(!RectClear(r))return false;}return true;}
 public static float SegmentRect(Vector2 a,Vector2 b,Rect r){float t0=0,t1=1;Vector2 d=b-a;for(int axis=0;axis<2;axis++){float s=axis==0?a.x:a.y,v=axis==0?d.x:d.y,lo=axis==0?r.xMin:r.yMin,hi=axis==0?r.xMax:r.yMax;if(Mathf.Abs(v)<.000001f){if(s<lo||s>hi)return float.PositiveInfinity;}else{float x=(lo-s)/v,y=(hi-s)/v;if(x>y){float z=x;x=y;y=z;}t0=Mathf.Max(t0,x);t1=Mathf.Min(t1,y);if(t0>t1)return float.PositiveInfinity;}}return t0;}
 public float WallRay(Vector2 a,Vector2 b){float first=float.PositiveInfinity;foreach(var w in arena.walls)first=Mathf.Min(first,SegmentRect(a,b,new Rect(w.x,w.y,1,1)));return first;}
 public bool LineClear(Vector2 a,Vector2 b)=>float.IsPositiveInfinity(WallRay(a,b));
 public void Step(float dt){
  if(paused||finished||dt<=0)return;
  // Small deterministic physics slices keep compound contacts stable even at a low render rate.
  int slices=Mathf.Max(1,Mathf.CeilToInt(dt*120-.0001f));float h=dt/slices,start=time;
  foreach(var r in robots)r.previousPosition=r.position;
  for(int i=0;i<slices&&!finished;i++){
   time=start+(i+1)*h;MoveRobots(h);
   foreach(var r in robots){foreach(var m in r.Live)m.aim=(m.targeted?m.aimTarget:r.aim)-r.CenterOf(m);if(r.shieldRequested)r.ActivateShield(time);r.shieldRequested=false;r.Fire(time,m=>Shoot(r,m));}
   foreach(var p in projectiles.ToArray())if(p.alive)Advance(p,h);
   foreach(var r in robots)r.ResolveDamage();
   if(robots.Any(r=>r.Dead)){finished=true;winner=robots[0].Dead?(robots[1].Dead?-1:1):0;}
   projectiles.RemoveAll(p=>!p.alive);
  }
 }
 public void Shoot(RobotModel r,ModuleState m){int direction=GameData.Direction(m.aim);Vector2 origin=r.CenterOf(m)+data.Muzzle(m.Type,direction,m.shots);var aim=((m.targeted?m.aimTarget:r.aim)-origin).normalized;if(aim.sqrMagnitude<.01f)aim=Vector2.right;
  for(int i=0;i<m.spec.pellets;i++){float spread=m.spec.pellets>1?(-m.spec.spread*.5f+m.spec.spread*i/(m.spec.pellets-1)):0;float radians=spread*Mathf.Deg2Rad;Vector2 v=new Vector2(aim.x*Mathf.Cos(radians)-aim.y*Mathf.Sin(radians),aim.x*Mathf.Sin(radians)+aim.y*Mathf.Cos(radians));
   projectiles.Add(new ProjectileState{id=nextProjectile++,owner=r.team,type=m.Type,position=origin,velocity=v*m.spec.projectileSpeed,damage=m.spec.damage*(r.team==0?data.balance.playerDamageScale:level.enemyDamageScale),range=m.spec.range,remaining=m.spec.range/m.spec.projectileSpeed,blast=m.spec.blastRadius});}
  Effect?.Invoke("muzzle_"+m.spec.key,origin,r.team);
 }
 public bool Reflect(ProjectileState p,RobotModel r,Vector2 from,Vector2 to){if(p.owner==r.team||!r.ShieldActive(time))return false;var center=r.CenterOf(r.Shield);Vector2 d=to-from,f=from-center;float radius=data.balance.shieldRadius,A=Vector2.Dot(d,d),B=2*Vector2.Dot(f,d),C=Vector2.Dot(f,f)-radius*radius,disc=B*B-4*A*C;if(A<.000001f||disc<0)return false;float t=(-B-Mathf.Sqrt(disc))/(2*A);if(t<0||t>1)return false;Vector2 hit=from+d*t,normal=(hit-center).normalized,face=(r.aim-center).normalized;if(Vector2.Dot(normal,face)<Mathf.Cos(data.balance.shieldArc*.5f*Mathf.Deg2Rad))return false;
  p.owner=r.team;p.reflected=true;p.velocity=-p.velocity;p.position=hit+p.velocity.normalized*.05f;p.trail.Clear();Effect?.Invoke("shield_reflect",hit,r.team);return true;
 }
 void Advance(ProjectileState p,float dt){Vector2 from=p.position,to=from+p.velocity*dt;p.remaining-=dt;p.distance+=p.velocity.magnitude*dt;
  var target=robots[1-p.owner];if(Reflect(p,target,from,to))return;
  float first=WallRay(from,to);ModuleState hit=null;
  foreach(var m in target.Live){float t=SegmentRect(from,to,target.RectOf(m));if(t<first){first=t;hit=m;}}
  if(!float.IsPositiveInfinity(first)){p.position=Vector2.Lerp(from,to,first);p.alive=false;if(p.blast>0)Explode(p);else if(hit!=null){float damage=p.type==7?p.damage*Mathf.Lerp(1,.03f,Mathf.Clamp01((p.distance-2)/5)):p.damage;target.Damage(hit,damage);if(p.reflected)robots[p.owner].reflectedHits++;}else Effect?.Invoke("hit_wall",p.position,p.owner);}
  else{p.position=to;if(p.remaining<=0||to.x<0||to.y<0||to.x>60||to.y>32){p.alive=false;if(p.blast>0)Explode(p);}}
  p.trail.Enqueue(p.position);while(p.trail.Count>7)p.trail.Dequeue();
 }
 public void Explode(ProjectileState p){var target=robots[1-p.owner];bool affected=false;foreach(var m in target.Live.ToArray()){var r=target.RectOf(m);Vector2 nearest=new Vector2(Mathf.Clamp(p.position.x,r.xMin,r.xMax),Mathf.Clamp(p.position.y,r.yMin,r.yMax));if(Vector2.Distance(nearest,p.position)<=p.blast&&LineClear(p.position,nearest)){target.Damage(m,p.damage);affected=true;}}if(affected&&p.reflected)robots[p.owner].reflectedHits++;Effect?.Invoke("explode_grenade",p.position,p.owner);}
}
}
