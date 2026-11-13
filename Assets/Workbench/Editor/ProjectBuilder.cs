using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace Workbench.Editor {
public static class ProjectBuilder {
 const string Folder="Assets/Workbench/";
 [MenuItem("Workbench/Prepare project and scenes")]
 public static void Prepare(){
  Directory.CreateDirectory(Folder+"Generated");Directory.CreateDirectory(Folder+"Prefabs");Directory.CreateDirectory(Folder+"Scenes");
  var art=AssetDatabase.LoadAssetAtPath<ArtCatalog>(Folder+"Generated/ArtCatalog.asset");if(!art){art=ScriptableObject.CreateInstance<ArtCatalog>();AssetDatabase.CreateAsset(art,Folder+"Generated/ArtCatalog.asset");}
  foreach(string file in Directory.GetFiles("Assets/RobotArenaArt","*.png",SearchOption.AllDirectories)){var path=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)continue;if(importer.filterMode!=FilterMode.Point||importer.spritePixelsPerUnit!=64||!path.Contains("/Fonts/")&&importer.textureType!=TextureImporterType.Sprite){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);}}
  foreach(string key in new[]{"UI/Bars/Frames/hud","UI/Bars/Frames/builder","UI/Bars/Frames/world","UI/Loading/progress_track"})AssetDatabase.ImportAsset("Assets/RobotArenaArt/"+key+".png",ImportAssetOptions.ForceUpdate);
  art.entries=Directory.GetFiles("Assets/RobotArenaArt","*.png",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')).Where(p=>!p.Contains("/Fonts/")).Select(p=>new ArtCatalog.Entry{key=p.Replace("Assets/RobotArenaArt/","").Replace(".png",""),sprite=AssetDatabase.LoadAssetAtPath<Sprite>(p)}).ToArray();
  if(art.entries.Any(e=>!e.sprite))throw new Exception("Some source sprites did not import");
  art.sounds=Directory.GetFiles(Folder+"Audio","*.wav").Select(p=>new ArtCatalog.Sound{key=Path.GetFileNameWithoutExtension(p),clip=AssetDatabase.LoadAssetAtPath<AudioClip>(p)}).ToArray();art.fontAtlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RobotArenaArt/Fonts/m6x11cyrillic_16.png");art.fontJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/RobotArenaArt/Fonts/m6x11cyrillic_16.json");EditorUtility.SetDirty(art);
  var data=GameData.Load();var module=CreateModule(art);var arenas=new GameObject[4];for(int i=0;i<4;i++)arenas[i]=CreateArena(data,art,i);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var cameraObject=new GameObject("Camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=8.4375f;camera.backgroundColor=new Color32(86,57,41,255);camera.clearFlags=CameraClearFlags.SolidColor;camera.transform.position=new Vector3(30,-16,-10);camera.allowHDR=false;camera.allowMSAA=false;
  var game=new GameObject("Workbench",typeof(GameRoot),typeof(AudioSource),typeof(InputRecognition));var root=game.GetComponent<GameRoot>();root.art=art;root.data=data;root.audioSource=game.GetComponent<AudioSource>();root.audioSource.playOnAwake=false;root.recognition=game.GetComponent<InputRecognition>();
  var world=new GameObject("World",typeof(WorldView));root.worldView=world.GetComponent<WorldView>();root.worldView.art=art;root.worldView.worldCamera=camera;root.worldView.modulePrefab=module;root.worldView.arenaPrefabs=arenas;
  var ui=new GameObject("Interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.canvas=ui.GetComponent<Canvas>();root.canvas.renderMode=RenderMode.ScreenSpaceOverlay;root.canvas.referencePixelsPerUnit=64;root.canvas.pixelPerfect=true;var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;scaler.referencePixelsPerUnit=64;
  root.uiRoot=UiFactory.Rect(ui.transform,"Viewport",0,0,1920,1080);root.uiRoot.anchorMin=root.uiRoot.anchorMax=new Vector2(.5f,.5f);root.uiRoot.anchoredPosition=new Vector2(-960,540);new GameObject("Input events",typeof(EventSystem),typeof(InputSystemUIInputModule));UiFactory.Build(root);foreach(Transform screen in root.uiRoot)screen.gameObject.SetActive(screen.name=="Intro");
  EditorSceneManager.SaveScene(scene,Folder+"Scenes/WorkbenchArena.unity");
  PrefabUtility.SaveAsPrefabAsset(ui,Folder+"Prefabs/Interface.prefab");
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Folder+"Scenes/WorkbenchArena.unity",true)};
  PlayerSettings.companyName="Workbench";PlayerSettings.productName="Workbench";PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;PlayerSettings.resizableWindow=true;PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);PlayerSettings.colorSpace=ColorSpace.Gamma;
  QualitySettings.antiAliasing=0;QualitySettings.vSyncCount=0;GraphicsSettingsFix();AssetDatabase.SaveAssets();CriticalChecks.CheckImportedArt();
  File.WriteAllText("TestResults/project-prepared.txt",DateTime.UtcNow.ToString("O")+"\n9 module definitions, 10 levels, 4 arena prefabs, module prefab, full interface prefab and scene.\n");Debug.Log("Workbench project prepared.");
 }
 static void GraphicsSettingsFix(){UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=null;}QualitySettings.SetQualityLevel(0,false);}
 static SpriteRenderer SR(Transform parent,string name,Sprite sprite,int order,Vector2 position,Vector2? size=null){var g=new GameObject(name,typeof(SpriteRenderer));g.transform.SetParent(parent,false);g.transform.localPosition=WorldView.World(position);var sr=g.GetComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;if(size.HasValue){sr.drawMode=SpriteDrawMode.Tiled;sr.size=size.Value;}return sr;}
 static GameObject CreateModule(ArtCatalog art){var go=new GameObject("Module",typeof(ModuleVisual),typeof(BoxCollider2D));var m=go.GetComponent<ModuleVisual>();m.footprint=go.GetComponent<BoxCollider2D>();m.footprint.isTrigger=true;m.body=SR(go.transform,"Foundation",art.Get("Modules/Player/core"),10,Vector2.zero);m.wheelLeft=SR(go.transform,"Wheel left",null,9,Vector2.zero);m.wheelRight=SR(go.transform,"Wheel right",null,9,Vector2.zero);m.damage=SR(go.transform,"Damage",null,11,Vector2.zero);m.turret=SR(go.transform,"Turret",null,12,Vector2.zero);m.indicator=SR(go.transform,"Indicator",null,14,Vector2.zero);m.shieldField=SR(go.transform,"Directional shield",null,28,Vector2.zero);var prefab=PrefabUtility.SaveAsPrefabAsset(go,Folder+"Prefabs/Module.prefab");UnityEngine.Object.DestroyImmediate(go);return prefab;}
 static GameObject CreateArena(GameData data,ArtCatalog art,int index){var go=new GameObject("Arena "+(index+1)+" — "+data.arenas[index].title);var floor=new GameObject("Workshop floor");floor.transform.SetParent(go.transform);
  for(int y=-8;y<40;y++)for(int x=-8;x<68;x++){if(x>=0&&x<60&&y>=0&&y<32)continue;int ix=Mathf.Clamp(x+4,0,67),iy=Mathf.Clamp(y+3,0,38),v=data.floor[iy].values[ix];SR(floor.transform,"Floor",art.Get("Environment/Variants/floor_"+v.ToString("00")),0,new Vector2(x+.5f,y+.5f));}
  var table=new GameObject("Arena surface");table.transform.SetParent(go.transform);for(int y=0;y<32;y++)for(int x=0;x<60;x++)SR(table.transform,"Surface",art.Get("Environment/Variants/arena_"+data.surface[y].values[x].ToString("00")),1,new Vector2(x+.5f,y+.5f));
  foreach(var p in data.props)SR(go.transform,p.id+" "+p.name,art.Get(p.path),3+p.layer,new Vector2((p.x-256+p.width*.5f)/64,(p.y-192+p.height*.5f)/64));
  foreach(var p in data.markings){var sprite=art.Get("Environment/Markings/"+p.name);var sr=SR(go.transform,"Marking",sprite,2,new Vector2((p.x-256+sprite.rect.width*.5f)/64,(p.y-192+sprite.rect.height*.5f)/64));sr.transform.rotation=Quaternion.Euler(0,0,-p.rotation);}
  for(int x=0;x<60;x++){SR(go.transform,"Rim top",art.Get("Environment/Rim/top"),4,new Vector2(x+.5f,-.5f));SR(go.transform,"Rim bottom",art.Get("Environment/Rim/bottom"),4,new Vector2(x+.5f,32.5f));}
  for(int y=0;y<32;y++){SR(go.transform,"Rim left",art.Get("Environment/Rim/left"),4,new Vector2(-.5f,y+.5f));SR(go.transform,"Rim right",art.Get("Environment/Rim/right"),4,new Vector2(60.5f,y+.5f));}
  string[] corners={"tl","tr","bl","br"};Vector2[] points={new Vector2(-.5f,-.5f),new Vector2(60.5f,-.5f),new Vector2(-.5f,32.5f),new Vector2(60.5f,32.5f)};for(int i=0;i<4;i++)SR(go.transform,"Rim "+corners[i],art.Get("Environment/Rim/"+corners[i]),4,points[i]);
  foreach(var w in data.arenas[index].walls){var sr=SR(go.transform,"Wall "+w.x+","+w.y,art.Get("Environment/Walls/Connected/mask_"+w.mask.ToString("000")),5,new Vector2(w.x+.5f,w.y+.5f));sr.gameObject.AddComponent<BoxCollider2D>().size=Vector2.one;}
  var result=PrefabUtility.SaveAsPrefabAsset(go,Folder+"Prefabs/Arena"+(index+1)+".prefab");UnityEngine.Object.DestroyImmediate(go);return result;
 }
 [MenuItem("Workbench/Build Windows x64")]
 public static void BuildWindows(){Prepare();CriticalChecks.Run();Directory.CreateDirectory("Builds/Windows");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Select(s=>s.path).ToArray(),locationPathName="Builds/Windows/Workbench.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});File.WriteAllText("TestResults/windows-build.txt",report.summary.result+"\n"+report.summary.totalErrors+" errors\n"+report.summary.totalSize+" bytes\n");if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed");}
}
}
