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
// Explicit -workbench-smoke only, separate save directory, no operating-system input.
public sealed class RuntimeAcceptance:MonoBehaviour {
 GameRoot app;string dir;readonly List<string> checks=new List<string>();Mouse testMouse;Keyboard testKeyboard;
 void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add("PASS "+name);File.WriteAllText(Path.Combine(dir,"smoke.txt"),"RUNNING\n"+string.Join("\n",checks));}
 void Click(string path)=>app.Obj(path).GetComponent<Button>().onClick.Invoke();
 void Screenshot(string name){app.notice.SetActive(false);RuntimeSmoke.Capture(app,Path.Combine(dir,name+".png"));}
 Vector2 ScreenPoint(Vector2 point){var r=app.uiRoot;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(new Vector3(r.rect.xMin+point.x,r.rect.yMax-point.y)));}
 IEnumerator Pointer(Vector2 position,bool held){InputSystem.QueueStateEvent(testMouse,new MouseState{position=ScreenPoint(position),buttons=(ushort)(held?1:0)});yield return new WaitForSecondsRealtime(.08f);}
 IEnumerator Start(){app=GetComponent<GameRoot>();dir=Path.Combine(Application.dataPath,"../TestResults/Runtime");Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"smoke.txt"),"RUNNING "+DateTime.UtcNow.ToString("O"));
  var scenario=Scenario();while(true){object current;try{if(!scenario.MoveNext())break;current=scenario.Current;}catch(Exception e){File.WriteAllText(Path.Combine(dir,"smoke.txt"),string.Join("\n",checks)+"\nFAIL "+e);Debug.LogException(e);Application.Quit(1);yield break;}yield return current;}
  app.BeforeSimulationStep=null;InputSystem.RemoveDevice(testMouse);InputSystem.RemoveDevice(testKeyboard);File.WriteAllText(Path.Combine(dir,"smoke.txt"),string.Join("\n",checks)+"\nCOMPLETE "+DateTime.UtcNow.ToString("O"));Application.Quit();
 }
 IEnumerator Settled(){yield return null;while(app.Transitioning)yield return null;yield return new WaitForSecondsRealtime(.55f);}
 IEnumerator Scenario(){yield return null;testMouse=InputSystem.AddDevice<Mouse>("Workbench test mouse");testKeyboard=InputSystem.AddDevice<Keyboard>("Workbench test keyboard");
  Check(app.State==ScreenState.Intro,"Startup shows story even with previously completed onboarding");app.save.Data.settings.controlHints=false;Screenshot("intro");Click("Intro/Next");yield return null;Screenshot("intro2");Click("Intro/Next");yield return null;Screenshot("intro3");Click("Intro/Next");yield return Settled();Check(app.State==ScreenState.Menu,"Intro advances to level selection");
  Check(app.uiRoot.Find("Menu/Credits")==null,"Font attribution removed from visible menu");
  for(int i=0;i<10;i++){Click("Menu/Level"+i);Check(app.selectedLevel==i,"Level "+(i+1)+" open from start");}Click("Menu/Level0");Screenshot("menu");Click("Menu/Build");yield return new WaitForSecondsRealtime(.3f);Screenshot("loading");yield return Settled();
  Check(!string.IsNullOrEmpty(app.Obj("Builder/Hint").GetComponent<PixelText>().value),"Builder tutorial reappears despite saved completion");
  app.build.Clear();app.Command("начать бой");Check(app.State==ScreenState.Builder,"Voice cannot start empty assembly");Click("Builder/Start");Check(app.State==ScreenState.Builder,"Button cannot start empty assembly");
  app.build.Add(new Placement{id=0,type=0,x=2,y=2});app.build.Add(new Placement{id=1,type=4,x=3,y=2});app.Command("начать бой");Check(app.battle==null,"Voice cannot bypass insufficient power");
  app.build.RemoveAt(1);app.StoreBuild();app.OpenBuilder();yield return Settled();
  yield return Pointer(BuilderLayout.InventorySlot(3,app.data.levels[0]).center,true);yield return Pointer(new Vector2(896,404),false);Check(app.build.Any(p=>p.type==3&&p.x==1&&p.y==2),"Mouse drag places battery in grid");
  yield return Pointer(BuilderLayout.InventorySlot(4,app.data.levels[0]).center,true);yield return Pointer(new Vector2(1024,404),false);Check(app.build.Any(p=>p.type==4&&p.x==3&&p.y==2),"Mouse drag places weapon with correct footprint");
  int count=app.build.Count;app.BuilderGrab(new Vector2(896,404),true);Check(app.DragActive,"Hand action grabs existing part");app.CancelDrag();Check(app.build.Count==count&&app.build.Any(p=>p.type==3&&p.x==1),"Hand loss restores grabbed part without duplication");
  app.BuilderGrab(new Vector2(1024,404));app.BuilderRelease(new Vector2(960,404));Check(app.build.Count==count&&app.build.Any(p=>p.type==4&&p.x==2)&&app.build.Any(p=>p.type==0&&p.x==3),"Occupied cell swaps grid parts");app.BuilderGrab(new Vector2(960,404));app.BuilderRelease(new Vector2(1024,404));
  yield return Pointer(BuilderLayout.InventorySlot(1,app.data.levels[0]).center,true);yield return Pointer(new Vector2(1024,404),false);Check(app.build.Any(p=>p.type==1&&p.x==3)&&!app.build.Any(p=>p.type==4),"Inventory drop replaces weapon and returns it to stock");yield return Pointer(BuilderLayout.InventorySlot(4,app.data.levels[0]).center,true);yield return Pointer(new Vector2(1024,404),false);
  Check(!app.Obj("Builder/Slot5").gameObject.activeSelf&&app.Obj("Builder/Slot3/Icon").GetComponent<Image>().color.r<.4f&&!app.Obj("Builder/Slot2/Count").gameObject.activeSelf,"Inventory filters unavailable types, dims empty and hides single count");Screenshot("builder");
  for(int i=0;i<4;i++){Click("Builder/Arena");yield return Settled();if(i==0)Screenshot("arenas");Click("Arenas/Scheme"+i);Check(app.selectedArena==i,"Arena card selects scheme "+(i+1));app.Command("начать бой");yield return Settled();Check(app.State==ScreenState.Battle,"Voice starts valid build on scheme "+(i+1));yield return new WaitForSecondsRealtime(.25f);Screenshot("arena"+(i+1));app.Pause();Click("Pause/Retry");yield return Settled();Check(app.build.Count==count,"Retry restores original assembly on scheme "+(i+1));}
  Click("Builder/Settings");yield return Settled();Screenshot("settings");float volume=app.save.Data.settings.effects;Click("Settings/Volume");Click("Settings/Close");Check(app.State==ScreenState.Builder&&app.save.Data.settings.effects!=volume,"Settings save and return to builder");
  Check(app.recognition.CameraName(-1)=="ПО УМОЛЧАНИЮ"&&string.IsNullOrEmpty(CameraDevices.LastError),"Camera selector offers default and enumerates real devices without error");
  app.selectedArena=0;app.Command("начать бой");yield return Settled();Check(!app.Obj("Battle/Controls").gameObject.activeSelf,"Combat control hints hidden by default");Check(app.uiRoot.Find("Battle/TargetStar")==null&&app.uiRoot.Find("Battle/TargetTime")==null&&app.Obj("Battle/Timer").GetComponent<PixelText>().scale==4,"HUD shows only one large elapsed timer");var original=Construction.Copy(app.build);Vector2 position=app.battle.robots[0].position;
  InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.D));yield return new WaitForSecondsRealtime(.2f);InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());Check(app.battle.robots[0].position.x>position.x+.1f,"Keyboard movement reaches actual simulation");
  yield return new WaitForSecondsRealtime(.3f);var controlled=app.battle.robots[0];position=controlled.position;
  InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.A,Key.D,Key.W));yield return new WaitForSecondsRealtime(.35f);InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());
  Check(Mathf.Abs(controlled.position.x-position.x)<.03f&&controlled.position.y<position.y-.5f,"Opposite horizontal keys cancel while W moves screen-up");yield return new WaitForSecondsRealtime(.35f);
  Check(controlled.velocity.sqrMagnitude<.0001f,"Released keyboard stops without invisible creeping");
  var wheel=app.worldView.GetComponentsInChildren<ModuleVisual>().First(v=>v.gameObject.name.StartsWith("Player "));var stoppedSprite=wheel.wheelLeft.sprite;yield return new WaitForSecondsRealtime(.1f);Check(wheel.wheelLeft.sprite==stoppedSprite,"Stopped wheels freeze their animation phase");
  Vector2 aim=app.battle.robots[1].Center;Vector2 screen=app.worldView.worldCamera.WorldToScreenPoint(WorldView.World(aim));InputSystem.QueueStateEvent(testMouse,new MouseState{position=screen,buttons=1});yield return new WaitForSecondsRealtime(.7f);InputSystem.QueueStateEvent(testMouse,new MouseState{position=screen});Check(app.battle.robots[0].modules.Where(m=>m.spec.Weapon).Sum(m=>m.shots)>0,"Mouse aiming and firing use moving world camera");Screenshot("battle");
  app.Command("пауза");float before=app.battle.time;var projectilePositions=app.battle.projectiles.Select(p=>p.position).ToArray();yield return new WaitForSecondsRealtime(.5f);Check(app.battle.time==before&&app.battle.projectiles.Select(p=>p.position).SequenceEqual(projectilePositions),"Voice pause freezes simulation and projectiles");Screenshot("pause");
  Click("Pause/Settings");yield return Settled();Check(app.battle.time==before,"Settings during pause leave battle frozen");Click("Settings/Controls");Click("Settings/Close");app.Command("продолжить");yield return new WaitForSecondsRealtime(.2f);Check(app.battle.time>before,"Voice resume restores simulation");Check(app.Obj("Battle/Controls").gameObject.activeSelf,"Control hints setting displays control panel");
  app.Pause();Click("Pause/Retry");yield return Settled();Check(app.build.Select(p=>p.id).SequenceEqual(original.Select(p=>p.id)),"Combat damage never changes pre-battle assembly");
  // Complete matches use only ordinary input fields, without injecting HP or an outcome.
  app.build=new List<Placement>{new Placement{id=0,type=0,x=2,y=2},new Placement{id=1,type=3,x=2,y=3},new Placement{id=2,type=4,x=2,y=4},new Placement{id=3,type=1,x=2,y=1}};app.StoreBuild();app.OpenBuilder();yield return Settled();app.Command("начать бой");yield return Settled();
  var pilot=new BotController(app.battle,new LevelSpec{reaction=.12f,aimError=.08f},0,71);app.BeforeSimulationStep=b=>pilot.Tick();float deadline=Time.realtimeSinceStartup+180;
  while(app.State==ScreenState.Battle&&Time.realtimeSinceStartup<deadline)yield return null;
  Check(app.State==ScreenState.Result,"Real match reaches result screen");yield return Settled();Screenshot(app.battle.winner==0?"victory":"defeat");Check(app.battle.winner==0,"Compact assembly wins full Windows match");Check(app.save.Data.levels[0].stars[0],"Victory star persisted by result flow");app.BeforeSimulationStep=null;
  Click("Result/Retry");yield return Settled();app.build=new List<Placement>{new Placement{id=0,type=0,x=2,y=2}};app.StoreBuild();app.OpenBuilder();yield return Settled();app.Command("начать бой");yield return Settled();deadline=Time.realtimeSinceStartup+180;while(app.State==ScreenState.Battle&&Time.realtimeSinceStartup<deadline)yield return null;
  Check(app.State==ScreenState.Result&&app.battle.winner==1,"Player core destruction reaches defeat screen");yield return Settled();Screenshot("defeat");Check(app.save.Data.levels[0].stars[0],"Defeat preserves earned stars");Click("Result/Menu");yield return Settled();Check(app.State==ScreenState.Menu,"Result returns to menu");
  Click("Menu/Level9");Click("Menu/Build");yield return Settled();Check(app.notice.activeSelf,"Special module tutorial repeats in a new session");Check(string.IsNullOrEmpty(app.Obj("Builder/Hint").GetComponent<PixelText>().value),"Basic tutorial completes after a win within this session");Screenshot("builder10");
  app.selectedLevel=0;app.OpenBuilder();yield return Settled();app.Command("начать бой");yield return Settled();app.Pause();
  app.battle.robots[0].position=new Vector2(.3f,.3f);app.battle.robots[1].position=new Vector2(57,28);foreach(var robot in app.battle.robots)robot.previousPosition=robot.position;yield return new WaitForSecondsRealtime(1);Screenshot("camera_edges");var camera=app.worldView.worldCamera;float halfW=camera.orthographicSize*16/9;Check(camera.transform.position.x-halfW>=-8&&camera.transform.position.x+halfW<=68&&camera.transform.position.y-camera.orthographicSize>=-40&&camera.transform.position.y+camera.orthographicSize<=8,"Camera frustum stays within fully decorated workshop");
  bool fits=true;foreach(var robot in app.battle.robots){var rect=robot.Bounds;foreach(var p in new[]{rect.min,rect.max,new Vector2(rect.xMin,rect.yMax),new Vector2(rect.xMax,rect.yMin)}){var viewport=camera.WorldToViewportPoint(WorldView.World(p));fits&=viewport.x>.02f&&viewport.x<.98f&&viewport.y>.02f&&viewport.y<.98f;}}Check(fits,"Camera holds complete robot footprints with edge margin");
  var reload=new SaveSystem(Path.GetDirectoryName(app.save.path));Check(reload.Data.levels[0].stars[0]&&reload.Data.levels[0].build.Length==1&&reload.Data.settings.effects==app.save.Data.settings.effects,"Player save reload preserves stars, build and settings");
 }
}
}
