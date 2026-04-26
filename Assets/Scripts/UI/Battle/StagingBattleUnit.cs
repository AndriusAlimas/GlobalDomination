using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GlobalDomination.UI.Battle
{
    /// <summary>
    /// Auto-marches toward a world goal on the XZ plane. Attackers have HP and can be damaged; defenders are static hazards.
    /// Visuals may be a capsule primitive or an optional <see cref="Resources"/> prefab with <see cref="Animator"/>.
    /// </summary>
    public sealed class StagingBattleUnit : MonoBehaviour
    {
        private const string UkSoldierModelResourcePath = "Battle/Countries/England/Units/Soldier/Model/Soldier";
        private const string UkSoldierIdleClipResourcePath = "Battle/Countries/England/Units/Soldier/Animations/Idle";

        [SerializeField] private float stopDistance = 1.2f;

        private float _marchSpeed;
        private Vector3 _marchTarget;
        private bool _autoMarch = true;
        private bool _isAttacker;
        private float _hp;
        private float _maxHp;
        private StagingBattleWorld _world;
        private Vector3 _lastPosition;
        private Animator _animator;
        private bool _animatorScanned;
        private string _walkBoolParameterName;
        private string _speedFloatParameterName;
        private static readonly Color SelectionRingGreen = new Color(0.2f, 0.95f, 0.35f, 1f);
        private static readonly Color ShootCooldownRed = new Color(0.92f, 0.14f, 0.16f, 1f);

        private const int CooldownCircleSegments = 80;
        private const float CooldownRingRadiusLocal = 0.78f;
        private const float CooldownLineLocalY = 0.11f;
        private const float CooldownRedLineWidth = 0.085f;
        private const float CooldownGreenLineWidth = 0.11f;

        private GameObject _selectionRing;
        private GameObject _cooldownLinesRoot;
        private LineRenderer _cooldownRedTrack;
        private LineRenderer _cooldownGreenArc;
        private bool _selectionVisible;
        private float _shootCooldownRemaining;
        private float _shootCooldownDuration;
        private static bool _loggedStaticSoldierMeshWarning;

        public bool IsAttacker => _isAttacker;

        /// <summary>Staging ranged reuse: 1 when ready to shoot, 0 just after firing (attackers only).</summary>
        public float ShootCooldownReadyFraction
        {
            get
            {
                if (!_isAttacker)
                {
                    return 1f;
                }

                if (_shootCooldownDuration <= 0.0001f)
                {
                    return 1f;
                }

                return 1f - Mathf.Clamp01(_shootCooldownRemaining / _shootCooldownDuration);
            }
        }

        public bool CanShoot()
        {
            return _shootCooldownRemaining <= 0f;
        }

        public void NotifyShotFired(float cooldownSeconds)
        {
            if (!_isAttacker || cooldownSeconds <= 0f)
            {
                return;
            }

            _shootCooldownDuration = Mathf.Max(0.01f, cooldownSeconds);
            _shootCooldownRemaining = _shootCooldownDuration;
            if (_selectionVisible && _cooldownLinesRoot != null)
            {
                _cooldownLinesRoot.SetActive(true);
            }
        }

        public void SetAutoMarch(bool enabled)
        {
            _autoMarch = enabled;
        }

        public void SetSelected(bool selected)
        {
            _selectionVisible = selected;
            EnsureSelectionRing();
            if (_isAttacker)
            {
                RefreshShootCooldownLinesActive();
            }
            else if (_selectionRing != null)
            {
                _selectionRing.SetActive(selected);
            }
        }

        public void Configure(
            StagingBattleWorld world,
            Vector3 marchTarget,
            bool autoMarch,
            bool isAttacker,
            float hitPoints,
            Color tint,
            float marchSpeed)
        {
            _world = world;
            _marchTarget = marchTarget;
            _marchSpeed = Mathf.Max(0.05f, marchSpeed);
            _autoMarch = autoMarch;
            _isAttacker = isAttacker;
            _maxHp = hitPoints;
            _hp = hitPoints;
            _lastPosition = transform.position;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;

            StagingBattleLitMaterial.ApplyTeamTintToHierarchy(transform, tint);
            TryAutoUprightSoldierMount(transform);
            TryNormalizeImportedModelScaleAndEnableRenderers(transform);
            ReplayStagingIdleIfPresent(transform);
            CacheAnimator();
            ScanAnimatorParametersIfNeeded();
        }

        /// <summary>
        /// Staging battle default: load England soldier from Resources and play idle. Hides root primitive mesh (capsule) when a model is present.
        /// </summary>
        internal static void EnsureDefaultSoldierVisual(Transform unitRoot)
        {
            if (unitRoot == null)
            {
                return;
            }

            Transform soldierModel = unitRoot.Find("SoldierModel");
            if (soldierModel == null)
            {
                GameObject go = new GameObject("SoldierModel");
                soldierModel = go.transform;
                soldierModel.SetParent(unitRoot, false);
                soldierModel.localPosition = Vector3.zero;
                soldierModel.localRotation = Quaternion.identity;
                soldierModel.localScale = Vector3.one;
            }
            else
            {
                soldierModel.localPosition = Vector3.zero;
                soldierModel.localRotation = Quaternion.identity;
                if (soldierModel.localScale.sqrMagnitude < 1e-8f)
                {
                    soldierModel.localScale = Vector3.one;
                }
            }

            bool modelInstantiated = false;
            GameObject modelInstance = null;
            if (soldierModel.childCount == 0)
            {
                GameObject modelPrefab = Resources.Load<GameObject>(UkSoldierModelResourcePath);
                if (modelPrefab != null)
                {
                    modelInstance = Object.Instantiate(modelPrefab, soldierModel, false);
                    modelInstance.name = "Model";
                    modelInstance.transform.localPosition = Vector3.zero;
                    modelInstance.transform.localRotation = Quaternion.identity;
                    modelInstance.transform.localScale = Vector3.one;
                    modelInstantiated = true;
                }
            }
            else
            {
                modelInstantiated = true;
                if (soldierModel.childCount > 0)
                {
                    modelInstance = soldierModel.GetChild(0).gameObject;
                }
            }

            if (modelInstantiated)
            {
                HideRootPrimitiveMeshIfPresent(unitRoot);
            }

            TryStandStaticMeshModelIfNoSkin(soldierModel);
            TryAutoUprightSoldierMount(unitRoot);

            AnimationClip idle = ResolveIdleClip();
            if (idle == null)
            {
                return;
            }

            Animator rigAnimator = ResolveStagingRigAnimator(unitRoot);
            if (rigAnimator == null && modelInstance == null)
            {
                rigAnimator = unitRoot.gameObject.AddComponent<Animator>();
            }

            TryCopyAvatarFromSoldierPrefab(rigAnimator);
            WarnIfSoldierMeshCannotDeform(unitRoot);

            StagingBattleIdlePlayablePlayer player = unitRoot.GetComponent<StagingBattleIdlePlayablePlayer>();
            if (player == null)
            {
                player = unitRoot.gameObject.AddComponent<StagingBattleIdlePlayablePlayer>();
            }

            player.PlayIfNotAlready(rigAnimator, idle);
        }

        private static void WarnIfSoldierMeshCannotDeform(Transform unitRoot)
        {
            if (_loggedStaticSoldierMeshWarning || unitRoot == null)
            {
                return;
            }

            Transform soldierModel = unitRoot.Find("SoldierModel");
            if (soldierModel == null)
            {
                return;
            }

            if (soldierModel.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            {
                return;
            }

            MeshRenderer staticMesh = soldierModel.GetComponentInChildren<MeshRenderer>(true);
            Animator animator = soldierModel.GetComponentInChildren<Animator>(true);
            if (staticMesh == null || animator == null)
            {
                return;
            }

            _loggedStaticSoldierMeshWarning = true;
            Debug.LogWarning(
                "[StagingBattle] Soldier has an Animator/bones, but the visible mesh is a MeshRenderer, not a SkinnedMeshRenderer. "
                + "Idle.fbx can animate the mixamorig bones, but the soldier body will stay frozen until the FBX is exported/imported as a skinned mesh.");
        }

        private static Animator ResolveStagingRigAnimator(Transform unitRoot)
        {
            if (unitRoot == null)
            {
                return null;
            }

            Transform soldierModel = unitRoot.Find("SoldierModel");
            if (soldierModel == null || soldierModel.childCount == 0)
            {
                return unitRoot.GetComponentInChildren<Animator>(true);
            }

            Transform modelTransform = soldierModel.GetChild(0);
            Animator rigAnimator = modelTransform.GetComponentInChildren<Animator>(true);
            if (rigAnimator == null)
            {
                rigAnimator = modelTransform.gameObject.AddComponent<Animator>();
            }

            return rigAnimator;
        }

        /// <summary>
        /// Tripo / single-mesh exports often have character "height" along local Z (or X) with a <see cref="MeshFilter"/> only.
        /// Rotate the instantiated <c>Model</c> root so world AABB height is maximized before mount-level upright passes.
        /// </summary>
        private static void TryStandStaticMeshModelIfNoSkin(Transform soldierModel)
        {
            if (soldierModel == null || soldierModel.childCount == 0)
            {
                return;
            }

            Transform model = soldierModel.GetChild(0);
            if (model.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            {
                return;
            }

            MeshFilter mf = model.GetComponentInChildren<MeshFilter>(true);
            if (mf == null || mf.sharedMesh == null)
            {
                return;
            }

            Vector3 s = mf.sharedMesh.bounds.size;
            if (s.y >= Mathf.Max(s.x, s.z) - 0.0001f)
            {
                return;
            }

            Quaternion saved = model.localRotation;
            float best = MeasureSoldierWorldHeight(soldierModel);
            Quaternion bestQ = saved;

            Vector3[] eulers = new Vector3[]
            {
                new Vector3(-90f, 0f, 0f),
                new Vector3(90f, 0f, 0f),
                new Vector3(0f, 0f, -90f),
                new Vector3(0f, 0f, 90f),
                new Vector3(0f, -90f, 0f),
                new Vector3(0f, 90f, 0f),
            };

            for (int i = 0; i < eulers.Length; i++)
            {
                model.localRotation = Quaternion.Euler(eulers[i]) * saved;
                float h = MeasureSoldierWorldHeight(soldierModel);
                if (h > best + 1e-4f)
                {
                    best = h;
                    bestQ = model.localRotation;
                }
            }

            model.localRotation = bestQ;
        }

        private static float MeasureSoldierWorldHeight(Transform soldierModel)
        {
            if (soldierModel == null)
            {
                return 0f;
            }

            Renderer[] renderers = soldierModel.GetComponentsInChildren<Renderer>(true);
            return MergedWorldBoundsExcludingLineRenderers(renderers).size.y;
        }

        private static void TryCopyAvatarFromSoldierPrefab(Animator rigAnimator)
        {
            if (rigAnimator == null)
            {
                return;
            }

            if (rigAnimator.avatar != null && rigAnimator.avatar.isValid)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(UkSoldierModelResourcePath);
            if (prefab == null)
            {
                return;
            }

            Animator source = prefab.GetComponentInChildren<Animator>(true);
            if (source != null && source.avatar != null && source.avatar.isValid)
            {
                rigAnimator.avatar = source.avatar;
            }
        }

        private static void TryAutoUprightSoldierMount(Transform unitRoot)
        {
            if (unitRoot == null)
            {
                return;
            }

            Transform soldierModel = unitRoot.Find("SoldierModel");
            if (soldierModel == null)
            {
                return;
            }

            Renderer[] renderers = soldierModel.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            int usable = 0;
            for (int r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] != null && !(renderers[r] is LineRenderer))
                {
                    usable++;
                }
            }

            if (usable == 0)
            {
                return;
            }

            float WorldHeight()
            {
                return MergedWorldBoundsExcludingLineRenderers(renderers).size.y;
            }

            float bestHeight = WorldHeight();
            Quaternion bestLocal = soldierModel.localRotation;

            Vector3[] eulerCandidates = new Vector3[]
            {
                Vector3.zero,
                new Vector3(90f, 0f, 0f),
                new Vector3(-90f, 0f, 0f),
                new Vector3(0f, 90f, 0f),
                new Vector3(0f, -90f, 0f),
                new Vector3(0f, 0f, 90f),
                new Vector3(0f, 0f, -90f),
                new Vector3(180f, 0f, 0f),
            };

            for (int i = 0; i < eulerCandidates.Length; i++)
            {
                soldierModel.localRotation = Quaternion.Euler(eulerCandidates[i]);
                float h = WorldHeight();
                if (h > bestHeight + 1e-4f)
                {
                    bestHeight = h;
                    bestLocal = soldierModel.localRotation;
                }
            }

            soldierModel.localRotation = bestLocal;
            soldierModel.localPosition = Vector3.zero;
            TryFlipSoldierIfRootBonePointsDown(soldierModel);
            TryGroundAlignSoldierModel(unitRoot, soldierModel);
        }

        private static void TryGroundAlignSoldierModel(Transform unitRoot, Transform soldierModel)
        {
            if (unitRoot == null || soldierModel == null)
            {
                return;
            }

            if (ModelUpDotWorldUp(soldierModel) < 0.35f)
            {
                return;
            }

            Renderer[] renderers = soldierModel.GetComponentsInChildren<Renderer>(true);
            Bounds b = MergedWorldBoundsExcludingLineRenderers(renderers);
            if (b.size.sqrMagnitude < 1e-8f)
            {
                return;
            }

            // Battle ground is the XZ plane at y = 0 (see StagingBattleWorld ground primitive).
            const float groundPlaneY = 0f;
            float deltaY = groundPlaneY - b.min.y;
            if (Mathf.Abs(deltaY) < 1e-4f)
            {
                return;
            }

            soldierModel.position += new Vector3(0f, deltaY, 0f);
        }

        private static float ModelUpDotWorldUp(Transform soldierModel)
        {
            if (soldierModel == null)
            {
                return 1f;
            }

            SkinnedMeshRenderer smr = soldierModel.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr != null && smr.rootBone != null)
            {
                Vector3 modelUp = smr.rootBone.rotation * Vector3.up;
                if (modelUp.sqrMagnitude > 1e-10f)
                {
                    return Vector3.Dot(modelUp.normalized, Vector3.up);
                }
            }

            if (soldierModel.childCount > 0)
            {
                Transform modelRoot = soldierModel.GetChild(0);
                Vector3 up = modelRoot.up;
                if (up.sqrMagnitude > 1e-10f)
                {
                    return Vector3.Dot(up.normalized, Vector3.up);
                }
            }

            return 1f;
        }

        private static void TryFlipSoldierIfRootBonePointsDown(Transform soldierModel)
        {
            if (soldierModel == null)
            {
                return;
            }

            if (ModelUpDotWorldUp(soldierModel) > -0.25f)
            {
                return;
            }

            Quaternion saved = soldierModel.localRotation;
            Vector3[] flips = new Vector3[]
            {
                new Vector3(180f, 0f, 0f),
                new Vector3(0f, 180f, 0f),
                new Vector3(0f, 0f, 180f),
            };

            for (int i = 0; i < flips.Length; i++)
            {
                soldierModel.localRotation = Quaternion.Euler(flips[i]) * saved;
                if (ModelUpDotWorldUp(soldierModel) > -0.25f)
                {
                    return;
                }
            }

            soldierModel.localRotation = saved;
        }

        private static Bounds MergedWorldBoundsExcludingLineRenderers(Renderer[] renderers)
        {
            Bounds? merged = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || r is LineRenderer)
                {
                    continue;
                }

                if (merged == null)
                {
                    merged = r.bounds;
                }
                else
                {
                    Bounds b = merged.Value;
                    b.Encapsulate(r.bounds);
                    merged = b;
                }
            }

            if (merged == null)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            return merged.Value;
        }

        private static void ReplayStagingIdleIfPresent(Transform unitRoot)
        {
            if (unitRoot == null)
            {
                return;
            }

            StagingBattleIdlePlayablePlayer player = unitRoot.GetComponent<StagingBattleIdlePlayablePlayer>();
            if (player == null)
            {
                return;
            }

            AnimationClip idle = ResolveIdleClip();
            Animator rig = ResolveStagingRigAnimator(unitRoot);
            if (idle == null || rig == null)
            {
                return;
            }

            TryCopyAvatarFromSoldierPrefab(rig);
            player.ReplayAfterOrientationChange(rig, idle);
        }

        private static AnimationClip ResolveIdleClip()
        {
            AnimationClip[] clips = Resources.LoadAll<AnimationClip>(UkSoldierIdleClipResourcePath);
            AnimationClip picked = PickClip(clips);
            if (picked != null)
            {
                return picked;
            }

            AnimationClip directIdle = Resources.Load<AnimationClip>(UkSoldierIdleClipResourcePath);
            if (directIdle != null)
            {
                return directIdle;
            }

            AnimationClip[] fromModel = Resources.LoadAll<AnimationClip>(UkSoldierModelResourcePath);
            return PickClip(fromModel);
        }

        private static void HideRootPrimitiveMeshIfPresent(Transform unitRoot)
        {
            if (unitRoot == null)
            {
                return;
            }

            MeshRenderer mr = unitRoot.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = false;
            }
        }

        private static AnimationClip PickClip(AnimationClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip c = clips[i];
                if (c == null)
                {
                    continue;
                }

                string n = c.name;
                if (string.IsNullOrEmpty(n))
                {
                    continue;
                }

                string lower = n.ToLowerInvariant();
                if (lower.Contains("__preview__"))
                {
                    continue;
                }

                if (lower.Contains("idle"))
                {
                    return c;
                }
            }

            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip c = clips[i];
                if (c == null)
                {
                    continue;
                }

                string n = c.name;
                if (string.IsNullOrEmpty(n))
                {
                    continue;
                }

                if (n.ToLowerInvariant().Contains("__preview__"))
                {
                    continue;
                }

                return c;
            }

            return clips[0];
        }

        /// <summary>
        /// Image-to-3D / FBX imports often come in at millimeter or huge scale; normalize so units are visible from the RTS camera.
        /// </summary>
        private static void TryNormalizeImportedModelScaleAndEnableRenderers(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Transform visual = root.Find("SoldierModel");
            if (visual == null && root.childCount > 0)
            {
                visual = root.GetChild(0);
            }

            if (visual == null)
            {
                return;
            }

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            Bounds? merged = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || r is LineRenderer)
                {
                    continue;
                }

                r.enabled = true;
                if (merged == null)
                {
                    merged = r.bounds;
                }
                else
                {
                    Bounds b = merged.Value;
                    b.Encapsulate(r.bounds);
                    merged = b;
                }
            }

            if (merged == null)
            {
                return;
            }

            float h = merged.Value.size.y;
            const float targetHeight = 1.65f;
            if (h < 0.18f || h > 14f)
            {
                float factor = targetHeight / Mathf.Max(h, 1e-4f);
                Vector3 s = visual.localScale;
                visual.localScale = new Vector3(s.x * factor, s.y * factor, s.z * factor);
            }
        }

        private void EnsureSelectionRing()
        {
            if (_isAttacker)
            {
                EnsureShootCooldownRingLines();
                return;
            }

            if (_selectionRing != null)
            {
                return;
            }

            _selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _selectionRing.name = "SelectionRing";
            _selectionRing.transform.SetParent(transform, false);
            _selectionRing.transform.localScale = new Vector3(1.15f, 0.04f, 1.15f);
            _selectionRing.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            Object.Destroy(_selectionRing.GetComponent<Collider>());
            MeshRenderer mr = _selectionRing.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                StagingBattleLitMaterial.ApplyColor(mr, SelectionRingGreen);
            }

            _selectionRing.SetActive(false);
        }

        /// <summary>
        /// Red ring track + green arc that grows clockwise around it as shoot cooldown completes (attackers).
        /// </summary>
        private void EnsureShootCooldownRingLines()
        {
            if (!_isAttacker || _cooldownLinesRoot != null)
            {
                return;
            }

            _cooldownLinesRoot = new GameObject("ShootCooldownRingLines");
            _cooldownLinesRoot.transform.SetParent(transform, false);
            _cooldownLinesRoot.transform.localPosition = Vector3.zero;

            GameObject redGo = new GameObject("CooldownRedTrack");
            redGo.transform.SetParent(_cooldownLinesRoot.transform, false);
            _cooldownRedTrack = redGo.AddComponent<LineRenderer>();
            ConfigureCooldownLineRenderer(_cooldownRedTrack, ShootCooldownRed, CooldownRedLineWidth);
            _cooldownRedTrack.loop = true;
            _cooldownRedTrack.positionCount = CooldownCircleSegments;

            GameObject greenGo = new GameObject("CooldownGreenArc");
            greenGo.transform.SetParent(_cooldownLinesRoot.transform, false);
            _cooldownGreenArc = greenGo.AddComponent<LineRenderer>();
            ConfigureCooldownLineRenderer(_cooldownGreenArc, SelectionRingGreen, CooldownGreenLineWidth);
            _cooldownGreenArc.sortingOrder = 9;
            _cooldownGreenArc.loop = false;
            _cooldownGreenArc.positionCount = 2;
            _cooldownGreenArc.SetPosition(0, Vector3.zero);
            _cooldownGreenArc.SetPosition(1, Vector3.zero);

            _cooldownLinesRoot.SetActive(false);
        }

        private static void ConfigureCooldownLineRenderer(LineRenderer lr, Color color, float width)
        {
            lr.useWorldSpace = true;
            lr.numCornerVertices = 3;
            lr.numCapVertices = 3;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.startColor = Color.white;
            lr.endColor = Color.white;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = 8;
            lr.maskInteraction = SpriteMaskInteraction.None;
            lr.alignment = LineAlignment.View;

            Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
            if (sh != null)
            {
                Material mat = new Material(sh);
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", color);
                }

                lr.material = mat;
            }
        }

        private Vector3 CooldownRingPointWorld(float angleRadians, float radiusLocal, float yLocal)
        {
            Vector3 local = new Vector3(
                Mathf.Cos(angleRadians) * radiusLocal,
                yLocal,
                Mathf.Sin(angleRadians) * radiusLocal);
            return transform.TransformPoint(local);
        }

        private void RefreshShootCooldownLinesActive()
        {
            if (_cooldownLinesRoot == null)
            {
                return;
            }

            _cooldownLinesRoot.SetActive(_selectionVisible);
        }

        private void CacheAnimator()
        {
            Transform soldier = transform.Find("SoldierModel");
            if (soldier != null && soldier.childCount > 0)
            {
                Transform model = soldier.GetChild(0);
                Animator onRig = model.GetComponentInChildren<Animator>(true);
                if (onRig != null)
                {
                    _animator = onRig;
                    return;
                }
            }

            _animator = GetComponentInChildren<Animator>(true);
        }

        private void ScanAnimatorParametersIfNeeded()
        {
            if (_animatorScanned || _animator == null || _animator.runtimeAnimatorController == null)
            {
                _animatorScanned = true;
                return;
            }

            AnimatorControllerParameter[] parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter p = parameters[i];
                if (p.type == AnimatorControllerParameterType.Bool && string.IsNullOrEmpty(_walkBoolParameterName))
                {
                    string n = p.name;
                    if (IsLikelyWalkBool(n))
                    {
                        _walkBoolParameterName = n;
                    }
                }

                if (p.type == AnimatorControllerParameterType.Float && string.IsNullOrEmpty(_speedFloatParameterName))
                {
                    string n = p.name;
                    if (IsLikelySpeedFloat(n))
                    {
                        _speedFloatParameterName = n;
                    }
                }
            }

            _animatorScanned = true;
        }

        private static bool IsLikelyWalkBool(string n)
        {
            string lower = n.ToLowerInvariant();
            return lower.Contains("walk") || lower.Contains("move") || lower.Contains("run") || lower == "grounded";
        }

        private static bool IsLikelySpeedFloat(string n)
        {
            string lower = n.ToLowerInvariant();
            return lower.Contains("speed") || lower.Contains("forward") || lower.Contains("velocity") || lower.Contains("move");
        }

        public void SetMarchTarget(Vector3 world)
        {
            _marchTarget = world;
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || _hp <= 0f)
            {
                return;
            }

            _hp -= amount;
            if (_hp > 0f)
            {
                return;
            }

            if (_isAttacker)
            {
                _world?.NotifyAttackerDestroyed(this);
            }

            Destroy(gameObject);
        }

        private void Update()
        {
            if (_isAttacker && _shootCooldownRemaining > 0f)
            {
                _shootCooldownRemaining = Mathf.Max(0f, _shootCooldownRemaining - Time.deltaTime);
            }

            Vector3 p = transform.position;
            Vector3 flatTarget = new Vector3(_marchTarget.x, p.y, _marchTarget.z);
            Vector3 toGoal = flatTarget - p;
            toGoal.y = 0f;
            float dist = toGoal.magnitude;

            if (!_autoMarch || dist <= stopDistance)
            {
                return;
            }

            Vector3 march = toGoal.normalized * _marchSpeed;
            p += march * Time.deltaTime;
            transform.position = p;
            march.y = 0f;
            if (march.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(march.normalized, Vector3.up),
                    8f * Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            UpdateAnimatorPresentation();
            UpdateShootCooldownLinesPresentation();
            StagingBattleIdlePlayablePlayer idlePlayer = GetComponent<StagingBattleIdlePlayablePlayer>();
            if (idlePlayer != null)
            {
                idlePlayer.TickIdlePlayback(IsActivelyMarching());
            }
        }

        /// <summary>Matches the movement gate in <see cref="Update"/>.</summary>
        private bool IsActivelyMarching()
        {
            if (!_autoMarch)
            {
                return false;
            }

            Vector3 p = transform.position;
            Vector3 flatTarget = new Vector3(_marchTarget.x, p.y, _marchTarget.z);
            Vector3 toGoal = flatTarget - p;
            toGoal.y = 0f;
            return toGoal.sqrMagnitude > stopDistance * stopDistance;
        }

        private void UpdateShootCooldownLinesPresentation()
        {
            if (!_isAttacker || _cooldownLinesRoot == null || _cooldownGreenArc == null
                || _cooldownRedTrack == null || !_selectionVisible || !_cooldownLinesRoot.activeInHierarchy)
            {
                return;
            }

            float r = CooldownRingRadiusLocal;
            float y = CooldownLineLocalY;

            if (CanShoot())
            {
                _cooldownRedTrack.gameObject.SetActive(false);
                _cooldownGreenArc.loop = true;
                _cooldownGreenArc.positionCount = CooldownCircleSegments;
                for (int i = 0; i < CooldownCircleSegments; i++)
                {
                    float a = (i / (float)CooldownCircleSegments) * Mathf.PI * 2f;
                    _cooldownGreenArc.SetPosition(i, CooldownRingPointWorld(a, r, y));
                }

                return;
            }

            _cooldownRedTrack.gameObject.SetActive(true);
            _cooldownGreenArc.loop = false;
            for (int i = 0; i < CooldownCircleSegments; i++)
            {
                float a = (i / (float)CooldownCircleSegments) * Mathf.PI * 2f;
                _cooldownRedTrack.SetPosition(i, CooldownRingPointWorld(a, r, y));
            }

            float f = ShootCooldownReadyFraction;
            float startAngle = Mathf.PI * 0.5f;

            if (f <= 0.0001f)
            {
                Vector3 p0 = CooldownRingPointWorld(startAngle, r, y);
                _cooldownGreenArc.positionCount = 2;
                _cooldownGreenArc.SetPosition(0, p0);
                _cooldownGreenArc.SetPosition(1, p0);
                return;
            }

            int steps = Mathf.Max(2, Mathf.CeilToInt(CooldownCircleSegments * f) + 1);
            _cooldownGreenArc.positionCount = steps;
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float ang = startAngle - t * f * 2f * Mathf.PI;
                _cooldownGreenArc.SetPosition(i, CooldownRingPointWorld(ang, r, y));
            }
        }

        private void UpdateAnimatorPresentation()
        {
            if (_animator == null)
            {
                return;
            }

            // Idle is driven by Playables on this root; do not set Animator.speed to 0 while stationary (defenders).
            if (GetComponent<StagingBattleIdlePlayablePlayer>() != null)
            {
                return;
            }

            Vector3 planarDelta = new Vector3(
                transform.position.x - _lastPosition.x,
                0f,
                transform.position.z - _lastPosition.z);
            _lastPosition = transform.position;

            bool moving = planarDelta.sqrMagnitude > 0.0000001f;
            if (!_autoMarch)
            {
                moving = false;
            }

            if (_animator.runtimeAnimatorController == null)
            {
                _animator.speed = moving ? 1f : 0f;
                return;
            }

            if (!string.IsNullOrEmpty(_walkBoolParameterName))
            {
                _animator.SetBool(_walkBoolParameterName, moving);
            }

            if (!string.IsNullOrEmpty(_speedFloatParameterName))
            {
                _animator.SetFloat(_speedFloatParameterName, moving ? 1f : 0f);
            }

            if (string.IsNullOrEmpty(_walkBoolParameterName) && string.IsNullOrEmpty(_speedFloatParameterName))
            {
                _animator.speed = moving ? 1f : 0f;
            }
        }

        private sealed class StagingBattleIdlePlayablePlayer : MonoBehaviour
        {
            private PlayableGraph _graph;
            private AnimationClip _clip;
            private Animator _targetAnimator;
            private AnimationClipPlayable _clipPlayable;
            private bool _playing;

            private void OnDestroy()
            {
                if (_graph.IsValid())
                {
                    _graph.Destroy();
                }
            }

            public void TickIdlePlayback(bool isActivelyMarching)
            {
                MaintainClipLoopWrap();
                if (!_playing || !_graph.IsValid() || !_clipPlayable.IsValid())
                {
                    return;
                }

                _clipPlayable.SetSpeed(isActivelyMarching ? 0f : 1f);
                _graph.Play();
            }

            private void MaintainClipLoopWrap()
            {
                if (!_playing || !_graph.IsValid() || !_clipPlayable.IsValid() || _clip == null)
                {
                    return;
                }

                float len = _clip.length;
                if (len <= 0.0001f || _clip.isLooping)
                {
                    return;
                }

                double t = _clipPlayable.GetTime();
                if (t < len - 1e-5)
                {
                    return;
                }

                _clipPlayable.SetTime(t % len);
            }

            public void ReplayAfterOrientationChange(Animator animator, AnimationClip clip)
            {
                _playing = false;
                _targetAnimator = null;
                PlayIfNotAlready(animator, clip);
            }

            public void PlayIfNotAlready(Animator animator, AnimationClip clip)
            {
                if (_playing && _clip == clip && _graph.IsValid() && _targetAnimator == animator)
                {
                    return;
                }

                if (_graph.IsValid())
                {
                    _graph.Destroy();
                }

                if (animator == null || clip == null)
                {
                    return;
                }

                _clip = clip;
                _targetAnimator = animator;
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;

                _graph = PlayableGraph.Create($"{nameof(StagingBattleIdlePlayablePlayer)}_{animator.gameObject.name}");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Idle", animator);
                AnimationClipPlayable playable = AnimationClipPlayable.Create(_graph, clip);
                _clipPlayable = playable;
                playable.SetSpeed(1f);
                output.SetSourcePlayable(playable);
                _graph.Play();
                _playing = true;
            }
        }
    }
}
