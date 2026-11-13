using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Workbench {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PixelText:MaskableGraphic {
 [Serializable] public class Glyph {public int id,x,y,width,height,xoffset,yoffset,xadvance;}
 [Serializable] public class FontData {public int fontSize,lineHeight;public Glyph[] glyphs;}
 public Texture2D atlas;public TextAsset definition;[TextArea]public string value="";public int scale=2;public float letterSpacing=1;public bool centered,wrap=true;
 Dictionary<int,Glyph> glyphs;FontData font;
 public override Texture mainTexture=>atlas?atlas:Texture2D.whiteTexture;
 public void Set(string s){if(value==s)return;value=s;SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();if(!atlas||!definition)return;if(glyphs==null){font=JsonUtility.FromJson<FontData>(definition.text);glyphs=new Dictionary<int,Glyph>();foreach(var g in font.glyphs)glyphs[g.id]=g;}
  var rect=rectTransform.rect;float x=rect.xMin,y=rect.yMax;int unit=Mathf.Max(1,scale);var lines=new List<string>();
  foreach(string original in (value??"").Replace('—','-').Replace('–','-').Replace('…','.').Replace('·','/').Split('\n')){string current="";foreach(string word in original.Split(' ')){string next=current.Length==0?word:current+" "+word;if(wrap&&Width(next)*unit>rect.width&&current.Length>0){lines.Add(current);current=word;}else current=next;}lines.Add(current);}
  foreach(string line in lines){x=rect.xMin+(centered?Mathf.Round((rect.width-Width(line)*unit)*.5f):0);foreach(char c in line){if(!glyphs.TryGetValue(c,out var g)){glyphs.TryGetValue('?',out g);if(g==null)continue;}float left=x+g.xoffset*unit,top=y-g.yoffset*unit;float u0=(float)g.x/atlas.width,u1=(float)(g.x+g.width)/atlas.width,v1=1f-(float)g.y/atlas.height,v0=1f-(float)(g.y+g.height)/atlas.height;int n=vh.currentVertCount;vh.AddVert(new Vector3(left,top-g.height*unit),color,new Vector2(u0,v0));vh.AddVert(new Vector3(left,top),color,new Vector2(u0,v1));vh.AddVert(new Vector3(left+g.width*unit,top),color,new Vector2(u1,v1));vh.AddVert(new Vector3(left+g.width*unit,top-g.height*unit),color,new Vector2(u1,v0));vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n+2,n+3,n);x+=g.xadvance*unit+letterSpacing;}y-=font.lineHeight*unit;}
 }
 float Width(string text){float width=0;foreach(char c in text)if(glyphs.TryGetValue(c,out var g))width+=g.xadvance;return width+maxSpace(text);}
 float maxSpace(string text)=>Mathf.Max(0,text.Length-1)*letterSpacing/Mathf.Max(1,scale);
}
}
