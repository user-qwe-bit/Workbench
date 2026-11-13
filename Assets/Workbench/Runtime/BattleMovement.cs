using System;
using UnityEngine;

namespace Workbench {
// Translation-only compound bodies: every remaining module has its own physical rectangle.
public sealed partial class BattleModel {
 const float ContactSlop=.00002f;
 static float Axis(Vector2 v,int axis)=>axis==0?v.x:v.y;
 static Vector2 Along(int axis,float amount)=>axis==0?new Vector2(amount,0):new Vector2(0,amount);

 public static Vector2 Drive(Vector2 current,Vector2 input,float mass,GameData d,float dt){
  input=Vector2.ClampMagnitude(input,1);float speed=d.Speed(mass);
  float acceleration=speed/d.AccelerationTime(mass),brake=speed/d.StopTime(mass);
  if(input.sqrMagnitude<.0001f){current=Vector2.MoveTowards(current,Vector2.zero,brake*dt);return current.magnitude<d.balance.stopEpsilon?Vector2.zero:current;}
  // Reversing first brakes the old velocity; any unused time then accelerates the new direction.
  if(Vector2.Dot(current,input)<-.0001f){float stop=current.magnitude/(brake*d.balance.reverseBraking);float used=Mathf.Min(dt,stop);current=Vector2.MoveTowards(current,Vector2.zero,brake*d.balance.reverseBraking*used);dt-=used;}
  return Vector2.MoveTowards(current,input*speed,acceleration*dt);
 }

 void MoveRobots(float dt){
  var a=robots[0];var b=robots[1];Vector2 oldA=a.position,oldB=b.position;
  foreach(var r in robots){r.move=Vector2.ClampMagnitude(r.move,1);r.driveVelocity=Drive(r.driveVelocity,r.move,r.Mass,data,dt);Move(r,r.driveVelocity*dt,true);}
  Contacts(dt,oldA,oldB);
  foreach(var r in robots){ConstrainVelocity(r);Vector2 delta=r.position-(r.team==0?oldA:oldB);r.velocity=delta/dt;r.wheelDistance+=delta.magnitude;
   bool nearWall=false;foreach(var m in r.Live){var rect=r.RectOf(m);rect.xMin-=.12f;rect.yMin-=.12f;rect.xMax+=.12f;rect.yMax+=.12f;if(!RectClear(rect)){nearWall=true;break;}}
   if(!nearWall){wallSeparated[r.team]+=dt;if(wallSeparated[r.team]>=.12f)wallContact[r.team]=false;}else wallSeparated[r.team]=0;
  }
 }

 // Sweep each rectangle against walls on one axis. Moving tangent to the wall is untouched.
 float WallTravel(RobotModel r,int axis,float distance,out ModuleState hit){
  hit=null;float allowed=distance;if(Mathf.Abs(distance)<.0000001f)return distance;
  foreach(var m in r.Live){var rect=r.RectOf(m);float lo=axis==0?rect.xMin:rect.yMin,hi=axis==0?rect.xMax:rect.yMax;
   float boundary=distance>0?(axis==0?60:32)-hi:-lo;
   if(distance>0?boundary<allowed:boundary>allowed){allowed=boundary;hit=m;}
   int minX=Mathf.Clamp(Mathf.FloorToInt(rect.xMin+(axis==0?Mathf.Min(0,distance):0)),0,59),maxX=Mathf.Clamp(Mathf.FloorToInt(rect.xMax+(axis==0?Mathf.Max(0,distance):0)),0,59);
   int minY=Mathf.Clamp(Mathf.FloorToInt(rect.yMin+(axis==1?Mathf.Min(0,distance):0)),0,31),maxY=Mathf.Clamp(Mathf.FloorToInt(rect.yMax+(axis==1?Mathf.Max(0,distance):0)),0,31);
   for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)if(walls[x,y]){
    float crossLo=axis==0?rect.yMin:rect.xMin,crossHi=axis==0?rect.yMax:rect.xMax,crossWall=axis==0?y:x;
    if(crossHi<=crossWall+ContactSlop||crossLo>=crossWall+1-ContactSlop)continue;
    float wall=axis==0?x:y;
    if(distance>0&&hi<=wall+ContactSlop){float travel=Mathf.Max(0,wall-hi);if(travel<allowed){allowed=travel;hit=m;}}
    else if(distance<0&&lo>=wall+1-ContactSlop){float travel=Mathf.Min(0,wall+1-lo);if(travel>allowed){allowed=travel;hit=m;}}
   }
  }
  return distance>0?Mathf.Clamp(allowed,0,distance):Mathf.Clamp(allowed,distance,0);
 }

