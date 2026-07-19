using UnityEngine;

namespace BaaroForce.Passives
{
    public class PassiveAbility
    {
        private readonly string _name;
        private readonly string _description;

        public string Name        => _name;
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
            Custom
        }

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
    }
}