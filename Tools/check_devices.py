"""Probe the shipped recognition worker. Save status and counts only; no audio, images or landmarks."""
from pathlib import Path
import subprocess,sys,json,time
root=Path(__file__).resolve().parents[1]
runtime=root/('Assets/StreamingAssets/InputRuntime' if '--source' in sys.argv else 'Builds/Windows/Workbench Arena_Data/StreamingAssets/InputRuntime')
proc=subprocess.Popen([str(runtime/'python/python.exe'),'-u',str(runtime/'input_worker.py'),'--voice','--hand']+(['--diagnose'] if '--diagnose' in sys.argv else []),stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf-8')
time.sleep(25)
out,err=proc.communicate('quit\n',timeout=12)
messages=[json.loads(line) for line in out.splitlines() if line.startswith('{')]
report={'exit_code':proc.returncode,'statuses':[m for m in messages if m.get('kind')=='status'],'microphones':[m['devices'] for m in messages if m.get('kind')=='devices'],'hand_frames':sum(m.get('kind')=='hand' for m in messages),'tracked_hand_frames':sum(m.get('kind')=='hand' and m.get('tracked',False) for m in messages),'commands':[m['text'] for m in messages if m.get('kind')=='command'],'live_user_validation':False,'recordings_saved':False}
(root/('TestResults/device-probe-source.json' if '--source' in sys.argv else 'TestResults/device-probe.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
(root/'TestResults/device-probe-diagnostics.txt').write_text(err,encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
