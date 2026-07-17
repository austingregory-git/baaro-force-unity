using UnityEngine;

namespace BaaroForce.Passives
{
    public class PassiveAbility
    {
        private readonly string name;
        private readonly string description;

        public string Name        => name;
        public string Description => description;
        public PassiveAbilityType AbilityType { get; private set; }

        public PassiveAbility(string name, string description, PassiveAbilityType abilityType)
        {
            this.name = name;
            this.description = description;
            this.AbilityType = abilityType;
        }

        public PassiveAbility(string name, string description)
        {
            this.name = name;
            this.description = description;
            this.AbilityType = PassiveAbilityType.CUSTOM;
        }

        public enum PassiveAbilityType
        {
            END_OF_TURN,
            START_OF_TURN,
            END_OF_COMBAT,
            START_OF_COMBAT,
            ON_RECEIVING_ATTACK,
            ON_DEALING_ATTACK,
            ON_TARGETED_BY_SPELL,
            ON_CASTING_SPELL,
            ON_LEVEL_UP,
            CUSTOM
        }

        /// <summary>
        /// Executes this passive ability's effects given the context.
        /// Override in concrete subclasses; the default is a no-op stub.
        /// Returns true if the ability resolved successfully.
        /// </summary>
        public virtual bool Execute(PassiveOnTurnContext context)
        {
            Debug.LogWarning($"[PassiveAbility] '{name}' has no Execute implementation.");
            return false;
        }

        public virtual bool Execute(PassiveOnReceivingAttackContext context)
        {
            Debug.LogWarning($"[PassiveAbility] '{name}' has no Execute implementation.");
            return false;
        }
    }
}