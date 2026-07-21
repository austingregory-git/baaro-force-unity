using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Root status — the afflicted unit cannot move for the duration.
    /// Enforced by TurnManager (see Character.IsRooted): blocks a player character from
    /// entering move mode (with a warning toast) and zeroes an Npc's movement budget for
    /// its turn. Attacks and spells are unaffected.
    /// </summary>
    public class RootStatus : StatusEffect
    {
        public RootStatus(int durationTurns)
            : base(
                name:        "Root",
                description: $"Cannot move for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.Debuff)
        {
        }

        public override void OnApply(CharacterStats stats)
        {
        }

        public override void OnTurnStart(CharacterStats stats)
        {
        }

        public override void OnRemove(CharacterStats stats)
        {
        }
    }
}
