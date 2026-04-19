using System.Collections.Generic;
using GlobalDomination.GameData;
using UnityEngine;

namespace GlobalDomination.UI.Battle
{
    /// <summary>
    /// 3D assault view: attackers march on the city. Battle ends when the city is destroyed (HP to 0) or all attackers are eliminated.
    /// </summary>
    public sealed class StagingBattleWorld : MonoBehaviour
    {
        private const int GridCols = 4;
        private const int GridRows = 6;
        private const float CityMaxHp = 120f;
        private const float SiegeRadius = 4.2f;
        private const float SiegeDpsPerAttacker = 9f;
        private const float AttackerHitPoints = 32f;

        private AttackStagingSummary _summary;
        private System.Action _onExit;
        private readonly List<Camera> _disabledCameras = new List<Camera>();
        private readonly List<StagingBattleUnit> _aliveAttackers = new List<StagingBattleUnit>();
        private Transform _cityRoot;
        private float _cityHp;
        private bool _battleEnded;

        public void Initialize(AttackStagingSummary summary, System.Action onExit)
        {
            _summary = summary;
            _onExit = onExit;
            _cityHp = CityMaxHp;

            DisableOtherCameras();
            BuildLightsAndGround();
            BuildCityMarker();
            BuildRtsCamera();
            SpawnAttackerFormation();
            SpawnDefenderSkirmishers();

            if (_aliveAttackers.Count == 0)
            {
                ExitBattle();
            }
        }

        public void NotifyAttackerDestroyed(StagingBattleUnit unit)
        {
            if (_battleEnded || unit == null)
            {
                return;
            }

            _aliveAttackers.Remove(unit);
            if (_aliveAttackers.Count == 0)
            {
                ExitBattle();
            }
        }

        private void Update()
        {
            if (_battleEnded || _cityRoot == null)
            {
                return;
            }

            ApplySiegeDamage();
            if (_cityHp <= 0f)
            {
                ExitBattle();
            }
        }

        private void ApplySiegeDamage()
        {
            Vector3 city = _cityRoot.position;
            city.y = 0f;
            float siege = 0f;
            for (int i = 0; i < _aliveAttackers.Count; i++)
            {
                StagingBattleUnit u = _aliveAttackers[i];
                if (u == null)
                {
                    continue;
                }

                Vector3 p = u.transform.position;
                p.y = 0f;
                if (Vector3.Distance(p, city) <= SiegeRadius)
                {
                    siege += SiegeDpsPerAttacker;
                }
            }

            if (siege > 0f)
            {
                _cityHp -= siege * Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            RestoreSceneCameras();
            EnsureAtLeastOneEnabledCameraForGameView();
        }

        private void RestoreSceneCameras()
        {
            for (int i = 0; i < _disabledCameras.Count; i++)
            {
                Camera c = _disabledCameras[i];
                if (c != null)
                {
                    c.enabled = true;
                }
            }

            _disabledCameras.Clear();
        }

        private void ExitBattle()
        {
            if (_battleEnded)
            {
                return;
            }

            _battleEnded = true;

            // Turn off our battle camera before re-enabling scene cameras (prevents two active cameras / two AudioListeners).
            Camera[] ours = GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < ours.Length; i++)
            {
                if (ours[i] != null)
                {
                    ours[i].enabled = false;
                }
            }

            RestoreSceneCameras();
            EnsureAtLeastOneEnabledCameraForGameView();

            System.Action exit = _onExit;
            _onExit = null;
            Destroy(gameObject);
            exit?.Invoke();
        }

        /// <summary>
        /// If every camera stayed disabled (edge cases during teardown), the Game view stays blank even with UI restored.
        /// </summary>
        private static void EnsureAtLeastOneEnabledCameraForGameView()
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam == null || cam.targetTexture != null)
                {
                    continue;
                }

