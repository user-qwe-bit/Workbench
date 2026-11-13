using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Workbench {
public sealed class PopupMotion:MonoBehaviour {
 readonly List<RectTransform> parts=new List<RectTransform>();readonly List<Vector2> positions=new List<Vector2>();float started;Image shade;CanvasGroup group;
 void Awake(){group=gameObject.AddComponent<CanvasGroup>();foreach(Transform child in transform){if(child.name=="Modal shade"){shade=child.GetComponent<Image>();continue;}if(child is RectTransform r){parts.Add(r);positions.Add(r.anchoredPosition);}}}
 void OnEnable(){started=Time.unscaledTime;Apply(0);}
 void Update(){Apply(Mathf.Clamp01((Time.unscaledTime-started)/.48f));}
 public void SetRestPosition(RectTransform part,Vector2 position){int i=parts.IndexOf(part);if(i>=0)positions[i]=position;part.anchoredPosition=position;}
 void Apply(float t){if(group==null)return;float u=t-1,ease=1+2.7f*u*u*u+1.7f*u*u;float offset=-700*(1-ease);for(int i=0;i<parts.Count;i++)parts[i].anchoredPosition=positions[i]+new Vector2(0,Mathf.Round(offset));group.interactable=t>=1;if(shade)shade.color=new Color(0,0,0,.5f*Mathf.Clamp01(t*3));}
}
}
