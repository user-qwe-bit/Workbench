// Optional Unity Editor helper. Scope is limited to Assets/RobotArenaArt/.
// This helper configures textures and builds SpriteRenderer animation clips;
// it does not create gameplay logic, scenes, Animator Controllers or prefabs.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RobotArenaArt.Editor
{
    public sealed class RobotArenaArtImporter : AssetPostprocessor
    {
        private const string Root = "Assets/RobotArenaArt/";
        [Serializable] private sealed class BorderItem { public string path; public int[] border_lbrt; }
        [Serializable] private sealed class BorderFile { public BorderItem[] items; }
        [Serializable] private sealed class ClipItem
        {
            public string name;
            public string[] frames;
            public float fps;
            public bool loop;
            public string mode;
            public string end;
        }
        [Serializable] private sealed class ClipFile { public ClipItem[] clips; }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal) ||
                !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;

            var t = (TextureImporter)assetImporter;
            t.textureType = assetPath.Contains("/Fonts/") ? TextureImporterType.Default : TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.spritePixelsPerUnit = 64;
            t.filterMode = FilterMode.Point;
            t.mipmapEnabled = false;
            t.wrapMode = TextureWrapMode.Clamp;
            t.npotScale = TextureImporterNPOTScale.None;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.crunchedCompression = false;
            t.alphaSource = TextureImporterAlphaSource.FromInput;
            t.alphaIsTransparency = true;
            t.anisoLevel = 0;
            t.maxTextureSize = 8192;
            t.sRGBTexture = true;
            var settings = new TextureImporterSettings();
            t.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            t.SetTextureSettings(settings);
            t.spriteBorder = Vector4.zero;

            string json = Root + "Config/nine-slice.json";
            if (!File.Exists(json)) return;
            var data = JsonUtility.FromJson<BorderFile>(File.ReadAllText(json));
            if (data == null || data.items == null) return;
            foreach (var item in data.items)
            {
                if (item.path != assetPath || item.border_lbrt == null || item.border_lbrt.Length != 4) continue;
                var b = item.border_lbrt;
                t.spriteBorder = new Vector4(b[0], b[1], b[2], b[3]);
                break;
            }
        }

        [MenuItem("Tools/Robot Arena Art/Reimport textures")]
        private static void ReimportTextures()
        {
            foreach (string path in Directory.GetFiles(Root, "*.png", SearchOption.AllDirectories))
                AssetDatabase.ImportAsset(path.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
            Debug.Log("Robot Arena Art: texture settings and 9-slice borders applied.");
        }

        [MenuItem("Tools/Robot Arena Art/Create SpriteRenderer clips")]
        private static void CreateClips()
        {
            string file = Root + "Config/animations.json";
            if (!File.Exists(file)) { Debug.LogError("Missing " + file); return; }
            var data = JsonUtility.FromJson<ClipFile>(File.ReadAllText(file));
            if (data == null || data.clips == null) return;
            string folder = Root + "GeneratedClips";
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            int count = 0;
            foreach (var spec in data.clips)
            {
                if (spec.mode == "states" || spec.fps <= 0 || spec.frames == null || spec.frames.Length == 0) continue;
                string destination = folder + "/" + spec.name + ".anim";
                // Re-running the command preserves any clips the developer has already edited.
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(destination) != null) continue;
                var frames = new ObjectReferenceKeyframe[spec.frames.Length + 1];
                bool valid = true;
                for (int i = 0; i < spec.frames.Length; ++i)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.frames[i]);
                    if (sprite == null) { Debug.LogError("Missing imported Sprite: " + spec.frames[i]); valid = false; break; }
                    frames[i] = new ObjectReferenceKeyframe { time = i / spec.fps, value = sprite };
                }
                if (!valid) continue;
                frames[spec.frames.Length] = new ObjectReferenceKeyframe
                {
                    time = spec.frames.Length / spec.fps,
                    value = spec.loop ? frames[0].value : (spec.end == "hold" ? frames[spec.frames.Length - 1].value : null)
                };
                var clip = new AnimationClip { name = spec.name, frameRate = spec.fps };
                var binding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
                AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = spec.loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, destination);
                ++count;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Robot Arena Art: created " + count + " SpriteRenderer clips. UI Image animations need a UI binding or a frame player.");
        }
    }
}
