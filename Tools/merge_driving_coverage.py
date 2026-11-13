"""Merge completed independent searches for this version, preserving their evidence."""
from pathlib import Path
import json, hashlib
r=Path(__file__).resolve().parents[1];folder=r/'TestResults'
base=json.loads((folder/'goal-coverage.json').read_text())
if 'searches' in base:
    print('Coverage already merged; no changes.')
    raise SystemExit(0)
assert len(base['coverage'])==40
extra=json.loads((folder/'goal-coverage-evasive.json').read_text())
assert len(extra['coverage'])==24
records={(row['level'],row['arena']):row for row in base['coverage']}
for row in extra['coverage']:
    found=records[(row['level'],row['arena'])]
    found['attempts']+=row['attempts']
    found['stars']=[a or b for a,b in zip(found['stars'],row['stars'])]
base['total']+=extra['total'];base['missing']=sum(row['stars'].count(False) for row in records.values())
base['game_data_sha256']=hashlib.sha256((r/'Assets/Workbench/Resources/GameData.json').read_bytes()).hexdigest()
base['searches']=['range/target pilot, up to 240 attempts per layout','evasive/range pilot, up to 48 attempts on 24 difficult layouts']
base['limitation']='Unconfirmed conditions mean no successful witness found by these automated pilots, not proof of impossibility.'
(folder/'goal-coverage.json').write_text(json.dumps(base,indent=2),encoding='utf-8')
witnesses=json.loads((folder/'goal-witnesses.json').read_text())
additional=folder/'goal-witnesses-evasive.json'
if additional.exists():witnesses+=json.loads(additional.read_text())
(folder/'goal-witnesses.json').write_text(json.dumps(witnesses,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'battles':base['total'],'conditions_confirmed':200-base['missing'],'unconfirmed':base['missing']}))
