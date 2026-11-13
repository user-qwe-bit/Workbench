using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Workbench {
public sealed partial class BotController {
 readonly BattleModel battle;readonly RobotModel self,other; readonly LevelSpec level;readonly System.Random random;
 float nextThink,decisionUntil,lastProgressTime,ramRetreatUntil;Vector2 lastProgress,goal; List<Vector2> path=new List<Vector2>();int pathIndex;int lastRevision=-1;int previousImpacts;
 public string State{get;private set;}="Ищет позицию";public float DesiredRange{get;private set;} public int Recoveries{get;private set;}
 public BotController(BattleModel b,LevelSpec l,int team=1,int seed=7){battle=b;level=l;self=b.robots[team];other=b.robots[1-team];random=new System.Random(seed);lastProgress=self.position;}
 public void Tick(){if(battle.paused||battle.finished)return;float t=battle.time;Observe();AimWeapons();
  if(t>=nextThink){nextThink=t+.2f;Think();}
  if(pathIndex<path.Count){Vector2 delta=path[pathIndex]-self.position;if(delta.magnitude<.25f){pathIndex++;}self.move=delta.normalized;}else self.move=(goal-self.position).magnitude>.25f?(goal-self.position).normalized:Vector2.zero;
  if(!battle.ClearAt(self,self.position+self.move*.4f,true)){self.move=Vector2.zero;nextThink=Mathf.Min(nextThink,t+.05f);}
 }
 void Think(){float t=battle.time;var guns=self.Weapons.ToArray();var enemyGuns=other.Weapons.ToArray();bool ramming=guns.Length==0;
  DesiredRange=ramming?0:guns.Any(m=>m.Type==5)?19:guns.Any(m=>m.Type==4)?13:guns.Any(m=>m.Type==6)?12:3.2f;
  if(!ramming&&guns.Any(m=>m.Type!=7)&&enemyGuns.Any(m=>m.Type==7))DesiredRange=Mathf.Max(DesiredRange,13);
  Vector2 target=self.aim;
  if(Vector2.Distance(lastProgress,self.position)>.6f){lastProgress=self.position;lastProgressTime=t;}
  bool stuck=t-lastProgressTime>2.2f&&Vector2.Distance(goal,self.position)>1;
  if(stuck){Recoveries++;lastProgressTime=t;decisionUntil=0;}
  if(ramming&&battle.impactCount!=previousImpacts){ramRetreatUntil=t+.7f;decisionUntil=0;}previousImpacts=battle.impactCount;
  if(t<decisionUntil&&lastRevision==self.revision+other.revision&&!stuck)return;lastRevision=self.revision+other.revision;decisionUntil=t+1.2f;
  State=ramming?"Таран":DesiredRange<5?"Сближается":"Держит дистанцию";
  var local=self.LocalBounds;Vector2 centerOffset=local.center;Vector2 candidate=self.position;float best=-float.MaxValue;
  if(ramming){candidate=other.Center-centerOffset;if(t<ramRetreatUntil)candidate=self.position+(self.Center-other.Center).normalized*3;}
  else for(int i=0;i<24;i++){float a=2*Mathf.PI*i/24;Vector2 p=other.Center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*DesiredRange-centerOffset;if(!battle.ClearAt(self,p,true))continue;
   float score=(battle.LineClear(p+centerOffset,target)?15:0)-Vector2.Distance(self.position,p)*.32f+Mathf.Min(2,Vector2.Distance(p,goal))*.05f;
   if(stuck)score+=(float)random.NextDouble()*10;if(score>best){best=score;candidate=p;}}
  if(stuck&&ramming)candidate=self.position+new Vector2((float)random.NextDouble()*8-4,(float)random.NextDouble()*8-4);
  goal=candidate;path=FindPath(candidate);pathIndex=0;
  if(path.Count==0){goal=self.position;State="Ищет обход";}else goal=path[path.Count-1];
 }
 List<Vector2> FindPath(Vector2 desired){
  // Breadth-first distance field on board origins, expanded by the complete robot bounds.
  // Every edge is swept; diagonal corner cutting is intentionally absent.
  var valid=new bool[60,32];for(int y=0;y<32;y++)for(int x=0;x<60;x++)valid[x,y]=battle.ClearAt(self,new Vector2(x,y),true);
  Vector2Int start=NearestValid(self.position,valid);var queue=new Queue<Vector2Int>();queue.Enqueue(start);var visited=new bool[60,32];var parent=new Vector2Int[60,32];visited[start.x,start.y]=true;Vector2Int best=start;float bestDistance=float.MaxValue;var offsets=new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
  while(queue.Count>0){var p=queue.Dequeue();float distance=Vector2.Distance(p,desired);if(distance<bestDistance){best=p;bestDistance=distance;}
   foreach(var delta in offsets){var n=p+delta;if(n.x<0||n.y<0||n.x>=60||n.y>=32||visited[n.x,n.y]||!valid[n.x,n.y])continue;visited[n.x,n.y]=true;parent[n.x,n.y]=p;queue.Enqueue(n);}}
  var result=new List<Vector2>();var c=best;while(c!=start){result.Add(c);c=parent[c.x,c.y];}result.Add(start);result.Reverse();
  // Keep the furthest directly traversable waypoint, sampling the full footprint.
  int skip=0;for(int i=0;i<result.Count;i++){Vector2 delta=result[i]-self.position;bool clear=true;int steps=Mathf.CeilToInt(delta.magnitude*5);for(int k=1;k<=steps;k++)if(!battle.ClearAt(self,self.position+delta*k/steps,true)){clear=false;break;}if(!clear)break;skip=i;}
  if(skip>0)result.RemoveRange(0,skip);return result;
 }
 static Vector2Int NearestValid(Vector2 position,bool[,] valid){float best=float.MaxValue;Vector2Int result=Vector2Int.zero;for(int y=0;y<32;y++)for(int x=0;x<60;x++)if(valid[x,y]){float d=(new Vector2(x,y)-position).sqrMagnitude;if(d<best){best=d;result=new Vector2Int(x,y);}}return result;}
}
}
