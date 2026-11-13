from pathlib import Path
p=Path('Assets/Workbench/Runtime/UiFactory.cs');s=p.read_text(encoding='utf-8')
s=s.replace('WORKBENCH ARENA','WORKBENCH')
s=s.replace('highlightedSprite=art.Get("UI/NineSlice/"+family+"_hover")','highlightedSprite=art.Get("UI/NineSlice/"+family+"_normal")')
s=s.replace('b.targetGraphic=image;','b.targetGraphic=image;b.navigation=new Navigation{mode=Navigation.Mode.None};')
s=s.replace('Rect(b.transform,"Robot",80,72,180,120)','Rect(b.transform,"Robot",12,68,320,124)')
s=s.replace('Panel(builder,a,"StatsPanel",16,388,352,392);Text(builder,a,"Stats","",40,416,304,340,2);','''Panel(builder,a,"StatsPanel",16,388,352,392);
  string[] statNames={"ЭНЕРГИЯ","МАССА","ПРОЧНОСТЬ","СКОРОСТЬ"},statIcons={"energy","mass","heart","speed"},statKeys={"energy","mass","hp","speed"};
  for(int i=0;i<4;i++){float y=408+i*88;Image(builder,a,"StatIcon"+i,"UI/Icons/"+statIcons[i],32,y-8,64,64);Text(builder,a,"StatLabel"+i,statNames[i],96,y,248,28,1);Text(builder,a,"StatValue"+i,"",96,y+22,248,32,2);Image(builder,a,"StatBar"+i,"UI/Bars/States/stat_"+statKeys[i]+"/step_00",48,y+56,280,24);}''')
