from pathlib import Path
p=Path('Assets/Workbench/Editor/ProjectBuilder.cs');s=p.read_text(encoding='utf-8').replace('Workbench Arena','Workbench')
s=s.replace('  art.entries=','  foreach(string key in new[]{"UI/Bars/Frames/hud","UI/Bars/Frames/builder","UI/Bars/Frames/world","UI/Loading/progress_track"})AssetDatabase.ImportAsset("Assets/RobotArenaArt/"+key+".png",ImportAssetOptions.ForceUpdate);\n  art.entries=')
p.write_text(s,encoding='utf-8')
# Keep the regeneration entry point current and idempotent.
p=Path('Tools/prepare_content.py');s=p.read_text(encoding='utf-8')
if '# Apply acceptance polish' not in s:s+='\n# Apply acceptance polish after regenerating the base review data.\nimport runpy\nrunpy.run_path(str(Path(__file__).with_name("apply_polish_content.py")))\n'
p.write_text(s,encoding='utf-8')
