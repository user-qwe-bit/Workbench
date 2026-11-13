from pathlib import Path
from PIL import Image
import json, hashlib
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads((ROOT/p).read_text(encoding='utf-8'))
sprites=read('Metadata/assets.json')['sprites'];fail=[];empty=[];paths=set()
for s in sprites:
    path=ROOT/s['path']
    if s['path'] in paths:fail.append('Duplicate path '+s['path'])
    paths.add(s['path'])
    if not path.exists():fail.append('Missing '+s['path']);continue
    im=Image.open(path).convert('RGBA');a=np.asarray(im)
    if list(im.size)!=[s['width'],s['height']]:fail.append('Size '+s['path'])
    if not np.isin(a[:,:,3],[0,255]).all():fail.append('Fractional alpha '+s['path'])
    if not a[:,:,3].any():empty.append(s['path'])
    px,py=s['pivot_px_top_left']
    if not (0<=px<=im.width and 0<=py<=im.height):fail.append('Pivot '+s['path'])

modules=read('Metadata/modules.json')['modules']
for m in modules:
    a=np.asarray(Image.open(ROOT/m['player']).convert('RGBA'));b=np.asarray(Image.open(ROOT/m['enemy']).convert('RGBA'))
    if not np.array_equal(a[:,:,3],b[:,:,3]):fail.append('Team silhouette '+m['name'])
    size=[64*m['footprint'][0],64*m['footprint'][1]]
    if [a.shape[1],a.shape[0]]!=size:fail.append('Module footprint '+m['name'])
    changed=np.any(a[:,:,:3]!=b[:,:,:3],axis=2)
    teal=(a[:,:,1].astype(int)>a[:,:,0].astype(int)*1.25)&(a[:,:,2].astype(int)>a[:,:,0].astype(int)*1.15)&(a[:,:,3]>0)
    if (changed&~teal).any():fail.append('Recolored non-team pixel '+m['name'])

clips=read('Metadata/animations.json')['clips']
for c in clips:
    sizes=set()
    for path in c['frames']:
        if not (ROOT/path).exists():fail.append('Missing frame '+path);continue
        sizes.add(Image.open(ROOT/path).size)
    if len(sizes)>1:fail.append('Mixed frame sizes '+c['name'])
    if c['mode']!='states' and c['fps']<=0:fail.append('FPS '+c['name'])

muzzle_distances={}
for t in read('Metadata/turrets.json')['turrets']:
    worst=0
    if len(t['directions'])!=32:fail.append('Direction count '+t['name'])
    for d in t['directions']:
        for team in ['player','enemy']:
            if not (ROOT/d[team]).exists():fail.append('Missing direction '+d[team])
        w,h=Image.open(ROOT/d['player']).size
        rgba=np.asarray(Image.open(ROOT/d['player']).convert('RGBA'));yy,xx=np.where(rgba[:,:,3]>0)
        for x,y in d['muzzles_px_top_left']:
            if not (0<=x<w and 0<=y<h):fail.append('Muzzle out of canvas '+t['name']+' '+str(d['index']))
            distance=int(np.min(np.maximum(abs(xx-x),abs(yy-y))));worst=max(worst,distance)
    muzzle_distances[t['name']]=worst
    if worst>2:fail.append('Muzzle far from barrel '+t['name']+' '+str(worst))

for n in read('Metadata/nine-slice.json')['items']:
    w,h=Image.open(ROOT/n['path']).size;l,b,r,t=n['border_lbrt']
    expected={'tl':(l,t),'t':(w-l-r,t),'tr':(r,t),'l':(l,h-t-b),'c':(w-l-r,h-t-b),'r':(r,h-t-b),'bl':(l,b),'b':(w-l-r,b),'br':(r,b)}
    rebuilt=Image.new('RGBA',(w,h));xy={'tl':(0,0),'t':(l,0),'tr':(w-r,0),'l':(0,t),'c':(l,t),'r':(w-r,t),'bl':(0,h-b),'b':(l,h-b),'br':(w-r,h-b)}
    for key,size in expected.items():
        piece=Image.open(ROOT/n['parts'][key]).convert('RGBA')
        if piece.size!=size:fail.append('9slice part size '+n['name']+' '+key)
        rebuilt.alpha_composite(piece,xy[key])
    if not np.array_equal(np.asarray(rebuilt),np.asarray(Image.open(ROOT/n['path']).convert('RGBA'))):fail.append('9slice reconstruction '+n['name'])

