from pathlib import Path
p=Path('Assets/Workbench/Runtime/GameRoot.cs');s=p.read_text(encoding='utf-8')
s=s.replace('public ArtCatalog art;','public ScreenMotion motion;public ArtCatalog art;')
s=s.replace('Bind("Builder/Back",()=>{StoreBuild();SetState(ScreenState.Menu);RefreshMenu();});','Bind("Builder/Back",()=>{StoreBuild();ToMenu();});')
s=s.replace('save.Data.settings.cameraDevice=(save.Data.settings.cameraDevice+1)%6;','recognition.NextCamera(save.Data.settings);')
s=s.replace('  Bind("Settings/Mirror"','  Bind("Settings/Controls",()=>{save.Data.settings.controlHints=!save.Data.settings.controlHints;RefreshSettings();});\n  Bind("Settings/Mirror"')
s=s.replace('  SetState(save.Data.introSeen','  motion.gameObject.SetActive(true);motion.lid.gameObject.SetActive(false);motion.loading.SetActive(false);\n  SetState(save.Data.introSeen')
s=s.replace('void FinishIntro(){save.Data.introSeen=true;save.Write();SetState(ScreenState.Menu);}','void FinishIntro(){save.Data.introSeen=true;save.Write();Navigate(()=>SetState(ScreenState.Menu));}\n void Navigate(Action action){CancelDrag();motion.Switch(action);}\n public bool Transitioning=>motion!=null&&motion.Busy;')
s=s.replace('public void OpenBuilder(){CancelDrag();','public void OpenBuilder(){Navigate(OpenBuilderNow);}\n void OpenBuilderNow(){CancelDrag();')
s=s.replace('void ToMenu(){CancelDrag();worldView.Clear();battle=null;bot=null;SetState(ScreenState.Menu);RefreshMenu();}','void ToMenu(){Navigate(()=>{worldView.Clear();battle=null;bot=null;SetState(ScreenState.Menu);RefreshMenu();});}')
s=s.replace('T("Settings/Camera/Label").Set("КАМЕРА: "+s.cameraDevice);','T("Settings/Camera/Label").Set("КАМЕРА: "+recognition.CameraName(s.cameraDevice));T("Settings/Controls/Label").Set("ПОДСКАЗКИ УПРАВЛЕНИЯ: "+(s.controlHints?"ВКЛ":"ВЫКЛ"));')
start=s.index(' public bool TryStartBattle()');end=s.index(' public void Command(',start)
old=s[start:end];new=old.replace('if(State!=ScreenState.Builder)','if(Transitioning||State!=ScreenState.Builder)').replace('StoreBuild();battle=new BattleModel','StoreBuild();Navigate(()=>{battle=new BattleModel').replace('T("Battle/BattleHelp").Set("WASD — движение   Мышь — прицел   ЛКМ — огонь   ПКМ — щит");return true;','RefreshHud();});return true;')
s=s[:start]+new+s[end:]
start=s.index(' void EndBattle()');end=s.index(' public static string FormatTime',start)
s=s[:start]+''' void EndBattle(){bool won=battle.winner==0;var level=data.levels[selectedLevel];earned=Stars.Evaluate(level,battle.robots[0],battle.robots[1],battle.time,won);Stars.Merge(save.Data.levels[selectedLevel].stars,earned);save.Data.tutorialDone|=won;save.Write();SetState(ScreenState.Result);
  string[] goals={"Победить","Победить за "+FormatTime(level.targetSeconds),level.goals[0].text,level.goals[1].text,level.goals[2].text};
  for(int star=0;star<5;star++){I("Result/Star"+star).sprite=art.Get("UI/Icons/"+(earned[star]?"star_gold":"star_empty"));T("Result/Goal"+star).Set(goals[star]);}
  T("Result/Level").Set((selectedLevel+1)+". "+level.title);T("Result/Title").Set(won?"ПОБЕДА!":battle.winner<0?"НИЧЬЯ":"ПОРАЖЕНИЕ");T("Result/Summary").Set("Время боя: "+FormatTime(battle.time));T("Result/Saved").Set("Всего звёзд уровня: "+save.Data.levels[selectedLevel].stars.Count(x=>x)+" / 5. Ранее полученные звёзды сохранены.");
  bool next=won&&selectedLevel<9;Obj("Result/Next").gameObject.SetActive(next);((RectTransform)Obj("Result/Retry")).anchoredPosition=new Vector2(next?544:720,-712);Sound(won?"victory":"defeat");
 }
''' +s[end:]
s=s.replace('if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)','if(!Transitioning&&keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)')
s=s.replace('State==ScreenState.Builder&&mouse!=null&&Time.unscaledTime>=inputBlockedUntil','State==ScreenState.Builder&&!Transitioning&&mouse!=null&&Time.unscaledTime>=inputBlockedUntil')
s=s.replace('if(State!=ScreenState.Battle||battle==null)return;','if(State!=ScreenState.Battle||battle==null||Transitioning)return;')
start=s.index(' void RefreshHud()');end=s.index(' Vector2 MousePoint()',start)
s=s[:start]+''' void RefreshHud(){var l=data.levels[selectedLevel];T("Battle/Timer").Set(FormatTime(battle.time));T("Battle/TargetTime").Set(FormatTime(l.targetSeconds));I("Battle/TargetStar").sprite=art.Get("UI/Icons/"+(battle.time<=l.targetSeconds?"star_gold":"star_empty"));
  for(int i=0;i<2;i++){var core=battle.robots[i].Core;Obj(i==0?"Battle/PlayerHP":"Battle/EnemyHP").GetComponent<HealthBar>().Set(core.hp/core.spec.hp,battle.time,i==1);}
  Obj("Battle/Controls").gameObject.SetActive(save.Data.settings.controlHints);var k=Keyboard.current;if(k!=null){bool[] down={k.wKey.isPressed,k.aKey.isPressed,k.sKey.isPressed,k.dKey.isPressed,k.escapeKey.isPressed};string[] names={"W","A","S","D","Esc"};for(int i=0;i<5;i++)I("Battle/Controls/Key"+i).sprite=art.Get("UI/Input/Keys/"+names[i]+(down[i]?"_down":"_up"));}}
''' +s[end:]
s=s.replace('var rect=new Rect(400+(i%6)*140,816+(i/6)*120,128,112);','var rect=BuilderLayout.InventorySlot(i,data.levels[selectedLevel]);')
s=s.replace('dragGhost.color=Construction.Fits(dragging,build,data)?','dragGhost.color=Construction.TryPlace(build,dragging,dragOriginal,data,out _)?')
s=s.replace('if(Construction.Fits(dragging,build,data)){build.Add(dragging.Copy());Sound("place");}','if(Construction.TryPlace(build,dragging,dragOriginal,data,out var exchanged)){build=exchanged;Sound("place");}')
s=s.replace('Место занято или за пределами поля. Деталь возвращена.','Для обмена не хватает места на поле. Деталь возвращена.')
start=s.index('T("Builder/Stats").Set(');end=s.index('\n  var s=data.modules',start)
s=s[:start]+'''float hp=build.Sum(p=>data.modules[p.type].hp),maxMass=0,maxHP=0;for(int i=0;i<9;i++){maxMass+=level.inventory[i]*data.modules[i].mass;maxHP+=level.inventory[i]*data.modules[i].hp;}
  string[] values={consumption+" / "+capacity,mass.ToString("0"),hp.ToString("0"),data.Speed(mass).ToString("0.0")};float[] ratios={capacity>0?(float)consumption/capacity:consumption>0?1:0,mass/Mathf.Max(1,maxMass),hp/Mathf.Max(1,maxHP),data.Speed(mass)/data.balance.speedMax};string[] barKeys={"energy","mass","hp","speed"};
  for(int i=0;i<4;i++){T("Builder/StatValue"+i).Set(values[i]);I("Builder/StatBar"+i).sprite=art.Get("UI/Bars/States/stat_"+barKeys[i]+"/step_"+Mathf.RoundToInt(Mathf.Clamp01(ratios[i])*12).ToString("00"));I("Builder/StatBar"+i).color=i==0&&consumption>capacity?new Color(1,.35f,.35f):Color.white;}
''' +s[end:]
start=s.index('  for(int i=0;i<9;i++){int count=');end=s.index('  var placed=',start)
s=s[:start]+'''  for(int i=0;i<9;i++){var slot=I("Builder/Slot"+i);slot.gameObject.SetActive(level.inventory[i]>0);if(level.inventory[i]<=0)continue;var rect=BuilderLayout.InventorySlot(i,level);slot.rectTransform.anchoredPosition=new Vector2(rect.x,-rect.y);
   int count=level.inventory[i]-build.Count(p=>p.type==i)-(dragging!=null&&dragging.type==i?1:0);var counter=I("Builder/Slot"+i+"/Count");counter.gameObject.SetActive(count>1);counter.sprite=art.Get("UI/Counters/Counts/count_"+Mathf.Max(0,count).ToString("00"));counter.SetNativeSize();counter.rectTransform.anchoredPosition=new Vector2(116-counter.rectTransform.sizeDelta.x,-88);
   slot.sprite=art.Get("UI/NineSlice/inventory_slot_"+(count<=0?"disabled":selectedType==i?"pressed":"normal"));slot.color=count<=0?new Color(.5f,.5f,.5f):Color.white;I("Builder/Slot"+i+"/Icon").color=count<=0?new Color(.25f,.25f,.25f):Color.white;
  }
''' +s[end:]
s=s.replace('ClearChildren(placed);var reachable=','ClearChildren(placed);DrawLinks(placed,build,false,64,new Vector2(800,244));var reachable=')
s=s.replace('RenderRobotPreview(Obj("Builder/EnemyRobot"),level.enemy,true,18)','RenderRobotPreview(Obj("Builder/EnemyRobot"),level.enemy,true,24)')
s=s.replace('if(save.Data.tutorialDone)hint="Уровень "+(selectedLevel+1)+": "+level.title+"\\n"+level.goals[0].text+" · "+level.goals[1].text;','if(save.Data.tutorialDone)hint="";')
start=s.index(' public void RenderRobotPreview(');end=s.index(' static void ClearChildren',start)
s=s[:start]+''' public void RenderRobotPreview(Transform parent,IEnumerable<Placement> parts,bool enemy,float cell){ClearChildren(parent);var list=parts.ToList();if(list.Count==0)return;float minX=list.Min(p=>p.x),minY=list.Min(p=>p.y),w=list.Max(p=>p.x+data.modules[p.type].width)-minX,h=list.Max(p=>p.y+data.modules[p.type].height)-minY;var rect=((RectTransform)parent).rect;cell=Mathf.Min(cell,Mathf.Floor(rect.width/w),Mathf.Floor(rect.height/h));Vector2 offset=new Vector2(Mathf.Round((rect.width-w*cell)/2)-minX*cell,Mathf.Round((rect.height-h*cell)/2)-minY*cell);
  DrawLinks(parent,list,enemy,cell,offset);foreach(var p in list){var spec=data.modules[p.type];var im=UiFactory.Image(parent,art,"Part"+p.id,"Modules/"+(enemy?"Enemy":"Player")+"/"+spec.key,offset.x+p.x*cell,offset.y+p.y*cell,spec.width*cell,spec.height*cell);if(spec.Weapon){float size=(spec.id==5||spec.id==6?2:1)*cell;UiFactory.Image(im.transform,art,"Gun","Turrets/"+(enemy?"Enemy":"Player")+"/"+spec.key+"/dir_00",(spec.width*cell-size)/2,(spec.height*cell-size)/2,size,size);}}
 }
 void DrawLinks(Transform parent,IList<Placement> parts,bool enemy,float cell,Vector2 offset){foreach(var link in Construction.Links(parts,data)){float w=(link.horizontal?24:12)*cell/64,h=(link.horizontal?12:24)*cell/64;UiFactory.Image(parent,art,"Connector","Connectors/"+(enemy?"enemy":"player")+"_"+(link.horizontal?"horizontal":"vertical"),offset.x+link.center.x*cell-w/2,offset.y+link.center.y*cell-h/2,w,h);}}
''' +s[end:]
s=s.replace('void HandPinch(bool down){if(down)','void HandPinch(bool down){if(Transitioning)return;if(down)')
p.write_text(s,encoding='utf-8')
