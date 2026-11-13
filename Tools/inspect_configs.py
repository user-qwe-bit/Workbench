from pathlib import Path
import json
p=Path('kek/pack/Metadata')
for name in ['modules','turrets','arenas','arena-rim','surface-placement','tile-variants','animations','enemy-previews','font','nine-slice','keycaps','counter-glyphs','palette','aliases','preview-composition']:
    d=json.loads((p/(name+'.json')).read_text(encoding='utf-8'))
    def brief(v):
        if isinstance(v,dict): return {k:brief(x) for k,x in v.items()}
        if isinstance(v,list): return {'length':len(v),'first':brief(v[0]) if v else None,'last':brief(v[-1]) if len(v)>1 else None}
        return v
    print(name,json.dumps(brief(d),ensure_ascii=False))
