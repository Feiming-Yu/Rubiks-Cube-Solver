using System.Collections.Generic;
using UnityEngine;

namespace Model
{
    public class Converter : MonoBehaviour
    {
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
        // indexes of the squares in a concatenated list from Facelet.Concat()
        // 

        #region Facelet To Cubie

        // indexes of squares for edge pieces
        private static readonly int[][] EdgeFaceletMap = new int[12][]
        {
            new int[] { 11, 41 }, // UR
            new int[] { 09, 25 }, // UF
            new int[] { 12, 33 }, // UL
            new int[] { 14, 17 }, // UB
            new int[] { 3 , 46 }, // DR
            new int[] { 6 , 30 }, // DF
            new int[] { 4 , 38 }, // DL
            new int[] { 1 , 22 }, // DB
            new int[] { 28, 43 }, // FR
            new int[] { 27, 36 }, // FL
            new int[] { 20, 35 }, // BL
            new int[] { 19, 44 }, // BR
        };

        // indexes of squares for corner pieces
        private static readonly int[][] CornerFaceletMap = new int[8][]
        {
            new int[] { 8 , 40, 26 }, // URF
            new int[] { 10, 24, 34 }, // UFL
            new int[] { 15, 32, 18 }, // ULB
            new int[] { 13, 16, 42 }, // UBR
            new int[] { 5 , 31, 45 }, // DFR
            new int[] { 7 , 39, 29 }, // DLF
            new int[] { 2 , 23, 37 }, // DBL
            new int[] { 0 , 47, 21 }, // DRB
        };
        
        public static Cubie FaceletToCubie(Facelet facelet)
        {
            Cubie cubie = new();

            var squares = facelet.Concat();

            for (int i = 0; i < 8; i++)
            {
                List<int> colours = new();

                // iterates through the 3 squares on a corner piece
                for (int j = 0; j < 3; j++)
                {
                    colours.Add(squares[CornerFaceletMap[i][j]]);
                }

                // Get original sequence of colours
                var homeColours = Cubie.FindHomeColours(colours);
                var orientation = Cubie.CalculateOrientation(homeColours, colours);
                cubie.Add(i, colours, orientation);
            }

            for (int i = 0; i < 12; i++)
            {
                List<int> colours = new();

                // iterates through the 2 squares on an edge piece
                for (int j = 0; j < 2; j++)
                {
                    colours.Add(squares[EdgeFaceletMap[i][j]]);
                }

                // Get original sequence of colours
                var homeColours = Cubie.FindHomeColours(colours);
                var orientation = Cubie.CalculateOrientation(homeColours, colours);
                cubie.Add(i, colours, orientation);
            }

            return cubie;
        }
        
        #endregion
        
        #region Cubie To Facelet

        /// <summary>
        /// 0 = corners, 1 = edges
        /// </summary>
        private static readonly int[] PieceTypeCubieMap = new int[8] { 0, 1, 0, 1, 1, 0, 1, 0 };
        
        /// <summary>
        /// First index = face
        /// Second index = piece index on face
        /// Third index = { piece index on cubie, square that corresponds to the face }
        /// </summary>
        private static readonly int[][][] CubieMap = new int[6][][]
        {
            new int[8][] // D FACE
            {
                new int[2] { 7, 0 }, // DRB
                new int[2] { 7, 0 }, // DB
                new int[2] { 6, 0 }, // DBL
                new int[2] { 4, 0 }, // DR
                new int[2] { 6, 0 }, // DL
                new int[2] { 4, 0 }, // DFR
                new int[2] { 5, 0 }, // DF
                new int[2] { 5, 0 }, // DLF
            },
            new int[8][] // U FACE
            {
                new int[2] { 0, 0 }, // URF
                new int[2] { 1, 0 }, // UF
                new int[2] { 1, 0 }, // UFL
                new int[2] { 0, 0 }, // UR
                new int[2] { 2, 0 }, // UL
                new int[2] { 3, 0 }, // UBR
                new int[2] { 3, 0 }, // UB
                new int[2] { 2, 0 }, // ULB
            },
            new int[8][] // B FACE
            {
                new int[2] { 3 , 1 }, // UBR
                new int[2] { 3 , 1 }, // UB
                new int[2] { 2 , 2 }, // ULB
                new int[2] { 11, 0 }, // BR
                new int[2] { 10, 0 }, // BL
                new int[2] { 7 , 2 }, // DRB
                new int[2] { 7 , 1 }, // DB
                new int[2] { 6 , 1 }, // DBL
            },
            new int[8][] // F FACE
            {
                new int[2] { 1, 1 }, // UFL
                new int[2] { 1, 1 }, // UF
                new int[2] { 0, 2 }, // URF
                new int[2] { 9, 0 }, // FL
                new int[2] { 8, 0 }, // FR
                new int[2] { 5, 2 }, // DLF
                new int[2] { 5, 1 }, // DF
                new int[2] { 4, 1 }, // DFR
            },
            new int[8][] // L FACE
            {
                new int[2] { 2 , 1 }, // ULB
                new int[2] { 2 , 1 }, // UL
                new int[2] { 1 , 2 }, // UFL
                new int[2] { 10, 1 }, // BL
                new int[2] { 9 , 1 }, // FL
                new int[2] { 6 , 2 }, // DBL
                new int[2] { 6 , 1 }, // DL
                new int[2] { 5 , 1 }, // DLF
            },
            new int[8][] // R FACE
            {
                new int[2] { 0 , 1 }, // URF
                new int[2] { 0 , 1 }, // UR
                new int[2] { 3 , 2 }, // UBR
                new int[2] { 8 , 1 }, // FR
                new int[2] { 11, 1 }, // BR
                new int[2] { 4 , 2 }, // DFR
                new int[2] { 4 , 1 }, // DR
                new int[2] { 7 , 1 }, // DRB
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
                    var pieces = PieceTypeCubieMap[j] == 0 ? cubie.Corners : cubie.Edges;
                    int pieceIndex = CubieMap[i][j][0];
                    int squareIndex = CubieMap[i][j][1];

                    var (colours, orientation) = pieces[pieceIndex];
                    int index = NormaliseIndex(squareIndex - orientation, colours.Count);

                    squares.Add(colours[index]);
                }
                
                facelet.Add(i, squares);
            }

            return facelet;
        }

        /// <summary>
        /// Supports cyclical data structure. Sets index to the max when decrement below 0
        /// </summary>
        /// <param name="index">New index</param>
        /// <param name="size">Size of list</param>
        /// <returns></returns>
        private static int NormaliseIndex(int index, int size) => ((index % size) + size) % size;

        #endregion
    }

}
