using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace BaaroForce.UI
{

    /// <summary>
    /// Attach to any ability-name label on a character card.
    /// Delegates to <see cref="TooltipSystem"/> to show a floating tooltip that
    /// contains the full ability name and its description (with keyword definitions)
    /// whenever the pointer enters the label.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class CardAbilityHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string _abilityName;
        private string _description;

        /// <summary>
        /// Call once after the label is created to supply the ability data.
        /// </summary>
        /// <param name="abilityName">Display name shown as the tooltip title.</param>
        /// <param name="description">
        ///   Raw description string.  May contain <c>[KeywordName]</c> tokens that
        ///   <see cref="TooltipSystem"/> will colour-highlight and expand into
        ///   keyword definitions.
        /// </param>
        public void Initialize(string abilityName, string description)
        {
            _abilityName = abilityName;
            _description = description;
        }

        public void OnPointerEnter(PointerEventData _)
            => TooltipSystem.Instance?.Show(_abilityName, _description);

        public void OnPointerExit(PointerEventData _)
            => TooltipSystem.Instance?.Hide();

        // Hide the tooltip if this object is disabled while the pointer is over it.
        private void OnDisable()
            => TooltipSystem.Instance?.Hide();
    }
}
