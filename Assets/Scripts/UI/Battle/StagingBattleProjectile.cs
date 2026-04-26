using UnityEngine;

namespace GlobalDomination.UI.Battle
{
    /// <summary>
    /// Kinematic trigger projectile for staging battle ranged attacks.
    /// </summary>
    public sealed class StagingBattleProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 24f;
        [SerializeField] private float damage = 14f;
        [SerializeField] private float maxLifetime = 10f;

        private StagingBattleWorld _world;
        private StagingBattleUnit _owner;
        private Vector3 _direction;
        private float _age;
        private bool _done;

        public void Launch(
            StagingBattleWorld world,
            StagingBattleUnit owner,
            Vector3 start,
            Vector3 direction,
            float speedOverride,
            float damageOverride)
        {
            _world = world;
            _owner = owner;
            transform.position = start;
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            speed = speedOverride > 0f ? speedOverride : speed;
            damage = damageOverride > 0f ? damageOverride : damage;
            _age = 0f;
            _done = false;
        }

        private void Update()
        {
            if (_done)
            {
                return;
            }

            _age += Time.deltaTime;
            if (_age >= maxLifetime)
            {
                DestroyProjectile();
                return;
            }

            transform.position += _direction * (speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_done || other == null)
            {
                return;
            }

            if (other.GetComponent<StagingBattleDefenderAura>() != null)
            {
                return;
            }

            if (other.GetComponent<StagingBattleGround>() != null)
            {
                DestroyProjectile();
                return;
            }

            StagingBattleUnit hitUnit = other.GetComponentInParent<StagingBattleUnit>();
            if (hitUnit != null)
            {
                if (hitUnit == _owner || hitUnit.IsAttacker)
                {
                    return;
                }

                hitUnit.ApplyDamage(damage);
                SpawnHitSpark(other.ClosestPoint(transform.position));
                DestroyProjectile();
                return;
            }

            if (other.GetComponent<StagingBattleCityTarget>() != null
                || other.GetComponentInParent<StagingBattleCityTarget>() != null)
            {
                _world?.DamageCity(damage);
                SpawnHitSpark(other.ClosestPoint(transform.position));
                DestroyProjectile();
            }
        }

        private void DestroyProjectile()
        {
            if (_done)
            {
                return;
            }

            _done = true;
            Destroy(gameObject);
        }

        private static void SpawnHitSpark(Vector3 p)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = "HitSpark";
            s.transform.position = p;
            s.transform.localScale = Vector3.one * 0.35f;
            Object.Destroy(s.GetComponent<Collider>());
            MeshRenderer mr = s.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                StagingBattleLitMaterial.ApplyColor(mr, new Color(1f, 0.92f, 0.35f, 1f));
            }

            Object.Destroy(s, 0.12f);
        }

        public static StagingBattleProjectile Spawn(
            StagingBattleWorld world,
            StagingBattleUnit owner,
            Vector3 start,
            Vector3 direction,
            float speedOverride,
            float damageOverride)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "StagingBattleProjectile";
            go.transform.SetParent(world != null ? world.transform : null, true);
            go.transform.localScale = Vector3.one * 0.22f;
            Object.Destroy(go.GetComponent<Collider>());

            SphereCollider trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                StagingBattleLitMaterial.ApplyColor(mr, new Color(0.95f, 0.85f, 0.2f, 1f));
            }

            StagingBattleProjectile proj = go.AddComponent<StagingBattleProjectile>();
            proj.Launch(world, owner, start, direction, speedOverride, damageOverride);
            return proj;
        }
    }
}
