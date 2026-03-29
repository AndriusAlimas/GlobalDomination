using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GlobalDomination.UI
{
    /// <summary>
    /// Creates an isolated temporary scene for the build-city dice roll and restores the previous UI/camera state.
    /// </summary>
    public static class BuildCityRollSceneScope
    {
        public static bool IsRollInProgress { get; private set; }

        private static Camera savedSourceCamera;
        private static bool savedSourceCameraWasEnabled;
        private static Scene savedRollScene;
        private static readonly List<CanvasState> savedHiddenCanvases = new List<CanvasState>();

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

            IsRollInProgress = true;

            Camera sourceCamera = Camera.main;
            bool sourceCameraWasEnabled = sourceCamera != null && sourceCamera.enabled;
            List<CanvasState> hiddenCanvases = new List<CanvasState>();

            savedSourceCamera = sourceCamera;
            savedSourceCameraWasEnabled = sourceCameraWasEnabled;
            savedHiddenCanvases.Clear();

            Scene rollScene = SceneManager.CreateScene("BuildCityRollSceneRuntime");
            savedRollScene = rollScene;

            GameObject rollCameraObj = new GameObject("BuildCityRollCamera");
            Camera rollCamera = rollCameraObj.AddComponent<Camera>();
            rollCamera.clearFlags = CameraClearFlags.SolidColor;
            rollCamera.backgroundColor = new Color(0.23f, 0.37f, 0.62f, 1f);
            rollCamera.fieldOfView = 60f;
            rollCamera.nearClipPlane = 0.1f;
            rollCamera.farClipPlane = 1000f;
            rollCameraObj.tag = "MainCamera";

            Canvas rollCanvas = RuntimeUiCanvasHelper.CreateScreenSpaceOverlayCanvas("BuildCityRollCanvas", new Vector2(1920f, 1080f));
            GameObject rollCanvasObj = rollCanvas.gameObject;

            SceneManager.MoveGameObjectToScene(rollCameraObj, rollScene);
            SceneManager.MoveGameObjectToScene(rollCanvasObj, rollScene);

            if (sourceCamera != null)
            {
                sourceCamera.enabled = false;
            }

            HideAllOtherCanvases(rollCanvas, hiddenCanvases);
            savedHiddenCanvases.AddRange(hiddenCanvases);

            IEnumerator rollRoutine = rollRoutineFactory(rollCanvas, rollCamera);
            if (rollRoutine != null)
            {
                yield return host.StartCoroutine(rollRoutine);
            }

            IsRollInProgress = false;

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

        /// <summary>
        /// Force-resets the in-progress state and restores the source camera and canvases.
        /// Call this when aborting mid-roll (e.g. dev skip).
        /// </summary>
        public static void ForceReset()
        {
            IsRollInProgress = false;

            RestoreHiddenCanvases(savedHiddenCanvases);
            savedHiddenCanvases.Clear();

            if (savedSourceCamera != null)
            {
                savedSourceCamera.enabled = savedSourceCameraWasEnabled;
            }

            if (savedRollScene.IsValid() && savedRollScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(savedRollScene);
            }

            savedSourceCamera = null;
            savedRollScene = default;
        }
    }
}
