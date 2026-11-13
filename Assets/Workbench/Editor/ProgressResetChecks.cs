using System;
using System.IO;
using UnityEngine;
namespace Workbench.Editor {
public static class ProgressResetChecks {
 public static string Run(){
  string directory=Path.GetFullPath("TestResults/ProgressReset");var save=new SaveSystem(directory);
  for(int i=0;i<10;i++){save.Data.levels[i].stars=new[]{true,true,true,true,true};save.Data.levels[i].arena=i%4;save.Data.levels[i].build=new[]{new Placement{id=i,type=0,x=2,y=2}};}
  save.Data.settings.effects=.4f;save.Data.settings.controlHints=true;save.Data.settings.cameraDevice=2;save.Data.introSeen=save.Data.tutorialDone=true;save.Data.hintsSeen[5]=true;
  if(!save.Write())throw new Exception("Reset fixture write failed");
  var expected=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save.Data));foreach(var level in expected.levels)level.stars=new bool[5];string expectedJson=JsonUtility.ToJson(expected);
  if(!save.ResetProgress()||JsonUtility.ToJson(save.Data)!=expectedJson)throw new Exception("Reset changed unrelated data or retained stars");
  if(JsonUtility.ToJson(new SaveSystem(directory).Data)!=expectedJson)throw new Exception("Reset did not persist");
  if(!save.ResetProgress()||JsonUtility.ToJson(save.Data)!=expectedJson)throw new Exception("Repeated reset is not idempotent");
  return "Reset clears all 50 stars, persists, preserves builds/settings/tutorial and is idempotent";
 }
}
}
