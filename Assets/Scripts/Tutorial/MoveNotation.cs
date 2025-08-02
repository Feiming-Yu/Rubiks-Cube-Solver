using System.Collections.Generic;
using static UI.Square;

namespace Tutorial
{
    public static class MoveNotation
    {
        private static readonly List<string> FaceDescription = new()
        {
            "bottom", "top", "back", "front", "left", "right"
        };

        public static string ConvertToMoveDescription(string move)
        {
            if (move == "") return "";

            string description = $"Rotate the {FaceDescription[FaceToIndex(move[0])]} face";
            if (move.Contains("2"))
                description += " 180 degrees";
            else if (move.Contains("'"))
                description += " counterclockwise";
            else
                description += " clockwise";

            return description;
        }
    }
}
