using Model;
using UnityEngine;
using static Manager;

namespace UI
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Square))]
    public class ColourApplier : MonoBehaviour
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

        private void OnMouseEnter()
        {
            if (Instance.isWindowOpen || Cube.Instance.IsSolving()) return;

            // Prevent modifying center squares
            if (_isCentre) return;

            if (Input.GetMouseButton(0))
                ApplyColour();
        }
        private void OnMouseDown()
        {

            if (Instance.isWindowOpen || Cube.Instance.IsSolving()) return;

            // Prevent modifying center squares
            if (_isCentre) return;

            ApplyColour();
        }

        private void ApplyColour()
        {
            if (Instance.isWindowOpen) return;

            if (Colour == Instance.currentColourInput) return;

            Colour = Instance.currentColourInput;
            Square.UpdateGraphics();
            Cube.Instance.TrackCube();
            Cube.Instance.UpdateModelsFromGraphics();
        }
    }
}
