using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Square))]
    public class ColourApplier : MonoBehaviour, IPointerDownHandler
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

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (_isCentre)
                return;

            ApplyColour();
        }

        private void OnMouseEnter()
        {
            if (_isCentre)
                return;

            if (Input.GetMouseButton(0))
                ApplyColour();
        }

        private void ApplyColour()
        {
            if (Colour == Player.Instance.currentColourInput) return;

            Colour = Player.Instance.currentColourInput;
            Square.UpdateGraphics();
            Cube.Instance.UpdateModelsFromGraphics();
        }
    }
}
