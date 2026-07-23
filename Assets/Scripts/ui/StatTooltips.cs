using UnityEngine.UIElements;

namespace BaaroForce.UI
{
    /// <summary>
    /// Canonical hover-tooltip text for each core character stat, shared by every panel that
    /// renders these rows (CharacterHudController's combat readout, CharacterSelectionManager's
    /// character-select cards, CharacterInspectUI's character sheet) so a stat always explains
    /// itself the same way no matter where it's shown.
    /// </summary>
    internal static class StatTooltips
    {
        public static void AttachHp(VisualElement row) =>
            Attach(row, "Health", "Current and maximum Health Points. Reaching 0 defeats the character.");

        public static void AttachMana(VisualElement row) =>
            Attach(row, "Mana", "Current and maximum Mana, spent to cast spells.");

        public static void AttachMovement(VisualElement row) =>
            Attach(row, "Movement", "How many tiles this character can move in a single turn.");

        public static void AttachAttack(VisualElement row) =>
            Attach(row, "Attack", "Damage dealt by this character's basic attacks.");

        public static void AttachShield(VisualElement row) =>
            Attach(row, "Shield", "A temporary shield that absorbs damage before Health is affected.");

        public static void AttachAttackBonus(VisualElement row) =>
            Attach(row, "Attack Bonus", "The portion of Attack granted by equipment or effects.");

        public static void AttachSpellPower(VisualElement row) =>
            Attach(row, "Spell Power", "Increases the effectiveness of this character's spells.");

        private static void Attach(VisualElement row, string title, string body)
        {
            if (row == null) return;
            row.RegisterCallback<PointerEnterEvent>(_ => TooltipSystem.Instance?.Show(title, body));
            row.RegisterCallback<PointerLeaveEvent>(_ => TooltipSystem.Instance?.Hide());
        }
    }
}
