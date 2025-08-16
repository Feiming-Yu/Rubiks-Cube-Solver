using System;
using System.Collections.Generic;
using System.Linq;
using static Model.Cubie;

namespace Model
{
    public static class Converter
    {
        #region Constants

        private const int NUM_CORNERS = 8;
        private const int NUM_EDGES = 12;

        private const int NUM_CORNER_STICKERS = 3;
        private const int NUM_EDGE_STICKERS = 2;

        #endregion

        //       
        //                   .------------.
        //                   | 15  14  13 |
        //                   | 12  U   11 |
        //                   | 10  09  08 |
        //      .------------+------------+------------.
        //      | 32  33  34 | 24  25  26 | 40  41  42 |
        //      | 35  L   36 | 27  F   28 | 43  R   44 |
        //      | 37  38  39 | 29  30  31 | 45  46  47 |
        //      '------------+------------+------------'
        //                   | 07  06  05 |
        //                   | 04  D   03 |
        //                   | 02  01  00 |
        //                   +------------+
        //                   | 23  22  21 |
        //                   | 20  B   19 |
        //                   | 18  17  16 |
        //                   '------------'
        // 
        // Indexes of the squares in a concatenated list from Facelet.Concat()
        // 

        #region Facelet To Cubie

        // Indexes of squares for edge pieces
        private static readonly int[][] EDGE_FACELET_MAP =
        {
            new[] { 11, 41 }, // UR
            new[] { 09, 25 }, // UF
            new[] { 12, 33 }, // UL
            new[] { 14, 17 }, // UB
            new[] { 3 , 46 }, // DR
            new[] { 6 , 30 }, // DF
            new[] { 4 , 38 }, // DL
            new[] { 1 , 22 }, // DB
            new[] { 28, 43 }, // FR
            new[] { 27, 36 }, // FL
            new[] { 20, 35 }, // BL
            new[] { 19, 44 }, // BR
        };

        // Indexes of squares for corner pieces
        private static readonly int[][] CORNER_FACELET_MAP =
        {
            new[] { 8 , 40, 26 }, // URF
            new[] { 10, 24, 34 }, // UFL
            new[] { 15, 32, 18 }, // ULB
            new[] { 13, 16, 42 }, // UBR
            new[] { 5 , 31, 45 }, // DFR
            new[] { 7 , 39, 29 }, // DLF
            new[] { 2 , 23, 37 }, // DBL
            new[] { 0 , 47, 21 }, // DRB
        };
        
        public static Cubie FaceletToCubie(Facelet facelet, bool doOrientation = true)
        {
            Cubie cubie = new();

            var squares = facelet.Concat();

            for (int i = 0; i < NUM_CORNERS; i++)
            {
                List<int> colours = new();

                // Iterates through the 3 squares on a corner piece
                for (int j = 0; j < NUM_CORNER_STICKERS; j++)
                {
                    colours.Add(squares[CORNER_FACELET_MAP[i][j]]);
                }

                var orientation = 0;

                if (doOrientation)
                { 
                    // Get original sequence of colours
                    var homeColours = Cubie.FindHomeColours(colours);
                    orientation = Cubie.CalculateOrientation(homeColours, colours);
                }

                cubie.Add(i, colours, orientation);
            }

            for (int i = 0; i < NUM_EDGES; i++)
            {
                List<int> colours = new();

                // Iterates through the 2 squares on an edge piece
                for (int j = 0; j < NUM_EDGE_STICKERS; j++)
                {
                    colours.Add(squares[EDGE_FACELET_MAP[i][j]]);
                }

                var orientation = 0;

                if (doOrientation)
                {
                    // Get original sequence of colours
                    var homeColours = Cubie.FindHomeColours(colours);
                    orientation = Cubie.CalculateOrientation(homeColours, colours);
                }

                cubie.Add(i, colours, orientation);
            }

            return cubie;
        }
        
        #endregion
        
        #region Cubie To Facelet

        /// <summary>
        /// 0 = corners, 1 = edges
        /// </summary>
        private static readonly int[] PIECE_TYPE_CUBIE_MAP = { 0, 1, 0, 1, 1, 0, 1, 0 };
        
