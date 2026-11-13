from pathlib import Path
r = Path(__file__).resolve().parents[1]
p = r/'Assets/Workbench/Runtime/BattleModel.cs'
s = p.read_text(encoding='utf-8').replace('public sealed class BattleModel {','public sealed partial class BattleModel {')
a = s.index(' void Move(')
z = s.index(' public void Shoot(',a)
s = s[:a] + ''' public void Step(float dt){
  if(paused||finished||dt<=0)return;
  // Small deterministic physics slices keep compound contacts stable even at a low render rate.
  int slices=Mathf.Max(1,Mathf.CeilToInt(dt*120-.0001f));float h=dt/slices;
  foreach(var r in robots)r.previousPosition=r.position;
  for(int i=0;i<slices&&!finished;i++){
   time+=h;MoveRobots(h);
   foreach(var r in robots){foreach(var m in r.Live)m.aim=(m.targeted?m.aimTarget:r.aim)-r.CenterOf(m);if(r.shieldRequested)r.ActivateShield(time);r.shieldRequested=false;r.Fire(time,m=>Shoot(r,m));}
   foreach(var p in projectiles.ToArray())if(p.alive)Advance(p,h);
   foreach(var r in robots)r.ResolveDamage();
   if(robots.Any(r=>r.Dead)){finished=true;winner=robots[0].Dead?(robots[1].Dead?-1:1):0;}
   projectiles.RemoveAll(p=>!p.alive);
  }
 }
''' + s[z:]
s = s.replace('var aim=(r.aim-origin).normalized;', 'var aim=((m.targeted?m.aimTarget:r.aim)-origin).normalized;')
s = s.replace('damage=m.spec.damage,','damage=m.spec.damage*(r.team==0?data.balance.playerDamageScale:1),')
p.write_text(s,encoding='utf-8')
p=r/'Assets/Workbench/Runtime/UiFactory.cs';s=p.read_text(encoding='utf-8');s=s.replace('Text(menu,a,"Credits","m6x11cyrillic — Daniel Linssen and Natto Games",80,1020,1760,32,1,true,true);','');p.write_text(s,encoding='utf-8')
p=r/'Assets/Workbench/Runtime/GameRoot.cs';s=p.read_text(encoding='utf-8').replace('core.hp/core.spec.hp','core.hp/core.maxHp');p.write_text(s,encoding='utf-8')
p=r/'Tools/apply_polish_content.py';s=p.read_text(encoding='utf-8');s+='\nimport runpy\nrunpy.run_path(str(root/"Tools/apply_driving_content.py"))\n';p.write_text(s,encoding='utf-8')
