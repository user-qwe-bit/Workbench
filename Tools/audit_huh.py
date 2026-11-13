from pathlib import Path
from PIL import Image,ImageDraw
import hashlib,json,shutil
root=Path(__file__).resolve().parents[1];out=root/'TestResults/HuhAudit';out.mkdir(exist_ok=True)
files=sorted((root/'huh').rglob('*'));report=[];groups={}
for p in files:
    if not p.is_file():continue
    rel=p.relative_to(root/'huh');entry={'path':str(rel),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
    if p.suffix.lower()=='.png':
        im=Image.open(p).convert('RGBA');entry.update(size=im.size,alpha_extrema=im.getchannel('A').getextrema(),bbox=im.getbbox())
        key=str(p.parent.relative_to(root/'huh/Assets/RobotArenaArt'))
        groups.setdefault(key,[]).append(p)
    report.append(entry)
(out/'inventory.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
for n,(key,paths) in enumerate(groups.items()):
    sheet=Image.new('RGB',(800,50+((len(paths)+5)//6)*145),(64,60,55));draw=ImageDraw.Draw(sheet);draw.text((10,10),key,fill='white')
    for i,p in enumerate(paths):
        im=Image.open(p).convert('RGBA');im.thumbnail((124,110),Image.Resampling.NEAREST);x=8+i%6*132;y=45+i//6*145
        sheet.paste(im,(x+(124-im.width)//2,y),im);draw.text((x,y+112),p.stem,fill='white')
    sheet.save(out/f'{n:02d}.png')
    print(f'{n:02d} {key} {len(paths)}')
shutil.copytree(root/'huh/Assets/RobotArenaArt',root/'Assets/RobotArenaArt',dirs_exist_ok=True)
print('Audited/copied',len(report),'files; originals preserved')
