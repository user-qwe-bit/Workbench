from pathlib import Path
r=Path(__file__).resolve().parents[1]
p=r/'Assets/Workbench/Runtime/BotController.cs';s=p.read_text(encoding='utf-8').replace('public sealed class BotController {','public sealed partial class BotController {')
s=s.replace('float t=battle.time;\n  if(t>=nextThink){nextThink=t+level.reaction;Think();}', 'float t=battle.time;Observe();AimWeapons();\n  if(t>=nextThink){nextThink=t+.2f;Think();}')
a=s.index('  float bestTarget=');z=s.index('  if(Vector2.Distance(lastProgress',a)
s=s[:a]+'  Vector2 target=self.aim;\n'+s[z:]
p.write_text(s,encoding='utf-8')
p=r/'Assets/Workbench/Runtime/ModuleVisual.cs';s=p.read_text(encoding='utf-8').replace('bool horizontal;','bool horizontal;int wheelSign=1;')
s=s.replace('transform.position=WorldView.World(center);','transform.position=WorldView.World(WorldView.RenderPosition(robot)+Construction.Rect(model.placement,robot.data).center);')
s=s.replace('model.spec.hp*.7f','model.maxHp*.7f').replace('model.spec.hp*.35f','model.maxHp*.35f')
s=s.replace('int frame=velocity.sqrMagnitude>.05f?(int)(time*Mathf.Max(4,velocity.magnitude*3))%4:0;if((horizontal?velocity.x:velocity.y)<0)frame=3-frame;', 'if(velocity.sqrMagnitude>.0001f)wheelSign=(horizontal?velocity.x:velocity.y)<0?-1:1;int frame=(int)(robot.wheelDistance*3)%4;if(wheelSign<0)frame=3-frame;')
p.write_text(s,encoding='utf-8')
p=r/'Assets/Workbench/Runtime/WorldView.cs';s=p.read_text(encoding='utf-8')
s=s.replace(' public static Vector3 World(',''' public static float Interpolation=1;
 public static Vector2 Pixel(Vector2 p)=>new Vector2(Mathf.Round(p.x*64)/64,Mathf.Round(p.y*64)/64);
 public static Vector2 RenderPosition(RobotModel r)=>Pixel(Vector2.Lerp(r.previousPosition,r.position,Interpolation));
 public static Vector3 World(''')
s=s.replace('World(v.robot.position+v.link.center)','World(RenderPosition(v.robot)+v.link.center)')
s=s.replace('go.transform.position=World(pos);','go.transform.position=World(Pixel(pos));')
s=s.replace('v.body.transform.position=World(p.position);','v.body.transform.position=World(Pixel(p.position));')
s=s.replace('v.points.Add(new TrailPoint{position=p.position,','v.points.Add(new TrailPoint{position=Pixel(p.position),')
s=s.replace('float f=immediate?1:1-Mathf.Exp(-Time.unscaledDeltaTime*4);', 'float f=immediate?1:1-Mathf.Exp(-Time.unscaledDeltaTime*(required>worldCamera.orthographicSize?12:3));')
p.write_text(s,encoding='utf-8')
p=r/'Assets/Workbench/Runtime/GameRoot.cs';s=p.read_text(encoding='utf-8').replace('accumulator-=1f/60;}RefreshHud();','accumulator-=1f/60;}WorldView.Interpolation=Mathf.Clamp01(accumulator*60);RefreshHud();');p.write_text(s,encoding='utf-8')
