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
        private string _summaryBody;
        private string _detailedBody;

        /// <summary>
        /// Call once after the label is created to supply the ability data.
        /// </summary>
        /// <param name="abilityName">Display name shown as the tooltip title.</param>
        /// <param name="summaryBody">
        ///   Raw description shown by default. May contain <c>[KeywordName]</c> tokens
        ///   that <see cref="TooltipSystem"/> will colour-highlight and expand into
        ///   keyword definitions.
        /// </param>
        /// <param name="detailedBody">
        ///   Raw description shown instead while the player holds Shift (e.g. a full
        ///   scaling breakdown). Pass null when there's nothing beyond the summary.
        /// </param>
        public void Initialize(string abilityName, string summaryBody, string detailedBody = null)
        {
            _abilityName  = abilityName;
            _summaryBody  = summaryBody;
            _detailedBody = detailedBody;
        }

        public void OnPointerEnter(PointerEventData _)
            => TooltipSystem.Instance?.Show(_abilityName, _summaryBody, _detailedBody);

        public void OnPointerExit(PointerEventData _)
            => TooltipSystem.Instance?.Hide();

        // Hide the tooltip if this object is disabled while the pointer is over it.
        private void OnDisable()
            => TooltipSystem.Instance?.Hide();
    }
}
