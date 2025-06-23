using UnityEngine;
using static UI.Square.Colour;

namespace UI
{
    public class Player : MonoBehaviour
    {
        public static Player Instance;

        [HideInInspector] public int currentColourInput = WHITE;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(Instance);
        }
    }
}
