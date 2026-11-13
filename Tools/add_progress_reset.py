from pathlib import Path
p=Path('Assets/Workbench/Runtime/GameRoot.cs');s=p.read_text(encoding='utf-8')
s=s.replace('Environment.GetCommandLineArgs().Contains("-workbench-smoke")?System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../TestResults/SmokeSave")):null','Environment.GetCommandLineArgs().Contains("-workbench-reset-check")?System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../TestResults/ResetSave")):Environment.GetCommandLineArgs().Contains("-workbench-smoke")?System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../TestResults/SmokeSave")):null')
s=s.replace('if(args.Contains("-workbench-smoke"))gameObject.AddComponent<RuntimeAcceptance>();','if(args.Contains("-workbench-smoke"))gameObject.AddComponent<RuntimeAcceptance>();if(args.Contains("-workbench-reset-check"))gameObject.AddComponent<ResetProgressAcceptance>();')
s=s.replace('else if(State==ScreenState.Settings){save.Write();','else if(State==ScreenState.ResetProgress)SetState(ScreenState.Settings);else if(State==ScreenState.Settings){save.Write();')
p.write_text(s,encoding='utf-8')
p=Path('Assets/Workbench/Runtime/UiFactory.cs');s=p.read_text(encoding='utf-8').replace('Button(settings,a,"Close","ГОТОВО",740,872,440,72,true);','Button(settings,a,"ResetProgress","СБРОСИТЬ ПРОГРЕСС",400,872,536,72);Button(settings,a,"Close","ГОТОВО",984,872,536,72,true);')
start=s.index('  var notice=')
s=s[:start]+'''  var reset=Rect(canvas,"ResetProgress",0,0,1920,1080);Panel(reset,a,"Panel",520,288,880,504);Text(reset,a,"Title","СБРОСИТЬ ПРОГРЕСС?",560,332,800,64,3,true);
  Text(reset,a,"Description","Все полученные звёзды на 10 уровнях будут удалены.\\n\\nСборки роботов и настройки сохранятся.",592,424,736,160,2,true);
  Button(reset,a,"Cancel","ОТМЕНА",592,664,336,80);Button(reset,a,"Confirm","СБРОСИТЬ",992,664,336,80,true);
''' +s[start:]
s=s.replace('new[]{"Pause","Result","Arenas","Settings"}','new[]{"Pause","Result","Arenas","Settings","ResetProgress"}')
p.write_text(s,encoding='utf-8')