s=s.replace('for(int y=0;y<5;y++)for(int x=0;x<5;x++)Image(builder,a,"Grid"+x+"_"+y,"UI/Builder/mat_grid",800+x*64,244+y*64,64,64);','for(int y=0;y<10;y++)for(int x=0;x<23;x++)Image(builder,a,"Grid"+x+"_"+y,"UI/Builder/mat_grid",400+x*64,124+y*64,64,64);')
s=s.replace('1516,616,380,164','1516,608,380,164').replace('1536,628,340,32','1536,620,340,32').replace('Rect(builder,"EnemyRobot",1640,674,180,96)','Rect(builder,"EnemyRobot",1536,656,340,108)')
s=s.replace('float x=400+(i%6)*140,y=816+(i/6)*120;var slot=Image(builder,a,"Slot"+i,"UI/NineSlice/inventory_slot_normal",x,y,128,112,true);Image(slot.transform,a,"Icon","UI/Builder/ModuleIcons/"+root.data.modules[i].key,32,8,64,64);Image(slot.transform,a,"Count","UI/Counters/Counts/count_00",56,76,68,28);','float x=408+(i%6)*136,y=812+(i/6)*124;var slot=Image(builder,a,"Slot"+i,"UI/NineSlice/inventory_slot_normal",x,y,112,112,true);Image(slot.transform,a,"Icon","UI/Builder/ModuleIcons/"+root.data.modules[i].key,8,8,96,96);Image(slot.transform,a,"Count","UI/Counters/Counts/count_00",56,84,68,28);')
start=s.index('  var hud=');end=s.index('  var pause=',start)
s=s[:start]+'''  var hud=Rect(canvas,"Battle",0,0,1920,1080);
  Panel(hud,a,"Top",800,12,320,88);Text(hud,a,"Timer","",816,24,288,32,2,true);Image(hud,a,"TargetStar","UI/Icons/star_gold",864,52,40,40);Text(hud,a,"TargetTime","",912,58,144,32,2);
  Button(hud,a,"Pause","ПАУЗА",1660,996,240,64);
  Health(hud,a,"PlayerHP",16,false);Health(hud,a,"EnemyHP",1376,true);
  var controls=Rect(hud,"Controls",0,0,1920,1080);Panel(controls,a,"Background",16,988,1576,80,"dark_panel");Text(controls,a,"BattleHelp","Мышь — прицел   ЛКМ — огонь   ПКМ — щит",410,1012,1160,48,2,true,true);for(int i=0;i<5;i++)Image(controls,a,"Key"+i,"UI/Input/Keys/"+new[]{"W","A","S","D","Esc"}[i]+"_up",24+i*70,996,64,64);
  var result=Rect(canvas,"Result",0,0,1920,1080);Panel(result,a,"Panel",480,128,960,824);
  Text(result,a,"Level","",520,164,880,40,2,true);Text(result,a,"Title","",520,220,880,80,4,true);Text(result,a,"Summary","",544,308,832,40,2,true);
  for(int star=0;star<5;star++){Image(result,a,"Star"+star,"UI/Icons/star_empty",544,366+star*56,48,48);Text(result,a,"Goal"+star,"",608,374+star*56,760,48,2);}
  Text(result,a,"Saved","",544,656,832,32,1,true);Button(result,a,"Retry","ПОВТОРИТЬ",720,712,480,80,true);Button(result,a,"Next","СЛЕДУЮЩИЙ",996,712,380,80);Button(result,a,"Menu","ВЫБОР УРОВНЯ",720,824,480,64);
''' +s[end:]
s=s.replace('Text(settings,a,"Status","",400,680,1120,180,2)','Text(settings,a,"Status","",400,704,1120,150,1)')
s=s.replace('Button(settings,a,"Close","ГОТОВО"','Button(settings,a,"Controls","",400,624,1120,64);Button(settings,a,"Close","ГОТОВО"')
s=s.replace('mask.raycastTarget=true;} root.dragGhost','mask.raycastTarget=true;canvas.Find(modal).gameObject.AddComponent<PopupMotion>();} root.dragGhost')
s=s.replace('root.handCursor=Image(canvas,a,"HandCursor","UI/Input/gesture_open",0,0,64,64);','''root.handCursor=Image(canvas,a,"HandCursor","UI/Input/gesture_open",0,0,64,64);
  var transition=Rect(canvas,"Transition",0,0,1920,1080);root.motion=transition.gameObject.AddComponent<ScreenMotion>();root.motion.art=a;root.motion.lid=Image(transition,a,"Lid","UI/Transitions/Close/frame_08",0,0,1920,1080);root.motion.lid.raycastTarget=true;
  var loading=Rect(transition,"Loading",0,0,1920,1080);root.motion.loading=loading.gameObject;Image(loading,a,"Mat","UI/Loading/mat_patch",576,344,768,288);root.motion.robot=Image(loading,a,"Robot","UI/Loading/Robot/frame_00",768,344,384,256);root.motion.gear=Image(loading,a,"Gear","UI/Loading/Gear/frame_00",928,676,64,64);Text(loading,a,"Title","WORKBENCH",480,180,960,96,4,true,true);Text(loading,a,"Preparing","ПОДГОТОВКА",640,752,640,40,2,true,true);Image(loading,a,"Track","UI/Loading/progress_track",704,816,512,40,true);root.motion.progress=Image(loading,a,"Progress","UI/Bars/Fills/player",712,824,496,24);root.motion.progress.type=UnityEngine.UI.Image.Type.Filled;root.motion.progress.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;''')
pos=s.index(' public static void Build(')
s=s[:pos]+''' public static void Health(Transform parent,ArtCatalog a,string name,float x,bool enemy){
  var panel=Rect(parent,name,x,16,528,80);Panel(panel,a,"Panel",0,0,528,80);Image(panel,a,"Core","Modules/"+(enemy?"Enemy":"Player")+"/core",enemy?448:16,8,64,64);
  var track=Image(panel,a,"Track","UI/Bars/Frames/hud",enemy?24:96,24,408,32,true);var bar=panel.gameObject.AddComponent<HealthBar>();bar.art=a;
  bar.damage=Image(track.transform,a,"Damage","UI/Bars/Fills/damage",4,4,400,24);bar.fill=Image(track.transform,a,"Fill","UI/Bars/Fills/"+(enemy?"enemy":"player"),4,4,400,24);
  foreach(var image in new[]{bar.damage,bar.fill}){image.type=UnityEngine.UI.Image.Type.Filled;image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;}
  bar.flash=Image(track.transform,a,"Flash","UI/Bars/Effects/hit_flash/frame_00",0,0,408,32);bar.flash.enabled=false;
 }
''' +s[pos:]
p.write_text(s,encoding='utf-8')
