using UnityEngine;
namespace Workbench {
public static class BuilderLayout {
 public static Rect InventorySlot(int type,LevelSpec level){
  if(level.inventory[type]<=0)return new Rect(-1000,-1000,0,0);
  int index=0;for(int i=0;i<type;i++)if(level.inventory[i]>0)index++;
  return new Rect(408+index%6*136,812+index/6*124,112,112);
 }
}
}
