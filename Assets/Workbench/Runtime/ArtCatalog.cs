using System;
using System.Collections.Generic;
using UnityEngine;
namespace Workbench {
[CreateAssetMenu(menuName="Workbench/Art catalog")]
public sealed class ArtCatalog:ScriptableObject {
 [Serializable] public class Entry {public string key;public Sprite sprite;}
 [Serializable] public class Sound {public string key;public AudioClip clip;}
 public Entry[] entries;public Sound[] sounds;public Texture2D fontAtlas;public TextAsset fontJson;
 Dictionary<string,Sprite> lookup; Dictionary<string,AudioClip> audioLookup;
 public Sprite Get(string key){if(lookup==null){lookup=new Dictionary<string,Sprite>();foreach(var e in entries)lookup[e.key]=e.sprite;}key=key.Replace("Assets/RobotArenaArt/","").Replace(".png","");return lookup.TryGetValue(key,out var sprite)?sprite:null;}
 public AudioClip Audio(string key){if(audioLookup==null){audioLookup=new Dictionary<string,AudioClip>();foreach(var s in sounds)audioLookup[s.key]=s.clip;}return audioLookup.TryGetValue(key,out var c)?c:null;}
}
}