                if (cam.enabled && cam.gameObject.activeInHierarchy)
                {
                    return;
                }
            }

            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam == null || cam.targetTexture != null)
                {
                    continue;
                }

                cam.gameObject.SetActive(true);
                cam.enabled = true;
                return;
            }
        }

        private void DisableOtherCameras()
        {
            Camera[] all = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                Camera c = all[i];
                if (c == null || !c.enabled)
                {
                    continue;
                }

                _disabledCameras.Add(c);
                c.enabled = false;
            }
        }

        private void BuildLightsAndGround()
        {
            GameObject lightGo = new GameObject("BattleSun");
            lightGo.transform.SetParent(transform, false);
            Light sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            sun.intensity = 1.05f;
            sun.color = new Color(1f, 0.96f, 0.9f, 1f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BattleGround";
            ground.transform.SetParent(transform, false);
            ground.transform.localScale = new Vector3(9f, 1f, 11f);
            ground.transform.position = new Vector3(0f, 0f, 5f);
            Object.Destroy(ground.GetComponent<Collider>());
            MeshRenderer gr = ground.GetComponent<MeshRenderer>();
            StagingBattleLitMaterial.ApplyColor(gr, new Color(0.15f, 0.22f, 0.12f, 1f));
        }

        private void BuildCityMarker()
        {
            _cityRoot = new GameObject("DefenderCity").transform;
            _cityRoot.SetParent(transform, false);
            _cityRoot.position = new Vector3(0f, 0f, 22f);

            GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spire.name = "CitySpire";
            spire.transform.SetParent(_cityRoot, false);
            spire.transform.localScale = new Vector3(3.2f, 2.2f, 3.2f);
            spire.transform.localPosition = Vector3.up * 2.2f;
            MeshRenderer mr = spire.GetComponent<MeshRenderer>();
            StagingBattleLitMaterial.ApplyColor(mr, new Color(0.55f, 0.2f, 0.18f, 1f));

            Object.Destroy(spire.GetComponent<Collider>());
        }

        private void BuildRtsCamera()
        {
            GameObject camGo = new GameObject("StagingBattleCamera");
            camGo.transform.SetParent(transform, false);
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 58f;

            cam.transform.position = new Vector3(0f, 44f, -32f);
            cam.transform.LookAt(new Vector3(0f, 0f, 10f));

            cam.enabled = true;
        }

        private void SpawnAttackerFormation()
        {
            IReadOnlyList<FortUnitEntry> units = _summary.StagedUnits;
            IReadOnlyList<int> cells = _summary.GridCellIndices;
            if (units == null || cells == null)
            {
                return;
            }

            int n = Mathf.Min(units.Count, cells.Count);
            Vector3 cityPos = _cityRoot.position;

            for (int i = 0; i < n; i++)
            {
                FortUnitEntry e = units[i];
                if (e == null)
                {
                    continue;
                }

                int idx = Mathf.Clamp(cells[i], 0, GridCols * GridRows - 1);
                Vector3 spawn = GridCellToWorld(idx, cityPos);
                StagingBattleUnit u = CreateUnitCapsule(
                    $"Attacker_{e.buildingType}_{i}",
                    spawn,
                    cityPos,
                    autoMarch: true,
                    isAttacker: true,
                    AttackerHitPoints,
                    new Color(0.35f, 0.55f, 0.95f, 1f),
                    addDefenderAura: false);
                _aliveAttackers.Add(u);
            }
        }

        private void SpawnDefenderSkirmishers()
        {
            Vector3 basePos = _cityRoot.position + new Vector3(0f, 0f, -3.5f);
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1f) * 2.8f;
                Vector3 spawn = basePos + new Vector3(x, 0.5f, 0f);
                CreateUnitCapsule(
                    $"Defender_Skirmisher_{i}",
                    spawn,
                    spawn,
                    autoMarch: false,
                    isAttacker: false,
                    hitPoints: 1f,
                    new Color(0.9f, 0.42f, 0.32f, 1f),
                    addDefenderAura: true);
            }
        }

        private StagingBattleUnit CreateUnitCapsule(
            string objectName,
            Vector3 position,
            Vector3 marchTarget,
            bool autoMarch,
            bool isAttacker,
            float hitPoints,
            Color tint,
            bool addDefenderAura)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = objectName;
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

            CapsuleCollider col = go.GetComponent<CapsuleCollider>();
            col.center = Vector3.zero;
            col.height = 2f;
            col.radius = 0.45f;

            StagingBattleUnit unit = go.AddComponent<StagingBattleUnit>();
            unit.Configure(this, marchTarget, autoMarch, isAttacker, hitPoints, tint);

            if (addDefenderAura)
            {
                GameObject auraGo = new GameObject("DefenderAura");
                auraGo.transform.SetParent(go.transform, false);
                auraGo.transform.localPosition = Vector3.zero;
                SphereCollider aura = auraGo.AddComponent<SphereCollider>();
                aura.isTrigger = true;
                aura.radius = 2.9f;
                auraGo.AddComponent<StagingBattleDefenderAura>();
            }

            return unit;
        }

        private static Vector3 GridCellToWorld(int cellIndex, Vector3 cityPos)
        {
            int row = cellIndex / GridCols;
            int col = cellIndex % GridCols;
            float tRow = GridRows > 1 ? row / (float)(GridRows - 1) : 0f;
            float zNearEnemy = cityPos.z - 8f;
            float zRear = -6f;
            float z = Mathf.Lerp(zNearEnemy, zRear, tRow);
            float x = (col - (GridCols - 1) * 0.5f) * 2.1f;
            return new Vector3(x, 0.85f, z);
        }
    }
}
