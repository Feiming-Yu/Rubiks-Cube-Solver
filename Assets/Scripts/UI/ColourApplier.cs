using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Square))]
    public class ColourApplier : MonoBehaviour, IPointerClickHandler
    {
        private bool _isCentre;

        private void Start()
        {
            _isCentre = GetComponent<Square>().IsCentre;
        }

        private int Colour
        {
            get => GetComponent<Square>().colour;
            set => GetComponent<Square>().colour = value;
        }

        private Square Square => GetComponent<Square>();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isCentre) return;

            if (eventData.button != PointerEventData.InputButton.Left) return;

            int clickCount = eventData.clickCount;

            if (clickCount == 2)
                SwitchColour();
            else if (clickCount == 1)
                ApplyColour();
        }

        private void SwitchColour()
        {
            Player.Instance.currentColourInput = (Colour + 1) % 6;
            ApplyColour();
        }

        private void ApplyColour()
        {
            if (Colour == Player.Instance.currentColourInput) return;

            Colour = Player.Instance.currentColourInput;
            Square.UpdateColour();
        }
    }
}
