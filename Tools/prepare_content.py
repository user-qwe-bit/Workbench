from pathlib import Path
import json, shutil, math, wave, struct, random
ROOT=Path(__file__).resolve().parents[1]; meta=ROOT/'kek/pack/Metadata'; out=ROOT/'Assets/Workbench/Resources'
out.mkdir(parents=True,exist_ok=True)
def read(n):return json.loads((meta/(n+'.json')).read_text(encoding='utf-8'))
def point(a):return {'x':a[0],'y':a[1]}
names=['core','chassis','armor','battery','machinegun','rifle','grenade','shotgun','shield']
ru=['Ядро','Корпус','Броня','Батарея','Пулемёт','Винтовка','Гранатомёт','Дробовик','Щит']
desc=['Потеря ядра завершает бой.','Соединяет и защищает детали.','Прочная защита. Добавляет массу.','Даёт 5 единиц питания.','Регулярный игрушечный огонь.','Редкие мощные выстрелы.','Слабый урон группе деталей.','Опасен вблизи, слаб вдали.','Отражает снаряды: 3 с, откат 30 с.']
hp=[300,120,240,90,100,140,170,90,180]; mass=[4,3,8,3,5,13,16,4,14]
damage=[0,0,0,0,12,48,15,8,0]; interval=[0,0,0,0,.5,3,2,1.1,0]; speed=[0,0,0,0,24,34,17,24,0]; ranges=[0,0,0,0,25,40,22,8,0]
modules=[]
for i,m in enumerate(read('modules')['modules']):
    assert m['name']==names[i]
    modules.append(dict(id=i,key=names[i],title=ru[i],description=desc[i],width=m['footprint'][0],height=m['footprint'][1],hp=hp[i],mass=mass[i],damage=damage[i],interval=interval[i],projectileSpeed=speed[i],range=ranges[i],pellets=6 if i==7 else 1,spread=24 if i==7 else 0,blastRadius=1.8 if i==6 else 0))
inventories=[[1,3,1,1,1,0,0,0,0],[1,4,3,2,2,0,0,0,0],[1,3,3,2,2,0,0,1,0],[1,3,2,2,2,1,0,1,0],[1,3,3,2,2,1,1,1,0],[1,3,2,2,2,1,0,1,1],[1,4,3,2,2,2,1,1,1],[1,3,3,2,3,1,1,2,1],[1,4,4,3,2,2,1,2,1],[1,5,4,3,3,2,1,2,1]]
titles=['Первый запуск','Крепыш','Прилипала','Батарея орудий','Панцирь','Зеркало','Разборщик','Шустрик','Два крыла','Финальный спор']
goals=[ [('core_hp',1,'Без потери HP ядра'),('keep_type',4,'Сохранить установленный пулемёт'),('no_type',2,'Без установленной брони')], [('destroy_weapons',1,'Уничтожить оружие до ядра'),('modules',6,'Не более 6 модулей на старте'),('keep_type',3,'Сохранить все батареи')], [('no_type',7,'Без установленного дробовика'),('keep_weapons',1,'Сохранить всё своё оружие'),('core_hp',.5,'Сохранить 50% HP ядра')], [('cells',8,'Не более 8 клеток на старте'),('destroy_rifles',2,'Уничтожить 2 винтовки до ядра'),('no_type',5,'Без установленной винтовки')], [('no_type',6,'Без установленного гранатомёта'),('destroy_armor',3,'Уничтожить 3 брони до ядра'),('keep_weapon',1,'Сохранить хотя бы одно оружие')], [('reflect',1,'Попасть отражённым снарядом'),('no_type',8,'Без установленного щита'),('destroy_rifles',1,'Уничтожить винтовку до ядра')], [('cells',10,'Не более 10 клеток на старте'),('keep_type',3,'Сохранить все батареи'),('destroy_grenade',1,'Уничтожить гранатомёт до ядра')], [('no_type',7,'Без установленного дробовика'),('keep_weapons',1,'Сохранить всё своё оружие'),('core_hp',.75,'Сохранить 75% HP ядра')], [('destroy_weapons',2,'Уничтожить оба оружия до ядра'),('no_shield_use',1,'Не активировать щит'),('batteries',2,'Не более 2 батарей на старте')], [('reflect',1,'Попасть отражённым снарядом'),('destroy_all',1,'Уничтожить всё оружие до ядра'),('cells',12,'Не более 12 клеток на старте')]]
levels=[]
for i,e in enumerate(read('enemy-previews')['enemies']):
    levels.append(dict(id=i,title=titles[i],inventory=inventories[i],enemy=[dict(id=j,type=names.index(m['module']),x=m['grid_x'],y=m['grid_y']) for j,m in enumerate(e['modules'])],targetSeconds=[90,100,100,110,130,130,140,110,150,180][i],reaction=.55-.035*i,aimError=1.4-.11*i,goals=[dict(kind=a,value=b,text=c) for a,b,c in goals[i]],defaultArena=i%4))
