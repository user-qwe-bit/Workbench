using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Workbench {
// Explicit command-line integration test, using the same gameplay actions as real input.
public sealed class RuntimeSmoke:MonoBehaviour {
 IEnumerator Start(){var app=GetComponent<GameRoot>();string dir=Path.Combine(Application.dataPath,"../TestResults/Runtime");Directory.CreateDirectory(dir);yield return null;
  app.selectedLevel=0;app.OpenBuilder();if(app.TryStartBattle()){} // Core-only is legal by the documented default.
  app.OpenBuilder();app.build.Clear();if(app.TryStartBattle())throw new Exception("Invalid assembly started");
  app.build.Add(new Placement{id=1,type=0,x=2,y=2});app.build.Add(new Placement{id=2,type=3,x=1,y=2});app.build.Add(new Placement{id=3,type=4,x=3,y=2});app.StoreBuild();app.OpenBuilder();yield return null;Capture(app,Path.Combine(dir,"builder.png"));
  app.SetState(ScreenState.Menu);yield return null;Capture(app,Path.Combine(dir,"menu.png"));
  app.SetState(ScreenState.Intro);yield return null;Capture(app,Path.Combine(dir,"intro.png"));app.OpenBuilder();
  app.Obj("Builder/Arena").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;Capture(app,Path.Combine(dir,"arenas.png"));app.SetState(ScreenState.Builder);
  app.Obj("Builder/Settings").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;Capture(app,Path.Combine(dir,"settings.png"));app.Obj("Settings/Close").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
  yield return new WaitForSecondsRealtime(.5f);if(!app.TryStartBattle())throw new Exception("Valid assembly rejected");yield return new WaitForSecondsRealtime(2);Capture(app,Path.Combine(dir,"battle.png"));app.Pause();float before=app.battle.time;yield return new WaitForSecondsRealtime(.5f);Capture(app,Path.Combine(dir,"pause.png"));if(app.battle.time!=before)throw new Exception("Pause advanced battle");app.Resume();yield return new WaitForSecondsRealtime(1);if(app.battle.time<=before)throw new Exception("Resume did not advance battle");
  File.WriteAllText(Path.Combine(dir,"smoke.txt"),"PASS: valid/invalid start, builder return, pause/resume, render captures.\n"+DateTime.UtcNow.ToString("O"));Application.Quit();
 }
 public static void Capture(GameRoot app,string path){
  // Render the live scene and its actual UI through Unity, even when Windows hides the test process.
  var camera=app.worldView.worldCamera;var target=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);
  var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;var previousMode=app.canvas.renderMode;var previousCamera=app.canvas.worldCamera;var previousRect=camera.rect;int previousOrder=app.canvas.sortingOrder;
  app.canvas.renderMode=RenderMode.ScreenSpaceCamera;app.canvas.worldCamera=camera;app.canvas.planeDistance=1;app.canvas.sortingOrder=1000;camera.rect=new Rect(0,0,1,1);camera.targetTexture=target;
  Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());Destroy(pixels);
  camera.targetTexture=previousTarget;camera.rect=previousRect;RenderTexture.active=previousActive;app.canvas.renderMode=previousMode;app.canvas.worldCamera=previousCamera;app.canvas.sortingOrder=previousOrder;RenderTexture.ReleaseTemporary(target);Canvas.ForceUpdateCanvases();
  if(new FileInfo(path).Length<10000)throw new Exception("Render capture is empty: "+path);
 }
}
}
