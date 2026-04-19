using UnityEngine;

namespace GlobalDomination.UI.Battle
{
    /// <summary>
    /// Avoids <see cref="Renderer.material"/> on primitives (can throw when internal material source is null).
    /// </summary>
    public static class StagingBattleLitMaterial
    {
        public static void ApplyColor(MeshRenderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("HDRP/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
            if (shader == null)
            {
                return;
            }

            Material mat = new Material(shader);
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
    }
}
