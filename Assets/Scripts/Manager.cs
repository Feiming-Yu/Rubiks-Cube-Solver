using UnityEngine;
using System.Collections.Generic;
using System.IO;
using SFB;
using Model;
using System;
using static Model.Facelet;
using System.Collections.Concurrent;
using Testing;
using UI;
using static Engine.Solver;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    [Serializable]
    public class CubeFile
    {
        public List<SerializableFace> startState;
        public List<string> moves;
        public int testNumber;
        public ExitCode exitCode;

        public CubeFile(Facelet startState, List<string> moves, int testNumber, ExitCode exitCode)
        {
            this.startState = startState.ToNestedList();
            this.moves = moves;
            this.testNumber = testNumber;
            this.exitCode = exitCode;
        }

        public override string ToString()
        {
            return "Test Number: " + testNumber.ToString() + $"  [{DateTime.Now}]" + "\n\n" +
                   startState + "\n" +
                   string.Join(" ", moves) + "\n\n"
                   + "Exit Code: " + exitCode + "\n"
                   + "===================================================" + "\n\n\n";
        }

        public Facelet ToFacelet()
        {
            var facelet = new Facelet();
            for (int i = 0; i < startState.Count; i++)
            {
                facelet.Add(i, startState[i].squares);
            }

            return facelet;
        }
    }

    public static Manager Instance;

    public static readonly ConcurrentQueue<CubeFile> LogQueue = new();

    [SerializeField] private GameObject invalidNotification;

    [SerializeField] private Transform palette;

    [SerializeField] private HorizontalMoveDisplay moveList;

    [SerializeField] private Transform settingsWindow;
    [SerializeField] private Transform cubeCanvas;
    [SerializeField] private Transform editorCanvas;
    [SerializeField] private Transform solverCanvas;

    private const string LOG_DIRECTORY = @"D:\Projects\Rubik's Cube Solver\Saves\";
    private const string LOG_FILE_PATH = @"D:\Projects\Rubik's Cube Solver\Log\Log.txt";

    public async void RunSuite(int frequency)
    {
        await TestSuitAutomation.RunRandomTestsAsync(frequency);
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(Instance);
    }


    public void Update()
    {
        if (LogQueue.Count > 0)
            Log();
    }

    private static void Log()
    {
        if (LogQueue.Count == 0)
            return;

        if (LogQueue.Count == 1)
            Debug.Log($"Tests logged: {LOG_DIRECTORY}");

        if (!LogQueue.TryDequeue(out var cubeFile))
            return;

        try
        {
            // Append to log file
            File.AppendAllText(LOG_FILE_PATH, cubeFile.ToString());

            // Save failed test JSON with timestamp and metadata
            string filename = $@"{LOG_DIRECTORY}[{DateTime.Now.ToFileTime()}] test no{cubeFile.testNumber} e{(int)cubeFile.exitCode}.cube";
            File.WriteAllText(filename, JsonUtility.ToJson(cubeFile));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to write log files: {ex.Message}");
        }
    }

    public void Import()
    {
        var path = StandaloneFileBrowser.OpenFilePanel("Open File", "", "cube", false);

        if (path.Length == 0)
            return;

        CubeFile cubeFile = JsonUtility.FromJson<CubeFile>(File.ReadAllText(path[0]));
        Facelet cube = cubeFile.ToFacelet();

        Cube.Instance.ClearQueue();
        Cube.Instance.SetFacelet(cube);
        Cube.Instance.UpdateCubie();
        Cube.Instance.SetColours(cube);
    }

    public void SwitchColour(int colour)
    {
        Player.Instance.currentColourInput = colour;
        ResetPaletteHighlighters();
    }

    public void ToggleInvalidNotification(bool active)
    {
        invalidNotification.SetActive(active);
    }

    private void ResetPaletteHighlighters()
    {
        int selectedColour = Player.Instance.currentColourInput;

        for (int i = 0; i < 6; i++)
        {
            Color32 color = (i == selectedColour)
                ? new Color32(94, 100, 111, 255)
                : new Color32(57, 60, 67, 255);

            palette.GetChild(i).GetComponent<Image>().color = color;
        }
    }

    public void ShowSettings()
    {
        settingsWindow.gameObject.SetActive(true);
        cubeCanvas.gameObject.SetActive(false);
    }

    public void HideSettings()
    {
        settingsWindow.gameObject.SetActive(false);
        cubeCanvas.gameObject.SetActive(true);
    }

    public void UpdateMoveList()
    {
        moveList.DisplayMoves(Cube.Instance.GetSolution());
    }

    public void SwitchMoveText(bool isProgress = true)
    {
        if (isProgress)
            moveList.Progress();
        else 
            moveList.Regress();
    }

    public void ListToStart()
    {
        moveList.ToStart();
    }

    public void ListToEnd()
    {
        moveList.ToEnd();
    }

    public void SwitchInterface()
    {
        editorCanvas.gameObject.SetActive(!editorCanvas.gameObject.activeSelf);
        solverCanvas.gameObject.SetActive(!solverCanvas.gameObject.activeSelf);
        
        Cube.Instance.SetIsSolving(solverCanvas.gameObject.activeSelf);
    }

    public void SwitchToEditor()
    {
        SwitchInterface();
        Cube.Instance.ClearQueue(); 
        ClearMoveListChildren();
    }

    public void ClearMoveListChildren()
    {
        moveList.ClearDisplay();
    }
}
