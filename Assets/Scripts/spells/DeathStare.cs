using BaaroForce.Characters;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Death Stare — Winston's signature dark-magic spell.
    ///
    /// Locks eyes with a single enemy within range 3, inflicting Fear for a duration
    /// scaled by the caster's level and dealing a small hit of dark damage.
    ///
    /// Level scaling:
    ///   Fear duration  = floor(1 + 0.25 × level)    (1 turn at level 1)
    ///   Damage         = floor(1 + 0.50 × level)    (1 damage at level 1)
    ///
    /// Fear effect: reduces the target's effective attack (via attackBonus) for
    /// the duration.  The penalty is reversed when the effect expires.
    /// </summary>
    public class DeathStare : CharacterSpell
    {
        public DeathStare()
            : base(
                name:        "Death Stare",
                description: "[Fear] an enemy for 1 + 0.25 × [Level] turns, " +
                             "dealing 2 + 0.5 × [Level] dark damage.",
                cost:        2,
                range:       3,
                area:        0,
                cooldown:    1,
                targetType:  SpellTargetType.Enemy)
        {
        }

        public override bool Execute(SpellContext context)
        {
            NPC target = context.TargetTile?.OccupyingNpc;
            if (target == null)
            {
                Debug.LogWarning("[DeathStare] No enemy found on the target tile.");
                return false;
            }

            int level = context.CasterLevel;

            // Apply Fear — reduces target's attack for N turns.
            int fearDuration = Mathf.FloorToInt(1f + 0.25f * level);
            var fear = new FearStatus(durationTurns: fearDuration, attackPenalty: 2);
            target.ApplyStatus(fear);

            // Deal dark damage.
            int damage = Mathf.FloorToInt(2f + 0.5f * level);
            target.characterStats.healthPoints -= damage;

            Debug.Log($"[DeathStare] '{context.Caster.characterName}' casts Death Stare on " +
                      $"'{target.characterName}'.  Damage: {damage}, " +
                      $"Fear: {fearDuration} turn(s).  " +
                      $"HP: {Mathf.Max(0, target.characterStats.healthPoints)}" +
                      $"/{target.characterStats.maxHealthPoints}");

            if (target.characterStats.healthPoints <= 0)
            {
                Debug.Log($"[DeathStare] '{target.characterName}' has been defeated!");
                context.TargetTile.RemoveNpc();
            }

            return true;
        }

        /// <summary>
        /// NPC-cast version: targets a player Character instead of an NPC.
        /// Deals dark damage and inflicts Fear on the character.
        /// </summary>
        public override bool Execute(NpcSpellContext context)
        {
            Character target = context.TargetTile?.OccupyingCharacter;
            if (target == null)
            {
                Debug.LogWarning("[DeathStare] No character found on the target tile.");
                return false;
            }

            int level = context.CasterLevel;

            int fearDuration = Mathf.FloorToInt(1f + 0.25f * level);
            var fear = new FearStatus(durationTurns: fearDuration, attackPenalty: 2);
            target.ApplyStatus(fear);

            int damage = Mathf.FloorToInt(2f + 0.5f * level);
            target.characterStats.healthPoints -= damage;

            Debug.Log($"[DeathStare] '{context.Caster.characterName}' casts Death Stare on "
                    + $"'{target.characterName}'.  Damage: {damage}, "
                    + $"Fear: {fearDuration} turn(s).  "
                    + $"HP: {Mathf.Max(0, target.characterStats.healthPoints)}"
                    + $"/{target.characterStats.maxHealthPoints}");

            if (target.characterStats.healthPoints <= 0)
                Debug.Log($"[DeathStare] '{target.characterName}' has been defeated!");

            return true;
        }
    }
}
