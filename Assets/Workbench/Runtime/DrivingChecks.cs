using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Workbench {
public static class DrivingChecks {
 static Placement P(int id,int type,int x,int y)=>new Placement{id=id,type=type,x=x,y=y};
 static void Assert(bool value,string message){if(!value)throw new Exception(message);}
 static void Near(float a,float b,float tolerance,string message)=>Assert(Mathf.Abs(a-b)<=tolerance,message+": "+a+" / "+b);
 static BattleModel Empty(GameData d,Placement[] player=null,Placement[] enemy=null){var b=new BattleModel(d,new LevelSpec{enemy=enemy??new[]{P(0,0,0,0)}},player??new[]{P(0,0,0,0)},0);Array.Clear(b.walls,0,b.walls.Length);b.robots[0].position=new Vector2(20,14);b.robots[1].position=new Vector2(45,24);return b;}
 static bool Overlap(BattleModel b)=>b.robots[0].Live.Any(a=>b.robots[1].Live.Any(c=>{var x=b.robots[0].RectOf(a);var y=b.robots[1].RectOf(c);return Mathf.Min(x.xMax-y.xMin,y.xMax-x.xMin)>.0001f&&Mathf.Min(x.yMax-y.yMin,y.yMax-x.yMin)>.0001f;}));
 static void RunFor(BattleModel b,float seconds,int fps=120){for(int i=0;i<Mathf.RoundToInt(seconds*fps);i++)b.Step(1f/fps);}

 public static List<string> Run(GameData d){var passed=new List<string>();Action<string,Action> check=(name,fn)=>{fn();passed.Add(name);};
  check("All ten durability steps scale every enemy part only; no compounded core multiplier",()=>{
   for(int i=0;i<10;i++){var b=new BattleModel(d,d.levels[i],d.levels[i].enemy,0);float factor=1+1.5f*i/9f;foreach(var m in b.robots[1].modules){Near(m.hp,m.spec.hp*factor,.001f,"Enemy durability level "+i);Near(m.maxHp,m.hp,.001f,"Full HUD denominator");}foreach(var m in b.robots[0].modules)Near(m.hp,m.spec.hp,.001f,"Player durability");}
  });
  check("Ten time goals halved; all four projectile speeds multiplied by 1.4",()=>{
   float[] times={45,50,50,55,65,65,70,55,75,90},speeds={33.6f,47.6f,23.8f,33.6f};for(int i=0;i<10;i++)Near(d.levels[i].targetSeconds,times[i],.001f,"Time goal");for(int i=0;i<4;i++)Near(d.modules[i+4].projectileSpeed,speeds[i],.001f,"Bullet speed");
  });
  check("Player weapon damage uses 1/1.1; enemy damage and raw catalogue stay unchanged",()=>{
   var b=Empty(d,new[]{P(0,0,0,0),P(1,4,1,0)},new[]{P(0,0,0,0),P(1,4,1,0)});foreach(var r in b.robots){r.aim=r.Center+Vector2.right*10;var gun=r.modules[1];gun.aim=Vector2.right;b.Shoot(r,gun);var bullet=b.projectiles.Last();Near(bullet.damage,gun.spec.damage*(r.team==0?1/1.1f:1),.0001f,"Damage multiplier");}Near(d.modules[4].damage,12,.001f,"Raw catalogue damage");
  });
  check("Mass drag strengthened by 1.25 and speed clamped in cells per second",()=>{Near(d.Speed(20),10/1.5f,.0001f,"Light speed");Near(d.Speed(40),5,.0001f,"Medium speed");Near(d.Speed(80),3.5f,.0001f,"Heavy floor");Near(d.Speed(0),9,.0001f,"Cap");});
  check("Light and heavy acceleration, braking, coast distance and reversal remain responsive",()=>{
   foreach(float mass in new[]{20f,80f}){Vector2 v=Vector2.zero;float h=1f/120,t=0;while(v.x<d.Speed(mass)-.0001f&&t<1){v=BattleModel.Drive(v,Vector2.right,mass,d,h);t+=h;}Near(t,d.AccelerationTime(mass),.009f,"Acceleration duration");float stop=0,distance=0;while(v.sqrMagnitude>0&&stop<1){v=BattleModel.Drive(v,Vector2.zero,mass,d,h);distance+=v.x*h;stop+=h;}Near(stop,d.StopTime(mass),.009f,"Stop duration");Assert(distance>.3f&&distance<.65f,"Coast path "+distance);v=Vector2.right*d.Speed(mass);float reverse=0;while(v.x>-d.Speed(mass)+.0001f&&reverse<1){v=BattleModel.Drive(v,Vector2.left,mass,d,h);reverse+=h;}Assert(reverse>=.28f&&reverse<=.46f,"Reversal "+reverse);}
  });
  check("Diagonal has no speed bonus and a ninety-degree turn preserves a short arc",()=>{
   var v=Vector2.zero;for(int i=0;i<120;i++)v=BattleModel.Drive(v,new Vector2(1,1),20,d,1f/120);Near(v.magnitude,d.Speed(20),.001f,"Diagonal speed");v=BattleModel.Drive(Vector2.right*d.Speed(20),Vector2.up,20,d,1f/60);Assert(v.x>0&&v.y>0,"No turning arc");
  });
  check("30, 60 and 144 Hz inputs produce matching travel and stop without creep",()=>{
   Vector2 baseline=Vector2.zero;foreach(int fps in new[]{30,60,144}){var b=Empty(d);var r=b.robots[0];r.move=Vector2.right;RunFor(b,1,fps);r.move=Vector2.up;RunFor(b,.5f,fps);r.move=Vector2.zero;RunFor(b,.5f,fps);if(fps==30)baseline=r.position;else Assert(Vector2.Distance(r.position,baseline)<.035f,"Frame rate drift: "+r.position+" / "+baseline);Assert(r.driveVelocity==Vector2.zero&&r.velocity.sqrMagnitude<.00001f,"Residual creep");}
  });
  check("Losing mass raises the limit without jumping current velocity; disabled parts still weigh",()=>{
   var b=Empty(d,new[]{P(0,0,0,0),P(1,2,1,0),P(2,2,2,0),P(3,2,3,0),P(4,2,4,0)});var r=b.robots[0];r.move=Vector2.right;RunFor(b,.5f);float old=r.driveVelocity.x,mass=r.Mass;r.Damage(r.modules[4],999);r.ResolveDamage();Assert(r.Mass<mass&&r.Speed>old,"No lighter limit");Near(r.driveVelocity.x,old,.001f,"Velocity jumped on damage");b.Step(1f/120);Assert(r.driveVelocity.x>old&&r.driveVelocity.x<r.Speed,"Acceleration skipped");float retained=r.Mass;r.ResolveDamage();Near(r.Mass,retained,.001f,"Power loss discarded mass");
  });
  check("Concave footprint preserves empty cells and projecting modules catch walls",()=>{
   var b=Empty(d,new[]{P(0,0,0,0),P(1,1,1,0),P(2,1,0,1)});b.walls[21,15]=true;Assert(b.ClearAt(b.robots[0],new Vector2(20,14)),"Empty corner blocked by bounding box");Assert(!b.ClearAt(b.robots[0],new Vector2(20.1f,14.1f)),"Projecting module passed wall");
  });
  check("Wall slide preserves tangent velocity, removes inward velocity and never stores launch force",()=>{
   var b=Empty(d);var r=b.robots[0];r.position=new Vector2(.5f,10);r.move=new Vector2(-1,1);RunFor(b,1);Assert(r.position.x>=-.0001f&&r.position.x<.001f&&r.velocity.y>4,"Slide failed");Near(r.driveVelocity.x,0,.001f,"Stored inward force");float y=r.position.y;r.move=Vector2.right;b.Step(1f/60);Assert(r.position.x>0&&r.driveVelocity.x<1&&r.position.y>=y,"Could not leave wall cleanly");
  });
  check("Low-speed brush has no ram damage; real collision damages both touched parts equally",()=>{
   var b=Empty(d);var a=b.robots[0];var e=b.robots[1];e.position=a.position+new Vector2(1.001f,0);a.move=new Vector2(.01f,0);RunFor(b,.3f);Assert(b.impactCount==0&&a.Core.hp==a.Core.maxHp&&e.Core.hp==e.Core.maxHp,"Brush caused damage");
   b=Empty(d);a=b.robots[0];e=b.robots[1];e.position=a.position+new Vector2(2,0);a.move=Vector2.right;e.move=Vector2.left;RunFor(b,.6f);Assert(b.impactCount==1,"Impact absent/repeated");Near(a.Core.maxHp-a.Core.hp,15,.001f,"Player impact");Near(e.Core.maxHp-e.Core.hp,15,.001f,"Enemy impact");Assert(!Overlap(b),"Robot overlap");
  });
  check("Equal mass is symmetric; heavy body pushes light and lateral escape remains possible",()=>{
   var b=Empty(d);var a=b.robots[0];var e=b.robots[1];a.position=new Vector2(24,14);e.position=new Vector2(25,14);a.move=Vector2.right;e.move=Vector2.left;RunFor(b,1);Near((a.Center.x+e.Center.x)/2,25,.002f,"Equal mass bias");
   b=Empty(d,new[]{P(0,0,0,0),P(1,2,0,1),P(2,2,1,1),P(3,2,2,1)});a=b.robots[0];e=b.robots[1];a.position=new Vector2(24,14);e.position=new Vector2(25,14);a.move=Vector2.right;e.move=Vector2.left;RunFor(b,1);Assert(e.position.x>25.2f&&!Overlap(b),"Heavy does not push: "+e.position);float y=e.position.y;e.move=Vector2.down;RunFor(b,.5f);Assert(e.position.y<y-.2f&&!Overlap(b),"Glued contact");
  });
  check("A pinned robot does not penetrate, jitter or accumulate velocity against a wall",()=>{
   var b=Empty(d);var a=b.robots[0];var e=b.robots[1];a.position=new Vector2(58,14);e.position=new Vector2(59,14);a.move=Vector2.right;RunFor(b,2);Assert(!Overlap(b)&&e.Bounds.xMax<=60.0001f&&a.position.x<=58.0001f,"Pinned penetration");Assert(a.velocity.magnitude<.03f&&e.velocity.magnitude<.03f,"Pinned jitter");e.move=Vector2.down;RunFor(b,.3f);Assert(e.position.y<13.8f&&!Overlap(b),"Pinned escape failed");
  });
  check("Ballistic intercept meets a moving target rather than its current location",()=>{
   Vector2 relative=new Vector2(20,0),velocity=new Vector2(0,6);float t=BotController.InterceptTime(relative,velocity,33.6f);Assert(t>.5f&&t<1,"Invalid intercept");Near((relative+velocity*t).magnitude,33.6f*t,.001f,"Ballistic meeting");
  });
  check("Bot observes delayed actual velocity, including after abrupt direction changes",()=>{
   var b=Empty(d,new[]{P(0,0,0,0)},new[]{P(0,0,0,0),P(1,4,1,0)});b.robots[0].velocity=Vector2.up*5;var ai=new BotController(b,new LevelSpec{reaction=.2f});ai.Tick();b.robots[0].velocity=Vector2.down*5;b.robots[0].move=Vector2.down;for(int i=1;i<=18;i++){b.time=i/120f;ai.Tick();}Assert(ai.ObservedVelocity.y<0==false,"Read current turn early");for(int i=19;i<=30;i++){b.time=i/120f;ai.Tick();}Assert(ai.ObservedVelocity.y<0,"Delayed direction never arrived");
  });
  check("Bot targets exposed core, then battery or dangerous weapon instead of a shielded core",()=>{
   var b=Empty(d,new[]{P(0,0,0,0),P(1,3,0,1)},new[]{P(0,0,0,0),P(1,4,1,0)});b.robots[0].position=new Vector2(30,14);b.robots[1].position=new Vector2(20,14);var ai=new BotController(b,new LevelSpec{reaction=.2f});ai.Tick();Assert(ai.TargetModuleId==0,"Exposed core ignored");
   b=Empty(d,new[]{P(0,0,2,0),P(1,2,1,0),P(2,3,1,1),P(3,4,2,1)},new[]{P(0,0,0,0),P(1,4,1,0)});b.robots[0].position=new Vector2(30,14);b.robots[1].position=new Vector2(20,14);ai=new BotController(b,new LevelSpec{reaction=.2f});ai.Tick();Assert(ai.TargetModuleId==2,"Occluded core/weapon selected instead of battery: "+ai.TargetModuleId);
  });
  check("Bot line of fire respects barriers and changes range after losing rifle",()=>{
   var b=Empty(d,new[]{P(0,0,0,0)},new[]{P(0,0,1,0),P(1,3,1,1),P(2,5,0,0),P(3,7,2,0)});b.robots[0].position=new Vector2(30,14);b.robots[1].position=new Vector2(20,14);for(int y=0;y<32;y++)b.walls[27,y]=true;
   // WallRay uses the arena's immutable obstacle list; test against an explicit barrier arena.
   var old=b.arena.walls;b.arena.walls=Enumerable.Range(0,32).Select(y=>new WallData{x=27,y=y}).ToArray();try{var ai=new BotController(b,new LevelSpec{reaction=.2f});ai.Tick();Assert(!b.robots[1].firing,"Shoots through barrier");Assert(ai.DesiredRange>=13,"Rifle distance");b.robots[1].modules[2].powered=false;b.robots[1].revision++;b.time=.3f;ai.Tick();Near(ai.DesiredRange,3.2f,.001f,"Lost rifle did not close");}finally{b.arena.walls=old;}
  });
  check("Enemy volleys pause independently of shield and do not change module cadence",()=>{
   var b=new BattleModel(d,d.levels[0],d.levels[5].enemy,0);b.robots[0].position=new Vector2(25,12);b.robots[1].position=new Vector2(35,12);b.robots[0].ActivateShield(0);var ai=new BotController(b,d.levels[0]);b.time=2.1f;ai.Tick();Assert(!b.robots[1].firing,"Missing volley pause");b.time=2.7f;ai.Tick();Assert(b.robots[0].ShieldActive(b.time)&&b.robots[1].firing,"Volley refuses active shield");b.paused=true;b.Step(10);ai.Tick();Near(b.time,2.7f,.0001f,"Pause advanced volley");Near(d.modules[4].interval,.5f,.0001f,"Machinegun cadence changed");Near(d.modules[5].interval,3,.0001f,"Rifle cadence changed");
  });
  check("Per-level enemy projectile balance preserves raw module damage and symmetric ram damage",()=>{
   var b=new BattleModel(d,d.levels[6],new[]{P(0,0,0,0)},0);var r=b.robots[1];var gun=r.Weapons.First();r.aim=r.Center+Vector2.right*10;gun.aim=Vector2.right;b.Shoot(r,gun);Near(b.projectiles.Last().damage,gun.spec.damage*.55f,.001f,"Level seven projectile tuning");Near(d.modules[4].damage,12,.001f,"Shared catalogue changed");Near(d.balance.impactDamage,15,.001f,"Ram damage changed");
  });
  check("Bot shield reacts after perception delay rather than instantly intercepting every return shot",()=>{
   var b=new BattleModel(d,d.levels[5],new[]{P(0,0,0,0)},0);var r=b.robots[1];b.projectiles.Add(new ProjectileState{id=900,owner=0,position=r.Center+Vector2.left*7,velocity=Vector2.right*34,remaining=1});var ai=new BotController(b,d.levels[5]);ai.Tick();Assert(!r.shieldRequested,"Instant enemy shield");b.time=.1f;ai.Tick();Assert(!r.shieldRequested,"Shield reaction too early");b.time=ai.ReactionDelay+.01f;ai.Tick();Assert(r.shieldRequested,"Missing delayed shield response");
  });
  return passed;
 }
}
}
