using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Workbench {
public sealed partial class BotController {
 struct Observation {public float time;public Vector2 position,velocity;}
 readonly Queue<Observation> observations=new Queue<Observation>();
 readonly Dictionary<int,int> gunTargets=new Dictionary<int,int>();
 float nextTargetChoice;Vector2 aimNoise;Observation perceived;
 public Vector2 ObservedVelocity=>perceived.velocity;
 public float ReactionDelay=>Mathf.Clamp(level.reaction,.15f,.25f);
 public int TargetModuleId{get;private set;}=-1;

 void Observe(){
  observations.Enqueue(new Observation{time=battle.time,position=other.position,velocity=other.velocity});
  while(observations.Count>1&&observations.ElementAt(1).time<=battle.time-ReactionDelay+.00001f)observations.Dequeue();
  perceived=observations.Peek();
 }
 Vector2 EstimatedPosition=>perceived.position+perceived.velocity*Mathf.Max(0,battle.time-perceived.time);

 // Positive root of |relative + velocity*t| = projectileSpeed*t, in the table plane.
 public static float InterceptTime(Vector2 relative,Vector2 velocity,float speed){
  float a=Vector2.Dot(velocity,velocity)-speed*speed,b=2*Vector2.Dot(relative,velocity),c=Vector2.Dot(relative,relative);
  if(c<.000001f)return 0;
  if(Mathf.Abs(a)<.000001f)return b<-.000001f?Mathf.Max(0,-c/b):0;
  float discriminant=b*b-4*a*c;if(discriminant<0)return 0;
  float root=Mathf.Sqrt(discriminant),first=(-b-root)/(2*a),second=(-b+root)/(2*a);
  if(first<0)first=float.PositiveInfinity;if(second<0)second=float.PositiveInfinity;
  float t=Mathf.Min(first,second);return float.IsPositiveInfinity(t)?0:t;
 }

 Vector2 PredictedAim(ModuleState gun,ModuleState target,out Vector2 muzzle,out float travel){
  Vector2 at=EstimatedPosition+Construction.Rect(target.placement,battle.data).center;
  Vector2 center=self.CenterOf(gun);Vector2 point=at;muzzle=center;travel=0;
  // Re-evaluate the real direction-specific muzzle, because tall turrets are not centered barrels.
  for(int i=0;i<3;i++){
   int direction=GameData.Direction(point-center);muzzle=center+battle.data.Muzzle(gun.Type,direction,gun.shots);
   travel=InterceptTime(at-muzzle,perceived.velocity,gun.spec.projectileSpeed);
   point=at+perceived.velocity*Mathf.Min(travel,gun.spec.range/gun.spec.projectileSpeed);
  }
  return point;
 }
 bool CanHit(ModuleState gun,ModuleState target,out Vector2 point){
  point=PredictedAim(gun,target,out var muzzle,out float travel);
  if(Vector2.Distance(muzzle,point)>gun.spec.range||!battle.LineClear(muzzle,point))return false;
  Vector2 predictedOrigin=EstimatedPosition+perceived.velocity*Mathf.Min(travel,gun.spec.range/gun.spec.projectileSpeed);
  var targetRect=Construction.Rect(target.placement,battle.data);targetRect.position+=predictedOrigin;
  float targetT=BattleModel.SegmentRect(muzzle,point,targetRect);
  if(float.IsPositiveInfinity(targetT))return false;
  foreach(var blocker in other.Live){if(blocker==target)continue;var rect=Construction.Rect(blocker.placement,battle.data);rect.position+=predictedOrigin;
   if(BattleModel.SegmentRect(muzzle,point,rect)<targetT-.0001f)return false;
  }
  return true;
 }
 float TargetValue(ModuleState m){
  int activeWeapons=other.Weapons.Count();float value=m.Type==0?100:m.Type==3?70:m.spec.Weapon&&m.powered?(activeWeapons==1?85:55):10;
  if(m.Type!=0){var remain=other.Live.Where(x=>x!=m).Select(x=>x.placement).ToList();var connected=Construction.Reachable(remain,battle.data);
   float lost=other.Live.Where(x=>x!=m&&!connected.Contains(x.Id)).Sum(x=>x.spec.Weapon&&x.powered?22:x.Type==3?15:5);
   value+=Mathf.Min(85,lost);
  }
  return value+(1-m.hp/m.maxHp)*8;
 }
 void AimWeapons(){
  var guns=self.Weapons.ToArray();bool choose=battle.time>=nextTargetChoice||lastAimRevision!=other.revision+self.revision;
  if(choose){nextTargetChoice=battle.time+.1f;lastAimRevision=other.revision+self.revision;aimNoise=new Vector2((float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f)*level.aimError;}
  bool canFire=false;TargetModuleId=-1;
  foreach(var gun in self.modules)if(gun.spec.Weapon){
   gun.targeted=true;gun.shotAllowed=false;if(!gun.alive||!gun.powered)continue;
   ModuleState target=null;Vector2 point=Vector2.zero;
   if(!choose&&gunTargets.TryGetValue(gun.Id,out int id)){target=other.Live.FirstOrDefault(m=>m.Id==id);if(target!=null&&!CanHit(gun,target,out point))target=null;}
   if(target==null||choose){float best=float.MinValue;foreach(var candidate in other.Live){if(!CanHit(gun,candidate,out var predicted))continue;float score=TargetValue(candidate);
     if(gunTargets.TryGetValue(gun.Id,out int previous)&&previous==candidate.Id)score+=4;
     if(score>best){best=score;target=candidate;point=predicted;}
    }}
   if(target==null){gun.aimTarget=EstimatedPosition+other.LocalBounds.center;continue;}
   gunTargets[gun.Id]=target.Id;gun.aimTarget=point+aimNoise;gun.shotAllowed=true;canFire=true;
   if(TargetModuleId<0){TargetModuleId=target.Id;self.aim=gun.aimTarget;}
  }
  // The enemy chooses short volleys; actual module cooldowns/round-robin remain unchanged.
  // This rhythm is independent of the player's shield and pauses with battle time.
  bool volley=self.team==0||level.burstOff<=0||battle.time%Mathf.Max(.1f,level.burstOn+level.burstOff)<level.burstOn;
  self.firing=canFire&&volley;
  if(TargetModuleId<0)self.aim=EstimatedPosition+other.LocalBounds.center;
  if(self.Shield==null||battle.time<self.shieldReadyAt)shieldReactAt=-1;
  else{
   if(shieldReactAt<0&&battle.projectiles.Any(p=>p.owner!=self.team&&Vector2.Distance(p.position,self.Center)<8&&Vector2.Dot(p.velocity,self.Center-p.position)>0))shieldReactAt=battle.time+ReactionDelay;
   // A perceived threat schedules a reaction. It is not cancelled merely because the first
   // projectile already passed during this delay; the shield can still block the next shot.
   if(shieldReactAt>=0&&battle.time>=shieldReactAt){self.shieldRequested=true;shieldReactAt=-1;}
  }
 }
 int lastAimRevision=-1;float shieldReactAt=-1;
}
}
