using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
namespace Workbench {
public sealed class InputRecognition:MonoBehaviour {
 [Serializable] public class Device {public int index;public string name;}
 [Serializable] class Message {public string kind,channel,text;public bool tracked;public float x,y,pinch;public Device[] devices;}
 public Device[] Microphones{get;private set;}=new Device[0];
 public Device[] Cameras{get;private set;}=new Device[0];
 public void RefreshCameras(){Cameras=CameraDevices.Enumerate();}
 public void NextCamera(SettingsData s){RefreshCameras();int i=Array.FindIndex(Cameras,d=>d.index==s.cameraDevice);s.cameraDevice=i+1>=Cameras.Length?-1:Cameras[i+1].index;}
 public string CameraName(int index){if(index<0)return "ПО УМОЛЧАНИЮ";var d=Array.Find(Cameras,x=>x.index==index);string name=d==null?"НЕДОСТУПНА":d.name;return name.Length>27?name.Substring(0,24)+"...":name;}
 public void NextMicrophone(SettingsData s){int i=Array.FindIndex(Microphones,d=>d.index==s.micDevice);s.micDevice=i+1>=Microphones.Length?-1:Microphones[i+1].index;}
 public string MicrophoneName(int index){if(index<0)return "ПО УМОЛЧАНИЮ";var d=Array.Find(Microphones,x=>x.index==index);string name=d==null?index.ToString():d.name;return name.Length>27?name.Substring(0,24)+"...":name;}
 Process worker;readonly ConcurrentQueue<string> messages=new ConcurrentQueue<string>();
 public string VoiceStatus{get;private set;}="Голос выключен";public string HandStatus{get;private set;}="Камера выключена";
 public Vector2 Pointer{get;private set;}public bool Tracked{get;private set;}public bool Pinching{get;private set;}public event Action<string> Command;public event Action<bool> PinchChanged;public event Action HandLost;
 SettingsData settings;float lastHand;bool hadHand;
 public void Apply(SettingsData settings){StopWorker();RefreshCameras();this.settings=settings;VoiceStatus=settings.voice?"Запуск голоса...":"Голос выключен";HandStatus=settings.hand?"Запуск камеры...":"Камера выключена";
  string root=Path.Combine(Application.streamingAssetsPath,"InputRuntime"),exe=Path.Combine(root,"python/python.exe");
  if(!File.Exists(exe)){VoiceStatus=HandStatus="Не найден автономный комплект распознавания";return;}
  try{worker=new Process{StartInfo=new ProcessStartInfo{FileName=exe,Arguments="-u \""+Path.Combine(root,"input_worker.py")+"\""+(settings.voice?" --voice":"")+(settings.hand?" --hand":"")+" --mic "+settings.micDevice+" --camera "+settings.cameraDevice,UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=root,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8}};
   worker.OutputDataReceived+=(s,e)=>{if(e.Data!=null)messages.Enqueue(e.Data);};worker.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)UnityEngine.Debug.Log("Recognition: "+e.Data);};worker.Start();worker.BeginOutputReadLine();worker.BeginErrorReadLine();
  }catch(Exception e){VoiceStatus=HandStatus="Ошибка запуска: "+e.Message;StopWorker();}
 }
 void Update(){while(messages.TryDequeue(out string line)){try{var m=JsonUtility.FromJson<Message>(line);if(m.kind=="devices"){Microphones=m.devices??new Device[0];}else if(m.kind=="status"){if(m.channel=="voice")VoiceStatus=m.text;else HandStatus=m.text;}else if(m.kind=="command")Command?.Invoke(m.text);else if(m.kind=="hand"&&m.tracked){lastHand=Time.unscaledTime;Tracked=true;hadHand=true;float x=settings.mirror?1-m.x:m.x;Vector2 target=new Vector2(Mathf.Clamp01((x-.1f)/.8f)*1920,Mathf.Clamp01((m.y-.1f)/.8f)*1080);Pointer=Vector2.Lerp(Pointer,target,1-settings.smoothing);bool next=m.pinch<(Pinching?settings.pinch*1.5f:settings.pinch);if(next!=Pinching){Pinching=next;PinchChanged?.Invoke(next);}}}catch(Exception e){UnityEngine.Debug.LogWarning("Recognition message: "+e.Message);}}
  if(hadHand&&Time.unscaledTime-lastHand>.45f){hadHand=false;Tracked=false;Pinching=false;HandLost?.Invoke();}
  if(worker!=null&&worker.HasExited){VoiceStatus=settings.voice?"Распознавание остановлено. Переключите голос в настройках.":VoiceStatus;HandStatus=settings.hand?"Распознавание остановлено. Переключите камеру в настройках.":HandStatus;worker.Dispose();worker=null;}
 }
 void StopWorker(){if(worker!=null){try{if(!worker.HasExited){worker.StandardInput.WriteLine("quit");if(!worker.WaitForExit(100))worker.Kill();}}catch{}worker.Dispose();worker=null;}Tracked=false;Pinching=false;hadHand=false;while(messages.TryDequeue(out _)){}HandLost?.Invoke();}
 void OnDestroy()=>StopWorker();void OnApplicationQuit()=>StopWorker();
}
}
