using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GlobalDomination.UI
{
    /// <summary>
    /// Creates an isolated temporary scene for the build-city dice roll and restores the previous UI/camera state.
    /// </summary>
    public static class BuildCityRollSceneScope
    {
        private struct CanvasState
        {
            public Canvas canvas;
            public bool wasEnabled;
        }

        public static IEnumerator Run(MonoBehaviour host, Func<Canvas, Camera, IEnumerator> rollRoutineFactory)
        {
            if (host == null || rollRoutineFactory == null)
            {
                yield break;
            }

            Camera sourceCamera = Camera.main;
            bool sourceCameraWasEnabled = sourceCamera != null && sourceCamera.enabled;
            List<CanvasState> hiddenCanvases = new List<CanvasState>();

            Scene rollScene = SceneManager.CreateScene("BuildCityRollSceneRuntime");

            GameObject rollCameraObj = new GameObject("BuildCityRollCamera");
            Camera rollCamera = rollCameraObj.AddComponent<Camera>();
            rollCamera.clearFlags = CameraClearFlags.SolidColor;
            rollCamera.backgroundColor = new Color(0.23f, 0.37f, 0.62f, 1f);
            rollCamera.fieldOfView = 60f;
            rollCamera.nearClipPlane = 0.1f;
            rollCamera.farClipPlane = 1000f;
            rollCameraObj.tag = "MainCamera";

            GameObject rollCanvasObj = new GameObject("BuildCityRollCanvas");
            Canvas rollCanvas = rollCanvasObj.AddComponent<Canvas>();
            rollCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = rollCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            rollCanvasObj.AddComponent<GraphicRaycaster>();

            SceneManager.MoveGameObjectToScene(rollCameraObj, rollScene);
            SceneManager.MoveGameObjectToScene(rollCanvasObj, rollScene);

            if (sourceCamera != null)
            {
                sourceCamera.enabled = false;
            }

            HideAllOtherCanvases(rollCanvas, hiddenCanvases);

            IEnumerator rollRoutine = rollRoutineFactory(rollCanvas, rollCamera);
            if (rollRoutine != null)
            {
                yield return host.StartCoroutine(rollRoutine);
            }

            RestoreHiddenCanvases(hiddenCanvases);

            if (sourceCamera != null)
            {
                sourceCamera.enabled = sourceCameraWasEnabled;
            }

            if (rollScene.IsValid() && rollScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(rollScene);
                while (unload != null && !unload.isDone)
                {
                    yield return null;
                }
            }
        }

        private static void HideAllOtherCanvases(Canvas keepCanvas, List<CanvasState> hiddenCanvases)
        {
            if (hiddenCanvases == null)
            {
                return;
            }

            hiddenCanvases.Clear();
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || canvas == keepCanvas)
                {
                    continue;
                }

                CanvasState state = new CanvasState
                {
                    canvas = canvas,
                    wasEnabled = canvas.enabled
                };

                hiddenCanvases.Add(state);
                canvas.enabled = false;
            }
        }

        private static void RestoreHiddenCanvases(List<CanvasState> hiddenCanvases)
        {
            if (hiddenCanvases == null)
            {
                return;
            }

            for (int i = 0; i < hiddenCanvases.Count; i++)
            {
                CanvasState state = hiddenCanvases[i];
                if (state.canvas != null)
                {
                    state.canvas.enabled = state.wasEnabled;
                }
            }

            hiddenCanvases.Clear();
        }
    }
}