# Balance revision: protect the final core behind its weapons, so dismantling all guns is
# achievable without accidentally destroying the exposed central core first. Same modules/power.
boss=levels[9]['enemy'];core=next(p for p in boss if p['type']==0);battery=next(p for p in boss if p['type']==3 and p['x']==4 and p['y']==2)
core['x'],core['y'],battery['x'],battery['y']=battery['x'],battery['y'],core['x'],core['y']
a=read('arenas'); surfaces=read('surface-placement'); turrets=[]
for t in read('turrets')['turrets']:
    turrets.append(dict(type=names.index(t['name']),recoil=t['recoil_px'],directions=[dict(index=d['index'],muzzles=[{'x':(m[0]-d['pivot_px_top_left'][0])/64,'y':(m[1]-d['pivot_px_top_left'][1])/64} for m in d['muzzles_px_top_left']]) for d in t['directions']]))
arena=[dict(id=s['id']-1,title=s['name'],walls=[dict(x=c[0],y=c[1],mask=m) for c,m in zip(s['occupied_cells'],s['autotile_masks'])],spawns=[point(p) for p in s['spawn_anchor_cells']]) for s in a['schemes']]
data=dict(modules=modules,levels=levels,arenas=arena,turrets=turrets,props=a['props'],markings=a['markings'],floor=[dict(values=r) for r in surfaces['floor_indices']],surface=[dict(values=r) for r in surfaces['arena_indices']],clips=read('animations')['clips'],balance=dict(gridSize=5,batteryCapacity=5,speedMax=8.5,speedMin=2.4,massScale=35,impactDamage=15,shieldDuration=3,shieldCooldown=30,shieldRadius=1.8,shieldArc=120,restorePower=False,cameraMinSize=8.4375,cameraMargin=3))
(out/'GameData.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
# Original synthesised toy sounds; no outside recordings or licensing dependencies.
audio=ROOT/'Assets/Workbench/Audio';audio.mkdir(exist_ok=True); rng=random.Random(12)
for index,name in enumerate(['click','place','invalid','machinegun','rifle','grenade','shotgun','impact','break','power_loss','shield','reflect','ready','victory','defeat']):
    duration=.12 if index<4 else (.9 if index>=13 else .3)
    pcm=[];rate=22050
    for k in range(int(rate*duration)):
        t=k/rate; env=(1-t/duration)**2; freq=(650-index*29)*(1-.55*t/duration)
        if index>=13:freq=[523,659,784,1046][min(3,int(t/.225))] if index==13 else [440,392,330,262][min(3,int(t/.225))]
        v=(math.sin(2*math.pi*freq*t)*.65+math.sin(2*math.pi*freq*1.99*t)*.15+rng.uniform(-1,1)*(.35 if index in range(3,10) else .06))*env*.32
        pcm.append(struct.pack('<h',int(max(-1,min(1,v))*32767)))
    with wave.open(str(audio/(name+'.wav')),'wb') as w:w.setparams((1,2,rate,len(pcm),'NONE','not compressed'));w.writeframes(b''.join(pcm))
print('Prepared 9 modules, 10 levels, 4 arenas, metadata, 15 original sounds')

# Apply acceptance polish after regenerating the base review data.
import runpy
runpy.run_path(str(Path(__file__).with_name("apply_polish_content.py")))
