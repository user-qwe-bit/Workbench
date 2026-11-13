using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Workbench {
public sealed class ScreenMotion:MonoBehaviour {
 public ArtCatalog art;public Image lid,robot,gear,progress;public GameObject loading;
 public bool Busy{get;private set;}
 public void Switch(Action action){if(!Busy)StartCoroutine(Run(action));}
 IEnumerator Run(Action action){
  Busy=true;gameObject.SetActive(true);lid.gameObject.SetActive(true);loading.SetActive(false);
  for(int i=0;i<=8;i++){lid.sprite=art.Get("UI/Transitions/Close/frame_"+i.ToString("00"));yield return new WaitForSecondsRealtime(.025f);}
  loading.SetActive(true);yield return null;action();
  float start=Time.unscaledTime;while(Time.unscaledTime-start<.4f){float t=Time.unscaledTime-start;robot.sprite=art.Get("UI/Loading/Robot/frame_"+((int)(t*10)%4).ToString("00"));gear.sprite=art.Get("UI/Loading/Gear/frame_"+((int)(t*30)%24).ToString("00"));progress.fillAmount=Mathf.Clamp01(t/.4f);yield return null;}
  loading.SetActive(false);for(int i=8;i>=0;i--){lid.sprite=art.Get("UI/Transitions/Close/frame_"+i.ToString("00"));yield return new WaitForSecondsRealtime(.025f);}
  lid.gameObject.SetActive(false);Busy=false;
 }
}
}
