using UnityEngine;
using UnityEngine.UI;

namespace GlobalDomination.UI.Hud
{
    /// <summary>
    /// Shared helper for canvases created at runtime (HUD, roll overlays, etc.).
    /// </summary>
    public static class RuntimeUiCanvasHelper
    {
        public static Canvas CreateScreenSpaceOverlayCanvas(string objectName, Vector2? referenceResolution = null)
        {
            GameObject canvasObject = new GameObject(objectName);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            if (referenceResolution.HasValue)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = referenceResolution.Value;
            }

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }
    }
}
