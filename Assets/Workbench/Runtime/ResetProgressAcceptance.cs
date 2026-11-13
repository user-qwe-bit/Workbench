using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Workbench {
// Only explicitly enabled against an isolated save; never touches the player's progress.
public sealed class ResetProgressAcceptance:MonoBehaviour {
 GameRoot app;string dir;Keyboard keyboard;readonly List<string> checks=new List<string>();
 void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add("PASS "+text);File.WriteAllText(Path.Combine(dir,"reset.txt"),string.Join("\n",checks));}
 void Click(string path)=>app.Obj(path).GetComponent<Button>().onClick.Invoke();
 void Capture(string name){app.notice.SetActive(false);RuntimeSmoke.Capture(app,Path.Combine(dir,name+".png"));}
 IEnumerator Start(){app=GetComponent<GameRoot>();dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../TestResults/ResetProgress"));Directory.CreateDirectory(dir);var routine=Run();
  while(true){object current;try{if(!routine.MoveNext())break;current=routine.Current;}catch(Exception e){File.WriteAllText(Path.Combine(dir,"reset.txt"),string.Join("\n",checks)+"\nFAIL "+e);Debug.LogException(e);Application.Quit(1);yield break;}yield return current;}
  if(keyboard!=null)InputSystem.RemoveDevice(keyboard);File.WriteAllText(Path.Combine(dir,"reset.txt"),string.Join("\n",checks)+"\nCOMPLETE "+DateTime.UtcNow.ToString("O"));Application.Quit();
 }
 IEnumerator Run(){yield return null;Check(Path.GetFullPath(app.save.path)==Path.GetFullPath(Path.Combine(Application.dataPath,"../TestResults/ResetSave/workbench-save.json")),"Reset acceptance uses an isolated save");
  for(int i=0;i<10;i++){app.save.Data.levels[i].stars=new[]{true,true,true,true,true};app.save.Data.levels[i].build=new[]{new Placement{id=i,type=0,x=2,y=2}};app.save.Data.levels[i].arena=i%4;}
  app.save.Data.settings.effects=.6f;app.save.Data.introSeen=app.save.Data.tutorialDone=true;app.save.Data.hintsSeen[5]=true;Check(app.save.Write(),"Seeded 50 stars in test save");
  string original=JsonUtility.ToJson(app.save.Data);var expected=JsonUtility.FromJson<SaveData>(original);foreach(var level in expected.levels)level.stars=new bool[5];string cleared=JsonUtility.ToJson(expected);
  app.SetState(ScreenState.Menu);Click("Menu/Level0");Click("Menu/Settings");yield return new WaitForSecondsRealtime(.6f);Capture("settings_reset");Click("Settings/ResetProgress");yield return new WaitForSecondsRealtime(.6f);Capture("reset_confirmation");
  Check(app.State==ScreenState.ResetProgress&&JsonUtility.ToJson(app.save.Data)==original,"Opening reset asks for confirmation without changing progress");
  Click("ResetProgress/Cancel");Check(app.State==ScreenState.Settings&&JsonUtility.ToJson(new SaveSystem(Path.GetDirectoryName(app.save.path)).Data)==original,"Cancel keeps all progress on disk");
  Click("Settings/ResetProgress");yield return new WaitForSecondsRealtime(.6f);keyboard=InputSystem.AddDevice<Keyboard>("Reset test keyboard");InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return new WaitForSecondsRealtime(.1f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());
  Check(app.State==ScreenState.Settings&&JsonUtility.ToJson(app.save.Data)==original,"Escape cancels confirmation");
  Click("Settings/ResetProgress");yield return new WaitForSecondsRealtime(.6f);Click("ResetProgress/Confirm");
  Check(app.State==ScreenState.Settings&&JsonUtility.ToJson(app.save.Data)==cleared,"Confirm clears every star and preserves builds, settings and tutorial");
  Check(JsonUtility.ToJson(new SaveSystem(Path.GetDirectoryName(app.save.path)).Data)==cleared,"Reset remains cleared after loading save again");
  bool icons=true;for(int level=0;level<10;level++)for(int star=0;star<5;star++)icons&=app.Obj("Menu/Level"+level+"/Star"+star).GetComponent<Image>().sprite==app.art.Get("UI/Icons/star_empty");Check(icons,"All 50 menu star icons update immediately");
  Click("Settings/Close");yield return new WaitForSecondsRealtime(.6f);Capture("menu_reset");Check(app.State==ScreenState.Menu&&Enumerable.Range(0,10).All(i=>app.Obj("Menu/Level"+i).GetComponent<Button>().interactable),"Reset leaves all ten levels available");
  Click("Menu/Build");yield return new WaitForSecondsRealtime(1.5f);app.Command("начать бой");yield return new WaitForSecondsRealtime(1.5f);Check(app.State==ScreenState.Battle,"Preserved assembly still starts battle after reset");app.Pause();Click("Pause/Settings");yield return new WaitForSecondsRealtime(.6f);float before=app.battle.time;
  Click("Settings/ResetProgress");yield return new WaitForSecondsRealtime(.6f);Click("ResetProgress/Confirm");yield return new WaitForSecondsRealtime(.2f);Check(app.battle.paused&&app.battle.time==before,"Reset from pause keeps combat frozen");
 }
}
}
