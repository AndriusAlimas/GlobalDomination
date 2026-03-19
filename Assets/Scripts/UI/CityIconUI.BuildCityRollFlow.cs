using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalDomination.UI
{
    public partial class CityIconUI
    {
        private void OnActionClicked(string actionName)
        {
            if (actionName == "Build new city")
            {
                CloseActionMenu();
                StartCoroutine(BuildCityRollSceneScope.Run(this, PlayBuildCityDiceRollAnimation));
                return;
            }

            Debug.Log($"City '{linkedCity?.cityName}' selected action: {actionName}");
        }

        private IEnumerator PlayBuildCityDiceRollAnimation(Canvas canvas, Camera sceneCamera)
        {
            if (sceneCamera == null)
            {
                yield break;
            }

            Vector3 originalCamPos = sceneCamera.transform.position;
            Quaternion originalCamRot = sceneCamera.transform.rotation;
            float originalCamFov = sceneCamera.fieldOfView;
            bool originalCamOrtho = sceneCamera.orthographic;

            if (activeDiceOverlay != null)
            {
                Destroy(activeDiceOverlay);
                activeDiceOverlay = null;
            }

            if (activeDiceWorldRoot != null)
            {
                Destroy(activeDiceWorldRoot);
                activeDiceWorldRoot = null;
            }

            GameObject overlayObj = new GameObject("DiceRollOverlay");
            if (canvas != null)
            {
                overlayObj.transform.SetParent(canvas.transform, false);
                overlayObj.transform.SetAsLastSibling();
            }
            activeDiceOverlay = overlayObj;

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayBg = overlayObj.AddComponent<Image>();
            // Keep interactions blocked without darkening the dedicated rolling view.
            overlayBg.color = new Color(0.23f, 0.37f, 0.62f, 0.06f);
            // Block other UI interactions while dice roll is active.
            overlayBg.raycastTarget = true;

            AnimatedDiceWorldContext animatedDiceContext = TryCreateAnimatedD6WorldRoller(sceneCamera);
            if (animatedDiceContext == null)
            {
                if (activeDiceOverlay == overlayObj)
                {
                    Destroy(activeDiceOverlay);
                    activeDiceOverlay = null;
                }

                yield break;
            }

            activeDiceWorldRoot = animatedDiceContext.root;

            // Keep a top-heavy camera angle so the roll is clearly visible from above.
            Vector3 viewCenter = animatedDiceContext.boundsCenter + new Vector3(0f, 0.15f, 0f);
            sceneCamera.orthographic = false;
            sceneCamera.fieldOfView = 30f;
            sceneCamera.transform.position = viewCenter + new Vector3(0f, 30.5f, -4.5f);
            sceneCamera.transform.rotation = Quaternion.LookRotation(viewCenter - sceneCamera.transform.position, Vector3.up);

            // Build colliders from the current screen corners so edge/corner hits always collide.
            RebuildDiceScreenBounds(animatedDiceContext, sceneCamera);

            TextMeshProUGUI resultText = BuildCityDiceUiFactory.CreateDiceText(overlayObj.transform, "Result", 38f, new Vector2(0f, -270f));
            resultText.color = new Color(1f, 0.92f, 0.2f, 1f);
            resultText.text = string.Empty;
            resultText.raycastTarget = false;

            TextMeshProUGUI hintText = BuildCityDiceUiFactory.CreateDiceText(overlayObj.transform, "Hint", 15f, new Vector2(0f, -340f));
            hintText.text = "Hold left mouse to shake up power, release to throw";
            hintText.color = new Color(0.8f, 0.9f, 1f, 0.9f);
            hintText.raycastTarget = false;

            Image handImage = BuildCityDiceUiFactory.CreateDiceHandImage(overlayObj.transform);
            RectTransform handRect = handImage != null ? handImage.rectTransform : null;

            Rigidbody diceRb = animatedDiceContext.rigidbody;
            Transform diceTransform = animatedDiceContext.diceObject != null
                ? animatedDiceContext.diceObject.transform
                : (diceRb != null ? diceRb.transform : null);
            Renderer[] diceRenderers = animatedDiceContext.diceObject != null
                ? animatedDiceContext.diceObject.GetComponentsInChildren<Renderer>(true)
                : new Renderer[0];
            bool releaseDetected = false;
            bool motionDetected = false;
            float settleTimer = 0f;
            bool holdStarted = Input.GetMouseButton(0);
            float holdStartTime = holdStarted ? Time.time : 0f;
            float handReleaseStartTime = -1f;
            float releaseStartTime = -1f;
            Vector2 lastMousePos = Input.mousePosition;
            float shakeTravel = 0f;
            float shakeEnergy = 0f;

            Vector3 flatForward = Vector3.ProjectOnPlane(sceneCamera.transform.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = Vector3.forward;
            }

            Vector3 flatRight = Vector3.ProjectOnPlane(sceneCamera.transform.right, Vector3.up).normalized;
            if (flatRight.sqrMagnitude < 0.001f)
            {
                flatRight = Vector3.right;
            }

            // Randomize setup presets so throw origin can be side, centered, or diagonal.
            int launchPreset = Random.Range(0, 6);
            float sideSign = Random.value < 0.5f ? -1f : 1f;
            float sideDistance;
            float forwardOffset;
            float holdHeight;
            Vector2 handHoldPos;
            Vector2 handReleasePos;
            Vector2 nearZeroThrowFallback;

            if (launchPreset <= 1)
            {
                // Classic side pickup.
                sideDistance = Random.Range(1.9f, 2.75f);
                forwardOffset = Random.Range(-0.65f, 0.55f);
                holdHeight = Random.Range(11.7f, 12.9f);
                handHoldPos = new Vector2(sideSign * Random.Range(145f, 205f), Random.Range(82f, 108f));
                handReleasePos = handHoldPos + new Vector2(-sideSign * Random.Range(120f, 150f), Random.Range(18f, 34f));
                nearZeroThrowFallback = new Vector2(-sideSign * Random.Range(190f, 235f), Random.Range(16f, 40f));
            }
            else if (launchPreset <= 3)
            {
                // Near-center "normal" pickup.
                sideDistance = Random.Range(0.35f, 1.05f) * sideSign;
                forwardOffset = Random.Range(-0.5f, 0.5f);
                holdHeight = Random.Range(11.4f, 12.5f);
                handHoldPos = new Vector2(sideSign * Random.Range(65f, 120f), Random.Range(88f, 116f));
                handReleasePos = handHoldPos + new Vector2(-sideSign * Random.Range(95f, 128f), Random.Range(16f, 30f));
                nearZeroThrowFallback = new Vector2(-sideSign * Random.Range(165f, 205f), Random.Range(14f, 30f));
            }
            else
            {
                // Diagonal pickup (front/back offset) for less predictable trajectories.
                sideDistance = Random.Range(1.2f, 2.2f) * sideSign;
                forwardOffset = Random.Range(-1.2f, 1.2f);
                holdHeight = Random.Range(11.8f, 13.2f);
                handHoldPos = new Vector2(sideSign * Random.Range(120f, 188f), Random.Range(78f, 122f));
                handReleasePos = handHoldPos + new Vector2(-sideSign * Random.Range(105f, 148f), Random.Range(20f, 38f));
                nearZeroThrowFallback = new Vector2(-sideSign * Random.Range(180f, 240f), Random.Range(18f, 45f));
            }

            Vector3 holdAnchor = animatedDiceContext.boundsCenter
                + flatRight * sideDistance
                + flatForward * forwardOffset
                + Vector3.up * holdHeight;
            Quaternion holdRotationBase = Quaternion.Euler(
                Random.Range(12f, 25f),
                sideSign * Random.Range(14f, 42f),
                -sideSign * Random.Range(10f, 30f));

            if (diceRb != null)
            {
                if (!diceRb.isKinematic)
                {
                    diceRb.linearVelocity = Vector3.zero;
                    diceRb.angularVelocity = Vector3.zero;
                }
                diceRb.isKinematic = true;
            }

            SetDiceRenderersVisible(diceRenderers, false);

            while (true)
            {
                if (animatedDiceContext.root == null || diceRb == null || diceTransform == null)
                {
                    break;
                }

                Vector3 pos = diceRb.position;
                bool isOutOfArena = pos.y < animatedDiceContext.floorY - 6f;
                if (isOutOfArena)
                {
                    if (releaseDetected)
                    {
                        break;
                    }

                    if (!diceRb.isKinematic)
                    {
                        diceRb.linearVelocity = Vector3.zero;
                        diceRb.angularVelocity = Vector3.zero;
                    }
                    diceRb.isKinematic = true;
                    diceRb.position = holdAnchor;
                    diceTransform.rotation = holdRotationBase;
                    releaseDetected = false;
                    holdStarted = false;
                    motionDetected = false;
                    settleTimer = 0f;
                    holdStartTime = 0f;
                    handReleaseStartTime = -1f;
                    releaseStartTime = -1f;
                    lastMousePos = Input.mousePosition;
                    shakeTravel = 0f;
                    shakeEnergy = 0f;
                    SetDiceRenderersVisible(diceRenderers, false);
                    if (handImage != null)
                    {
                        handImage.color = Color.white;
                    }
                    if (handRect != null)
                    {
                        handRect.anchoredPosition = handHoldPos;
                        handRect.localRotation = Quaternion.identity;
                    }
                    hintText.text = "Hold left mouse to shake up power, release to throw";
                }

                if (!releaseDetected)
                {
                    SetDiceRenderersVisible(diceRenderers, false);

                    if (!holdStarted && (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)))
                    {
                        holdStarted = true;
                        holdStartTime = Time.time;
                        lastMousePos = Input.mousePosition;
                        shakeTravel = 0f;
                        shakeEnergy = 0f;
                    }

                    float timeCharge = holdStarted ? Mathf.Clamp01((Time.time - holdStartTime) / 0.95f) : 0f;
                    if (holdStarted && Input.GetMouseButton(0))
                    {
                        Vector2 currentMousePos = Input.mousePosition;
                        Vector2 mouseDelta = currentMousePos - lastMousePos;
                        float deltaMagnitude = mouseDelta.magnitude;
                        shakeTravel = Mathf.Min(shakeTravel + deltaMagnitude, 2000f);

                        float normalizedSpeed = deltaMagnitude / (Mathf.Max(1f, Time.deltaTime) * 1100f);
                        shakeEnergy = Mathf.Clamp01(shakeEnergy * 0.88f + normalizedSpeed * 0.2f);
                        lastMousePos = currentMousePos;
                    }
                    else
                    {
                        shakeEnergy = Mathf.Max(0f, shakeEnergy - Time.deltaTime * 1.6f);
                    }

                    float shakeCharge = Mathf.Clamp01(shakeTravel / 540f);
                    float holdCharge = Mathf.Clamp01(timeCharge * 0.35f + shakeCharge * 0.65f);
                    float shakeTime = Time.time * 24f;
                    float worldShakeScale = 0.06f + holdCharge * 0.36f;
                    Vector3 worldShake = flatRight * Mathf.Sin(shakeTime * 1.9f) * worldShakeScale
                        + flatForward * Mathf.Cos(shakeTime * 1.4f) * worldShakeScale * 0.75f
                        + Vector3.up * Mathf.Abs(Mathf.Sin(shakeTime * 2.8f)) * worldShakeScale * 0.35f;

                    diceRb.position = holdAnchor + worldShake;
                    diceTransform.rotation = holdRotationBase * Quaternion.Euler(
                        Mathf.Sin(shakeTime * 2.3f) * (8f + holdCharge * 18f),
                        shakeTime * (6f + holdCharge * 18f),
                        Mathf.Cos(shakeTime * 1.7f) * (5f + holdCharge * 12f));

                    if (handRect != null)
                    {
                        Vector2 targetHandPos = new Vector2(
                            Mathf.Clamp(Input.mousePosition.x - Screen.width * 0.5f, -300f, 300f),
                            Mathf.Clamp(Input.mousePosition.y, 70f, Screen.height * 0.58f));

                        Vector2 handShake = new Vector2(
                            Mathf.Sin(shakeTime * 1.5f),
                            Mathf.Cos(shakeTime * 1.2f)) * (4f + holdCharge * 18f);
                        Vector2 handBasePos = holdStarted ? Vector2.Lerp(handRect.anchoredPosition, targetHandPos, 0.2f) : handHoldPos;
                        handRect.anchoredPosition = handBasePos + handShake;
                        handRect.localRotation = Quaternion.Euler(0f, 0f, -6f + Mathf.Sin(shakeTime * 1.3f) * (2f + holdCharge * 7f));
                    }

                    if (holdStarted && Input.GetMouseButtonUp(0))
                    {
                        releaseDetected = true;
                        handReleaseStartTime = Time.time;
                        releaseStartTime = Time.time;
                        hintText.text = string.Empty;

                        Vector2 releaseHandUiPos = handRect != null ? handRect.anchoredPosition : handHoldPos;
                        handHoldPos = releaseHandUiPos;
                        handReleasePos = releaseHandUiPos + new Vector2(-sideSign * Random.Range(118f, 154f), Random.Range(16f, 36f));
                        Vector2 releaseHandScreenPos = new Vector2(
                            Screen.width * 0.5f + releaseHandUiPos.x,
                            Mathf.Max(8f, releaseHandUiPos.y));

                        float handPlaneY = holdAnchor.y + 0.05f;
                        Plane handThrowPlane = new Plane(Vector3.up, new Vector3(0f, handPlaneY, 0f));

                        Ray handRay = sceneCamera.ScreenPointToRay(releaseHandScreenPos);
                        Vector3 releasePoint = holdAnchor;
                        if (handThrowPlane.Raycast(handRay, out float handHitDist))
                        {
                            releasePoint = handRay.GetPoint(handHitDist);
                        }
                        releasePoint += Vector3.up * 0.06f;

                        Vector2 targetScreenPos = Input.mousePosition;
                        Vector2 releaseToMouse = targetScreenPos - releaseHandScreenPos;
                        if (releaseToMouse.sqrMagnitude < 64f)
                        {
                            // Avoid near-zero throws if the cursor is too close to the release point.
                            targetScreenPos = releaseHandScreenPos + nearZeroThrowFallback;
                        }
                        Ray targetRay = sceneCamera.ScreenPointToRay(targetScreenPos);
                        Vector3 targetPoint = releasePoint + flatRight;
                        if (handThrowPlane.Raycast(targetRay, out float targetHitDist))
                        {
                            targetPoint = targetRay.GetPoint(targetHitDist);
                        }

                        int throwStyle = Random.Range(0, 5);
                        Vector3 styleOffset = Vector3.zero;
                        if (throwStyle == 1)
                        {
                            // Side skim toward walls.
                            styleOffset = flatRight * sideSign * Random.Range(0.9f, 1.7f);
                        }
                        else if (throwStyle == 2)
                        {
                            // Counter-side cross throw.
                            styleOffset = flatRight * -sideSign * Random.Range(0.8f, 1.5f) + flatForward * Random.Range(-0.3f, 0.5f);
                        }
                        else if (throwStyle == 3)
                        {
                            // Forward-heavy push.
                            styleOffset = flatForward * Random.Range(0.9f, 1.7f);
                        }
                        else if (throwStyle == 4)
                        {
                            // Slight backward/diagonal pull.
                            styleOffset = flatForward * Random.Range(-1.2f, -0.35f) + flatRight * sideSign * Random.Range(0.35f, 1.1f);
                        }

                        Vector3 throwDir = Vector3.ProjectOnPlane((targetPoint + styleOffset) - releasePoint, Vector3.up);
                        if (throwDir.sqrMagnitude < 0.0001f)
                        {
                            throwDir = (flatRight * -sideSign) + (flatForward * Random.Range(-0.08f, 0.12f));
                        }
                        throwDir.Normalize();

                        float forceFromShake = Mathf.Lerp(0.62f, 1.45f, holdCharge);
                        float throwImpulse = Mathf.Lerp(5.4f, 9.8f, holdCharge) * forceFromShake * Random.Range(0.92f, 1.14f);
                        float upImpulse = Mathf.Lerp(1.15f, 2.25f, holdCharge) * Mathf.Lerp(0.92f, 1.22f, holdCharge) * Random.Range(0.9f, 1.16f);
                        Vector3 spinAxis = Vector3.Cross(Vector3.up, throwDir).normalized;
                        Vector3 throwSpin = spinAxis * Mathf.Lerp(5f, 9f, holdCharge)
                            + Vector3.up * Mathf.Lerp(7f, 13f, holdCharge);
                        throwSpin += Random.onUnitSphere * Random.Range(2.4f, 6.4f) * Mathf.Lerp(0.8f, 1.35f, holdCharge);
                        Quaternion randomizedReleaseRotation = Random.rotationUniform;

                        SetDiceRenderersVisible(diceRenderers, true);
                        diceRb.isKinematic = false;
                        diceRb.useGravity = true;
                        diceRb.position = releasePoint;
                        diceTransform.rotation = randomizedReleaseRotation;
                        diceRb.rotation = randomizedReleaseRotation;
                        diceRb.linearVelocity = Vector3.zero;
                        diceRb.angularVelocity = Vector3.zero;
                        diceRb.AddForce(throwDir * throwImpulse + Vector3.up * upImpulse, ForceMode.Impulse);
                        diceRb.AddTorque(throwSpin, ForceMode.Impulse);
                        diceRb.WakeUp();
                    }
                }

                if (releaseDetected)
                {
                    if (releaseStartTime > 0f && Time.time - releaseStartTime >= 6.5f)
                    {
                        break;
                    }

                    if (handImage != null && handRect != null && handReleaseStartTime >= 0f)
                    {
                        float releaseT = Mathf.Clamp01((Time.time - handReleaseStartTime) / 0.18f);
                        handRect.anchoredPosition = Vector2.Lerp(handHoldPos, handReleasePos, releaseT);
                        handRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-10f, -24f, releaseT));
                        handImage.color = new Color(1f, 1f, 1f, 1f - releaseT);
                        if (releaseT >= 1f)
                        {
                            Object.Destroy(handImage.gameObject);
                            handImage = null;
                            handRect = null;
                        }
                    }

                    float linearSpeed = diceRb.linearVelocity.magnitude;
                    float angularSpeed = diceRb.angularVelocity.magnitude;
                    if (linearSpeed > 0.2f || angularSpeed > 0.2f)
                    {
                        motionDetected = true;
                    }

                    if (motionDetected)
                    {
                        bool looksSettled = diceRb.IsSleeping()
                            || (linearSpeed < 0.08f && angularSpeed < 0.08f);

                        settleTimer = looksSettled ? settleTimer + Time.deltaTime : 0f;
                        if (settleTimer >= 0.55f)
                        {
                            break;
                        }
                    }
                }

                yield return null;
            }

            if (diceRb != null)
            {
                if (!diceRb.isKinematic)
                {
                    diceRb.linearVelocity = Vector3.zero;
                    diceRb.angularVelocity = Vector3.zero;
                }

                diceRb.useGravity = false;
                diceRb.isKinematic = true;
            }

            int finalRoll = animatedDiceContext.diceStats != null
                ? Mathf.Clamp(animatedDiceContext.diceStats.side, 1, 6)
                : DiceRoller.RollD6();

            resultText.text = $"Result: {finalRoll}";
            Debug.Log($"City '{linkedCity?.cityName}' Build new city roll: {finalRoll}");

            yield return new WaitForSeconds(2.5f);

            sceneCamera.transform.position = originalCamPos;
            sceneCamera.transform.rotation = originalCamRot;
            sceneCamera.fieldOfView = originalCamFov;
            sceneCamera.orthographic = originalCamOrtho;

            animatedDiceContext.Dispose();
            activeDiceWorldRoot = null;

            if (activeDiceOverlay == overlayObj)
            {
                Destroy(activeDiceOverlay);
                activeDiceOverlay = null;
            }
        }
    }
}
