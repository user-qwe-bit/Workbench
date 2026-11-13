using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Workbench {
public sealed class WorldView:MonoBehaviour {
 public ArtCatalog art;public Camera worldCamera;public GameObject modulePrefab;public GameObject[] arenaPrefabs;
 BattleModel battle;GameObject world;GameData data;Material ink;PixelEffects sparks;
 readonly Dictionary<ModuleState,ModuleVisual> views=new Dictionary<ModuleState,ModuleVisual>();readonly Dictionary<int,ProjectileVisual> bullets=new Dictionary<int,ProjectileVisual>();readonly List<EffectVisual> effects=new List<EffectVisual>();readonly List<LinkVisual> links=new List<LinkVisual>();
 public static float Interpolation=1;
 public static Vector2 Pixel(Vector2 p)=>new Vector2(Mathf.Round(p.x*64)/64,Mathf.Round(p.y*64)/64);
 public static Vector2 RenderPosition(RobotModel r)=>Pixel(Vector2.Lerp(r.previousPosition,r.position,Interpolation));
 public static Vector3 World(Vector2 p)=>new Vector3(p.x,-p.y,0);
 public void Begin(BattleModel b){Clear();battle=b;data=b.data;world=new GameObject("Current battle");world.transform.SetParent(transform);Instantiate(arenaPrefabs[b.arena.id],world.transform);ink=new Material(Shader.Find("Sprites/Default"));sparks=new PixelEffects(world.transform,ink);
  foreach(var r in b.robots){foreach(var m in r.modules){var go=Instantiate(modulePrefab,world.transform);go.name=(r.team==0?"Player ":"Enemy ")+m.spec.title+" "+m.Id;var view=go.GetComponent<ModuleVisual>();view.Initialize(art,r,m);views[m]=view;}
   foreach(var link in Construction.Links(r.modules.Select(m=>m.placement).ToList(),data)){var go=new GameObject("Connector");go.transform.SetParent(world.transform);var sr=go.AddComponent<SpriteRenderer>();sr.sortingOrder=8;links.Add(new LinkVisual{renderer=sr,robot=r,link=link,a=r.modules.First(m=>m.Id==link.a),b=r.modules.First(m=>m.Id==link.b)});}
  }battle.Effect+=OnEffect;FitCamera(true);
 }
 public void Clear(){if(battle!=null)battle.Effect-=OnEffect;battle=null;sparks?.Dispose();sparks=null;foreach(var bullet in bullets.Values)Destroy(bullet.mesh);if(ink)Destroy(ink);if(world)Destroy(world);views.Clear();links.Clear();bullets.Clear();effects.Clear();}
 void OnEffect(string key,Vector2 pos,int team){sparks.Burst(pos,key,team,battle.time);var spec=data.clips.FirstOrDefault(c=>c.name==key);if(spec==null)return;var go=new GameObject(key);go.transform.SetParent(world.transform);go.transform.position=World(Pixel(pos));go.transform.localScale=Vector3.one*(key.StartsWith("muzzle")?2:key.StartsWith("break")?1.5f:key=="shield"?1:2);var sr=go.AddComponent<SpriteRenderer>();sr.sortingOrder=30;effects.Add(new EffectVisual{renderer=sr,clip=spec,started=battle.time});}
 void LateUpdate(){if(battle==null)return;FitCamera(false);foreach(var v in views.Values)v.Refresh(battle.time);
  foreach(var v in links){v.renderer.enabled=v.a.alive&&v.b.alive;if(!v.renderer.enabled)continue;string team=!v.a.powered||!v.b.powered?"disabled":v.robot.team==0?"player":"enemy";v.renderer.sprite=art.Get("Connectors/"+team+"_"+(v.link.horizontal?"horizontal":"vertical"));v.renderer.transform.position=World(RenderPosition(v.robot)+v.link.center);}
  foreach(var p in battle.projectiles)if(!bullets.ContainsKey(p.id)){var go=new GameObject("Projectile "+p.id);go.transform.SetParent(world.transform);var body=new GameObject("Bullet").AddComponent<SpriteRenderer>();body.transform.SetParent(go.transform);body.sortingOrder=25;var trail=new GameObject("Continuous fading trail",typeof(MeshFilter),typeof(MeshRenderer));trail.transform.SetParent(go.transform);var mesh=new Mesh();mesh.MarkDynamic();trail.GetComponent<MeshFilter>().sharedMesh=mesh;var mr=trail.GetComponent<MeshRenderer>();mr.sharedMaterial=ink;mr.sortingOrder=24;bullets[p.id]=new ProjectileVisual{root=go,body=body,mesh=mesh,model=p,owner=p.owner};}
  foreach(var pair in bullets.ToArray()){var v=pair.Value;var p=v.model;bool alive=p.alive&&battle.projectiles.Contains(p);v.body.enabled=alive;
   if(v.owner!=p.owner){v.points.Clear();v.owner=p.owner;}if(!v.ended&&(v.points.Count==0||v.points[v.points.Count-1].position!=p.position))v.points.Add(new TrailPoint{position=Pixel(p.position),time=battle.time});if(!alive)v.ended=true;
   v.points.RemoveAll(point=>battle.time-point.time>.3f);if(!alive&&v.points.Count==0){Destroy(v.mesh);Destroy(v.root);bullets.Remove(pair.Key);continue;}
   v.body.transform.position=World(Pixel(p.position));string key=p.type==5?"RifleDirections/dir_"+GameData.Direction(p.velocity).ToString("00"):p.type==7?"pellet":p.type==6?"grenade_spin/frame_"+((int)(battle.time*8)%4).ToString("00"):"machinegun";
   v.body.sprite=art.Get("Projectiles/"+key);v.body.transform.rotation=p.type==5?Quaternion.identity:Quaternion.Euler(0,0,Mathf.Atan2(-p.velocity.y,p.velocity.x)*Mathf.Rad2Deg);v.body.transform.localScale=Vector3.one*(p.type==6?1.5f:2.5f);DrawTrail(v,battle.time);
  }
  foreach(var e in effects.ToArray()){int f=(int)((battle.time-e.started)*e.clip.fps);if(f>=e.clip.frames.Length){Destroy(e.renderer.gameObject);effects.Remove(e);}else e.renderer.sprite=art.Get(e.clip.frames[f]);}sparks.Draw(battle.time);
 }
 void DrawTrail(ProjectileVisual v,float time){int count=v.points.Count;var verts=new Vector3[count*2];var colors=new Color[count*2];var triangles=new int[Mathf.Max(0,count-1)*6];Color color=v.owner==0?new Color(.15f,.65f,1):new Color(1,.22f,.18f);float width=v.model.type==5?.08f:v.model.type==7?.04f:.06f;
  for(int i=0;i<count;i++){Vector2 direction=i+1<count?v.points[i+1].position-v.points[i].position:v.model.velocity;Vector2 side=new Vector2(-direction.y,direction.x).normalized*width;verts[i*2]=World(v.points[i].position+side);verts[i*2+1]=World(v.points[i].position-side);color.a=Mathf.Clamp01(1-(time-v.points[i].time)/.3f)*.9f;colors[i*2]=colors[i*2+1]=color;if(i+1<count){int j=i*6,k=i*2;triangles[j]=k;triangles[j+1]=k+1;triangles[j+2]=k+2;triangles[j+3]=k+2;triangles[j+4]=k+1;triangles[j+5]=k+3;}}
  v.mesh.Clear();v.mesh.vertices=verts;v.mesh.colors=colors;v.mesh.triangles=triangles;v.mesh.RecalculateBounds();
 }
 void FitCamera(bool immediate){var a=battle.robots[0].Bounds;var b=battle.robots[1].Bounds;float margin=data.balance.cameraMargin;var combined=Rect.MinMaxRect(Mathf.Min(a.xMin,b.xMin)-margin,Mathf.Min(a.yMin,b.yMin)-margin,Mathf.Max(a.xMax,b.xMax)+margin,Mathf.Max(a.yMax,b.yMax)+margin);Vector3 target=World(combined.center);target.z=-10;float aspect=16f/9,required=Mathf.Max(data.balance.cameraMinSize,combined.height*.5f+1.5f,combined.width/(2*aspect));
  float f=immediate?1:1-Mathf.Exp(-Time.unscaledDeltaTime*(required>worldCamera.orthographicSize?12:3));Vector3 current=Vector3.Lerp(worldCamera.transform.position,target,f);float size=Mathf.Lerp(worldCamera.orthographicSize,required,f);
  size=Mathf.Max(size,Mathf.Abs(-combined.yMin-current.y)+1,Mathf.Abs(-combined.yMax-current.y)+1,Mathf.Abs(combined.xMin-current.x)/aspect,Mathf.Abs(combined.xMax-current.x)/aspect);size=Mathf.Min(size,23.8f,37.8f/aspect);
  current.x=Mathf.Clamp(current.x,-7.8f+size*aspect,67.8f-size*aspect);current.y=Mathf.Clamp(current.y,-39.8f+size,7.8f-size);worldCamera.orthographicSize=size;
  float px=2*size/1080;current.x=Mathf.Round(current.x/px)*px;current.y=Mathf.Round(current.y/px)*px;worldCamera.transform.position=current;
 }
 class TrailPoint {public Vector2 position;public float time;}
 class ProjectileVisual {public GameObject root;public SpriteRenderer body;public Mesh mesh;public ProjectileState model;public int owner;public bool ended;public List<TrailPoint> points=new List<TrailPoint>();}
 class EffectVisual {public SpriteRenderer renderer;public ClipData clip;public float started;}
 class LinkVisual {public SpriteRenderer renderer;public RobotModel robot;public ModuleState a,b;public Construction.Link link;}
}
}
