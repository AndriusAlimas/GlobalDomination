using UnityEngine;

namespace GlobalDomination.UI.Battle
{
    /// <summary>
    /// Avoids <see cref="Renderer.material"/> on primitives (can throw when internal material source is null).
    /// </summary>
    public static class StagingBattleLitMaterial
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static void ApplyColor(MeshRenderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            Shader shader = ResolveLitShader();
            if (shader == null)
            {
                return;
            }

            Material mat = new Material(shader);
            ApplyColorToMaterial(mat, color);
            renderer.sharedMaterial = mat;
        }

        /// <summary>
        /// Team tint: multiply <c>_BaseColor</c> / <c>_Color</c> via <see cref="MaterialPropertyBlock"/> when the shader exposes them;
        /// otherwise leaves the renderer unchanged so imported FBX materials stay intact.
        /// </summary>
        public static void ApplyTeamTintToHierarchy(Transform root, Color tint)
        {
            if (root == null)
            {
                return;
            }

            if (IsEffectivelyWhite(tint))
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null)
                {
                    continue;
                }

                Material shared = r.sharedMaterial;
                if (shared != null && shared.HasProperty(BaseColorId))
                {
                    var block = new MaterialPropertyBlock();
                    r.GetPropertyBlock(block);
                    Color baseC = shared.GetColor(BaseColorId);
                    block.SetColor(BaseColorId, new Color(baseC.r * tint.r, baseC.g * tint.g, baseC.b * tint.b, baseC.a * tint.a));
                    r.SetPropertyBlock(block);
                    continue;
                }

                if (shared != null && shared.HasProperty(ColorId))
                {
                    var block = new MaterialPropertyBlock();
                    r.GetPropertyBlock(block);
                    Color baseC = shared.GetColor(ColorId);
                    block.SetColor(ColorId, new Color(baseC.r * tint.r, baseC.g * tint.g, baseC.b * tint.b, baseC.a * tint.a));
                    r.SetPropertyBlock(block);
                    continue;
                }

                // Keep imported materials as-is (no flat team-color replacement; that washed out textures / unused slots).
            }
        }

        private static bool IsEffectivelyWhite(Color color)
        {
            return Mathf.Approximately(color.r, 1f)
                && Mathf.Approximately(color.g, 1f)
                && Mathf.Approximately(color.b, 1f)
                && Mathf.Approximately(color.a, 1f);
        }

        private static Shader ResolveLitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("HDRP/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }

        private static void ApplyColorToMaterial(Material mat, Color color)
        {
            if (mat.HasProperty(BaseColorId))
            {
                mat.SetColor(BaseColorId, color);
            }
            else if (mat.HasProperty(ColorId))
            {
                mat.SetColor(ColorId, color);
            }
            else
            {
                mat.color = color;
            }
        }
    }
}
