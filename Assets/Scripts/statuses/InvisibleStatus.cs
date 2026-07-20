using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Invisible status — most Npcs cannot target the afflicted unit with attacks or spells
    /// while this is active (enforced by AggressiveNpcAI's target-selection and, as a
    /// defense-in-depth backstop, TurnManager.NpcExecuteAttack/NpcExecuteSpell). An Npc
    /// subclass can opt out of that restriction via <see cref="Npc.IgnoresInvisibility"/>.
    ///
    /// Broken early if the unit attacks, casts a spell, or takes damage from any other
    /// source (see Character.BreakInvisibility) — otherwise lasts the full duration.
    /// </summary>
    public class InvisibleStatus : StatusEffect
    {
        public InvisibleStatus(int durationTurns)
            : base(
                name:        "Invisible",
                description: $"Most enemies cannot target this unit for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.Buff)
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
