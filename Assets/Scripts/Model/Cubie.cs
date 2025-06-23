using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using static UI.Square;
using static UI.Square.Colour;

namespace Model
{
    public class Cubie
    {
        /*
        Index   Corner    ||   Index    Edge
          0	      URF     ||     0	     UR
          1	      UFL     ||     1	     UF
          2	      ULB     ||     2	     UL
          3	      UBR     ||     3	     UB
          4	      DFR     ||     4	     DR
          5	      DLF     ||     5	     DF
          6	      DBL     ||     6	     DL
          7	      DRB     ||     7	     DB
                          ||     8	     FR
                          ||     9	     FL
                          ||     10      BL
                          ||     11      BR

        Order of letters for corner and edge corresponds to order of the squares on the piece
         */
        public Dictionary<int, (List<int> colours, int orientation)> Corners { get; private set; }
        public Dictionary<int, (List<int> colours, int orientation)> Edges { get; private set; }

        /// <summary>
        /// A solved cube
        /// </summary>
        private static readonly Cubie Identity = new(
            new Dictionary<int, (List<int> colours, int orientation)>
            {
                { 0, (new() { YELLOW, RED, BLUE }, 0) },
                { 1, (new() { YELLOW, BLUE, ORANGE }, 0) },
                { 2, (new() { YELLOW, ORANGE, GREEN }, 0) },
                { 3, (new() { YELLOW, GREEN, RED }, 0) },
                { 4, (new() { WHITE, BLUE, RED }, 0) },
                { 5, (new() { WHITE, ORANGE, BLUE }, 0) },
                { 6, (new() { WHITE, GREEN, ORANGE }, 0) },
                { 7, (new() { WHITE, RED, GREEN }, 0) }
            },
            new Dictionary<int, (List<int> colours, int orientation)>
            {
                { 0, (new() { YELLOW, RED }, 0) },
                { 1, (new() { YELLOW, BLUE }, 0) },
                { 2, (new() { YELLOW, ORANGE }, 0) },
                { 3, (new() { YELLOW, GREEN }, 0) },
                { 4, (new() { WHITE, RED }, 0) },
                { 5, (new() { WHITE, BLUE }, 0) },
                { 6, (new() { WHITE, ORANGE }, 0) },
                { 7, (new() { WHITE, GREEN }, 0) },
                { 8, (new() { BLUE, RED }, 0) },
                { 9, (new() { BLUE, ORANGE }, 0) },
                { 10, (new() { GREEN, ORANGE }, 0) },
                { 11, (new() { GREEN, RED }, 0) }
            }
        );

        private Cubie(Dictionary<int, (List<int> colours, int orientation)> corners, Dictionary<int, (List<int> colours, int orientation)> edges)
        {
            this.Corners = corners;
            this.Edges = edges;
        }

        public Cubie()
        {
            Corners = new Dictionary<int, (List<int> colours, int orientation)>();
            Edges = new Dictionary<int, (List<int> colours, int orientation)>();
        }

        public void Add(int index, List<int> colours, int orientation)
        {
            (colours.Count == 3 ? Corners : Edges).Add(index, (colours, orientation));
        }


        public static List<int> FindHomeColours(List<int> colours)
        {
            // validity check for correct number of colours
            if (colours.Count != 2 && colours.Count != 3)
                throw new ArgumentException("Impossible piece");

            // identify which to search
            var pieces = colours.Count == 2 ? Identity.Edges : Identity.Corners;

            foreach (var kvp in pieces)
            {
                // check if colours are matching, ignoring order
                if (kvp.Value.colours.All(colours.Contains))
                    return kvp.Value.colours;
            }

            throw new ArgumentException("Piece not found");
        }

        public static int CalculateOrientation(List<int> original, List<int> data)
        {
            int orientation = 0;

            for (int i = 0; i < 3; i++)
            {
                // check if order of colours are matching
                if (data.SequenceEqual(original))
                    return orientation;

                // needs a rotation to match
                orientation++;

                // list of colours is a circular structure
                // shifts all colours to the right, moves last item to start
                // this is equivalent to one clockwise rotation of the piece
                data.Add(data[0]);
                data.RemoveAt(0);
            }

            // colours are matching but sequence will never match
            // unexpected error
            throw new ArgumentException("Invalid piece sequence");
        }

        public void Log()
        {
            string message = "";
            foreach (var corner in Corners)
            {
                message += $"Corner {corner.Key} : {string.Join("", corner.Value.colours.Select(ColourToString).ToList())} - {corner.Value.orientation}\n";
            }

            foreach (var edge in Edges)
            {
                message += $"Edge {edge.Key} : {string.Join("", edge.Value.colours.Select(ColourToString).ToList())} - {edge.Value.orientation}\n";
            }

            Debug.Log(message);
        }

        #region Move Logic

        /// <summary>
        /// Key represents the face index.
        /// 
        /// Value consists of two integer arrays.
        /// First array is index of the corner pieces on the face.
        /// Second array is the adjustment to the orientation so that the layer rotates properly
        /// </summary>

