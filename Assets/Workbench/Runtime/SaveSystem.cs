using System;
using System.IO;
using UnityEngine;
namespace Workbench {
[Serializable] public class LevelSave { public bool[] stars=new bool[5]; public Placement[] build;public int arena; }
[Serializable] public class SettingsData {public float effects=.7f;public bool voice,hand,controlHints;public int cameraDevice=-1,micDevice=-1;public bool mirror=true;public float pinch=.28f,smoothing=.4f; }
[Serializable] public class SaveData {public int version=1;public bool introSeen,tutorialDone;public bool[] hintsSeen=new bool[9];public LevelSave[] levels=new LevelSave[10];public SettingsData settings=new SettingsData();public SaveData(){for(int i=0;i<10;i++)levels[i]=new LevelSave{arena=i%4};}}
public sealed class SaveSystem {
 public readonly string path;public SaveData Data{get;private set;}public string Warning{get;private set;}="";
 public SaveSystem(string directory=null){path=Path.Combine(directory??Application.persistentDataPath,"workbench-save.json");if(directory==null&&!File.Exists(path)){string previous=Path.Combine(Path.GetDirectoryName(Application.persistentDataPath),"Workbench Arena","workbench-save.json");Migrate(previous,path);}Load();}
 public static bool Migrate(string previous,string destination){if(File.Exists(destination)||!File.Exists(previous)||Path.GetFullPath(previous)==Path.GetFullPath(destination))return false;try{Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(previous,destination);if(File.Exists(previous+".bak"))File.Copy(previous+".bak",destination+".bak",false);return true;}catch(Exception e){Debug.LogWarning("Save migration: "+e.Message);return false;}}
 void Load(){Data=new SaveData();if(!File.Exists(path))return;try{Data=JsonUtility.FromJson<SaveData>(File.ReadAllText(path));Validate();}catch(Exception e){Warning="Сохранение повреждено. Загружена резервная копия, если доступна.";Debug.LogWarning(e.Message);try{Data=JsonUtility.FromJson<SaveData>(File.ReadAllText(path+".bak"));Validate();}catch{Data=new SaveData();}}}
 void Validate(){if(Data==null||Data.version!=1||Data.levels==null||Data.levels.Length!=10)throw new InvalidDataException();if(Data.settings==null)Data.settings=new SettingsData();if(Data.hintsSeen==null||Data.hintsSeen.Length!=9)Data.hintsSeen=new bool[9];for(int i=0;i<10;i++){if(Data.levels[i]==null)Data.levels[i]=new LevelSave();if(Data.levels[i].stars==null||Data.levels[i].stars.Length!=5)Data.levels[i].stars=new bool[5];Data.levels[i].arena=Mathf.Clamp(Data.levels[i].arena,0,3);}Data.settings.effects=Mathf.Clamp01(Data.settings.effects);}
 public bool ResetProgress(){
  var previous=new bool[Data.levels.Length][];
  for(int i=0;i<Data.levels.Length;i++){previous[i]=Data.levels[i].stars;Data.levels[i].stars=new bool[5];}
  if(Write())return true;
  for(int i=0;i<Data.levels.Length;i++)Data.levels[i].stars=previous[i];
  return false;
 }
 public bool Write(){try{Directory.CreateDirectory(Path.GetDirectoryName(path));string tmp=path+".tmp";File.WriteAllText(tmp,JsonUtility.ToJson(Data,true));if(File.Exists(path))File.Replace(tmp,path,path+".bak");else File.Move(tmp,path);return true;}catch(Exception e){Warning="Не удалось сохранить: "+e.Message;Debug.LogError(Warning);return false;}}
}
}
