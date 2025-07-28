using System.Collections.Generic;

public class Sequence
{
    public readonly int StageIndex;
    public readonly List<string> Moves;
    public readonly string Message;

    public Sequence(int stageIndex, List<string> moves, string message = "")
    {
        StageIndex = stageIndex;
        Moves = moves;
        Message = message;
    }
}

public class Stage
{
    public string Name { get; set; }
    public string FriendlyName { get; set; }
    public string Description { get; set; }

    public Stage(string name, string friendlyName, string description)
    {
        Name = name;
        FriendlyName = friendlyName;
        Description = description;
    }
    
    public static readonly Stage[] Stages = new Stage[]
    {
        new(
            "White Daisy",
            "Make a White Daisy",
            "Place the white edge pieces around the yellow center piece on the top face to create a daisy pattern."
        ),
        new(
            "White Cross",
            "Flip the Petals",
            "Align each white edge with its matching center color and rotate it down to the white face to form a full cross."
        ),
        new(
            "White Corners",
            "Finish the White Face",
            "Insert the white corner pieces between the matching side colors to complete the white face."
        ),
        new(
            "Middle Layer Edges",
            "Solve the Middle Belt",
            "Move the non-yellow edge pieces into the middle layer to connect the first and second layers properly."
        ),
        new(
            "Yellow Cross",
            "Make a Yellow Plus Sign",
            "Create a yellow '+' on the top face by correctly orienting the yellow edge pieces."
        ),
        new(
            "Yellow Edges",
            "Match the Yellow Plus",
            "Rotate the yellow edges until they align with the correct center colors on the side faces."
        ),
        new(
            "Yellow Corners Placement",
            "Put Yellow Corners in Place",
            "Position all yellow corner pieces in the correct locations, even if their orientations are incorrect."
        ),
        new(
            "Yellow Corners Orientation",
            "Turn the Yellow Corners",
            "Rotate the yellow corner pieces one by one to complete the yellow face and solve the cube."
        )
        
    };
}

