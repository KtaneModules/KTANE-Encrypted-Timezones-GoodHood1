using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class EncryptedTimezones : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMColorblindMode Colorblind;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    public TextMesh[] cbTexts;
    public TextMesh middleText;
    public KMSelectable[] lightButtons;
    public KMSelectable middleButton;
    public Renderer[] lightRends;
    public KMHighlightable[] lightHighlights;

    int[] allLights = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

    public Material[] colors;
    string[] binaryColors = {"100", "010", "001", "011", "101", "110", "111", "000" };
    string[] shortColorNames = {"R", "G", "B", "C", "M", "Y", "W", "K" };
    string[] colorNames = {"Red", "Green", "Blue", "Cyan", "Magenta", "Yellow", "White", "Black" };

    List<string> encryptedLights = new List<string>(); //These 3 are lists of binary colors
    List<string> decryptedLights = new List<string>();
    List<string> submitLights = new List<string> { "000", "000", "000", "000", "000", "000", "000", "000", "000", "000", "000", "000" };

    List<string> currentLights = new List<string> { "000", "000", "000", "000", "000", "000", "000", "000", "000", "000", "000", "000" };

    int[] currentTimeIndexes = new int[2]; //Hours, minutes
    int[] goalTimeIndexes = new int[2]; //Hours, minutes
    string[] timeColorComponents = new string[3]; //Base, Hour color, Minute Color
    string currentCity;
    string goalCity;
    Dictionary<string, int> cities = new Dictionary<string, int>
    {
        {"Bucharest", 3}, {"Cairns", 10}, {"Cape Town", 2},
        {"Casablanca", 1}, {"Detroit", -4}, {"Hanoi", 7},
        {"Islamabad", 5}, {"Jakarta", 7}, {"Lima", -5},
        {"Los Angeles", -7}, {"Madrid", 2}, {"Managua", -6},
        {"Mexico City", -5}, {"Monrovia", 0}, {"Nairobi", 3},
        {"Nuuk", -2}, {"Omsk", 6}, {"Oslo", 2},
        {"Perth", 8}, {"Philadelphia", -4}, {"Pyongyang", 9},
        {"Reykjavik", 0}, {"Salt Lake\nCity", -6}, {"Shanghai", 8},
        {"Sydney", 10}, {"Taipei", 8}, {"Tokyo", 9},
        {"Toronto", -4}, {"Winnipeg", -5}
    };

    bool inputMode;
    bool isInteractable = true;

    List<string> logQueue = new List<string>();

    void Awake()
    {
        for (int i = 0; i < 12; i++)
        {
            cbTexts[i].gameObject.SetActive(Colorblind.ColorblindModeActive);
        }
        ModuleId = ModuleIdCounter++;
        foreach (KMSelectable button in lightButtons) {
            button.OnInteract += delegate () { LightPress(button); return false; };
            button.transform.GetChild(0).gameObject.transform.localScale = Vector3.zero;
        }

        middleButton.OnInteract += delegate () { MiddlePress(); return false; };

            
    }

    void Start()
    {
        inputMode = false;
        GetTimeAndCity();
        Debug.LogFormat("[Encrypted Timezones #{0}] ----BEGINNING STARTING CITY DECRYPTION----", ModuleId);
        GenerateDecryptedLights();
        EncryptStartingLights();

        LogColors(encryptedLights, "The current time colors before decryption are");
        foreach (string logElement in logQueue)
        {
            Debug.Log(logElement);
        }

        LogColors(decryptedLights, "The starting city decrypted colors are");
        Debug.LogFormat("[Encrypted Timezones #{0}] After decrypting, the base color is {1}. The hour hand is at {2} o'clock and is {3}. The minute hand is at {4} o'clock and is {5}", ModuleId,
            colorNames[Array.IndexOf(binaryColors, timeColorComponents[0])], currentTimeIndexes[0], colorNames[Array.IndexOf(binaryColors, timeColorComponents[1])], currentTimeIndexes[1], colorNames[Array.IndexOf(binaryColors, timeColorComponents[2])]);

        NotateGoalTime();
        Debug.LogFormat("[Encrypted Timezones #{0}] ----BEGINNING GOAL CITY ENCRYPTION----", ModuleId);
        LogColors(submitLights, "The goal time colors before encryption are");
        EncryptGoalLights();

        ShowLights(encryptedLights);
        StartCoroutine("CycleCity");

        Debug.LogFormat("[Encrypted Timezones #{0}] ----INPUT----", ModuleId);
        LogColors(submitLights, "The final colors you should submit are");



    }

    void NotateGoalTime()
    {
        AddColorsToArray(submitLights, allLights, timeColorComponents[0]);
        AddColorsToArray(submitLights, new int[] { goalTimeIndexes[0] - 1 }, timeColorComponents[1]);
        AddColorsToArray(submitLights, new int[] { goalTimeIndexes[1] - 1 }, timeColorComponents[2]);
    }

    void LightPress(KMSelectable button)
    {
        if (ModuleSolved || isInteractable == false)
            return;
        string colorToAdd = "000";

        int timeRemainingSeconds = Mathf.FloorToInt(Bomb.GetTime());
        switch ((Mathf.FloorToInt((timeRemainingSeconds % 60) / 10)).ToString())
        {
            case "0":
            case "3":
                colorToAdd = "100";
                break;
            case "1":
            case "4":
                colorToAdd = "010";
                break;
            case "2":
            case "5":
                colorToAdd = "001";
                break;
            default:
                break;
        }
        AddColorsToArray(currentLights, new int[] { Array.IndexOf(lightButtons, button) }, colorToAdd);
        ShowLights(currentLights);
    }

    void MiddlePress()
    {
        if (!isInteractable || ModuleSolved)
            return;
        if (!inputMode)
        {
            StartCoroutine(SwitchToInput());
            return;
        }
        if (currentLights.SequenceEqual(submitLights))
        {
            ModuleSolved = true;
            StartCoroutine(Solve());
            StartCoroutine(WaveAnimation());
        }
        else
        {
            Module.HandleStrike();
            StartCoroutine("Reset");
        }


    }

    IEnumerator Solve()
    {
        float timeDelay = 0.1f;
        Audio.PlaySoundAtTransform("ticking", Module.transform);
        for (int component = 0; component < 3; component++)
        {
            for (int i = 0; i < 12; i++)
            {
                Module.transform.Find("Tick Marks").GetChild(i).GetComponentInChildren<Renderer>().material.color = Color.white;
                if (currentLights[i][component] == '1')
                {
                    AddColorsToArray(currentLights, new int[] { i }, binaryColors[component]);
                }
                ShowLights(currentLights);
                yield return new WaitForSeconds(timeDelay);
                Module.transform.Find("Tick Marks").GetChild(i).GetComponentInChildren<Renderer>().material.color = Color.black;

            }
        }
        foreach (var rend in lightRends)
            rend.material = colors[1];
        Audio.PlaySoundAtTransform("bell", Module.transform);
        Module.HandlePass();
        for (int i = 0; i < 12; i++)
            cbTexts[i].gameObject.SetActive(false);

    }
    IEnumerator SwitchToInput()
    {
        isInteractable = false;
        inputMode = true;
        for (int i = 0; i < lightRends.Length; i++)
        {
            lightRends[i].material = colors[7];
            cbTexts[i].text = "K";
            Audio.PlaySoundAtTransform("dong", lightButtons[i].transform);
            yield return new WaitForSeconds(0.25f);
        }
        StopCoroutine("CycleCity");
        Audio.PlaySoundAtTransform("ding", middleButton.transform);

        if (goalCity.Contains("\n"))
            middleText.fontSize = 85;
        else if (goalCity.Length < 7)
            middleText.fontSize = 115;
        else if (goalCity.Length < 11)
            middleText.fontSize = 90;
        else middleText.fontSize = 75;


        middleText.text = goalCity;
        foreach (KMSelectable button in lightButtons)
            button.transform.GetChild(0).gameObject.transform.localScale = new Vector3(1.1f, 0.001f, 1.1f);
        isInteractable = true;
    }

    IEnumerator Reset()
    {
        isInteractable = false;
        inputMode = false;
        currentLights = new List<string> { "000", "000", "000", "000", "000", "000", "000", "000", "000", "000", "000", "000" };
        for (int i = 0; i < lightRends.Length; i++)
        {
            lightRends[i].material = colors[Array.IndexOf(binaryColors, encryptedLights[i])];
            cbTexts[i].text = shortColorNames[Array.IndexOf(binaryColors, encryptedLights[i])];
            Audio.PlaySoundAtTransform("ding", lightButtons[i].transform);
            yield return new WaitForSeconds(0.1f);
        }
        StartCoroutine("CycleCity");
        middleText.fontSize = 300;
        Audio.PlaySoundAtTransform("dong", middleButton.transform);

        foreach (KMSelectable button in lightButtons)
            button.transform.GetChild(0).gameObject.transform.localScale = new Vector3(0.000001f, 0.00000001f, 0.0000001f);
        isInteractable = true;
    }

    void EncryptStartingLights()
    {
        string[] positionChars = { "LTY48", "JOP05", "ABC12", "DMZ37", "EGN69" };
        bool[] positionsPresent = new bool[] { false, false, false, false, false };
        for (int i = 0; i < 5; i++)
            if (Bomb.GetSerialNumber().Any(x => positionChars[i].Contains(x)))
                positionsPresent[i] = true;

        int[,] posDatabase = new int[,]
        {
            {2, 4, 3, 7, 9, 10, 11, 12, 1, 6},
            {8, 10, 3, 12, 6, 7, 1, 9, 4, 11},
            {5, 8, 1, 11, 4, 6, 2, 10, 3, 12},
            {3, 12, 4, 5, 2, 7, 1, 6, 8, 9},
            {8, 10, 1, 2, 4, 7, 3, 12, 5, 11},
            {3, 9, 4, 7, 5, 12, 1, 10, 2, 11},
            {3, 10, 2, 4, 1, 8, 11, 12, 7, 9},
            {5, 11, 9, 10, 2, 12, 1, 8, 3, 4},
            {7, 11, 5, 9, 4, 6, 2, 12, 3, 8},
            {3, 6, 2, 11, 5, 7, 8, 9, 10, 12},
            {7, 12, 1, 3, 4, 5, 9, 11, 8, 10},
            {3, 8, 4, 5, 2, 10, 7, 9, 1, 12}
        };
        bool[] conditions = new bool[]
{
            (Bomb.GetSerialNumber().Any(ch => "AEIOU".Contains(ch))),
            !(Bomb.GetSerialNumber().Any(ch => "AEIOU".Contains(ch))),
            Bomb.GetBatteryHolderCount() >= 3,
            Bomb.GetBatteryCount(Battery.D) >= 1,
            Bomb.GetOnIndicators().Count() > Bomb.GetOffIndicators().Count(),
            Bomb.GetOnIndicators().Count() < Bomb.GetOffIndicators().Count(),
            Bomb.GetOnIndicators().Count() == Bomb.GetOffIndicators().Count(),
            Helper.IsPrime(Bomb.GetSolvableModuleNames().Count()),
            Bomb.GetPortCount(Port.Parallel) >= 1 || Bomb.GetPortCount(Port.Serial) >= 1,
            Bomb.GetPortCount(Port.DVI) >= 1 || Bomb.GetPortCount(Port.RJ45) >= 1,
            Bomb.GetModuleNames().Count() - Bomb.GetSolvableModuleNames().Count() == 0,
            Bomb.GetModuleNames().Any(l => l.ToLower() == "timezones")

        };
        string[] colorToggleDatabase = { "R", "C", "M", "B", "W", "B", "Y", "G", "R", "G", "M", "W" };

        List<string>[] colorsToAdd = new List<string>[12];
        for (int i = 0; i < colorsToAdd.Length; i++)
            colorsToAdd[i] = new List<string>();

        for (int i = 0; i < conditions.Length; i++)
        {
            if (conditions[i])
            {
                string curDigitsString = "";
                for (int position = 0; position < 5; position++)
                {
                    if (positionsPresent[position])
                    {
                        int[] currentDigits = new int[] { posDatabase[i, position * 2], posDatabase[i, position * 2 + 1] };
                        curDigitsString += currentDigits[0].ToString() + "/" + currentDigits[1].ToString() + ", ";
                        colorsToAdd[currentDigits[0] - 1].Add(binaryColors[Array.IndexOf(shortColorNames, colorToggleDatabase[i])]);
                        colorsToAdd[currentDigits[1] - 1].Add(binaryColors[Array.IndexOf(shortColorNames, colorToggleDatabase[i])]);
                    }
                }
                curDigitsString = curDigitsString.Substring(0, curDigitsString.Length - 2);
                logQueue.Add(string.Format("[Encrypted Timezones #{0}] Rule {1} applies, toggling {2} at positions {3}", ModuleId, i + 1, colorNames[Array.IndexOf(shortColorNames, colorToggleDatabase[i])], curDigitsString));

            }
        }

        List<string> colorsToAddMerged = Helper.MergeColors(colorsToAdd);

        encryptedLights = new List<string>(decryptedLights);
        for (int color = 0; color < colorsToAddMerged.Count; color++)
        {
            if (colorsToAddMerged[color] != "000")
            {
                encryptedLights = AddColorsToArray(encryptedLights, new int[] { color }, colorsToAddMerged[color]);
            }
        }


    }

    void EncryptGoalLights()
    {
        int[] primeNumbered = new int[] { 1, 2, 4, 6, 10 };
        if (Bomb.GetOnIndicators().Contains("SND") || Bomb.GetOffIndicators().Contains("CAR"))
        {
            for (int i = 0; i < 12; i++)
                if (i % 2 == 1)
                    submitLights[i] = "101";
            LogRule(1, "all even numbered positions turn magenta.");
        }
        if (goalTimeIndexes[1] % 3 == 0)
        {
            AddColorsToArray(submitLights, primeNumbered, "010");
            LogRule(2, "toggling green in all prime numbered positions.");
        }
        if (!goalCity.ToUpper().Contains("E"))
        {
            AddColorsToArray(submitLights, allLights, "010");
            LogRule(3, "toggling green in all positions.");
        }
        if (goalCity.ToUpper().Contains("I"))
        {
            AddColorsToArray(submitLights, allLights, "001");
            LogRule(4, "toggling blue in all positions.");
        }
        if (Math.Abs(cities[goalCity]) % 2 == 0)
        {
            AddColorsToArray(submitLights, allLights.Where(x => x % 2 == 0).ToArray(), "100");
            LogRule(5, "toggling red in all odd numbered positions.");
        }
        submitLights = Helper.ShiftListClockwise(submitLights, Bomb.GetSerialNumberNumbers().Last());
        Debug.LogFormat("[Encrypted Timezones #{0}] Rule {1} applied, shifting all lights clockwise {2} times.", ModuleId, 6, Bomb.GetSerialNumberNumbers().Last());
        if (Bomb.GetSerialNumberLetters().Where(x => "AEIOU".Contains(x)).Count() != 1)
        {
            AddColorsToArray(submitLights, allLights.Where(x => (x + 1) % 4 == 0).ToArray(), "110");
            LogRule(7, "toggling red and green in all positions which are multiples of 4.");
        }
        if (Bomb.GetPortCount(Port.StereoRCA) >= 1)
        {
            AddColorsToArray(submitLights, new int[] { 2, 3, 4, 5, 6, 7 }, "011");
            LogRule(8, "toggling green and blue on positions 3 through 8.");
        }
        else
        {
            AddColorsToArray(submitLights, new int[] { 8, 9, 10, 11 }, "001");
            LogRule(9, "toggling blue on positions 9 through 12.");
        }
        if (cities[goalCity] > -3)
        {
            AddColorsToArray(submitLights, new int[] { 0, 1, 7 }, "100");
            LogRule(10, "toggling red on positions 1, 2 and 8.");
        }
        if (Bomb.GetModuleIDs().Any(l => l.ToLower() == "stopwatch" || l.ToLower() == "theclockmodule"))
        {
            AddColorsToArray(submitLights, new int[] { 5, 11 }, "111");
            LogRule(11, "toggling red, blue and green on positions 5 and 11.");
        }

    }

    void LogRule(int rule, string text)
    {
        Debug.LogFormat("[Encrypted Timezones #{0}] Rule {1} applied, {2}", ModuleId, rule, text);
    }

    void LogColors(List<string> colorList, string message)
    {
        string logLine = "";
        foreach (var color in colorList)
        {
            logLine += colorNames[Array.IndexOf(binaryColors, color)] + ", ";
        }
        logLine = logLine.Substring(0, logLine.Length - 2);
        Debug.LogFormat("[Encrypted Timezones #{0}] {1} {2}.", ModuleId, message, logLine);
    }

    void GenerateDecryptedLights()
    {
        int randomBase = Rnd.Range(0, 8);
        int[,] handTable =
        {
            {1, 2},
            {2, 0},
            {1, 0},
            {0, 2},
            {0, 1},
            {2, 1},
            {2, 0},
            {0, 1}
        };

        int hourColor = handTable[randomBase, Bomb.GetIndicators().Count() <= 2 ? 0 : 1];

        int minuteColor = Rnd.Range(0, 3);
        while (minuteColor == hourColor)
            minuteColor = Rnd.Range(0, 3);

        timeColorComponents = new string[] { binaryColors[randomBase], binaryColors[hourColor], binaryColors[minuteColor] };

        decryptedLights = AddColorsToArray(decryptedLights, allLights, binaryColors[randomBase]);
        decryptedLights = AddColorsToArray(decryptedLights, new int[] { currentTimeIndexes[0]-1 }, binaryColors[hourColor]);
        decryptedLights = AddColorsToArray(decryptedLights, new int[] { currentTimeIndexes[1]-1 }, binaryColors[minuteColor]);

    }

    List<string> AddColorsToArray(List<string> list, int[] indexes, string ColorToAdd)
    {
        if (list.Count() == 0)
        {
            list = new List<string> {"000", "000", "000", "000", "000", "000", "000", "000", "000", "000", "000", "000"};
        }
        for (int light = 0; light < 12; light++)
        {
            if (!indexes.Contains(light))
                continue;
            string newColor = "";
            for (int component = 0; component < 3; component++)
            {
                newColor += ((Int32.Parse(list[light][component].ToString()) + Int32.Parse(ColorToAdd[component].ToString())) % 2).ToString();
            }
            list[light] = newColor;
        }
        return list;
    }

    void GetTimeAndCity()
    {
        currentTimeIndexes[0] = Rnd.Range(1, 13);
        currentTimeIndexes[1] = Rnd.Range(1, 12);

        currentCity = cities.ElementAt(Rnd.Range(0, cities.Count)).Key;
        goalCity = cities.ElementAt(Rnd.Range(0, cities.Count)).Key;

        Debug.LogFormat("[Encrypted Timezones #{0}] The current city is {1} (UTC {2}{3}) and the goal city is {4} (UTC {5}{6}).", ModuleId, 
            currentCity, cities[currentCity] >= 0 ? "+" : "", cities[currentCity], goalCity, cities[goalCity] >= 0 ? "+" : "", cities[goalCity]);

        goalTimeIndexes[0] = currentTimeIndexes[0] - cities[currentCity] + cities[goalCity];
        goalTimeIndexes[0] = (goalTimeIndexes[0] + 12) % 12;
        goalTimeIndexes[1] = currentTimeIndexes[1];

        string currentMinutes = currentTimeIndexes[1] * 5.ToString().Length < 2 ? "0" + currentTimeIndexes[1] * 5 : (currentTimeIndexes[1] * 5).ToString();
        string goalMinutes = goalTimeIndexes[1] * 5.ToString().Length < 2 ? "0" + goalTimeIndexes[1] * 5 : (goalTimeIndexes[1] * 5).ToString();
        Debug.LogFormat("[Encrypted Timezones #{0}] The current time is {1}:{2} and converting from {3} to {4} gets the goal time of {5}:{6}", ModuleId,
            currentTimeIndexes[0], currentMinutes, currentCity, goalCity, goalTimeIndexes[0], goalMinutes);


    }

    IEnumerator CycleCity()
    {
        string ClearedCurrentCity = currentCity;
        ClearedCurrentCity = ClearedCurrentCity.Replace(" ", "").ToUpper();
        while (true)
        {
            for (int i = 0; i < ClearedCurrentCity.Length; i++)
            {
                middleText.text = ClearedCurrentCity[i].ToString();
                yield return new WaitForSeconds(0.8f);
            }
        }
    }

    void ShowLights(List<string> binaryInput)
    {
        for (int i = 0; i < 12; i++)
        {
            lightRends[i].material = colors[Array.IndexOf(binaryColors,binaryInput[i])];
            if (Colorblind.ColorblindModeActive)
                cbTexts[i].text = shortColorNames[Array.IndexOf(binaryColors, binaryInput[i])];

        }
    }

    IEnumerator WaveAnimation()
    {
        Animator[] animators = lightButtons.Select(b => b.GetComponent<Animator>()).ToArray();
        float t = 0;
        int step = -1;
        for (int i = 0; i < 3; i++)
        {
            while (t <= 12)
            {
                if (Mathf.FloorToInt(t) != step)
                {
                    step = Mathf.FloorToInt(t);
                    animators[step].SetTrigger("DoWave");
                }
                t = (t + Time.deltaTime * 10);
                yield return null;
            }
            t %= 12;
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use '!{0} middle' to press the center button | "
                                                + "'!{0} <digit> press <buttons>' to press that button in clock directions from 1-12 at that tens timer digit; "
                                                + "chain button presses using spaces, "
                                                + "eg. '!{0} 4 press 1 2 3 10 11 12' | "
                                                + "'!{0} <colourblind/cb>' to toggle colourblind mode.";
#pragma warning restore 414

    private string[] _cbCommands = new string[] { "CB", "COLOURBLIND", "COLORBLIND" };
    private string[] _buttonNumbers = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
    private string[] _timerDigits = new string[] { "0", "1", "2", "3", "4", "5" };

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToUpper();

        if (_cbCommands.Contains(command))
        {
            yield return null;
            for (int i = 0; i < 12; i++)
                cbTexts[i].gameObject.SetActive(!cbTexts[i].gameObject.activeSelf);
            yield break;
        }

        if (command == "MIDDLE")
        {
            yield return null;
            middleButton.OnInteract();
            yield break;
        }

        string[] commands = command.Split(' ');

        if (!inputMode)
        {
            yield return "sendtochaterror You are not able to select the lights now!";
        }
        if (commands.Length == 0)
        {
            yield return "sendtochaterror That is an empty command!";
        }
        if (commands.Length == 1)
        {
            yield return "sendtochaterror That is an invalid command!";
        }

        if (commands[1] == "PRESS")
        {
            if (!_timerDigits.Contains(commands[0]))
            {
                yield return "sendtochaterror " + commands[0] + " is not a valid timer digit.";
            }
            if (commands.Length < 3) 
            {
                yield return "sendtochaterror No buttons were specified.";
            }
            for (int i = 2; i < commands.Length; i++)
            {
                if (!_buttonNumbers.Contains(commands[i]))
                {
                    yield return "sendtochaterror " + commands[i] + " is not a valid button.";
                }
            }
            yield return null;

            while ((Mathf.FloorToInt(((Mathf.FloorToInt(Bomb.GetTime())) % 60) / 10)).ToString() != commands[0])
                yield return null;
            for (int i = 2; i < commands.Length; i++)
            {
                lightButtons[Int32.Parse(commands[i]) - 1].OnInteract();
            }
        }
        else
        {
            yield return "sendtochaterror Invalid command!"; // Probably Mar typed this command
        }
    }

    private string[] componentNums = new string[] { "0", "1", "2", "3", "4", "5"};

    IEnumerator TwitchHandleForcedSolve()
    {
        if (!inputMode)
            middleButton.OnInteract();

        while (!isInteractable)
            yield return null;

        string currentDigit = ((Mathf.FloorToInt(((Mathf.FloorToInt(Bomb.GetTime())) % 60) / 10)).ToString());
        int componentIndex = Array.IndexOf(componentNums, currentDigit);
        for (int comp = 0; comp < 3; comp++)
        {
            while (!componentNums[componentIndex].Contains((Mathf.FloorToInt(((Mathf.FloorToInt(Bomb.GetTime())) % 60) / 10)).ToString()))
                yield return null;
            for (int light = 0; light < 12; light++)
            {
                if (currentLights[light][componentIndex % 3] != submitLights[light][componentIndex % 3])
                {
                    lightButtons[light].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }
            componentIndex = (componentIndex - 1 + 6) % 6;
        }

        middleButton.OnInteract();
    }
}
