using UnityEngine;
using UnityEngine.UI;
using static UI.Square;
using static Manager;

namespace UI
{
    public class PaletteButton : MonoBehaviour
    {
        public Color normalColor, highlightedColor;

        private int _colour;
        private bool _hovering;

        private void Awake()
        {
            _colour = FaceToIndex(transform.parent.name);
        }

        public void OnMouseEnter()
        {
            if (Instance.isWindowOpen) return;

            _hovering = true;
            transform.parent.GetComponent<Image>().color = highlightedColor;
        }

        public void OnMouseExit()
        {
            if (Instance.isWindowOpen) return;

            _hovering = false;
            
            // Only reset to normal color if this button is not currently selected
            if (IsSelected)
                return;

            transform.parent.GetComponent<Image>().color = normalColor;
        }

        public void OnMouseDown()
        {
            if (Instance.isWindowOpen) return;

            // Reset color if it was clicked without hovering
            // Clicked a different button
            if (!_hovering)
                transform.parent.GetComponent<Image>().color = normalColor;

            Instance.SwitchInputColour(_colour);
        }

        public void UpdateColour()
        {
            transform.parent.GetComponent<Image>().color = IsSelected
                ? highlightedColor
                : normalColor;
        }

        private bool IsSelected => _colour == Instance.currentColourInput;
    }
}
