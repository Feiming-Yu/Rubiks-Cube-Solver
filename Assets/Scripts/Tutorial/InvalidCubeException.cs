using System;
using static UI.Square;
using System.Collections.Generic;

public class InvalidCubeException : Exception
{
    public InvalidCubeException(string message) : base(message) { }

    public InvalidCubeException() : base("An error occurred with the cube. Please check your input and try again.") { }
}

// Too many stickers of one color
public class ColorFrequencyException : InvalidCubeException
{
    public readonly int Colour;

    public ColorFrequencyException(int colour)
        : base($"There are too many {ColourToString(colour).ToLower()} stickers on the cube. Please make sure each color appears the correct number of times.")
    {
        Colour = colour;
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
        var colorNames = colours.ConvertAll(c => ColourToString(c).ToLower());

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
