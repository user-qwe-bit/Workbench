"""Fetch redistributable offline input runtime into project, without system installation."""
from pathlib import Path
import urllib.request, zipfile, subprocess, json, hashlib
root=Path(__file__).resolve().parents[1]
dest=root/'Assets/StreamingAssets/InputRuntime'; dest.mkdir(parents=True,exist_ok=True)
cache=root/'Tools/Downloads'; cache.mkdir(exist_ok=True)
def download(url,name):
    p=cache/name
    if not p.exists():
        print('Download',name,flush=True)
        urllib.request.urlretrieve(url,p)
    return p
pyzip=download('https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip','python-3.11.9-embed-amd64.zip')
runtime=dest/'python'
if not (runtime/'python.exe').exists():
    runtime.mkdir(exist_ok=True)
    with zipfile.ZipFile(pyzip) as z:z.extractall(runtime)
(runtime/'python311._pth').write_text('python311.zip\n.\nLib/site-packages\nimport site\n')
pip=download('https://bootstrap.pypa.io/get-pip.py','get-pip.py')
if not (runtime/'Lib/site-packages/pip').exists():subprocess.run([str(runtime/'python.exe'),str(pip),'--no-warn-script-location'],check=True)
subprocess.run([str(runtime/'python.exe'),'-m','pip','install','--no-warn-script-location','mediapipe==0.10.21','vosk==0.3.45','sounddevice==0.5.1','opencv-contrib-python==4.11.0.86','numpy==1.26.4'],check=True)
model=download('https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task','hand_landmarker.task')
import shutil
shutil.copy2(model,dest/'hand_landmarker.task')
voice=download('https://alphacephei.com/vosk/models/vosk-model-small-ru-0.22.zip','vosk-model-small-ru-0.22.zip')
if not (dest/'vosk-model-small-ru-0.22').exists():
    with zipfile.ZipFile(voice) as z:z.extractall(dest)
manifest=[{'file':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in cache.iterdir() if p.is_file()]
(dest/'download-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Offline input dependencies installed',flush=True)
