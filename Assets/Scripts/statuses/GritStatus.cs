using BaaroForce.Characters;
using UnityEngine;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Grit status — expands maximum health for the remainder of the fight.
    /// Never expires on its own (RemainingTurns -1); removed by
    /// <see cref="Character.ResetPostCombatState"/> once the fight ends.
    /// </summary>
    public class GritStatus : StatusEffect
    {
        private int _healthBonus;

        public GritStatus(int healthBonus)
            : base(
                name:        "Grit",
                description: $"Increases maximum health by {healthBonus} for the fight.",
                durationTurns: -1,
                effectType: StatusEffectType.Buff)
        {
            this._healthBonus = healthBonus;
        }

        public override void OnApply(CharacterStats stats)
        {
            stats.MaxHealthPoints += _healthBonus;
            stats.Heal(_healthBonus);
            Debug.Log($"[GritStatus] Applied. Max health increased by {_healthBonus}. New max: {stats.MaxHealthPoints}");
        }

        public override void OnTurnStart(CharacterStats stats)
        {

        }

        public override void OnRemove(CharacterStats stats)
        {
            stats.MaxHealthPoints -= _healthBonus;   // restore what OnApply added
            stats.HealthPoints = Mathf.Min(stats.HealthPoints, stats.MaxHealthPoints);
            Debug.Log($"[GritStatus] Removed. Max health decreased by {_healthBonus}. New max: {stats.MaxHealthPoints}");
        }

        /// <summary>Re-applying Grit adds the new cast's bonus onto the existing one,
        /// rather than replacing it — mirrors RallyStatus.</summary>
        public override void Stack(StatusEffect incoming, CharacterStats stats)
        {
            base.Stack(incoming, stats);
            if (incoming is GritStatus grit)
            {
                _healthBonus += grit._healthBonus;
                stats.MaxHealthPoints += grit._healthBonus;
                stats.Heal(grit._healthBonus);
                Description = $"Increases maximum health by {_healthBonus} for the fight.";
                Debug.Log($"[GritStatus] Stacked. Max health increased by {grit._healthBonus}. New max: {stats.MaxHealthPoints}");
            }
        }
    }
}
