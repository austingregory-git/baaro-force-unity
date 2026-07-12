using UnityEngine;
using UnityEngine.EventSystems;
using BaaroForce.UI;

namespace BaaroForce.UI
{
    /// <summary>
    /// Lightweight pointer-enter/exit handler that shows a tooltip via
    /// <see cref="TooltipSystem"/>.  Unlike <see cref="CardAbilityHoverHandler"/>
    /// this does not require a <see cref="TMPro.TextMeshProUGUI"/> component, so
    /// it can be attached to any UI element (Button, Image, etc.).
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string _title;
        private string _body;

        /// <summary>Supplies the title and description shown in the tooltip.</summary>
        public void Initialize(string title, string body)
        {
            _title = title;
            _body  = body;
        }

        public void OnPointerEnter(PointerEventData _)
            => TooltipSystem.Instance?.Show(_title, _body);

        public void OnPointerExit(PointerEventData _)
            => TooltipSystem.Instance?.Hide();

        private void OnDisable()
            => TooltipSystem.Instance?.Hide();
    }
}
