from pathlib import Path
p=Path('Assets/Workbench/Editor/CriticalChecks.cs');s=p.read_text(encoding='utf-8');needle='passed.Add("Save/load stars, assembly and settings roundtrip");'
addition='''
var oldSave=Path.GetFullPath("TestResults/SaveMigration/Old/workbench-save.json");var newSave=Path.GetFullPath("TestResults/SaveMigration/New/workbench-save.json");Directory.CreateDirectory(Path.GetDirectoryName(oldSave));Directory.CreateDirectory(Path.GetDirectoryName(newSave));File.WriteAllText(oldSave,JsonUtility.ToJson(save.Data));if(File.Exists(newSave))File.Delete(newSave);if(!SaveSystem.Migrate(oldSave,newSave))throw new Exception("Migration did not copy");var migrated=new SaveSystem(Path.GetDirectoryName(newSave));if(!migrated.Data.levels[9].stars[4]||migrated.Data.levels[9].build[0].id!=8||!File.Exists(oldSave))throw new Exception("Migration lost progress or source");File.WriteAllText(oldSave,"invalid");if(SaveSystem.Migrate(oldSave,newSave)||!new SaveSystem(Path.GetDirectoryName(newSave)).Data.levels[9].stars[4])throw new Exception("Migration overwrote current save");passed.Add("Rename migrates progress once and preserves old/current saves");
'''
if 'Rename migrates progress' not in s:s=s.replace(needle,needle+addition)
p.write_text(s,encoding='utf-8')
