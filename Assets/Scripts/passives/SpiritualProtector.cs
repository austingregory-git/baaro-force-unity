using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.Formulas;
using BaaroForce.Map;
using BaaroForce.Spells;
using BaaroForce.UI;

namespace BaaroForce.Passives
{
    /// <summary>
    /// Spiritual Protector — Buggles' signature passive ability.
    /// Once per combat, when a nearby ally is reduced below 50% health, heal them for
    /// 4 + Level. "Nearby" follows the same TrueCircle shape as Arcane Explosion (see
    /// ArcaneExplosion.cs), with an area of 2 centred on Buggles.
    /// Also fires if the hit is lethal (HP <= 0) — since this is once-per-combat, that
    /// only happens the first time the ally crosses the 50% line, which includes being
    /// one-shot straight past it. TurnManager.ResolveBasicAttack checks the defeat
    /// condition only after this runs, so healing them back above 0 here saves them —
    /// provided the heal outweighs the overkill damage; CharacterStats.Heal just adds
    /// the flat amount, so a bigger overkill can still leave them dead.
    /// </summary>
    public class SpiritualProtector : PassiveAbility
    {
        private const int ProtectionRadius = 2;

        private bool _hasTriggeredThisCombat;

        public SpiritualProtector()
            : base("Spiritual Protector",
                  "Once per combat, when a nearby ally drops below 50% health, heal them for {0}.",
                  PassiveAbilityType.OnAllyDamaged)
        {
        }

        public override ScalingValue[] ComputeValues(Character owner) =>
            new[]
            {
                new ScalingValue("Healing")
                    .Add("Base", 4)
                    .Add("Level", owner.Level)
            };

        public override void OnCombatStart()
        {
            _hasTriggeredThisCombat = false;
        }

        public override bool Execute(PassiveOnAllyDamagedContext context)
        {
            if (_hasTriggeredThisCombat) return false;

            CharacterStats allyStats = context.DamagedAlly.CharacterStats;
            if (allyStats.HealthPoints * 2 >= allyStats.MaxHealthPoints)
                return false;   // not below 50% health (includes lethal hits — see class summary)

            // includeCenter: true — Self is an Ally, so Buggles can trigger this on his own tile too.
            List<MapTile> rangeTiles = SpellAreaUtils.GetTrueCircleAreaTiles(
                context.OwnerTile, range: 0, area: ProtectionRadius, context.AllTiles, context.GridSize,
                includeCenter: true);
            if (!rangeTiles.Contains(context.DamagedAllyTile))
                return false;   // ally is out of range

            _hasTriggeredThisCombat = true;

            int healing = ComputeValues(context.Owner)[0].Total;
            int healed  = allyStats.Heal(healing);
            FloatingCombatTextSystem.Instance?.ShowHeal(context.DamagedAlly, healed);

            Debug.Log($"[SpiritualProtector] '{context.Owner.CharacterName}' channels healing energy into " +
                      $"'{context.DamagedAlly.CharacterName}', restoring {healed} health. " +
                      $"HP: {allyStats.HealthPoints}/{allyStats.MaxHealthPoints}");
            return true;
        }
    }
}
