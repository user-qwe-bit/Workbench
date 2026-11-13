from pathlib import Path
import hashlib,json
r=Path(__file__).resolve().parents[1]
old=r/'TestResults/BuildHistory/Windows-BeforeOnboarding/Workbench_Data/StreamingAssets/InputRuntime'
new=r/'Builds/Windows/Workbench_Data/StreamingAssets/InputRuntime'
def files(root):return {str(p.relative_to(root)):p for p in root.rglob('*') if p.is_file() and '__pycache__' not in p.parts and p.suffix!='.pyc'}
a,b=files(old),files(new);assert a.keys()==b.keys(),a.keys()^b.keys()
changed=[name for name in a if hashlib.sha256(a[name].read_bytes()).digest()!=hashlib.sha256(b[name].read_bytes()).digest()]
assert not changed,changed
report={'same_files':len(a),'changed':changed,'result':'Previously tested native input runtime, libraries and models copied byte-for-byte into Windows; live devices were not reopened.'}
(r/'TestResults/input-runtime-identity.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
