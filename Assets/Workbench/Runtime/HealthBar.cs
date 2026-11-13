using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Workbench {
public sealed class HealthBar:MonoBehaviour {
 public Image fill,damage,flash;public ArtCatalog art;float shown=1,lastRatio=1,hitAt=-10;
 public void Set(float ratio,float time,bool reversed){ratio=Mathf.Clamp01(ratio);if(ratio<lastRatio)hitAt=time;if(ratio>lastRatio)shown=ratio;lastRatio=ratio;if(time-hitAt>.4f)shown=Mathf.MoveTowards(shown,ratio,.018f);fill.fillAmount=ratio;damage.fillAmount=Mathf.Max(ratio,shown);fill.fillOrigin=damage.fillOrigin=reversed?1:0;int frame=(int)((time-hitAt)*16);flash.enabled=frame>=0&&frame<4;if(flash.enabled)flash.sprite=art.Get("UI/Bars/Effects/hit_flash/frame_"+frame.ToString("00"));}
}
}
