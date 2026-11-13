using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random=UnityEngine.Random;
namespace Workbench {
public sealed class UiParticles:MaskableGraphic {
 struct Dot {public Vector2 p,v;public float born,life,size;public Color tint;}
 readonly List<Dot> dots=new List<Dot>();
 public void Burst(Vector2 p,int count,Color color){for(int i=0;i<count;i++){var v=Random.insideUnitCircle*180;v.y=Mathf.Abs(v.y)+70;dots.Add(new Dot{p=new Vector2(p.x,-p.y),v=v,born=Time.unscaledTime,life=Random.Range(.35f,.7f),size=Random.Range(2,5),tint=color});}SetVerticesDirty();}
 void Update(){if(dots.Count==0)return;dots.RemoveAll(d=>Time.unscaledTime-d.born>d.life);SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();foreach(var d in dots){float t=Time.unscaledTime-d.born;Vector2 p=d.p+d.v*t+Vector2.down*300*t*t;float size=d.size;var c=d.tint;c.a=1-t/d.life;int n=vh.currentVertCount;vh.AddVert(new Vector3(Mathf.Round(p.x-size),Mathf.Round(p.y-size)),c,Vector2.zero);vh.AddVert(new Vector3(Mathf.Round(p.x-size),Mathf.Round(p.y+size)),c,Vector2.zero);vh.AddVert(new Vector3(Mathf.Round(p.x+size),Mathf.Round(p.y+size)),c,Vector2.zero);vh.AddVert(new Vector3(Mathf.Round(p.x+size),Mathf.Round(p.y-size)),c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}}
}
}