        private static readonly Dictionary<int, (int[] piecesIndexes, int[] orientationDeltas)> CornerMoveMap = new()
        {
            { YELLOW, (new[] { 0, 1, 2, 3 }, new[] { 0, 0, 0, 0 }) },
            { WHITE,  (new[] { 7, 6, 5, 4 }, new[] { 0, 0, 0, 0 }) },
            { RED,    (new[] { 3, 7, 4, 0 }, new[] { 1, 2, 1, 2 }) },
            { ORANGE, (new[] { 1, 5, 6, 2 }, new[] { 1, 2, 1, 2 }) },
            { BLUE,   (new[] { 0, 4, 5, 1 }, new[] { 1, 2, 1, 2 }) },
            { GREEN,  (new[] { 2, 3, 7, 6 }, new[] { 1, 2, 1, 2 }) },
        };

        /// <summary>
        /// Key represents the face index.
        /// 
        /// Value consists of two integer arrays.
        /// First array is index of the edge pieces on the face.
        /// Second array is the adjustment to the orientation so that the layer rotates properly
        /// </summary>
        private static readonly Dictionary<int, (int[] piecesIndexes, int[] orientationDeltas)> EdgeMoveMap = new()
        {
            { YELLOW, (new[] { 0, 1 , 2 , 3  }, new[] { 0, 0, 0, 0 }) }, 
            { WHITE , (new[] { 7, 6 , 5 , 4  }, new[] { 0, 0, 0, 0 }) }, 
            { RED   , (new[] { 0, 11, 4 , 8  }, new[] { 0, 0, 0, 0 }) },
            { ORANGE, (new[] { 9, 6 , 10, 2  }, new[] { 0, 0, 0, 0 }) },
            { BLUE  , (new[] { 1, 8 , 5 , 9  }, new[] { 1, 1, 1, 1 }) },
            { GREEN , (new[] { 3, 11, 7 , 10 }, new[] { 1, 1, 1, 1 }) },
        };

        public void Move(string move)
        {
            string layer = move[0].ToString(); 
            
            // identify if the move is followed by a prime (') or double (2)
            string special = move[^1].ToString();

            RotateLayer(layer, special == "'", special == "2");
        }

        private void RotateLayer(string layer, bool prime, bool twice)
        {
            RotatePieces(CornerMoveMap[FaceToIndex(layer)], Corners, prime, twice);
            RotatePieces(EdgeMoveMap[FaceToIndex(layer)], Edges, prime, twice);
        }

        private void RotatePieces((int[] indexes, int[] orientationDelta) moveInfo, Dictionary<int, (List<int> colours, int orientation)> pieces, bool prime, bool twice)
        {
            // Not possible to have both true as U'2 == U2
            if(prime && twice)
                throw new ArgumentException("Prime and twice cannot both be true");

            if (prime)
                PrimeRotatePieces(moveInfo, pieces);
            else if (twice)
                DoubleRotatePieces(moveInfo, pieces);
            else
                NormalRotatePieces(moveInfo, pieces);

        }

        private void  NormalRotatePieces((int[] indexes, int[] orientationDelta) moveInfo, Dictionary<int, (List<int> colours, int orientation)> pieces)
        {
            // temporarily store the last piece
            var lastPiece = pieces[moveInfo.indexes[^1]]; 

            for (int i = moveInfo.indexes.Length - 1; i > 0; i--)
            {
                // shift pieces to the right and adjust orientation
                pieces[moveInfo.indexes[i]] = Reorientate(pieces[moveInfo.indexes[i - 1]], moveInfo.orientationDelta[i]); 
            }

            // move last piece to the start
            pieces[moveInfo.indexes[0]] = Reorientate(lastPiece, moveInfo.orientationDelta[0]); 
        }

        /// <summary>
        /// Swap the pieces directly opposite, an iteration is not needed.
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="pieces"></param>
        private void DoubleRotatePieces((int[] indexes, int[] orientationDelta) moveInfo, Dictionary<int, (List<int> colours, int orientation)> pieces)
        {
            // swap opposite pieces
            (pieces[moveInfo.indexes[0]], pieces[moveInfo.indexes[2]]) = (pieces[moveInfo.indexes[2]], pieces[moveInfo.indexes[0]]);
            (pieces[moveInfo.indexes[1]], pieces[moveInfo.indexes[3]]) = (pieces[moveInfo.indexes[3]], pieces[moveInfo.indexes[1]]);
        }

        /// <summary>
        /// Shift the pieces in the left direction to do a prime rotation
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="pieces"></param>
        private void PrimeRotatePieces((int[] indexes, int[] orientationDelta) moveInfo, Dictionary<int, (List<int> colours, int orientation)> pieces)
        {
            // temporarily store the first piece
            var firstPiece = pieces[moveInfo.indexes[0]];

            for (int i = 0; i < moveInfo.indexes.Length - 1; i++)
            {
                // shift pieces to the left and adjust orientation
                pieces[moveInfo.indexes[i]] = Reorientate(pieces[moveInfo.indexes[i + 1]], moveInfo.orientationDelta[i]);
            }

            // move first piece to the end
            pieces[moveInfo.indexes[^1]] = Reorientate(firstPiece, moveInfo.orientationDelta[^1]);
        }

        private (List<int> colours, int orientation) Reorientate((List<int> colours, int orientation) piece, int orientationDelta)
        {
            return (piece.colours, (piece.orientation + orientationDelta) % piece.colours.Count);
        }

        #endregion
    }
}