coverage=read('Metadata/coverage.json')
for item in coverage['items']:
    if item['status']=='MISSING' or not item['files']:fail.append('Catalogue '+item['id'])
    for path in item['files']:
        if not (ROOT/path).exists():fail.append('Catalogue path '+path)

for path in (ROOT/'Previews').glob('*1920x1080.png'):
    im=Image.open(path).convert('RGBA');a=np.asarray(im)
    if im.size!=(1920,1080):fail.append('FullHD size '+path.name)

# V3 native composition and continuous environment invariants.
preview=read('Metadata/preview-composition.json')
if preview['final_resize'] or preview['global_pixelation'] or preview['constructor_sprite_scale']!=1 or preview['battle_sprite_scale']!=1:fail.append('Preview scaling metadata')
workshop=np.asarray(Image.open(ROOT/'Previews/workshop_1920x1080.png').convert('RGBA'))
for p,(x,y) in [('Modules/Player/armor.png',(1698,238)),('Modules/Player/core.png',(912,380)),('UI/Counters/Counts/count_02.png',(466,900))]:
    tile=np.asarray(Image.open(ROOT/'Assets/RobotArenaArt'/p).convert('RGBA'));mask=tile[:,:,3]>0;actual=workshop[y:y+tile.shape[0],x:x+tile.shape[1]]
    if not np.array_equal(actual[mask],tile[mask]):fail.append('Native preview sprite mismatch '+p)
wall=read('Metadata/wall-blocks.json');arena=read('Metadata/arenas.json');clearance=[]
tiles={t['mask']:np.asarray(Image.open(ROOT/t['path']).convert('RGBA')) for t in wall['tiles']};fill=np.asarray(Image.open(ROOT/'Assets/RobotArenaArt/Environment/Walls/Connected/fill.png').convert('RGBA'))
def normalized(mask):
    for bit,required in [(16,3),(32,6),(64,12),(128,9)]:
        if mask&required!=required:mask&=~bit
    return mask
expected={normalized(i) for i in range(256)}
if set(tiles)!=expected or len(tiles)!=47:fail.append('Autotile47lookup incomplete')
if wall['cell_px']!=64 or wall['collider_px']!=[0,0,64,64]:fail.append('Wall logical footprint')
for mask,tile in tiles.items():
    if tile.shape!=(64,64,4) or not np.all(tile[:,:,3]==255):fail.append('Wall gaps or dimensions '+str(mask))
    if mask&1 and not np.array_equal(tile[:3,16:48],fill[:3,16:48]):fail.append('Internal north seam '+str(mask))
    if mask&2 and not np.array_equal(tile[16:48,-3:],fill[16:48,-3:]):fail.append('Internal east seam '+str(mask))
    if mask&4 and not np.array_equal(tile[-3:,16:48],fill[-3:,16:48]):fail.append('Internal south seam '+str(mask))
    if mask&8 and not np.array_equal(tile[16:48,:3],fill[16:48,:3]):fail.append('Internal west seam '+str(mask))
