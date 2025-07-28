using UnityEngine;

namespace UI
{
    [CreateAssetMenu(fileName = "Theme", menuName = "Scriptable Objects/Theme")]
    public class Theme : ScriptableObject
    {
        public Color
            primary,
            secondary,
            tertiary,
            highlightPrimary,
            highlightSecondary,
            selected,
            opposite,
            oppositeSecondary;
    }
}
