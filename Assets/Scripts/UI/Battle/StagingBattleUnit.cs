using UnityEngine;

namespace GlobalDomination.UI.Battle
{
    /// <summary>
    /// Auto-marches toward a world goal on the XZ plane. Attackers have HP and can be damaged; defenders are static hazards.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class StagingBattleUnit : MonoBehaviour
    {
        [SerializeField] private float marchSpeed = 4.5f;
        [SerializeField] private float stopDistance = 1.2f;

        private Vector3 _marchTarget;
        private bool _autoMarch = true;
        private bool _isAttacker;
        private float _hp;
        private float _maxHp;
        private StagingBattleWorld _world;

        public bool IsAttacker => _isAttacker;

        public void Configure(
            StagingBattleWorld world,
            Vector3 marchTarget,
            bool autoMarch,
            bool isAttacker,
            float hitPoints,
            Color tint)
        {
            _world = world;
            _marchTarget = marchTarget;
            _autoMarch = autoMarch;
            _isAttacker = isAttacker;
            _maxHp = hitPoints;
            _hp = hitPoints;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;

            MeshRenderer mr = GetComponent<MeshRenderer>();
            StagingBattleLitMaterial.ApplyColor(mr, tint);
        }

        public void SetMarchTarget(Vector3 world)
        {
            _marchTarget = world;
        }

        public void ApplyDamage(float amount)
        {
            if (!_isAttacker || amount <= 0f || _hp <= 0f)
            {
                return;
            }

            _hp -= amount;
            if (_hp <= 0f)
            {
                _world?.NotifyAttackerDestroyed(this);
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            Vector3 p = transform.position;
            Vector3 flatTarget = new Vector3(_marchTarget.x, p.y, _marchTarget.z);
            Vector3 toGoal = flatTarget - p;
            toGoal.y = 0f;
            float dist = toGoal.magnitude;

            if (!_autoMarch || dist <= stopDistance)
            {
                return;
            }

            Vector3 march = toGoal.normalized * marchSpeed;
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
    }
}
