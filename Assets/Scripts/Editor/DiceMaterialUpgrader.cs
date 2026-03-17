using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility: upgrades all Animated Dice materials from the Built-in Standard
/// shader to Universal Render Pipeline/Lit so they render correctly in URP.
/// Run via  Tools > Upgrade Dice Materials to URP
/// </summary>
public static class DiceMaterialUpgrader
{
    private const string DiceMaterialsPath = "Assets/Animated Dice (Random Art Attack)/Materials";

    [MenuItem("Tools/Upgrade Dice Materials to URP")]
    public static void UpgradeDiceMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Universal Render Pipeline/Lit shader not found.\n" +
                "Make sure the URP package is installed.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { DiceMaterialsPath });
        if (guids.Length == 0)
        {
            Debug.LogWarning($"[DiceMaterialUpgrader] No materials found under {DiceMaterialsPath}");
            return;
        }

        int upgraded = 0;
        int skipped  = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Only touch materials still using the Built-in Standard (or Specular) shader
            if (mat.shader.name != "Standard" && mat.shader.name != "Standard (Specular setup)")
            {
                skipped++;
                continue;
            }

            // ------ Cache Built-in properties before switching shader ------
            Texture albedo       = mat.GetTexture("_MainTex");
            Color   albedoColor  = mat.HasProperty("_Color")         ? mat.GetColor("_Color")           : Color.white;
            Texture bumpMap      = mat.GetTexture("_BumpMap");
            float   bumpScale    = mat.HasProperty("_BumpScale")      ? mat.GetFloat("_BumpScale")       : 1f;
            Texture metallicGloss = mat.GetTexture("_MetallicGlossMap");
            float   metallic     = mat.HasProperty("_Metallic")       ? mat.GetFloat("_Metallic")        : 0f;
            float   smoothness   = mat.HasProperty("_Glossiness")     ? mat.GetFloat("_Glossiness")      : 0.5f;
            Texture occlusion    = mat.GetTexture("_OcclusionMap");
            Texture emission     = mat.GetTexture("_EmissionMap");
            Color   emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor")  : Color.black;

            // ------ Switch to URP/Lit ------
            mat.shader = urpLit;

            // ------ Remap properties to URP names ------
            // Albedo
            if (albedo != null) mat.SetTexture("_BaseMap", albedo);
            mat.SetColor("_BaseColor", albedoColor);

            // Normal map
            if (bumpMap != null)
            {
                mat.SetTexture("_BumpMap", bumpMap);
                mat.SetFloat("_BumpScale", bumpScale);
                mat.EnableKeyword("_NORMALMAP");
            }

            // Metallic / Smoothness
            if (metallicGloss != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallicGloss);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            // Occlusion
            if (occlusion != null) mat.SetTexture("_OcclusionMap", occlusion);

            // Emission
            if (emission != null)
            {
                mat.SetTexture("_EmissionMap", emission);
                mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            EditorUtility.SetDirty(mat);
            upgraded++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"Upgraded {upgraded} material(s) to URP/Lit.\nSkipped {skipped} material(s) (already non-Standard).";
        Debug.Log($"[DiceMaterialUpgrader] {msg}");
        EditorUtility.DisplayDialog("Done", msg, "OK");
    }
}
