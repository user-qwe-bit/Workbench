from pathlib import Path
import shutil,zipfile,json,hashlib
root=Path(__file__).resolve().parents[1]; dest=root/'Assets/StreamingAssets/InputRuntime';dest.mkdir(parents=True,exist_ok=True)
source=Path('C:/Coding/games/Coinbound/Assets/StreamingAssets/Hands')
shutil.copytree(source/'python',dest/'python',dirs_exist_ok=True,ignore=shutil.ignore_patterns('*.meta','__pycache__'))
shutil.copy2(source/'hand_landmarker.task',dest/'hand_landmarker.task')
voice=Path('C:/Coding/games/Neonflow/Assets/StreamingAssets/Recognition/vosk-model-small-ru-0.22')
shutil.copytree(voice,dest/'vosk-model-small-ru-0.22',dirs_exist_ok=True,ignore=shutil.ignore_patterns('*.meta'))
native=Path('C:/Coding/games/Coinbound/Assets/Plugins/x86_64')
(dest/'vosk').mkdir(exist_ok=True)
for p in native.glob('*.dll'):shutil.copy2(p,dest/'vosk'/p.name)
(dest/'provenance.json').write_text(json.dumps({'python':'3.12 embedded, existing local redistributable','mediapipe':'0.10.32, local wheel installation','opencv':'5.0.0.93','sounddevice':'0.5.6','numpy':'2.5.4','hand_model_sha256':hashlib.sha256((dest/'hand_landmarker.task').read_bytes()).hexdigest(),'vosk_model':'vosk-model-small-ru-0.22','source_projects':['Coinbound (dependencies only)','Neonflow (speech model only)']},indent=2),encoding='utf-8')
print('Copied existing offline dependencies; verification pending')
