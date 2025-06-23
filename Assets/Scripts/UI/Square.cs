using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using static UI.Square.Colour;

namespace UI
{
    [RequireComponent(typeof(MeshRenderer))]
    public class Square : MonoBehaviour, IPointerClickHandler
    {
        public struct Colour
        {
            public const int WHITE = 0;
            public const int YELLOW = 1;
            public const int GREEN = 2;
            public const int BLUE = 3;
            public const int ORANGE = 4;
            public const int RED = 5;
        }
        
        [SerializeField] private Material[] squareMaterials;
        
        public int colour;

        private bool _isCentre;

        private void Start()
        {
            _isCentre = transform.parent.name.Count(c => c == '0') == 2;
        }

        public static string ColourToString(int colour)
        {
            return colour switch
            {
                WHITE => "WHITE",
                YELLOW => "YELLOW",
                GREEN => "GREEN",
                BLUE => "BLUE",
                ORANGE => "ORANGE",
                RED => "RED",
                _ => "-",
            };
        }

        public static int FaceToIndex(string face)
        {
            return face switch
            {
                "D" => WHITE,
                "U" => YELLOW,
                "B" => GREEN,
                "F" => BLUE,
                "L" => ORANGE,
                "R" => RED,
                _ => -1,
            };
        }

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
            Player.Instance.currentColourInput = (colour + 1) % 6;
            ApplyColour();
        }

        private void ApplyColour()
        {
            if (colour == Player.Instance.currentColourInput) return;
            
            colour = Player.Instance.currentColourInput;
            UpdateColour();
        }

        public void UpdateColour()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material = squareMaterials[colour];
        }
    }
}
