using System;
using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.Formulas;

namespace BaaroForce.Passives
{
    public class PassiveAbility
    {
        private readonly string _name;
        private readonly string _description;

        public string Name        => _name;
        /// <summary>Template text for tooltips — may contain <c>{0}</c>/<c>{1}</c>
        /// placeholders filled in by <see cref="ComputeValues"/>'s totals, and
        /// <c>[Keyword]</c> tokens resolved by <see cref="BaaroForce.Keywords.KeywordRegistry"/>.</summary>
        public string Description => _description;
        public PassiveAbilityType AbilityType { get; private set; }

        public PassiveAbility(string name, string description, PassiveAbilityType abilityType)
        {
            this._name = name;
            this._description = description;
            this.AbilityType = abilityType;
        }

        public PassiveAbility(string name, string description)
        {
            this._name = name;
            this._description = description;
            this.AbilityType = PassiveAbilityType.Custom;
        }

        public enum PassiveAbilityType
        {
            EndOfTurn,
            StartOfTurn,
            EndOfCombat,
            StartOfCombat,
            OnReceivingAttack,
            OnDealingAttack,
            OnTargetedBySpell,
            OnCastingSpell,
            OnLevelUp,
            /// <summary>A nearby ally (not this passive's owner) has just taken damage and
            /// survived — see <see cref="PassiveOnAllyDamagedContext"/> and SpiritualProtector.</summary>
            OnAllyDamaged,
            Custom
        }

        /// <summary>
        /// Called once at the start of every combat for every passive the character has,
        /// regardless of <see cref="AbilityType"/> — separate from the StartOfCombat-gated
        /// <see cref="Execute(PassiveOnTurnContext)"/> dispatch. Override to reset internal
        /// per-combat state (e.g. a "used this fight already" flag) on passives whose actual
        /// trigger is something else, like Spiritual Protector's OnAllyDamaged reaction.
        /// </summary>
        public virtual void OnCombatStart() { }

        /// <summary>
        /// Executes this passive ability's effects given the context.
        /// Override in concrete subclasses; the default is a no-op stub.
        /// Returns true if the ability resolved successfully.
        /// </summary>
        public virtual bool Execute(PassiveOnTurnContext context)
        {
            Debug.LogWarning($"[PassiveAbility] '{_name}' has no Execute implementation.");
            return false;
        }

        public virtual bool Execute(PassiveOnReceivingAttackContext context)
        {
            Debug.LogWarning($"[PassiveAbility] '{_name}' has no Execute implementation.");
            return false;
        }

        public virtual bool Execute(PassiveOnAllyDamagedContext context)
        {
            Debug.LogWarning($"[PassiveAbility] '{_name}' has no Execute implementation.");
            return false;
        }

        /// <summary>Bonus range (in tiles) this passive grants to its owner's basic attacks
        /// and spells — e.g. Long Bow's +1. Additive across all of a character's passives;
        /// most passives don't affect range and leave this at 0.</summary>
        public virtual int RangeBonus => 0;

        /// <summary>
        /// Computes this passive's scaling numbers for <paramref name="owner"/> (the
        /// character the passive belongs to), in the same order as the <c>{0}</c>/<c>{1}</c>
        /// placeholders in <see cref="Description"/>. Override alongside Execute so the
        /// tooltip and the actual effect always agree.
        /// </summary>
        public virtual ScalingValue[] ComputeValues(Character owner) => Array.Empty<ScalingValue>();

        /// <summary>Description with each scaling value's total substituted in — what the
        /// tooltip shows by default.</summary>
        public string GetSummary(Character owner) =>
            ScalingDescriptionFormatter.GetSummary(Description, ComputeValues(owner));

        /// <summary>Summary plus a full term-by-term breakdown of every scaling value —
        /// what the tooltip shows while Shift is held. Null when there's nothing to add
        /// beyond the summary.</summary>
        public string GetDetailedDescription(Character owner) =>
            ScalingDescriptionFormatter.GetDetailedDescription(Description, ComputeValues(owner));
    }
}