 void Move(RobotModel r,Vector2 delta,bool effects=false){
  for(int axis=0;axis<2;axis++){
   float desired=Axis(delta,axis),travel=WallTravel(r,axis,desired,out var hit);r.position+=Along(axis,travel);
   if(hit==null||Mathf.Abs(desired-travel)<.000001f)continue;
   float speed=Mathf.Max(0,Axis(r.driveVelocity,axis)*Mathf.Sign(desired));
   if(effects){int team=r.team;if(!wallContact[team]&&speed>=data.balance.impactMinSpeed){r.Damage(hit,data.balance.impactDamage*data.balance.wallDamageFactor);wallImpactCount++;Effect?.Invoke("collision",r.CenterOf(hit)+Along(axis,Mathf.Sign(desired)*.5f),team);}wallContact[team]=true;wallSeparated[team]=0;}
   if(Axis(r.driveVelocity,axis)*desired>0)r.driveVelocity-=Along(axis,Axis(r.driveVelocity,axis));
  }
 }

 void ConstrainVelocity(RobotModel r){for(int axis=0;axis<2;axis++){float v=Axis(r.driveVelocity,axis);if(Mathf.Abs(v)<.00001f)continue;float sample=Mathf.Sign(v)*.002f;if(Mathf.Abs(WallTravel(r,axis,sample,out _))<.00001f)r.driveVelocity-=Along(axis,v);}}

 bool Contact(out ModuleState hitA,out ModuleState hitB,out Vector2 correction){
  hitA=hitB=null;correction=Vector2.zero;float closest=float.MaxValue;var a=robots[0];var b=robots[1];
  foreach(var ma in a.Live)foreach(var mb in b.Live){var ra=a.RectOf(ma);var rb=b.RectOf(mb);
   float dx=Mathf.Min(ra.xMax-rb.xMin,rb.xMax-ra.xMin),dy=Mathf.Min(ra.yMax-rb.yMin,rb.yMax-ra.yMin);
   if(dx<=ContactSlop||dy<=ContactSlop)continue;float overlap=Mathf.Min(dx,dy);if(overlap>=closest)continue;
   closest=overlap;hitA=ma;hitB=mb;correction=dx<dy?new Vector2(ra.center.x<rb.center.x?-dx:dx,0):new Vector2(0,ra.center.y<rb.center.y?-dy:dy);
  }
  return hitA!=null;
 }

 void Contacts(float dt,Vector2 oldA,Vector2 oldB){
  var a=robots[0];var b=robots[1];
  if(Contact(out var hitA,out var hitB,out var separation)){
   Vector2 normal=separation.normalized;float approach=-Vector2.Dot(a.driveVelocity-b.driveVelocity,normal);
   if(!contacting&&approach>=data.balance.impactMinSpeed){a.Damage(hitA,data.balance.impactDamage);b.Damage(hitB,data.balance.impactDamage);impactCount++;Effect?.Invoke("collision",(a.CenterOf(hitA)+b.CenterOf(hitB))*.5f,0);}
   contacting=true;separatedTime=0;
   for(int iteration=0;iteration<24;iteration++){
    if(!Contact(out _,out _,out separation))break;normal=separation.normalized;
    float invA=1/Mathf.Max(1,a.Mass),invB=1/Mathf.Max(1,b.Mass),sum=invA+invB;
    float closing=Vector2.Dot(a.driveVelocity-b.driveVelocity,normal);
    if(closing<0){float impulse=-closing/sum;a.driveVelocity+=normal*(impulse*invA);b.driveVelocity-=normal*(impulse*invB);}
    // Share positional correction by inverse mass, then give blocked-wall residual to the free body.
    Vector2 beforeA=a.position,beforeB=b.position;Move(a,separation*(invA/sum));Move(b,-separation*(invB/sum));
    float remaining=separation.magnitude-Vector2.Dot((a.position-beforeA)-(b.position-beforeB),normal);
    if(remaining>ContactSlop){beforeA=a.position;Move(a,normal*remaining);remaining-=Vector2.Dot(a.position-beforeA,normal);if(remaining>ContactSlop)Move(b,-normal*remaining);}
   }
   // Concave interlocks can offer opposing normals. Preserve the last nonpenetrating configuration.
   if(Contact(out _,out _,out _)){a.position=oldA;b.position=oldB;a.driveVelocity-=normal*Vector2.Dot(a.driveVelocity,normal);b.driveVelocity-=normal*Vector2.Dot(b.driveVelocity,normal);}
  }else{
   float gap=float.MaxValue;foreach(var ma in a.Live)foreach(var mb in b.Live){var ra=a.RectOf(ma);var rb=b.RectOf(mb);float x=Mathf.Max(0,Mathf.Max(ra.xMin-rb.xMax,rb.xMin-ra.xMax)),y=Mathf.Max(0,Mathf.Max(ra.yMin-rb.yMax,rb.yMin-ra.yMax));gap=Mathf.Min(gap,Mathf.Sqrt(x*x+y*y));}
   if(gap>.12f){separatedTime+=dt;if(separatedTime>=.12f)contacting=false;}else separatedTime=0;
  }
 }
}
}
