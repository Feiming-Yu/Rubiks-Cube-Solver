using UnityEngine;
using UnityEngine.UI;
using static UI.Square;

namespace UI
{
    public class PaletteButton : MonoBehaviour
    {
        private int _colour;
        private bool _hovering;

        private void Awake()
        {
            _colour = FaceToIndex(transform.parent.name);
        }

        public void OnMouseEnter()
        {
            _hovering = true;
            transform.parent.GetComponent<Image>().color = new Color32(94, 100, 111, 255);
        }

        public void OnMouseExit()
        {
            _hovering = false;
            if (_colour == Player.Instance.currentColourInput)
                return;

            transform.parent.GetComponent<Image>().color = new Color32(57, 60, 67, 255);
        }

        public void OnMouseDown()
        {
            if (!_hovering)
                transform.parent.GetComponent<Image>().color = new Color32(57, 60, 67, 255);

            Manager.Instance.SwitchColour(_colour);
        }
    }
}
