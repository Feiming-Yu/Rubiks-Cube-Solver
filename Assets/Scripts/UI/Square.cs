using System.Linq;
using UnityEngine;
using static UI.Square.Colour;

namespace UI
{
    public class Square : MonoBehaviour
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

        public bool IsCentre { get; private set; }

        private void Start()
        {
            IsCentre = transform.parent.name.Count(c => c == '0') == 2;
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

        public static string ColourToFace(int colour)
        {
            return colour switch
            {
                WHITE => "D",
                YELLOW => "U",
                GREEN => "B",
                BLUE => "F",
                ORANGE => "L",
                RED => "R",
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

        public void UpdateColour()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material = squareMaterials[colour];
        }
    }
}
