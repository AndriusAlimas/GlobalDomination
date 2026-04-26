using UnityEngine;

namespace GlobalDomination.UI.Battle
{
    /// <summary>
    /// Staging battle only: left-click selects attackers and issues move to ground (or horizontal plane under cursor);
    /// right-click fires one projectile while an attacker is selected (per-unit cooldown; ring UI on unit).
    /// </summary>
    public sealed class StagingBattlePlayerController : MonoBehaviour
    {
        [SerializeField] private float shootRayDistance = 500f;
        [SerializeField] private float projectileSpeed = 26f;
        [SerializeField] private float projectileDamage = 14f;
        [SerializeField] private float planeAimFallbackDistance = 120f;
        [SerializeField] private float shootCooldownSeconds = 3f;

        private StagingBattleWorld _world;
        private StagingBattleUnit _selected;

        public void Bind(StagingBattleWorld world)
        {
            _world = world;
        }

        private void OnDestroy()
        {
            if (_selected != null)
            {
                _selected.SetSelected(false);
            }
        }

        private void Update()
        {
            if (_world == null || _world.BattleCamera == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClick();
            }

            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }
        }

        private void HandleLeftClick()
        {
            Ray ray = _world.BattleCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, shootRayDistance);
            if (hits.Length > 1)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            }

            if (TryPickAttacker(hits, out StagingBattleUnit attacker))
            {
                SelectUnit(attacker);
                return;
            }

            if (_selected != null)
            {
                if (TryPickGround(hits, out Vector3 groundPoint))
                {
                    groundPoint.y = _selected.transform.position.y;
                    _selected.SetMarchTarget(groundPoint);
                    _selected.SetAutoMarch(true);
                    return;
                }

                if (TryRayPlaneOnHeight(ray, _selected.transform.position.y, out Vector3 planePoint))
                {
                    _selected.SetMarchTarget(planePoint);
                    _selected.SetAutoMarch(true);
                    return;
                }
            }

            if (HitsIncludeGround(hits) || hits.Length == 0)
            {
                ClearSelection();
            }
        }

        private void HandleRightClick()
        {
            if (_selected == null || !_selected.IsAttacker || !_selected.CanShoot())
            {
                return;
            }

            Ray ray = _world.BattleCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, shootRayDistance);
            if (hits.Length > 1)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            }

            if (TryPickShootTarget(hits, out Vector3 aimPoint))
            {
                TryFireProjectile(aimPoint);
                return;
            }

            if (TryPickGround(hits, out aimPoint))
            {
                TryFireProjectile(aimPoint);
                return;
            }

            if (TryRayPlaneOnHeight(ray, _selected.transform.position.y, out aimPoint))
            {
                TryFireProjectile(aimPoint);
                return;
            }

            TryFireProjectile(ray.GetPoint(planeAimFallbackDistance));
        }

        private static bool TryRayPlaneOnHeight(Ray ray, float heightY, out Vector3 point)
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, heightY, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                point.y = heightY;
                return true;
            }

            point = default;
            return false;
        }

        private static bool HitsIncludeGround(RaycastHit[] hits)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null
                    && hits[i].collider.GetComponentInParent<StagingBattleGround>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryPickGround(RaycastHit[] hits, out Vector3 point)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i].collider;
                if (c != null && c.GetComponentInParent<StagingBattleGround>() != null)
                {
                    point = hits[i].point;
                    return true;
                }
            }

            point = default;
            return false;
        }

        private static bool TryPickAttacker(RaycastHit[] hits, out StagingBattleUnit unit)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                StagingBattleUnit u = hits[i].collider.GetComponentInParent<StagingBattleUnit>();
                if (u != null && u.IsAttacker)
                {
                    unit = u;
                    return true;
                }
            }

            unit = null;
            return false;
        }

        private static bool TryPickShootTarget(RaycastHit[] hits, out Vector3 aimPoint)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit h = hits[i];
                Collider c = h.collider;
                if (c == null)
                {
                    continue;
                }

                if (c.GetComponentInParent<StagingBattleCityTarget>() != null)
                {
                    aimPoint = h.point;
                    return true;
                }

                StagingBattleUnit u = c.GetComponentInParent<StagingBattleUnit>();
                if (u != null && !u.IsAttacker)
                {
                    aimPoint = h.point;
                    return true;
                }
            }

            aimPoint = default;
            return false;
        }

        private void TryFireProjectile(Vector3 aimPoint)
        {
            if (_selected == null || !_selected.IsAttacker || !_selected.CanShoot())
            {
                return;
            }

            Vector3 start = _selected.transform.position + Vector3.up * 0.85f
                + _selected.transform.forward * 0.25f;
            Vector3 dir = aimPoint - start;
            StagingBattleProjectile.Spawn(_world, _selected, start, dir, projectileSpeed, projectileDamage);
            _selected.NotifyShotFired(shootCooldownSeconds);
        }

        private void SelectUnit(StagingBattleUnit unit)
        {
            if (_selected == unit)
            {
                return;
            }

            if (_selected != null)
            {
                _selected.SetSelected(false);
                _selected.SetAutoMarch(true);
            }

            _selected = unit;
            if (_selected != null)
            {
                _selected.SetSelected(true);
            }
        }

        private void ClearSelection()
        {
            if (_selected != null)
            {
                _selected.SetAutoMarch(true);
                _selected.SetSelected(false);
            }

            _selected = null;
        }
    }
}
