#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GlobalDomination.Editor
{
    /// <summary>
    /// One-click: set Rig + loop settings on England staging soldier and idle FBX assets (Unity ModelImporter API).
    /// </summary>
    public static class StagingBattleEnglandFbxImportSetup
    {
        private const string SoldierFbxPath = "Assets/Resources/Battle/Countries/England/Units/Soldier/Model/Soldier.fbx";
        private const string IdleFbxPath = "Assets/Resources/Battle/Countries/England/Units/Soldier/Animations/Idle.fbx";

        [MenuItem("Global Domination/Battle/Apply England FBX rig (Soldier + Idle)")]
        public static void ApplyEnglandFbxRigAndIdleLoop()
        {
            if (!AssetDatabase.LoadAssetAtPath<Object>(SoldierFbxPath))
            {
                EditorUtility.DisplayDialog(
                    "Staging battle",
                    "Soldier FBX not found at:\n" + SoldierFbxPath,
                    "OK");
                return;
            }

            if (!AssetDatabase.LoadAssetAtPath<Object>(IdleFbxPath))
            {
                EditorUtility.DisplayDialog(
                    "Staging battle",
                    "Idle FBX not found at:\n" + IdleFbxPath,
                    "OK");
                return;
            }

            Avatar soldierAvatar = FindFirstAvatarAtPath(SoldierFbxPath);
            if (soldierAvatar == null)
            {
                EditorUtility.DisplayDialog(
                    "Staging battle",
                    "Could not find an Avatar sub-asset on Soldier.fbx after import. Check the Rig tab manually.",
                    "OK");
                return;
            }

            ApplySoldierImporter();
            AssetDatabase.ImportAsset(SoldierFbxPath, ImportAssetOptions.ForceUpdate);

            soldierAvatar = FindFirstAvatarAtPath(SoldierFbxPath);
            if (soldierAvatar == null)
            {
                EditorUtility.DisplayDialog(
                    "Staging battle",
                    "Soldier Avatar still missing after import. Open Soldier.fbx → Rig → Create From This Model, Apply, then run this again.",
                    "OK");
                return;
            }

            ApplyIdleImporter(soldierAvatar);
            AssetDatabase.ImportAsset(IdleFbxPath, ImportAssetOptions.ForceUpdate);

            ApplyIdleClipLoopSettings();
            AssetDatabase.ImportAsset(IdleFbxPath, ImportAssetOptions.ForceUpdate);

            EditorUtility.DisplayDialog(
                "Staging battle",
                "Applied Generic rig + avatar on Soldier, Copy Avatar + loop time on Idle clips.\n\nRe-enter Play Mode to test staging battle.",
                "OK");
        }

        private static void ApplySoldierImporter()
        {
            var importer = AssetImporter.GetAtPath(SoldierFbxPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.optimizeBones = false;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
        }

        private static void ApplyIdleImporter(Avatar soldierAvatar)
        {
            var importer = AssetImporter.GetAtPath(IdleFbxPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = soldierAvatar;
            importer.importAnimation = true;
            importer.optimizeBones = false;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
        }

        private static void ApplyIdleClipLoopSettings()
        {
            var importer = AssetImporter.GetAtPath(IdleFbxPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            ModelImporterClipAnimation[] existing = importer.clipAnimations;
            if (existing != null && existing.Length > 0)
            {
                for (int i = 0; i < existing.Length; i++)
                {
                    ModelImporterClipAnimation c = existing[i];
                    c.loopTime = true;
                    c.loopPose = false;
                    existing[i] = c;
                }

                importer.clipAnimations = existing;
                importer.SaveAndReimport();
                return;
            }

            TakeInfo[] takes = importer.importedTakeInfos;
            if (takes != null && takes.Length > 0)
            {
                var list = new List<ModelImporterClipAnimation>();
                for (int i = 0; i < takes.Length; i++)
                {
                    TakeInfo t = takes[i];
                    float fps = t.sampleRate > 0.01f ? t.sampleRate : 30f;
                    var ca = new ModelImporterClipAnimation
                    {
                        name = string.IsNullOrEmpty(t.defaultClipName) ? t.name : t.defaultClipName,
                        takeName = t.name,
                        firstFrame = Mathf.Round(t.startTime * fps),
                        lastFrame = Mathf.Max(1f, Mathf.Round(t.stopTime * fps)),
                        loopTime = true,
                        loopPose = false,
                    };
                    list.Add(ca);
                }

                importer.clipAnimations = list.ToArray();
                importer.SaveAndReimport();
                return;
            }

            ModelImporterClipAnimation[] built = BuildClipAnimationsFromSubAssets(IdleFbxPath);
            if (built.Length > 0)
            {
                importer.clipAnimations = built;
                importer.SaveAndReimport();
            }
        }

        private static ModelImporterClipAnimation[] BuildClipAnimationsFromSubAssets(string assetPath)
        {
            var list = new List<ModelImporterClipAnimation>();
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < objs.Length; i++)
            {
                if (!(objs[i] is AnimationClip clip))
                {
                    continue;
                }

                if (clip.name.IndexOf("__preview__", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                var ca = new ModelImporterClipAnimation
                {
                    name = clip.name,
                    takeName = clip.name,
                    firstFrame = 0f,
                    lastFrame = ComputeLastFrame(clip),
                    loopTime = true,
                    loopPose = false,
                };
                list.Add(ca);
            }

            return list.ToArray();
        }

        private static float ComputeLastFrame(AnimationClip clip)
        {
            float fps = clip.frameRate > 0.01f ? clip.frameRate : 30f;
            float frames = clip.length * fps;
            return Mathf.Max(1f, Mathf.Round(frames));
        }

        private static Avatar FindFirstAvatarAtPath(string assetPath)
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i] is Avatar avatar && avatar != null && avatar.isValid)
                {
                    return avatar;
                }
            }

            return null;
        }
    }
}
#endif
