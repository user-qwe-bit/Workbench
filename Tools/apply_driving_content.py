"""Idempotent balance requested on 13 November, applied after the original art/content import."""
from pathlib import Path
import json
root = Path(__file__).resolve().parents[1]
p = root / 'Assets/Workbench/Resources/GameData.json'
d = json.loads(p.read_text(encoding='utf-8'))
for m, speed in zip(d['modules'], [0, 0, 0, 0, 24, 34, 17, 24, 0]):
    m['projectileSpeed'] = round(speed * 1.4, 4)
for i, seconds in enumerate([90, 100, 100, 110, 130, 130, 140, 110, 150, 180]):
    d['levels'][i]['targetSeconds'] = seconds / 2
    d['levels'][i]['reaction'] = round(.25 - i * .03 / 9, 4)
    d['levels'][i]['aimError'] = round(max(.8, (1.4 - .11*i)*1.4), 4)
    d['levels'][i]['enemyDamageScale'] = [.9, .9, .9, .7, .7, .7, .55, .85, .7, .7][i]
    d['levels'][i]['burstOn'] = 2
    d['levels'][i]['burstOff'] = .6
b = d['balance']
for key in ['massScale', 'acceleration', 'braking', 'impactImpulse', 'impulseDamping', 'wallRestitution', 'enemyCoreHpScale']:
    b.pop(key, None)
b.update(speedBase=10, speedMax=9, speedMin=3.5, massDrag=.02, massInfluence=1.25,
         lightMass=20, heavyMass=80, lightAccelerationTime=.22, heavyAccelerationTime=.32,
         lightStopTime=.15, heavyStopTime=.23, reverseBraking=2, stopEpsilon=.015,
         impactMinSpeed=.75, wallDamageFactor=.5, playerDamageScale=1/1.1, enemyDurabilityFirst=1, enemyDurabilityLast=2.5)
p.write_text(json.dumps(d, ensure_ascii=False, indent=2), encoding='utf-8')
print('Applied driving, projectile speed, difficulty and ten time targets.')
