using Model;
using SimpleFileBrowser;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Testing;
using Tutorial;
using UI;
using UnityEngine;
using UnityEngine.UI;
using static Engine.Solver;
using static Model.Facelet;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using SFB; // For StandaloneFileBrowser
#endif

public class Manager : MonoBehaviour
{
    [Serializable]
    public class CubeFile
    {
        public List<SerializableFace> startState;
        public List<string> moves;
        public int testNumber;
        public ExitCode exitCode;

        public CubeFile(Facelet startState, List<Sequence> solution, int testNumber, ExitCode exitCode)
        {
            this.startState = startState.ToNestedList();
            this.testNumber = testNumber;
            this.exitCode = exitCode;
            
            moves = solution.SelectMany(s => s.Moves).ToList();
        }

        public override string ToString()
        {
            return "Test Number: " + testNumber + $"  [{DateTime.Now}]" + "\n\n" +
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

    [SerializeField] private Transform palette;

    [SerializeField] private HorizontalMoveDisplay moveList;

    [SerializeField] private Transform importButton;

    [SerializeField] private TMPro.TextMeshProUGUI moveDescription, invalidNotification;
    [SerializeField] private GameObject showSequenceButton, showStageButton;
    [SerializeField] private Toggle tutorialsToggle;

    [Header("Interfaces")] 
    [SerializeField] private Transform settingsWindow;
    [SerializeField] private Transform cubeCanvas;
    [SerializeField] private Transform editorCanvas;
    [SerializeField] private Transform solverCanvas;
    [SerializeField] private Transform fileWindow;

    [Header("Theme")]
    [SerializeField] private Theme[] themes;
    [SerializeField] private Button[] mainButtons, windowButtons, closeButtons;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Image[] windowBackgrounds;
    [SerializeField] private TMPro.TextMeshProUGUI[] texts;
    [SerializeField] private TMPro.TextMeshProUGUI[] titles;
    [SerializeField] private Slider[] sliders;
    [SerializeField] private PaletteButton[] paletteButtons;
    [SerializeField] private SpriteRenderer[] popUpBoxes;
    [SerializeField] private SpriteRenderer warningIcon;

    private const string LOG_DIRECTORY = @"D:\Projects\Rubik's Cube Solver\Saves\";
    private const string LOG_FILE_PATH = @"D:\Projects\Rubik's Cube Solver\Log\Log.txt";

    [HideInInspector] public bool isWindowOpen;
    [HideInInspector] public bool highlightPiece = true;
    [HideInInspector] public bool useTutorials = true;
    [HideInInspector] public bool useStages = true;
    [HideInInspector] public int currentThemeIndex;
    [HideInInspector] public int currentColourInput = Square.Colour.WHITE;

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
        HandleLogs();
    }

    #region File Handling
    
    private static void HandleLogs()
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
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        var path = StandaloneFileBrowser.OpenFilePanel("Load Cube", "", "cube", false);
        if (path.Length == 0) return;

        LoadCubeFromFile(path[0]);
#elif UNITY_ANDROID
        StartCoroutine(ImportAndroid());
#else
        Debug.LogWarning("Import not supported on this platform.");
#endif
    }

    public void Export()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        var cubeFile = CreateCubeFile();

        var path = StandaloneFileBrowser.SaveFilePanel("Save Cube", "", "exported_cube", "cube");
        if (string.IsNullOrEmpty(path)) return;

        File.WriteAllText(path, JsonUtility.ToJson(cubeFile));
#elif UNITY_ANDROID
        StartCoroutine(ExportAndroid());
#else
        Debug.LogWarning("Export not supported on this platform.");
#endif
    }

    private void LoadCubeFromFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            CubeFile cubeFile = JsonUtility.FromJson<CubeFile>(json);
            Facelet cube = cubeFile.ToFacelet();

            Cube.Instance.TrackCube();
            Cube.Instance.ClearQueue();
            Cube.Instance.SetFacelet(cube);
            Cube.Instance.UpdateCubie();
            Cube.Instance.SetColours(cube);

            Debug.Log("Cube imported successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError("Error loading cube file: " + e.Message);
        }
    }

    private CubeFile CreateCubeFile()
    {
        return new CubeFile(
            Cube.Instance.GetFacelet(),
            Cube.Instance.GetSolution(),
            -1,
            (ExitCode)(-1));
    }

    private IEnumerator ImportAndroid()
    {
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageRead);
            yield return new WaitForSeconds(1f);
        }

        FileBrowser.SetFilters(true, new[] { ".cube" });
        FileBrowser.SetDefaultFilter(".cube");

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, "", "Load Cube");

        if (FileBrowser.Success)
        {
            LoadCubeFromFile(FileBrowser.Result[0]);
        }
        else
        {
            Debug.Log("File picking cancelled.");
        }
#endif
    }

    private IEnumerator ExportAndroid()
    {
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageWrite);
            yield return new WaitForSeconds(1f);
        }
        var cubeFile = CreateCubeFile();

        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, null, "", "Save Cube");

        if (FileBrowser.Success)
        {
            File.WriteAllText(FileBrowser.Result[0], JsonUtility.ToJson(cubeFile));
            Debug.Log("Cube saved successfully.");
        }
        else
        {
            Debug.Log("Save cancelled.");
        }
