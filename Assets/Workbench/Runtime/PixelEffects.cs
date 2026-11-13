using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Workbench {
// Square quads; simulation time freezes the entire effect in pause.
public sealed class PixelEffects {
 struct Spark {public Vector2 p,v;public float born,life,size;public Color color;}
 readonly List<Spark> sparks=new List<Spark>();readonly System.Random rng=new System.Random(231);readonly Mesh mesh;
 public PixelEffects(Transform parent,Material material){var go=new GameObject("Pixel sparks",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);mesh=new Mesh();mesh.MarkDynamic();go.GetComponent<MeshFilter>().sharedMesh=mesh;var sr=go.GetComponent<MeshRenderer>();sr.sharedMaterial=material;sr.sortingOrder=32;}
 float Range(float min,float max)=>min+(float)rng.NextDouble()*(max-min);
 public void Burst(Vector2 p,string kind,int team,float time){bool big=kind.StartsWith("break")||kind=="explode_grenade";bool muzzle=kind.StartsWith("muzzle");int count=big?36:muzzle?5:kind=="shield_reflect"?22:14;
  Color accent=kind=="shield_reflect"?(team==0?new Color(.3f,.85f,1):new Color(1,.35f,.3f)):big?new Color(1,.48f,.12f):new Color(1,.82f,.4f);
  for(int i=0;i<count&&sparks.Count<900;i++){float angle=Range(0,Mathf.PI*2),speed=Range(.8f,big?6:3);sparks.Add(new Spark{p=p,v=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*speed,born=time,life=Range(.15f,big?.75f:.4f),size=Range(3,big?8:5)/64,color=i%3==0?Color.white:accent});}
 }
 public void Draw(float time){sparks.RemoveAll(p=>time-p.born>=p.life);var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
  foreach(var p in sparks){float age=time-p.born,t=age/p.life;Vector2 point=p.p+p.v*(1-Mathf.Exp(-age*4))/4;point.x=Mathf.Round(point.x*64)/64;point.y=Mathf.Round(point.y*64)/64;Vector3 c=WorldView.World(point);float size=p.size*(1-t*.55f);Color color=p.color;color.a=1-t*t;int n=vertices.Count;vertices.Add(c+new Vector3(-size,-size));vertices.Add(c+new Vector3(-size,size));vertices.Add(c+new Vector3(size,size));vertices.Add(c+new Vector3(size,-size));for(int i=0;i<4;i++)colors.Add(color);triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
  mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
 }
 public void Dispose(){Object.Destroy(mesh);}
}
}
