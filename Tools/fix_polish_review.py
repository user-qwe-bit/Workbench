from pathlib import Path
p=Path('Assets/Workbench/Runtime/GameRoot.cs');s=p.read_text(encoding='utf-8');start=s.index(' public void SetState(');end=s.index(' void FinishIntro',start)
s=s[:start]+''' public void SetState(ScreenState s){State=s;if(notice)notice.SetActive(false);ScreenState background=s==ScreenState.Arenas?ScreenState.Builder:s==ScreenState.Pause||s==ScreenState.Result?ScreenState.Battle:s==ScreenState.Settings?(settingsReturn==ScreenState.Pause?ScreenState.Battle:settingsReturn):s;
  foreach(string name in screens)Obj(name).gameObject.SetActive(name==s.ToString()||name==background.ToString());if(battle!=null)battle.paused=s!=ScreenState.Battle;inputBlockedUntil=Time.unscaledTime+.2f;
 }
''' +s[end:]
s=s.replace('Sound(won?"victory":"defeat");','((RectTransform)Obj("Result/Retry")).sizeDelta=new Vector2(next?380:480,80);((RectTransform)Obj("Result/Retry/Label")).sizeDelta=new Vector2(next?348:448,48);Sound(won?"victory":"defeat");')
p.write_text(s,encoding='utf-8')
p=Path('Assets/Workbench/Runtime/RuntimeAcceptance.cs');s=p.read_text(encoding='utf-8').replace('app.SetState(ScreenState.Intro);','app.save.Data.settings.controlHints=false;app.SetState(ScreenState.Intro);')
s=s.replace('Check(app.recognition.CameraName(-1)=="ПО УМОЛЧАНИЮ","Camera selector offers default device");','Check(app.recognition.CameraName(-1)=="ПО УМОЛЧАНИЮ"&&string.IsNullOrEmpty(CameraDevices.LastError),"Camera selector offers default and enumerates real devices without error");')
# Bounds/particles visual exercise is explicitly separate from winning matches.
s=s.replace('  var reload=new SaveSystem','''  app.selectedLevel=0;app.OpenBuilder();yield return Settled();app.Command("начать бой");yield return Settled();app.Pause();
  app.battle.robots[0].position=new Vector2(.3f,.3f);app.battle.robots[1].position=new Vector2(57,28);yield return new WaitForSecondsRealtime(1);Screenshot("camera_edges");var camera=app.worldView.worldCamera;float halfW=camera.orthographicSize*16/9;Check(camera.transform.position.x-halfW>=-8&&camera.transform.position.x+halfW<=68&&camera.transform.position.y-camera.orthographicSize>=-40&&camera.transform.position.y+camera.orthographicSize<=8,"Camera frustum stays within fully decorated workshop");
  var reload=new SaveSystem''')
p.write_text(s,encoding='utf-8')
p=Path('Assets/Workbench/Editor/CriticalChecks.cs');s=p.read_text(encoding='utf-8');needle='passed.Add("Rename migrates progress once and preserves old/current saves");'
s=s.replace(needle,needle+'var cameras=CameraDevices.Enumerate();if(!string.IsNullOrEmpty(CameraDevices.LastError))throw new Exception(CameraDevices.LastError);File.WriteAllText("TestResults/camera-enumeration.txt",string.Join("\\n",Array.ConvertAll(cameras,c=>c.index+": "+c.name)));passed.Add("Native Windows camera enumeration reads real names without opening devices");')
p.write_text(s,encoding='utf-8')
