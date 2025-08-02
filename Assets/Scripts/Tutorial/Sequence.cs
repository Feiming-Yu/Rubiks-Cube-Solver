using System.Collections.Generic;

namespace Tutorial
{
    public class Sequence
    {
        public readonly int StageIndex;
        public readonly List<string> Moves;
        public readonly string Message;
        public readonly List<int> InterestPiece;

        public Sequence(int stageIndex, List<string> moves, List<int> interestPiece, string message = "")
        {
            StageIndex = stageIndex;
            Moves = moves;
            Message = message;
            InterestPiece = interestPiece;
        }
    }

    public class Stage
    {
        public readonly int Index;
        public readonly string Name;
        public readonly string FriendlyName;
        public readonly string Description;

        private Stage(string name, string friendlyName, string description, int index)
        {
            Name = name;
            FriendlyName = friendlyName;
            Description = description;
            Index = index;
        }

        public override string ToString()
        {
            return $"Index: {Index}, Name: {Name}, FriendlyName: {FriendlyName}, Description: {Description}";
        }

        public static readonly Stage[] Stages =
        {
            new(
                "White Daisy",
                "Make a White Daisy",
                "Place the white edge pieces around the yellow center piece on the top face to create a daisy pattern.",
                0
            ),
            new(
                "White Cross",
                "Flip the Petals",
                "Align each white edge with its matching center color and rotate it down to the white face to form a full cross.",
                1
            ),
            new(
                "White Corners",
                "Finish the White Face",
                "Insert the white corner pieces between the matching side colors to complete the white face.",
                2
            ),
            new(
                "Middle Layer Edges",
                "Solve the Middle Belt",
                "Move the non-yellow edge pieces into the middle layer to connect the first and second layers properly.",
                3
            ),
            new(
                "Yellow Cross",
                "Make a Yellow Plus Sign",
                "Create a yellow '+' on the top face by correctly orienting the yellow edge pieces.",
                4
            ),
            new(
                "Yellow Edges",
                "Match the Yellow Plus",
                "Rotate the yellow edges until they align with the correct center colors on the side faces.",
                5
            ),
            new(
                "Yellow Corners Placement",
                "Put Yellow Corners in Place",
                "Position all yellow corner pieces in the correct locations, even if their orientations are incorrect.",
                6
            ),
            new(
                "Yellow Corners Orientation",
                "Turn the Yellow Corners",
                "Rotate the yellow corner pieces one by one to complete the yellow face and solve the cube.",
                7
            )
        };
    }
}