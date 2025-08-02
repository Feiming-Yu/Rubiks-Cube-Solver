using System.Collections.Generic;
using System.Linq;
using Model;
using Tutorial;
using static Model.Converter;
using static UI.Square.Colour;
using static Model.Cubie;

namespace Engine
{
    // For clarity, I have not used boolean logic
    // e.g. return CheckLegal(cube) && !CheckSolvable(Converter.FaceletToCubie(cube), cube)
    // as separating the selection constructs improves clarity
    // and allows ease of following the step-by-step process to check validation.
    public static class Validation
    {
        public static InvalidCubeException InvalidCubeException { get; private set; }

        // Entry point: validate a cube represented as Facelets
        public static bool Validate(Facelet cube)
        {
            if (!CheckLegal(cube)) return false;
            if (!CheckSolvable(FaceletToCubie(cube), cube)) return false;
            if (CheckAlreadySolved(cube)) return false;

            return true;
        }

        private static bool CheckAlreadySolved(Facelet cube)
        {
            var solvedCubeSquares = CubieToFacelet(Cubie.Identity).Concat();
            if (!cube.Concat().SequenceEqual(solvedCubeSquares)) return false;

            InvalidCubeException = new CubeAlreadySolvedException();
            return true;
        }

        #region Legal

        private static bool CheckLegal(Facelet cube)
        {
            if (!ColourFrequencyCheck(cube))
                return false;

            if (!CheckIllegalPieces(FaceletToCubie(cube, false)))
                return false;

            return true;
        }
        
        /// <summary>
        /// Verify that each color appears exactly 8 times on the cube
        /// </summary>
        /// <param name="cube"></param>
        /// <returns></returns>
        private static bool ColourFrequencyCheck(Facelet cube)
        {
            // A list of the frequency of each colour (6 colors)
            List<int> frequencies = new() { 0, 0, 0, 0, 0, 0 };

            // Increment for the corresponding colour on every square
            foreach (var s in cube.Faces.SelectMany(f => f.Value))
                frequencies[s]++;

            // Convert to a set to check if all frequencies are equal
            var s_frequencies = frequencies.ToHashSet();

            // Valid only if there is exactly one unique frequency, and it equals 8
            if (s_frequencies.Count == 1 && s_frequencies.ElementAt(0) == 8)
                return true;

            InvalidCubeException = new ColorFrequencyException(frequencies.IndexOf(frequencies.Max()));
            return false;
        }

        private static bool CheckIllegalPieces(Cubie cube)
        {
            var pieces = cube.Corners.Values.Concat(cube.Edges.Values);

            foreach (var piece in pieces)
            {
                var homeColours = FindHomeColours(piece.colours);
                // Return false if home colors cannot be found
                if (homeColours == null)
                {
                    InvalidCubeException = new ImpossiblePieceConfigurationException(piece.colours);
                    return false;
                }

                var orientation = CalculateOrientation(piece.colours, homeColours);
                // Return false if orientation calculation fails (-1)
                if (orientation == -1)
                {
                    InvalidCubeException = new ImpossiblePieceConfigurationException(piece.colours);
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Solvable

        private static bool CheckSolvable(Cubie cubie, Facelet facelet)
        {
            InvalidCubeException = new ImpossibleCubeConfigurationException();
            if (!PermutationParityCheck(cubie)) return false;

            InvalidCubeException = new TwistedCornerException();
            if (!TwistedCornerCheck(cubie)) return false;

            InvalidCubeException = new FlippedEdgeException();
            if (!EdgeParityCheck(facelet.Concat())) return false;

            InvalidCubeException = null;

            return true;
        }
        
        /// <summary>
        /// Check that total number of swaps needed to solve corners and edges is even
        /// </summary>
        /// <returns></returns>
        private static bool PermutationParityCheck(Cubie p_cube)
        {
            Cubie cube = new(p_cube.Corners, p_cube.Edges);

            return (CountSwaps(cube.Corners) + CountSwaps(cube.Edges)) % 2 == 0;
        }

        /// <summary>
        /// Count the number of swaps to return pieces to their home positions
        /// </summary>
        private static int CountSwaps(IDictionary<int, Piece> pieces)
        {
            int swaps = 0;

            for (int i = 0; i < pieces.Count; i++)
            {
                int homeIndex = FindHomeIndex(pieces[i].colours);

                // Skip if piece is already in home position
                if (i == homeIndex)
                    continue;

                // Swap piece with the one in its home position
                (pieces[i], pieces[homeIndex]) = (pieces[homeIndex], pieces[i]); 

                swaps++;

                // Force re-inspection of this position
                i--;
            }

            return swaps;
        }

        private static bool TwistedCornerCheck(Cubie cube)
        {
            int cubeValue = cube.Corners.Sum(corner => corner.Value.orientation);

            return cubeValue % 3 == 0;
        }

        // Indices of "key" squares used for edge parity check
        private static readonly List<int> KeyIndexes = new() { 12, 11, 25, 27, 28, 30, 4, 3, 22, 20, 19, 17 };

        // Edge parity check using super key criteria on specific squares
        private static bool EdgeParityCheck(IReadOnlyList<int> squares)
        {
            int superKeysCount = 0;

            foreach (int index in KeyIndexes)
            {
                var square = squares[index];
                var adjacentSquare = squares[GetOtherEdgeSquareIndex(index)];

                // First qualification for a super key
                if (square is WHITE or YELLOW)
                    superKeysCount++;
                // Second qualification for a super key
                else if (adjacentSquare != WHITE && adjacentSquare != YELLOW && square is BLUE or GREEN)
                    superKeysCount++;
            }

            return superKeysCount % 2 == 0;
        }

        #endregion
    }
}
