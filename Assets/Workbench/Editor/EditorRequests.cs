using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Workbench.Editor {
[InitializeOnLoad]
public static class EditorRequests {
 static double next;static bool running;
 static EditorRequests(){EditorApplication.update+=Poll;}
 static void Poll(){if(running||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2;const string request="Tools/editor-request.txt";if(!File.Exists(request))return;
  string command=File.ReadAllText(request).Trim();string stamp=command+":"+File.GetLastWriteTimeUtc(request).Ticks;
  // A background Editor does not necessarily auto-import external changes. Refresh first and
  // execute on a later update/domain, after the Editor and Player share the same script layout.
  if(SessionState.GetString("Workbench.RefreshedRequest","")!=stamp){SessionState.SetString("Workbench.RefreshedRequest",stamp);AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);next=EditorApplication.timeSinceStartup+5;return;}
  File.Delete(request);running=true;Directory.CreateDirectory("TestResults");try{if(command=="prepare")ProjectBuilder.Prepare();else if(command=="test")CriticalChecks.Run();else if(command=="build")ProjectBuilder.BuildWindows();else throw new Exception("Unknown editor request");File.WriteAllText("TestResults/editor-request-result.txt","SUCCESS "+command+" "+DateTime.UtcNow.ToString("O"));}catch(Exception e){File.WriteAllText("TestResults/editor-request-result.txt","FAIL "+command+"\n"+e);Debug.LogException(e);}finally{running=false;}
 }
}
}
