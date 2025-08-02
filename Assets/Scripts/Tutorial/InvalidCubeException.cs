using System;
using System.Collections.Generic;
using UI;

namespace Tutorial
{
    public abstract class InvalidCubeException : Exception
    {
        protected InvalidCubeException(string message) : base(message) { }
    }

    // Too many stickers of one color
    public class ColorFrequencyException : InvalidCubeException
    {
        public ColorFrequencyException(int colour)
            : base($"There are too many {Square.ColourToString(colour).ToLower()} stickers on the cube. Please make sure each color appears the correct number of times.")
        {
        }
    }

    // Twisted corner
    public class TwistedCornerException : InvalidCubeException
    {
        public TwistedCornerException()
            : base("The cube appears to be unsolvable. You may have twisted a corner.") { }
    }

    // Flipped edge
    public class FlippedEdgeException : InvalidCubeException
    {
        public FlippedEdgeException()
            : base("The cube appears to be unsolvable. You may have twisted an edge.") { }
    }

    // Unsolvable cube
    public class ImpossibleCubeConfigurationException : InvalidCubeException
    {
        public ImpossibleCubeConfigurationException()
            : base("The cube appears to be unsolvable. It may have been reassembled incorrectly.") { }
    }

    // Illegal piece
    public class ImpossiblePieceConfigurationException : InvalidCubeException
    {
        public readonly List<int> PieceColours;

        public ImpossiblePieceConfigurationException(List<int> pieceColours)
            : base(FormatMessage(pieceColours))
        {
            PieceColours = pieceColours;
        }

        private static string FormatMessage(List<int> colours)
        {
            var colorNames = colours.ConvertAll(c => Square.ColourToString(c).ToLower());

            // Natural language
            string colorList = colorNames.Count switch
            {
                2 => $"{colorNames[0]} and {colorNames[1]}",
                3 => $"{colorNames[0]}, {colorNames[1]}, and {colorNames[2]}",
                _ => string.Join(", ", colorNames)
            };

            return $"The piece with the {colorList} stickers is not valid. Please check this piece.";
        }
    }

    // Already solved cube
    public class CubeAlreadySolvedException : InvalidCubeException
    {
        public CubeAlreadySolvedException()
            : base("The cube is already solved!") { }
    }
}