from pathlib import Path
import json,hashlib,zipfile
root=Path(__file__).resolve().parents[1]
inventory=json.loads((root/'TestResults/SourceAudit/inventory.json').read_text(encoding='utf-8'))
changed=[]
for entry in inventory:
    p=root/'kek'/entry['path']
    if not p.exists() or hashlib.sha256(p.read_bytes()).hexdigest()!=entry['sha256']:changed.append(entry['path'])
assert not changed,changed
huh=json.loads((root/'TestResults/HuhAudit/inventory.json').read_text(encoding='utf-8'))
assert all(hashlib.sha256((root/'huh'/e['path']).read_bytes()).hexdigest()==e['sha256'] for e in huh)
with zipfile.ZipFile(root/'Builds/Workbench-Windows-x64.zip') as z:
    names=set(z.namelist());prefix='Workbench/'
    required=['Workbench.exe','UnityPlayer.dll','Workbench_Data/Managed/Assembly-CSharp.dll','Workbench_Data/StreamingAssets/InputRuntime/input_worker.py','Workbench_Data/StreamingAssets/InputRuntime/python/python.exe','Workbench_Data/StreamingAssets/InputRuntime/vosk/libvosk.dll','Workbench_Data/StreamingAssets/InputRuntime/hand_landmarker.task','Workbench_Data/StreamingAssets/InputRuntime/vosk-model-small-ru-0.22/am/final.mdl','README_RU.md','Docs/VERIFICATION_RU.md','THIRD_PARTY_NOTICES.md','Licenses/APACHE-2.0.txt']
    assert all(prefix+p in names for p in required)
    assert not any('TestResults/' in p or 'BackUpThisFolder' in p for p in names)
report={'original_files_unchanged':len(inventory),'changed':changed,'huh_files_unchanged':len(huh),'required_release_entries':len(required),'required_entries_present':True,'source_project_scene_exists':(root/'Assets/Workbench/Scenes/WorkbenchArena.unity').exists(),'release_test_saves_excluded':True}
(root/'TestResults/release-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
