from pathlib import Path
import json, hashlib, runpy, shutil
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]
src=ROOT/'kek'; out=ROOT/'TestResults'/'SourceAudit'; out.mkdir(parents=True,exist_ok=True)
inventory=[]; configs=[]
for p in sorted(src.rglob('*')):
    if not p.is_file(): continue
    raw=p.read_bytes(); item={'path':p.relative_to(src).as_posix(),'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
    if p.suffix.lower() in ('.png','.gif'):
        with Image.open(p) as im:
            item.update(size=list(im.size),mode=im.mode,frames=getattr(im,'n_frames',1),alpha=im.convert('RGBA').getchannel('A').getextrema())
    if p.suffix=='.json':
        data=json.loads(raw); configs.append({'path':item['path'],'data':data})
        print(item['path'],list(data) if isinstance(data,dict) else 'array',len(raw))
    inventory.append(item)
(out/'inventory.json').write_text(json.dumps(inventory,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'all-configurations.json').write_text(json.dumps(configs,ensure_ascii=False,indent=2),encoding='utf-8')
# Run supplied validator on an exact audit copy: original material is never modified.
audit=out/'pack'
if not audit.exists(): shutil.copytree(src/'pack',audit)
runpy.run_path(str(audit/'Tools/validate_pack.py'),run_name='__audit__')