#endif
    }


    #endregion

    public void SwitchInputColour(int colour)
    {
        Instance.currentColourInput = colour;
        ResetPaletteHighlighters();
    }

    private void ResetPaletteHighlighters()
    {
        for (int i = 0; i < 6; i++)
            palette.GetChild(i).GetChild(0).GetComponent<PaletteButton>().UpdateColour();
    }

    #region Window Management
    
    public void ShowSettings()
    {
        settingsWindow.gameObject.SetActive(true);

        isWindowOpen = true;
    }

    public void HideSettings()
    {
        settingsWindow.gameObject.SetActive(false);

        isWindowOpen = false;
    }
    
    public void ShowFile()
    {
        fileWindow.gameObject.SetActive(true);
        importButton.gameObject.SetActive(!Cube.Instance.IsSolving());

        isWindowOpen = true;
    }

    public void HideFile()
    {
        fileWindow.gameObject.SetActive(false);

        isWindowOpen = false;
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

        Cube.Instance.TurnOffAutomation();
        Cube.Instance.RemoveHighlightedPiece();
    }

    #endregion

    #region Move List

    public void UpdateMoveList()
    {
        moveList.DisplayMoves(Cube.Instance.GetCurrentMoveQueue());
        UpdateMoveDescription();
    }

    public void SwitchMoveText(bool isProgress = true)
    {
        if (isProgress) 
            moveList.Progress();
        else 
            moveList.Regress();
        UpdateMoveDescription();
    }

    private void UpdateMoveDescription()
    {
        moveDescription.text = MoveNotation.ConvertToMoveDescription(moveList.GetCurrentDisplayedMove());
    }

    public void ListToStart() => moveList.ToStart();

    public void ListToEnd() => moveList.ToEnd();

    private void ClearMoveListChildren() => moveList.ClearDisplay();
    
    #endregion

    public void SwitchTheme(TMPro.TMP_Dropdown dropdown)
    {
        currentThemeIndex = dropdown.value;

        Theme theme = themes[currentThemeIndex];

        mainCamera.backgroundColor = theme.primary;

        foreach (var mainButton in mainButtons)
        {
            ColorBlock colorBlock = mainButton.colors;
            colorBlock.normalColor = theme.secondary;
            colorBlock.highlightedColor = theme.highlightPrimary;
            colorBlock.pressedColor = theme.selected;
            mainButton.colors = colorBlock;

            mainButton.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().color = theme.opposite;
        }

        foreach (var windowButton in windowButtons)
        {
            ColorBlock colorBlock = windowButton.colors;
            colorBlock.normalColor = theme.tertiary;
            colorBlock.highlightedColor = theme.highlightSecondary;
            colorBlock.pressedColor = theme.selected;
            windowButton.colors = colorBlock;

            windowButton.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().color = theme.opposite;
        }

        foreach (var closeButton in closeButtons)
        {
            ColorBlock colorBlock = closeButton.colors;
            colorBlock.normalColor = theme.tertiary;
            closeButton.colors = colorBlock;
        }    

        foreach (var windowBackground in windowBackgrounds)
        {
            windowBackground.color = theme.secondary;
        }

        foreach (var text in texts)
        {
            text.color = theme.opposite;
        }

        foreach (var title in titles)
        {
            title.color = theme.oppositeSecondary;
        }

        foreach (var slider in sliders)
        {
            slider.transform.GetChild(1).GetComponent<Image>().color = theme.tertiary;

            ColorBlock colorBlock = slider.colors;
            colorBlock.normalColor = theme.opposite;
            colorBlock.highlightedColor = theme.oppositeSecondary;
            colorBlock.pressedColor = theme.selected;
            slider.colors = colorBlock;

            slider.transform.GetChild(2).GetChild(0).GetComponent<Image>().color = theme.opposite;

            slider.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().color = theme.opposite;
        }

        foreach (var box in popUpBoxes)
        {
            box.color = theme.secondary;
        }

        warningIcon.color = theme.oppositeSecondary;

        moveList.UpdateColours();
    }

    public void TogglePieceHighlighter(Toggle toggle)
    {
        highlightPiece = toggle.isOn;

        if (highlightPiece)
            Cube.Instance.SetHighlightedPiece();
        else
            Cube.Instance.RemoveHighlightedPiece();
    }

    public void ToggleSolvingStages(Toggle toggle)
    {
        useStages = toggle.isOn;
        if (toggle.isOn)
            Cube.Instance.SetSequenceFromIndex();
        else
            Cube.Instance.SetIndexFromSequence();
    }

    public void ToggleDefinitions(Toggle toggle)
    {
        moveDescription.gameObject.SetActive(toggle.isOn);
    }

    public void ToggleTutorials(Toggle toggle)
    {
        SetTutorialButtonActive(tutorialsToggle.isOn);
    }

    public void SetTutorialButtonActive(bool isActive)
    {
        if (isActive && tutorialsToggle.isOn)
        {
            showSequenceButton.SetActive(true);
            showStageButton.SetActive(true);
            useTutorials = true;
        }
        else if (!isActive && Cube.Instance.IsSolving())
        {
            showSequenceButton.SetActive(false);
            showStageButton.SetActive(false);
            StageBox.Instance.Hide(false);
            SequenceBox.Instance.Hide(false);
            useTutorials = false;
        }
    }

    public void SetInvalidNotificationActive(bool isActive)
    {
        invalidNotification.gameObject.SetActive(isActive);
    }
}
