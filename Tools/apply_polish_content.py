from pathlib import Path
import json,random,copy
root=Path(__file__).resolve().parents[1];p=root/'Assets/Workbench/Resources/GameData.json';data=json.loads(p.read_text(encoding='utf-8'))
base=root/'Tools/base-props-v4.json'
if not base.exists():base.write_text(json.dumps(data['props'],ensure_ascii=False,indent=2),encoding='utf-8')
props=json.loads(base.read_text(encoding='utf-8'));parents=[p for p in props if not p.get('parent_id')];rng=random.Random(613);occupied=[];placed=[]
for i in range(240):
    original=parents[i%len(parents)];w=original['width']/64;h=original['height']/64;side=i%4
    for attempt in range(600):
        if side==0:x,y=rng.uniform(-7.4,67.4-w),rng.uniform(-7.4,-1.5-h)
        elif side==1:x,y=rng.uniform(-7.4,67.4-w),rng.uniform(33.5,39.4-h)
        elif side==2:x,y=rng.uniform(-7.4,-1.5-w),rng.uniform(-.5,32.5-h)
        else:x,y=rng.uniform(61.5,67.4-w),rng.uniform(-.5,32.5-h)
        x=round(x*64)/64;y=round(y*64)/64
        if any(x+w+.25>a and x-.25<c and y+h+.25>b and y-.25<d for a,b,c,d in occupied):continue
        break
    else:continue
    occupied.append((x,y,x+w,y+h));parent=copy.deepcopy(original);parent['id']=f'scatter_{i:03d}';parent['x']=round(x*64+256);parent['y']=round(y*64+192);placed.append(parent)
    for child in props:
        if child.get('parent_id')!=original['id']:continue
        new=copy.deepcopy(child);new.update(id=f"scatter_{i:03d}_{child['id']}",parent_id=parent['id'],x=parent['x']+child['x']-original['x'],y=parent['y']+child['y']-original['y']);placed.append(new)
data['props']=placed
data['balance'].update(acceleration=32,braking=42,impactImpulse=3.6,impulseDamping=10,wallDamageFactor=.5,wallRestitution=.24)
p.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print('Workshop groups',len(occupied),'props including attached tools',len(placed))

import runpy
runpy.run_path(str(root/"Tools/apply_driving_content.py"))
