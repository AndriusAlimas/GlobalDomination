#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace GlobalDomination.Editor
{
    /// <summary>
    /// One-click placeholders under <c>Assets/Resources/Battle/</c> so staging battle uses prefabs without leaving Unity.
    /// Replace these prefabs later with a Mixamo / Asset Store humanoid + walk loop (see Resources/Battle/README.txt).
    /// </summary>
    public static class StagingBattlePrefabMenu
    {
        private const string BattleResourcesFolder = "Assets/Resources/Battle";
        private const string AttackerPrefabPath = "Assets/Resources/Battle/StagingBattleAttacker.prefab";
        /// <summary>Legacy optional staging soldier FBX under <c>Assets/Art/Battle/CartoonSoldier/</c>.</summary>
        private const string DefaultSoldierFbxPath = "Assets/Art/Battle/CartoonSoldier/1776625287549_Cartoon_style_soldier_hold.fbx";
        private const string SoldierImportFolder = "Assets/Art/Battle/CartoonSoldier";

        [MenuItem("Global Domination/Battle/Create placeholder unit prefabs (Resources)")]
        public static void CreatePlaceholderPrefabs()
        {
            EnsureFolderExists("Assets/Resources");
            EnsureFolderExists(BattleResourcesFolder);

            CreateCapsulePrefab(AttackerPrefabPath, "StagingBattleAttacker", new Color(0.35f, 0.55f, 0.95f, 1f));
            CreateCapsulePrefab($"{BattleResourcesFolder}/StagingBattleDefender.prefab", "StagingBattleDefender", new Color(0.9f, 0.42f, 0.32f, 1f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Staging battle prefabs",
                "Created:\n" +
                "• Resources/Battle/StagingBattleAttacker.prefab\n" +
                "• Resources/Battle/StagingBattleDefender.prefab\n\n" +
                "They are tinted capsules (same look as before) but now load from Resources. " +
                "Replace them with your animated soldier prefabs when ready.",
                "OK");
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static void CreateCapsulePrefab(string assetPath, string objectName, Color tint)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = objectName;
            go.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

            CapsuleCollider col = go.GetComponent<CapsuleCollider>();
            col.center = Vector3.zero;
            col.height = 2f;
            col.radius = 0.45f;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("HDRP/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
            if (shader != null)
            {
                Material mat = new Material(shader);
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", tint);
                }
                else
                {
                    mat.color = tint;
                }

                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            UnityEngine.Object.DestroyImmediate(go);
        }

        /// <summary>
        /// Wraps your imported FBX as a child under <c>StagingBattleAttacker</c> with a root capsule collider (staging code expects root collider).
        /// Uses <see cref="DefaultSoldierFbxPath"/> or the first <c>.fbx</c> under <see cref="SoldierImportFolder"/> if renamed.
        /// If <see cref="AttackerPrefabPath"/> is missing but a soldier FBX exists under <see cref="SoldierImportFolder"/>,
        /// writes the prefab once (called from <see cref="StagingBattleAttackerPrefabEnsure"/> on editor load).
        /// </summary>
        public static void EnsureAttackerPrefabFromSoldierFbxIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AttackerPrefabPath) != null)
            {
                return;
            }

            string fbxPath = ResolveSoldierFbxPath();
            if (string.IsNullOrEmpty(fbxPath))
            {
                return;
            }

            if (!TryWriteAttackerPrefabFromFbx(fbxPath, out string error))
            {
                Debug.LogWarning("[Global Domination] Could not auto-create StagingBattleAttacker: " + error);
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Global Domination] Created " + AttackerPrefabPath + " from " + fbxPath
                + " — staging battle will use this soldier instead of a capsule.");
        }

        [MenuItem("Global Domination/Battle/Build StagingBattleAttacker from Art/Battle FBX (Resources)")]
        public static void BuildAttackerPrefabFromArtBattleFbx()
        {
            EnsureFolderExists("Assets/Resources");
            EnsureFolderExists(BattleResourcesFolder);

            string fbxPath = ResolveSoldierFbxPath();
            if (string.IsNullOrEmpty(fbxPath))
            {
                EditorUtility.DisplayDialog(
                    "Staging battle",
                    "No FBX found. Expected:\n" + DefaultSoldierFbxPath + "\n" +
                    "or any *.fbx under " + SoldierImportFolder + "/",
                    "OK");
                return;
            }

            if (!TryWriteAttackerPrefabFromFbx(fbxPath, out string error))
            {
                EditorUtility.DisplayDialog("Staging battle", error, "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Staging battle",
                "Wrote:\n" + AttackerPrefabPath + "\n\nfrom:\n" + fbxPath + "\n\n" +
                "Assign an Animator Controller on the model if you have clips; otherwise unit motion still toggles Animator.speed.",
                "OK");
        }

        /// <summary>Writes attacker prefab from FBX path. Replaces existing <see cref="AttackerPrefabPath"/>.</summary>
        private static bool TryWriteAttackerPrefabFromFbx(string fbxPath, out string error)
        {
            error = null;
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (modelAsset == null)
            {
                error = "Could not load GameObject from:\n" + fbxPath;
                return false;
            }

            GameObject root = new GameObject("StagingBattleAttacker");
            try
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
                visual.name = "SoldierModel";
                visual.transform.localPosition = Vector3.zero;
                // Many Blender / image-to-3D exports are Z-up or sideways; Unity expects Y-up — stand the mesh upright.
                visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                visual.transform.localScale = Vector3.one;

                RemoveCollidersInChildren(root.transform);

                CapsuleCollider cap = root.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 0.9f, 0f);
                cap.height = 1.8f;
                cap.radius = 0.35f;

                if (root.GetComponentInChildren<Animator>(true) == null)
                {
                    visual.AddComponent<Animator>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, AttackerPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return true;
        }

        private static string ResolveSoldierFbxPath()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSoldierFbxPath) != null)
            {
                return DefaultSoldierFbxPath;
            }

            if (!AssetDatabase.IsValidFolder(SoldierImportFolder))
            {
                return null;
            }

            string[] guids = AssetDatabase.FindAssets("", new[] { SoldierImportFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }

            return null;
        }

        private static void RemoveCollidersInChildren(Transform root)
        {
            Collider[] cols = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(cols[i], true);
            }
        }
    }
}
#endif
