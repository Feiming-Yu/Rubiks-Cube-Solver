using System.Collections.Generic;
using System.Linq;
using Model;
using UnityEngine;
using static Model.Converter;
using static UI.Square.Colour;
using static UI.Square;

namespace Engine
{
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
            List<int> frequencies = new() { 0, 0, 0, 0, 0, 0 };

            foreach (var f in cube.Faces)
            foreach (var s in f.Value)
                frequencies[s]++;
        
            return frequencies.ToHashSet().Count == 1;
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
                if (homeColours == null) return false;

                var orientation = Cubie.CalculateOrientation(piece.colours, homeColours);
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

            if (!EdgeParityCheck(cubie, facelet.Concat())) return false;

            return true;
        }

        private static bool PermutationParityCheck(Cubie p_cube)
        {
            Cubie cube = new (p_cube.Corners, p_cube.Edges);

            return (CountSwaps(cube.Corners) + CountSwaps(cube.Edges)) % 2 == 0;
        }

        private static int CountSwaps(IDictionary<int, (List<int> colours, int orientation)> pieces)
        {
            int swaps = 0;

            for (int i = 0; i < pieces.Count; i++)
            {
                int homeIndex = Cubie.FindHomeIndex(pieces[i].colours);

                if (i == homeIndex)
                    continue;

                (pieces[i], pieces[homeIndex]) = (pieces[homeIndex], pieces[i]);

                swaps++;
                i--;
            }

            return swaps;
        }

        private static bool TwistedCornerCheck(Cubie cube)
        {
            int cubeValue = cube.Corners.Sum(corner => corner.Value.orientation);

            return cubeValue % 3 == 0;
        }

        private static bool EdgeParityCheck(Cubie cubie, IReadOnlyList<int> squares)
        {
            List<int> keyIndexes = new() { 12, 11, 25, 27, 28, 30, 4, 3, 22, 20, 19, 17 };

            int superKeySCount = 0;

            foreach (int index in keyIndexes)
            {
                var square = squares[index];
                var adjacentSquare = squares[GetOtherEdgeSquareIndex(index)];

                Debug.Log(ColourToString(square) + ColourToString(adjacentSquare));

                if (square == WHITE || square == YELLOW)
                    superKeySCount++;
                else if (adjacentSquare != WHITE && adjacentSquare != YELLOW && (square == BLUE || square == GREEN))
                    superKeySCount++;
            }

            return superKeySCount % 2 == 0;
        }

        #endregion
    }
}
