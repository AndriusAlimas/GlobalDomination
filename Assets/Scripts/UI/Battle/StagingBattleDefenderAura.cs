using UnityEngine;

namespace GlobalDomination.UI.Battle
{
    /// <summary>
    /// Trigger volume on defender skirmishers: damages nearby attacker units.
    /// </summary>
    public sealed class StagingBattleDefenderAura : MonoBehaviour
    {
        [SerializeField] private float damagePerSecond = 7f;

        private void OnTriggerStay(Collider other)
        {
            StagingBattleUnit u = other.GetComponent<StagingBattleUnit>();
            if (u == null || !u.IsAttacker)
            {
                return;
            }

            u.ApplyDamage(damagePerSecond * Time.deltaTime);
        }
    }
}
