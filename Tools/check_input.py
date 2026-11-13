from pathlib import Path
import sys,json,subprocess,wave,importlib.util,time
root=Path(__file__).resolve().parents[1];runtime=Path(sys.argv[1]) if len(sys.argv)>1 else root/'Assets/StreamingAssets/InputRuntime'
spec=importlib.util.spec_from_file_location('input_worker',runtime/'input_worker.py');worker=importlib.util.module_from_spec(spec);spec.loader.exec_module(worker)
report={'native_models':False,'speech_fixtures':[],'missing_devices':[],'live_human_gesture_verified':False,'live_human_speech_verified':False}
worker.selftest();report['native_models']=True
import numpy as np,cv2
mp,hand=worker.hand_model()
image_path=Path('C:/Coding/games/Neonflow/TestResults/mediapipe-hands.jpg')
if image_path.exists():
    frame=cv2.cvtColor(cv2.imread(str(image_path)),cv2.COLOR_BGR2RGB)
    result=hand.detect_for_video(mp.Image(image_format=mp.ImageFormat.SRGB,data=frame),1)
    report['static_hand_photo']={'detected':len(result.hand_landmarks),'landmarks_per_hand':[len(h) for h in result.hand_landmarks],'source':'existing local test photo; not a live webcam test'}
hand.close()
for i,expected in enumerate(['начать бой','пауза','продолжить']):
    path=root/f'TestResults/InputFixtures/command_{i}.wav'
    if not path.exists() and i in (1,2):
        path=Path(f'C:/Coding/games/Neonmoves/AI/testdata/Microsoft_Irina_Desktop_{i-1}.wav')
    if not path.exists():
        report['speech_fixtures'].append({'expected':expected,'passed':False,'not_run':'Synthetic fixture unavailable; PowerShell script execution is disabled'})
        continue
    with wave.open(str(path),'rb') as w:
        assert w.getsampwidth()==2 and w.getnchannels()==1
        rate=w.getframerate();samples=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2')
        if rate!=16000:samples=np.interp(np.arange(int(len(samples)*16000/rate))*rate/16000,np.arange(len(samples)),samples).astype('<i2')
        raw=samples.tobytes()+b'\0'*64000
    decoder=worker.Speech();results=[]
    for offset in range(0,len(raw),3200):
        text=decoder.accept(raw[offset:offset+3200])
        if text:results.append(text)
    decoder.close();report['speech_fixtures'].append({'expected':expected,'recognized':results,'passed':expected in results,'source':'Windows SAPI Irina synthetic audio, not live microphone'})
for mode in ['--voice','--hand']:
    proc=subprocess.Popen([sys.executable,'-u',str(runtime/'input_worker.py'),mode,'--mic','999','--camera','999'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf-8')
    time.sleep(3);out,err=proc.communicate('quit\n',timeout=8);messages=[json.loads(line) for line in out.splitlines() if line.startswith('{')]
    report['missing_devices'].append({'mode':mode,'messages':messages,'exit':proc.returncode,'passed':any('недоступ' in m.get('text','') for m in messages)})
(root/('TestResults/input-checks-build.json' if len(sys.argv)>1 else 'TestResults/input-checks.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
if not all(x['passed'] for x in report['speech_fixtures']+report['missing_devices']):sys.exit(1)
