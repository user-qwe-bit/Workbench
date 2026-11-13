from pathlib import Path
import json
p=Path('Assets/Workbench/Runtime/ModuleVisual.cs');s=p.read_text(encoding='utf-8')
s=s.replace('int[] recoilFrames;','int[] recoilFrames;float lastHp,hitAt=-10;bool horizontal;')
s=s.replace('model=m;recoilFrames','model=m;lastHp=m.hp;recoilFrames')
s=s.replace('damage.sprite=model.hp', 'if(model.hp<lastHp)hitAt=time;lastHp=model.hp;body.color=time-hitAt<.08f?new Color(1,.55f,.4f):Color.white;\n  damage.sprite=model.hp')
start=s.index('  int frame=');end=s.index('  if(model.spec.Weapon)',start)
s=s[:start]+'''  var velocity=robot.velocity;if(Mathf.Abs(velocity.x)>Mathf.Abs(velocity.y)*1.3f&&Mathf.Abs(velocity.x)>.2f)horizontal=true;else if(Mathf.Abs(velocity.y)>Mathf.Abs(velocity.x)*1.3f&&Mathf.Abs(velocity.y)>.2f)horizontal=false;
  int frame=velocity.sqrMagnitude>.05f?(int)(time*Mathf.Max(4,velocity.magnitude*3))%4:0;if((horizontal?velocity.x:velocity.y)<0)frame=3-frame;
  wheelLeft.sprite=wheelRight.sprite=art.Get("Wheels/wheel_"+(horizontal?"horizontal":"vertical")+"/frame_"+frame.ToString("00"));
  Vector2 axis=horizontal?Vector2.up:Vector2.right;float distance=(horizontal?model.spec.height:model.spec.width)*.5f;wheelLeft.transform.localPosition=WorldView.World(-axis*distance);wheelRight.transform.localPosition=WorldView.World(axis*distance);
  wheelLeft.enabled=wheelRight.enabled=true;foreach(var other in robot.Live){if(other==model)continue;var r=robot.RectOf(other);if(r.Contains(center-axis*(distance+.1f)))wheelLeft.enabled=false;if(r.Contains(center+axis*(distance+.1f)))wheelRight.enabled=false;}
''' +s[end:];p.write_text(s,encoding='utf-8')
p=Path('Assets/Workbench/Runtime/GameRoot.cs');s=p.read_text(encoding='utf-8').replace('public ScreenMotion motion;','public UiParticles uiParticles;public ScreenMotion motion;')
s=s.replace('motion.gameObject.SetActive(true);','uiParticles.gameObject.SetActive(true);motion.gameObject.SetActive(true);')
s=s.replace('build=exchanged;Sound("place");','build=exchanged;Sound("place");uiParticles.Burst(point,12,new Color(.4f,1,.85f));')
s=s.replace('Sound(won?"victory":"defeat");','Sound(won?"victory":"defeat");if(won)StartCoroutine(ResultSparkles());')
s=s.replace(' public static string FormatTime',' System.Collections.IEnumerator ResultSparkles(){yield return new WaitForSecondsRealtime(.5f);for(int i=0;i<5;i++)if(earned[i])uiParticles.Burst(new Vector2(568,390+i*56),16,new Color(1,.8f,.3f));}\n public static string FormatTime')
p.write_text(s,encoding='utf-8')
p=Path('Assets/Workbench/Runtime/UiFactory.cs');s=p.read_text(encoding='utf-8').replace('  var transition=Rect','  root.uiParticles=Rect(canvas,"UiParticles",0,0,1920,1080).gameObject.AddComponent<UiParticles>();root.uiParticles.raycastTarget=false;\n  var transition=Rect');p.write_text(s,encoding='utf-8')
p=Path('Assets/RobotArenaArt/Config/nine-slice.json');data=json.loads(p.read_text(encoding='utf-8'))
for name,border in [('UI/Bars/Frames/hud',2),('UI/Bars/Frames/builder',2),('UI/Bars/Frames/world',2),('UI/Loading/progress_track',4)]:
    path='Assets/RobotArenaArt/'+name+'.png'
    if not any(x['path']==path for x in data['items']):data['items'].append({'path':path,'border_lbrt':[border]*4})
p.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
p=Path('Assets/Workbench/Resources/GameData.json');data=json.loads(p.read_text(encoding='utf-8'));data['balance'].update(acceleration=32,braking=42,impactImpulse=3.6,impulseDamping=10,wallDamageFactor=.5,wallRestitution=.24);p.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