dirs=[(0,-1,1),(1,0,2),(0,1,4),(-1,0,8),(1,-1,16),(1,1,32),(-1,1,64),(-1,-1,128)]
tx,ty,tw,th=arena['table_rect'];camera=preview['battle_camera_origin_px']
for scheme in arena['schemes']:
    blocked={tuple(c) for c in scheme['occupied_cells']};checks=[]
    if len(blocked)!=scheme['block_count'] or len(blocked)!=len(scheme['occupied_cells']):fail.append('Duplicate wall cells')
    masks=[normalized(sum(bit for dx,dy,bit in dirs if (x+dx,y+dy) in blocked)) for x,y in scheme['occupied_cells']]
    if masks!=scheme['autotile_masks']:fail.append('Stale topology '+str(scheme['id']))
    for size in range(1,6):
        valid={(x,y) for y in range(33-size) for x in range(61-size) if not any((x+dx,y+dy) in blocked for dy in range(size) for dx in range(size))};remaining=set(valid);components=[]
        while remaining:
            queue=[remaining.pop()];count=1
            while queue:
                x,y=queue.pop()
                for p in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
                    if p in remaining:remaining.remove(p);queue.append(p);count+=1
            components.append(count)
        spawn=all(tuple(p) in valid for p in scheme['spawn_anchor_cells'])
        if len(components)!=1 or not spawn:fail.append('Arena clearance '+str(scheme['id'])+' size '+str(size))
        checks.append({'size':size,'components':len(components),'valid_positions':len(valid),'spawns_clear':spawn})
    clearance.append({'arena':scheme['id'],'checks':checks})
    native=Image.open(ROOT/f'Previews/arena_scheme_{scheme["id"]:02d}_native_4352x2448.png').convert('RGBA')
    if native.size!=tuple(arena['room_size']):fail.append('Native room size')
    crop=np.asarray(native.crop((*camera,camera[0]+1920,camera[1]+1080)));fullhd=np.asarray(Image.open(ROOT/f'Previews/arena_scheme_{scheme["id"]:02d}_1920x1080.png').convert('RGBA'))
    if not np.array_equal(crop,fullhd):fail.append('Viewport not native crop')
    for (x,y),mask in zip(scheme['occupied_cells'],scheme['autotile_masks']):
        actual=np.asarray(native.crop((tx+x*64,ty+y*64,tx+x*64+64,ty+y*64+64)))
        if not np.array_equal(actual,tiles[mask]):fail.append('Wrong wall compositing')
    if scheme['id']==1:
        for prop in arena['props']:
            x,y,w,h=prop['x'],prop['y'],prop['width'],prop['height'];source=np.asarray(Image.open(ROOT/prop['path']).convert('RGBA'));opaque=source[:,:,3]>0;actual=np.asarray(native.crop((x,y,x+w,y+h)))
            if 'parent_id' not in prop:
                combined=Image.fromarray(source)
                for child in arena['props']:
                    if child.get('parent_id')==prop['id']:combined.alpha_composite(Image.open(ROOT/child['path']).convert('RGBA'),(child['x']-x,child['y']-y))
                source=np.asarray(combined);opaque=source[:,:,3]>0
            if source.shape!=(h,w,4) or prop['scale']!=1 or max(w,h)>256:fail.append('Furniture size '+prop['name'])
            if not (x+w<=tx or x>=tx+tw or y+h<=ty or y>=ty+th):fail.append('Furniture inside arena '+prop['name'])
            if not (0<=x and 0<=y and x+w<=arena['room_size'][0] and y+h<=arena['room_size'][1]):fail.append('Furniture out of world')
            if not np.array_equal(actual[opaque],source[opaque]):fail.append('Furniture is scaled or covered '+prop['name'])
base_props=[p for p in arena['props'] if 'parent_id' not in p]
for i,a in enumerate(base_props):
    for b in base_props[i+1:]:
        if max(a['x'],b['x'])<min(a['x']+a['width'],b['x']+b['width']) and max(a['y'],b['y'])<min(a['y']+a['height'],b['y']+b['height']):fail.append('Overlapping furniture '+a['name']+' '+b['name'])
for prop in arena['props']:
    if 'parent_id' in prop:
        parent=next((p for p in base_props if p['id']==prop['parent_id']),None)
        if parent is None:fail.append('Missing parent '+prop['id']);continue
        if not (parent['x']<=prop['x'] and parent['y']<=prop['y'] and prop['x']+prop['width']<=parent['x']+parent['width'] and prop['y']+prop['height']<=parent['y']+parent['height']):fail.append('Tool outside parent '+prop['id'])
    x,y,w,h=prop['x'],prop['y'],prop['width'],prop['height']
    if not (x+w<=tx-64 or x>=tx+tw+64 or y+h<=ty-64 or y>=ty+th+64):fail.append('Prop over rim '+prop['id'])
for prop in read('Metadata/props.json')['items']:
    source=ROOT/prop['path']
    if hashlib.sha256(source.read_bytes()).hexdigest()!=prop['sha256']:fail.append('Original prop hash '+prop['name'])
    if list(Image.open(source).size)!=prop['size']:fail.append('Original prop dimensions '+prop['name'])
rim={k:np.asarray(Image.open(ROOT/p).convert('RGBA')) for k,p in read('Metadata/arena-rim.json')['parts'].items()}
for side in ['top','bottom']:
    if not np.array_equal(rim[side][:,0],rim[side][:,-1]):fail.append('Rim longitudinal seam '+side)
for side in ['left','right']:
    if not np.array_equal(rim[side][0],rim[side][-1]):fail.append('Rim longitudinal seam '+side)