        /// <summary>
        /// First index = face
        /// Second index = piece index on face
        /// Third index = { piece index on cubie, square that corresponds to the face }
        /// </summary>
        private static readonly int[][][] CUBIE_MAP =
        {
            new[] // D FACE
            {
                new[] { 7, 0 }, // DRB
                new[] { 7, 0 }, // DB
                new[] { 6, 0 }, // DBL
                new[] { 4, 0 }, // DR
                new[] { 6, 0 }, // DL
                new[] { 4, 0 }, // DFR
                new[] { 5, 0 }, // DF
                new[] { 5, 0 }, // DLF
            },
            new[] // U FACE
            {
                new[] { 0, 0 }, // URF
                new[] { 1, 0 }, // UF
                new[] { 1, 0 }, // UFL
                new[] { 0, 0 }, // UR
                new[] { 2, 0 }, // UL
                new[] { 3, 0 }, // UBR
                new[] { 3, 0 }, // UB
                new[] { 2, 0 }, // ULB
            },
            new[] // B FACE
            {
                new[] { 3 , 1 }, // UBR
                new[] { 3 , 1 }, // UB
                new[] { 2 , 2 }, // ULB
                new[] { 11, 0 }, // BR
                new[] { 10, 0 }, // BL
                new[] { 7 , 2 }, // DRB
                new[] { 7 , 1 }, // DB
                new[] { 6 , 1 }, // DBL
            },
            new[] // F FACE
            {
                new[] { 1, 1 }, // UFL
                new[] { 1, 1 }, // UF
                new[] { 0, 2 }, // URF
                new[] { 9, 0 }, // FL
                new[] { 8, 0 }, // FR
                new[] { 5, 2 }, // DLF
                new[] { 5, 1 }, // DF
                new[] { 4, 1 }, // DFR
            },
            new[] // L FACE
            {
                new[] { 2 , 1 }, // ULB
                new[] { 2 , 1 }, // UL
                new[] { 1 , 2 }, // UFL
                new[] { 10, 1 }, // BL
                new[] { 9 , 1 }, // FL
                new[] { 6 , 2 }, // DBL
                new[] { 6 , 1 }, // DL
                new[] { 5 , 1 }, // DLF
            },
            new[] // R FACE
            {
                new[] { 0 , 1 }, // URF
                new[] { 0 , 1 }, // UR
                new[] { 3 , 2 }, // UBR
                new[] { 8 , 1 }, // FR
                new[] { 11, 1 }, // BR
                new[] { 4 , 2 }, // DFR
                new[] { 4 , 1 }, // DR
                new[] { 7 , 1 }, // DRB
            },
        };

        public static Facelet CubieToFacelet(Cubie cubie)
        {
            Facelet facelet = new();

            // faces
            for (int i = 0; i < 6; i++)
            {
                List<int> squares = new();

                // squares on the face
                for (int j = 0; j < 8; j++)
                {
                    // Corners or edges
                    var pieces = PIECE_TYPE_CUBIE_MAP[j] == 0 ? cubie.Corners : cubie.Edges;
                    
                    int pieceIndex = CUBIE_MAP[i][j][0];
                    int squareIndex = CUBIE_MAP[i][j][1];

                    Piece piece = pieces[pieceIndex];
                    int index = NormaliseIndex(squareIndex - piece.orientation, piece.colours.Count);

                    squares.Add(piece.colours[index]);
                }
                
                facelet.Add(i, squares);
            }

            return facelet;
        }

        /// <summary>
        /// Supports circular data structure. Sets index to the max when decrement below 0
        /// </summary>
        /// <param name="index">New index</param>
        /// <param name="size">Size of list</param>
        /// <returns></returns>
        private static int NormaliseIndex(int index, int size) => ((index % size) + size) % size;

        #endregion

        public static int GetOtherEdgeSquareIndex(int index)
        {
            foreach (var edge in EDGE_FACELET_MAP.Where(edge => edge.Contains(index)))
                return edge[1 - edge.ToList().IndexOf(index)];

            throw new ArgumentException("Square index out of bounds");
        }
    }
}
