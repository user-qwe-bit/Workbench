using UnityEngine;
using System.Linq;
namespace Workbench {
public sealed class ModuleVisual:MonoBehaviour {
 public SpriteRenderer body,turret,indicator,damage,wheelLeft,wheelRight,shieldField;public BoxCollider2D footprint;
 ArtCatalog art;RobotModel robot;ModuleState model;int[] recoilFrames;float lastHp,hitAt=-10;bool horizontal;int wheelSign=1;
 public void Initialize(ArtCatalog a,RobotModel r,ModuleState m){art=a;robot=r;model=m;lastHp=m.hp;recoilFrames=r.data.turrets.FirstOrDefault(t=>t.type==m.Type)?.recoil;footprint.size=new Vector2(m.spec.width,m.spec.height);footprint.enabled=true;}
 public void Refresh(float time){if(model==null)return;gameObject.SetActive(model.alive);if(!model.alive)return;Vector2 center=robot.CenterOf(model);transform.position=WorldView.World(WorldView.RenderPosition(robot)+Construction.Rect(model.placement,robot.data).center);string team=robot.team==0?(model.powered?"Player":"Disabled"):(model.powered?"Enemy":"EnemyDisabled");body.sprite=art.Get("Modules/"+team+"/"+model.spec.key);
  if(model.hp<lastHp)hitAt=time;lastHp=model.hp;body.color=time-hitAt<.08f?new Color(1,.55f,.4f):Color.white;
  damage.sprite=model.hp<model.maxHp*.7f?art.Get("Modules/Damage/"+(model.hp<model.maxHp*.35f?"heavy_0":"light_0")):null;
  var velocity=robot.velocity;if(Mathf.Abs(velocity.x)>Mathf.Abs(velocity.y)*1.3f&&Mathf.Abs(velocity.x)>.2f)horizontal=true;else if(Mathf.Abs(velocity.y)>Mathf.Abs(velocity.x)*1.3f&&Mathf.Abs(velocity.y)>.2f)horizontal=false;
  if(velocity.sqrMagnitude>.0001f)wheelSign=(horizontal?velocity.x:velocity.y)<0?-1:1;int frame=(int)(robot.wheelDistance*3)%4;if(wheelSign<0)frame=3-frame;
  wheelLeft.sprite=wheelRight.sprite=art.Get("Wheels/wheel_"+(horizontal?"horizontal":"vertical")+"/frame_"+frame.ToString("00"));
  Vector2 axis=horizontal?Vector2.up:Vector2.right;float distance=(horizontal?model.spec.height:model.spec.width)*.5f;wheelLeft.transform.localPosition=WorldView.World(-axis*distance);wheelRight.transform.localPosition=WorldView.World(axis*distance);
  wheelLeft.enabled=wheelRight.enabled=true;foreach(var other in robot.Live){if(other==model)continue;var r=robot.RectOf(other);if(r.Contains(center-axis*(distance+.1f)))wheelLeft.enabled=false;if(r.Contains(center+axis*(distance+.1f)))wheelRight.enabled=false;}
  if(model.spec.Weapon){int d=GameData.Direction(model.aim);turret.sprite=art.Get("Turrets/"+(robot.team==0?"Player":"Enemy")+"/"+model.spec.key+"/dir_"+d.ToString("00"));turret.color=model.powered?Color.white:new Color(.5f,.5f,.5f);int frameIndex=Mathf.FloorToInt((time-model.lastShot)*24);float recoil=recoilFrames!=null&&frameIndex>=0&&frameIndex<recoilFrames.Length?recoilFrames[frameIndex]:0;float angle=d*11.25f*Mathf.Deg2Rad;turret.transform.localPosition=new Vector3(Mathf.Round(-Mathf.Cos(angle)*recoil)/64,Mathf.Round(-Mathf.Sin(angle)*recoil)/64);}
  else{turret.sprite=model.Type==8?art.Get(model.powered?"Shield/shield_emitter/frame_"+((int)(time*6)%4).ToString("00"):"Shield/emitter"):null;}
  if(!model.powered){indicator.sprite=art.Get("Modules/no_power_blink/frame_"+((int)(time*2)%2).ToString("00"));}
  else if(model.Type==0||model.Type==3){string name=model.Type==0?"core_indicator":"battery_indicator";int count=model.Type==0?4:3;indicator.sprite=art.Get("Modules/"+name+"/frame_"+((int)(time*(model.Type==0?5:3))%count).ToString("00"));}
  else if(model.Type==8){int step=Mathf.FloorToInt(Mathf.Clamp01(1-(robot.shieldReadyAt-time)/robot.data.balance.shieldCooldown)*32);indicator.sprite=art.Get("Shield/Cooldown/step_"+step.ToString("00"));}else indicator.sprite=null;
  if(model.Type==8&&robot.ShieldActive(time)){float elapsed=time-robot.shieldStarted;string phase=elapsed<.15f?"activate":elapsed>2.85f?"deactivate":"active";int f=phase=="active"?(int)(elapsed*8)%4:Mathf.Min(2,(int)((phase=="activate"?elapsed:elapsed-2.85f)*20));int d=GameData.Direction(robot.aim-center);shieldField.sprite=art.Get("Shield/"+phase+"_dir_"+d.ToString("00")+"/frame_"+f.ToString("00"));}else shieldField.sprite=null;
 }
}
}