rimjoins=[(rim['tl'][:,-1],rim['top'][:,0]),(rim['tl'][-1],rim['left'][0]),(rim['tr'][:,0],rim['top'][:,-1]),(rim['tr'][-1],rim['right'][0]),(rim['bl'][:,-1],rim['bottom'][:,0]),(rim['bl'][0],rim['left'][-1]),(rim['br'][:,0],rim['bottom'][:,-1]),(rim['br'][0],rim['right'][-1])]
if any(not np.array_equal(a,b) for a,b in rimjoins):fail.append('Rim corner seam')
variant_counts={}
for family in read('Metadata/tile-variants.json')['families']:
    images=[np.asarray(Image.open(ROOT/p).convert('RGBA')) for p in family['variants']];hashes={hashlib.sha256(a.tobytes()).hexdigest() for a in images};name=family['family'];variant_counts[name]=len(hashes)
    if name in ['floor','mat'] and len(hashes)!=16:fail.append('Variant count '+name)
    for a in images:
        if not np.array_equal(a[:,0],a[:,-1]) or not np.array_equal(a[0],a[-1]):fail.append('Tile seam '+name)
        if name in ['floor','arena'] and int(np.ptp(a[:,:,:3].astype(int),axis=(0,1)).max())>({'floor':7,'arena':4}[name]):fail.append('Surface contrast too high '+name)
placement=read('Metadata/surface-placement.json')
for key in ['floor_indices','arena_indices']:
    grid=placement[key]
    for y,row in enumerate(grid):
        for x,v in enumerate(row):
            for dx,dy in [(-1,0),(0,-1),(-1,-1),(1,-1),(-2,0),(0,-2)]:
                nx,ny=x+dx,y+dy
                if 0<=ny<len(grid) and 0<=nx<len(row) and grid[ny][nx]==v:fail.append('Repeated neighbor index')
digits=read('Metadata/counter-glyphs.json')
if digits['font_dependency'] is not None or set(digits['characters'])!=set('0123456789×+'):fail.append('Custom glyph set')
for sheet in read('Metadata/sheets.json')['sheets']:
    atlas=Image.open(ROOT/sheet['path']).convert('RGBA')
    for cell in sheet['sprites']:
        x,y,w,h=cell['rect_png'];source=np.asarray(Image.open(ROOT/cell['source']).convert('RGBA'));actual=np.asarray(atlas.crop((x,y,x+w,y+h)))
        if not np.array_equal(actual,source):fail.append('Stale atlas '+cell['source'])
for p in (ROOT/'Assets/RobotArenaArt/Config').glob('*.json'):
    if (ROOT/'Metadata'/p.name).exists() and read('Metadata/'+p.name)!=json.loads(p.read_text(encoding='utf-8')):fail.append('Config differs '+p.name)


allowed_empty=[];unexpected=[]
for p in empty:
    if '/Masks/' in p or p.endswith('step_00.png') or p.endswith('Parts/mat_border/c.png'):allowed_empty.append(p)
    else:unexpected.append(p)
fail.extend(['Unexpected empty '+p for p in unexpected])
font=ROOT/'Assets/RobotArenaArt/Fonts/m6x11cyrillic.ttf'
if hashlib.sha256(font.read_bytes()).hexdigest()!=read('Metadata/font.json')['source_sha256']:fail.append('Font hash')
report={'passed':not fail,'png_sprites':len(sprites),'catalogue_items':coverage['total_catalogue_items'],'animation_and_state_sets':len(clips),'nine_slice_templates':len(read('Metadata/nine-slice.json')['items']),'sprite_sheets':len(read('Metadata/sheets.json')['sheets']),'intentional_empty_assets':allowed_empty,'errors':fail,'verified':['PNG dimensions','binary alpha','module footprint sizes','same player/enemy silhouettes','only teal pixels recolored','animation canvas consistency','32 directions and muzzle coordinates','exact 9-slice reconstruction','all catalogue paths','FullHD dimensions and exact1:1sprite placement','all four native arena viewports','47neighbor-driven connected walls with no internal seams','original small props at native size outsidearena, intentional child tools','continuous rim and allcorner joints','grid path connectivity for footprints1..5 and spawn clearance','16floorvariants and quiet arena materials','surface border compatibility and nonrepeated neighbors','custom digits and keys','packed sheet pixels equal current PNGs','Config/Metadata synchronization','provided font SHA-256'],'not_verified':['Unity compilation and Editor import execution','play mode, gameplay balance, continuous runtime collisions, audio','webcam and voice recognition','final shield collider geometry']}
report['maximum_muzzle_distance_to_opaque_pixel']=muzzle_distances
report['unique_surface_variants']=variant_counts;report['arena_clearance']=clearance
(ROOT/'Metadata'/'validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
raise SystemExit(0 if not fail else 1)
