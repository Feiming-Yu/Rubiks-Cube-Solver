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
using static UI.Cube;
using System.Linq;
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
                   startState.ToString() + "\n" +
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

    [SerializeField] private GameObject _invalidNotification;

    [SerializeField] private Transform _palette;

    [SerializeField] private Transform _settingsWindow, _cubeCanvas;

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
        Log();
    }

    private static void Log()
    {
        if (LogQueue.Count <= 0) return;
        
        if (LogQueue.Count == 1)
            Debug.Log(@"Tests logged: D:\Projects\Rubik's Cube Solver\Saves\");

        // dequeue the next log entry for processing
        LogQueue.TryDequeue(out var cubeFile);
        
        File.AppendAllText(@"D:\Projects\Rubik's Cube Solver\Log\Log.txt", cubeFile.ToString());

        // save the failed test as a JSON file with a unique timestamped filename
        File.WriteAllText(@$"D:\Projects\Rubik's Cube Solver\Saves\[{DateTime.Now.ToFileTime()}] test no{cubeFile.testNumber} e{(int)cubeFile.exitCode}.cube", JsonUtility.ToJson(cubeFile));
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
        _invalidNotification.SetActive(active);
    }

    public void ResetPaletteHighlighters()
    {
        for (int i = 0; i < 6; i++)
        {
            if (i == Player.Instance.currentColourInput)
            {
                _palette.GetChild(i).GetComponent<Image>().color = new Color32(94, 100, 111, 255);
                continue;
            }

            _palette.GetChild(i).GetComponent<Image>().color = new Color32(57, 60, 67, 255);
        }
    }

    public void ShowSettings()
    {
        _settingsWindow.gameObject.SetActive(true);
        _cubeCanvas.gameObject.SetActive(false);
    }

    public void HideSettings()
    {
        _settingsWindow.gameObject.SetActive(false);
        _cubeCanvas.gameObject.SetActive(true);
    }
}
