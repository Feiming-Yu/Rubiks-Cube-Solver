using System.Collections.Generic;
using System.Linq;
using Model;
using static Model.Converter;
using static UI.Square.Colour;
using static Model.Cubie;

namespace Engine
{
    // For clarity, I have not used boolean logic
    // e.g. return CheckLegal(cube) && !CheckSolvable(Converter.FaceletToCubie(cube), cube)
    // as separating the selection constructs improves clarity
    // and allows ease of following the step by step process to check validation.
    public static class Validation
    {
        public static bool Validate(Facelet cube)
        {
            if (!CheckLegal(cube)) return false;
            if (!CheckSolvable(Converter.FaceletToCubie(cube), cube)) return false;

            return true;
        }

        #region Legal

        private static bool CheckLegal(Facelet cube)
        {
            if (!ColourFrequencyCheck(cube))
                return false;

            if (!AdjacentSquareCheck(Converter.FaceletToCubie(cube, false)))
                return false;

            return true;
        }

        private static bool ColourFrequencyCheck(Facelet cube)
        {
            // a list of the frequency of each colour
            List<int> frequencies = new() { 0, 0, 0, 0, 0, 0 };

            // increment for the corresponding colour on every square
            foreach (var s in cube.Faces.SelectMany(f => f.Value))
                frequencies[s]++;

            var s_frequencies = frequencies.ToHashSet();

            return s_frequencies.Count == 1 && s_frequencies.ElementAt(0) == 8;
        }

        private static bool AdjacentSquareCheck(Cubie cube)
        {
            return CheckRepeatedSquares(cube) && CheckIllegalPieces(cube);
        }

        private static bool CheckRepeatedSquares(Cubie cube)
        {
            return 
                cube.Corners.All(corner => corner.Value.colours.ToHashSet().Count == 3) && 
                cube.Edges.All(edge => edge.Value.colours.ToHashSet().Count == 2);
        }

        private static bool CheckIllegalPieces(Cubie cube)
        {
            var pieces = cube.Corners.Values.Concat(cube.Edges.Values);

            foreach (var piece in pieces)
            {
                var homeColours = Cubie.FindHomeColours(piece.colours);
                // null if homeColour search fails
                if (homeColours == null) return false;

                var orientation = Cubie.CalculateOrientation(piece.colours, homeColours);
                // -1 if orientation calculation fails
                if (orientation == -1) return false;
            }

            return true;
        }

        #endregion

        #region Solvable

        private static bool CheckSolvable(Cubie cubie, Facelet facelet)
        {
            if (!PermutationParityCheck(cubie)) return false;

            if (!TwistedCornerCheck(cubie)) return false;

            if (!EdgeParityCheck(facelet.Concat())) return false;

            return true;
        }

        private static bool PermutationParityCheck(Cubie p_cube)
        {
            Cubie cube = new (p_cube.Corners, p_cube.Edges);

            return (CountSwaps(cube.Corners) + CountSwaps(cube.Edges)) % 2 == 0;
        }

        private static int CountSwaps(IDictionary<int, Piece> pieces)
        {
            int swaps = 0;

            for (int i = 0; i < pieces.Count; i++)
            {
                int homeIndex = Cubie.FindHomeIndex(pieces[i].colours);

                if (i == homeIndex)
                    continue;

                // one piece is swapped into its correct position.
                (pieces[i], pieces[homeIndex]) = (pieces[homeIndex], pieces[i]); 

                swaps++;

                // force re-inspection of this position;
                i--;
            }

            return swaps;
        }

        private static bool TwistedCornerCheck(Cubie cube)
        {
            int cubeValue = cube.Corners.Sum(corner => corner.Value.orientation);

            return cubeValue % 3 == 0;
        }

        private static readonly List<int> KeyIndexes = new() { 12, 11, 25, 27, 28, 30, 4, 3, 22, 20, 19, 17 };

        private static bool EdgeParityCheck(IReadOnlyList<int> squares)
        {
            int superKeysCount = 0;

            foreach (int index in KeyIndexes)
            {
                var square = squares[index];
                var adjacentSquare = squares[GetOtherEdgeSquareIndex(index)];

                // first qualification for a super key
                if (square == WHITE || square == YELLOW)
                    superKeysCount++;
                // second qualification for a super key
                else if (adjacentSquare != WHITE && adjacentSquare != YELLOW && (square == BLUE || square == GREEN))
                    superKeysCount++;
            }

            return superKeysCount % 2 == 0;
        }

        #endregion
    }
}
