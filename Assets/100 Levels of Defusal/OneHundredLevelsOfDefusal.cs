﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;

public class OneHundredLevelsOfDefusal : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable[] Letters;
    public TextMesh[] LetterTexts;
    public Renderer[] LetterButtons;

    public TextMesh LevelText;
    public Renderer[] ProgressBar;
    public Material[] ProgressBarColors;

    public Color[] TextColors;

    public KMSelectable SubmitBtn;
    public TextMesh SubmitText;
    public Renderer SubmitBtnModel;

    public KMSelectable ToggleBtn;
    public TextMesh ToggleText;
    public Renderer ToggleBtnModel;

    // Solving info
    private int level = 0;
    private double progress = 0;
    private int progressBars = 0;
    private bool canFlashNext = true;

    private double solves = 0;
    private double solvesNeeded = 0;
    private int moduleCount = 0;

    private int letters = 0;
    private bool levelFound = true;
    private bool solvesReached = false;

    private readonly string[] LETTERS = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
    private readonly int[] FIRSTVALUE = { 0, 1, 1, 1, 0, 1, 1, 1, 0, 2, 2, 2, 2, 2, 0, 3, 3, 3, 3, 4, 0, 4, 4, 5, 5, 6 };
    private readonly int[] SECNDVALUE = { 1, 1, 2, 3, 3, 4, 5, 6, 4, 2, 3, 4, 5, 6, 5, 3, 4, 5, 6, 4, 6, 5, 6, 5, 6, 6 };

    private int[] letterIndexes = new int[33];
    private string[] letterDisplays = new string[33];

    private int[] lettersUsed = new int[12];
    private int letterSlotsUsed = 6;

    private string screenDisplay;
    private string correctMessage;
    private bool lockButtons = true;

    private bool direction = true;

    private int moduleStrikes = 0;

    // Testing variables
    private readonly int FIXLETTERS = 6; // 6
    private readonly int FIXLEVEL = 15; // 15

    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved = false;

    // Ran as bomb loads
    private void Awake() {
        moduleId = moduleIdCounter++;

        for (int i = 0; i < Letters.Length; i++) {
            int j = i;
            Letters[i].OnInteract += delegate () { LetterPressed(j); return false; };
        }

        SubmitBtn.OnInteract += delegate () { SubmitButtonPressed(); return false; };
        ToggleBtn.OnInteract += delegate () { ToggleButtonPressed(); return false; };

        Module.OnActivate += OnActivate;
    }

    // Starts the module
    private void Start() {
        DisableAll();
        DetermineLevel();
    }

    // Bomb lights turn on
    private void OnActivate() {
        if (levelFound == false)
            StartCoroutine(PendingText());

        else
            StartCoroutine(LevelFound());
    }

    // Disables all letters and selectables
    private void DisableAll() {
        for (int i = 0; i < Letters.Length; i++) {
            letterIndexes[i] = 0;
            letterDisplays[i] = "A";
            Letters[i].gameObject.SetActive(false);
            LetterTexts[i].text = "";
            LetterButtons[i].enabled = false;
        }

        SubmitBtn.gameObject.SetActive(false);
        SubmitText.text = "";
        SubmitBtnModel.enabled = false;
        ToggleBtn.gameObject.SetActive(false);
        ToggleText.text = "";
        ToggleBtnModel.enabled = false;
    }


    // Tracking solve count
    private void Update() {
        if (levelFound == true)
            solves = Bomb.GetSolvedModuleNames().Count();

        progress = solves / solvesNeeded * 100;

        // Shows progress
        if (progressBars < 10 && progressBars < (int)Math.Floor(progress / 10) && canFlashNext == true) {
            StartCoroutine(ShowProgress());
        }

        // Progress bar fills
        if (progressBars >= 10 && solvesReached == false) {
            solvesReached = true;

            if (levelFound == false) {
                Debug.LogFormat("[100 Levels of Defusal #{0}] Module solved!", moduleId);
                moduleSolved = true;
                GetComponent<KMBombModule>().HandlePass();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
            }

            else {
                Audio.PlaySoundAtTransform("100Levels_ProgressFilled", transform);
                Debug.LogFormat("[100 Levels of Defusal #{0}] Progress bar filled! Generating cipher...", moduleId);
                StartCoroutine(DelayGeneration());
            }
        }
    }

    // Displays progress bars
    private IEnumerator ShowProgress() {
        canFlashNext = false;
        progressBars++;

        for (int i = 1; i <= progressBars && i <= 10; i++) {
            ProgressBar[i - 1].material = ProgressBarColors[1];
        }

        yield return new WaitForSeconds(0.25f);
        canFlashNext = true;
    }

    // Delays cipher generation
    private IEnumerator DelayGeneration() {
        yield return new WaitForSeconds(0.8f);
        GenerateCipher();
    }


    // Letter is pressed
    private void LetterPressed(int index) {
        Letters[index].AddInteractionPunch(0.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Letters[index].transform);

        if (lockButtons == false)
            Increment(index);
    }

    // Increments the letter
    private void Increment(int index) {
        if (direction == true) {
            letterIndexes[index]++;

            if (letterIndexes[index] == 26)
                letterIndexes[index] = 0;
        }

        else {
            letterIndexes[index]--;

            if (letterIndexes[index] == -1)
                letterIndexes[index] = 25;
        }

        letterDisplays[index] = LETTERS[letterIndexes[index]];
        LetterTexts[index].text = letterDisplays[index];
    }

    // Submit button pressed
    private void SubmitButtonPressed() {
        SubmitBtn.AddInteractionPunch(0.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitBtn.transform);

        if (lockButtons == false) {
            string str = "";
            for (int i = 0; i < letterSlotsUsed; i++)
                str += LETTERS[letterIndexes[lettersUsed[i]]];

            Debug.LogFormat("[100 Levels of Defusal #{0}] You submitted: {1}", moduleId, str);

            // Turns the buttons off
            lockButtons = true;
            SubmitBtn.gameObject.SetActive(false);
            SubmitText.text = "";
            SubmitBtnModel.enabled = false;
            ToggleBtn.gameObject.SetActive(false);
            ToggleText.text = "";
            ToggleBtnModel.enabled = false;


            // Correct answer
            if (str == correctMessage) {
                StartCoroutine(CorrectAnswer());
                StartCoroutine(ShowSolveText());

                if (solvesReached == false)
                    solves++;

                else {
                    Debug.LogFormat("[100 Levels of Defusal #{0}] Module solved!", moduleId);
                    moduleSolved = true;
                    GetComponent<KMBombModule>().HandlePass();
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
                }
            }

            else {
                Debug.LogFormat("[100 Levels of Defusal #{0}] That was wrong...", moduleId);
                GetComponent<KMBombModule>().HandleStrike();
                moduleStrikes++;
                StartCoroutine(IncorrectAnswer());
            }
        }
    }

    // Toggle button pressed
    private void ToggleButtonPressed() {
        ToggleBtn.AddInteractionPunch(0.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ToggleBtn.transform);

        if (lockButtons == false) {
            if (direction == true)
                direction = false;

            else
                direction = true;
        }
    }


    // Pending text
    private IEnumerator PendingText() {
        LevelText.text = "PENDING.";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING..";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING...";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING.";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING..";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING...";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING.";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING..";
        yield return new WaitForSeconds(0.4f);
        LevelText.text = "PENDING...";
        float randomWait = UnityEngine.Random.Range(0.2f, 0.6f);
        yield return new WaitForSeconds(randomWait);
        Audio.PlaySoundAtTransform("100Levels_LevelNotFound", transform);
        yield return new WaitForSeconds(0.04f);
        LevelText.text = "ERROR";
        yield return new WaitForSeconds(0.25f);
        LevelText.text = "";
        yield return new WaitForSeconds(0.15f);
        LevelText.text = "ERROR";
        yield return new WaitForSeconds(0.25f);
        LevelText.text = "";
        yield return new WaitForSeconds(0.15f);
        LevelText.text = "ERROR";
        yield return new WaitForSeconds(0.25f);
        Debug.LogFormat("[100 Levels of Defusal #{0}] No level found. Generating cipher...", moduleId);
        GenerateCipher();
        yield return new WaitForSeconds(0.55f);
        LevelText.text = "";
        yield return new WaitForSeconds(0.15f);
        LevelText.text = "LEVEL #??";
    }

    // Level found
    private IEnumerator LevelFound() {
        LevelText.text = "";
        string txtDisplayed;

        if (level < 10)
            txtDisplayed = "LEVEL #0" + level;

        else
            txtDisplayed = "LEVEL #" + level;

        yield return new WaitForSeconds(1.0f);
        LevelText.text = txtDisplayed;
        PlayIntroSound();
        yield return new WaitForSeconds(0.25f);
        LevelText.text = "";
        yield return new WaitForSeconds(0.15f);
        LevelText.text = txtDisplayed;
        yield return new WaitForSeconds(0.25f);
        LevelText.text = "";
        yield return new WaitForSeconds(0.15f);
        LevelText.text = txtDisplayed;
    }

    // Plays the intro sound according to the level
    private void PlayIntroSound() {
        switch (letters) {
        case 4: Audio.PlaySoundAtTransform("100Levels_Stage2", transform); break;
        case 5: Audio.PlaySoundAtTransform("100Levels_Stage3", transform); break;
        case 6: Audio.PlaySoundAtTransform("100Levels_Stage4", transform); break;
        case 7: Audio.PlaySoundAtTransform("100Levels_Stage5", transform); break;
        case 8: Audio.PlaySoundAtTransform("100Levels_Stage6", transform); break;
        case 9: Audio.PlaySoundAtTransform("100Levels_Stage7", transform); break;
        case 10: Audio.PlaySoundAtTransform("100Levels_Stage8", transform); break;
        case 11: Audio.PlaySoundAtTransform("100Levels_Stage9", transform); break;
        case 12: Audio.PlaySoundAtTransform("100Levels_Stage10", transform); break;
        default: Audio.PlaySoundAtTransform("100Levels_Stage1", transform); break;
        }
    }


    // Runs a cipher
    private void GenerateCipher() {
        DisableAll();
        lockButtons = true;
        ColorText(3);

        if (Bomb.GetStrikes() > moduleStrikes)
            letterSlotsUsed = letters - Bomb.GetStrikes();

        else if (Bomb.GetStrikes() < moduleStrikes)
            letterSlotsUsed = letters - moduleStrikes;

        else
            letterSlotsUsed = letters - Bomb.GetStrikes();

        if (letterSlotsUsed < 2)
            letterSlotsUsed = 2;


        // Determines which slots are used
        switch (letterSlotsUsed) {
        case 3:
        lettersUsed[0] = 24; lettersUsed[1] = 25; lettersUsed[2] = 26; lettersUsed[3] = -1; lettersUsed[4] = -1; lettersUsed[5] = -1;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 4:
        lettersUsed[0] = 7; lettersUsed[1] = 8; lettersUsed[2] = 9; lettersUsed[3] = 10; lettersUsed[4] = -1; lettersUsed[5] = -1;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 5:
        lettersUsed[0] = 23; lettersUsed[1] = 24; lettersUsed[2] = 25; lettersUsed[3] = 26; lettersUsed[4] = 27; lettersUsed[5] = -1;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 6:
        lettersUsed[0] = 6; lettersUsed[1] = 7; lettersUsed[2] = 8; lettersUsed[3] = 9; lettersUsed[4] = 10; lettersUsed[5] = 11;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 7:
        lettersUsed[0] = 1; lettersUsed[1] = 2; lettersUsed[2] = 3; lettersUsed[3] = 4; lettersUsed[4] = 29; lettersUsed[5] = 30;
        lettersUsed[6] = 31; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 8:
        lettersUsed[0] = 1; lettersUsed[1] = 2; lettersUsed[2] = 3; lettersUsed[3] = 4; lettersUsed[4] = 13; lettersUsed[5] = 14;
        lettersUsed[6] = 15; lettersUsed[7] = 16; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 9:
        lettersUsed[0] = 18; lettersUsed[1] = 19; lettersUsed[2] = 20; lettersUsed[3] = 21; lettersUsed[4] = 22; lettersUsed[5] = 13;
        lettersUsed[6] = 14; lettersUsed[7] = 15; lettersUsed[8] = 16; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 10:
        lettersUsed[0] = 18; lettersUsed[1] = 19; lettersUsed[2] = 20; lettersUsed[3] = 21; lettersUsed[4] = 22; lettersUsed[5] = 28;
        lettersUsed[6] = 29; lettersUsed[7] = 30; lettersUsed[8] = 31; lettersUsed[9] = 32; lettersUsed[10] = -1; lettersUsed[11] = -1; break;

        case 11:
        lettersUsed[0] = 0; lettersUsed[1] = 1; lettersUsed[2] = 2; lettersUsed[3] = 3; lettersUsed[4] = 4; lettersUsed[5] = 5;
        lettersUsed[6] = 28; lettersUsed[7] = 29; lettersUsed[8] = 30; lettersUsed[9] = 31; lettersUsed[10] = 32; lettersUsed[11] = -1; break;

        case 12:
        lettersUsed[0] = 0; lettersUsed[1] = 1; lettersUsed[2] = 2; lettersUsed[3] = 3; lettersUsed[4] = 4; lettersUsed[5] = 5;
        lettersUsed[6] = 12; lettersUsed[7] = 13; lettersUsed[8] = 14; lettersUsed[9] = 15; lettersUsed[10] = 16; lettersUsed[11] = 17; break;

        default:
        lettersUsed[0] = 8; lettersUsed[1] = 9; lettersUsed[2] = -1; lettersUsed[3] = -1; lettersUsed[4] = -1; lettersUsed[5] = -1;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1; break;
        }


        // Generates the letters
        screenDisplay = "";
        int[] availableLetters = { 1, 2, 3, 5, 6, 7, 9, 10, 11, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25 };

        for (int i = 0; i < letterSlotsUsed; i++) {
            letterIndexes[lettersUsed[i]] = availableLetters[UnityEngine.Random.Range(0, availableLetters.Length)];
            letterDisplays[lettersUsed[i]] = LETTERS[letterIndexes[lettersUsed[i]]];
            screenDisplay += LETTERS[letterIndexes[lettersUsed[i]]];
        }

        Debug.LogFormat("[100 Levels of Defusal #{0}] The cipher is {1} letters long, and the display is: {2}", moduleId, letterSlotsUsed, screenDisplay);


        // Turns the letters into the number
        int firstPairSum = 0;
        int secndPairSum = 0;

        for (int i = 0; i < letterSlotsUsed - 1; i++) {
            for (int j = i + 1; j < letterSlotsUsed; j++) {
                firstPairSum += FIRSTVALUE[letterIndexes[lettersUsed[i]]] * FIRSTVALUE[letterIndexes[lettersUsed[j]]];
                secndPairSum += SECNDVALUE[letterIndexes[lettersUsed[i]]] * SECNDVALUE[letterIndexes[lettersUsed[j]]];
            }
        }

        Debug.LogFormat("[100 Levels of Defusal #{0}] Value A is {1}, and Value B is {2}.", moduleId, firstPairSum, secndPairSum);

        int calculatedValue = firstPairSum + secndPairSum;

        if (levelFound == false)
            calculatedValue *= FIXLEVEL;

        else
            calculatedValue *= level;


        // Modifies the number into the number to decrypt
        string convertedNumber = BaseConvert(calculatedValue);
        Debug.LogFormat("[100 Levels of Defusal #{0}] The calculated value is {1}, which converts to {2} in base 6.", moduleId, calculatedValue, convertedNumber);

        string usedNumber = DetermineUsedNumber(convertedNumber.ToCharArray());
        Debug.LogFormat("[100 Levels of Defusal #{0}] After further modifcations, the number used in calculations is {1}.", moduleId, usedNumber);


        // Converts the number back to letters
        correctMessage = "";
        char[] usedNumberArray = usedNumber.ToCharArray();

        for (int i = usedNumberArray.Length - 1; i > 0; i--) {
            switch (usedNumberArray[i].ToString() + usedNumberArray[i - 1].ToString()) {
            case "01": correctMessage = "A" + correctMessage; break;
            case "02": correctMessage = "E" + correctMessage; break;
            case "03": correctMessage = "E" + correctMessage; break;
            case "04": correctMessage = "I" + correctMessage; break;
            case "05": correctMessage = "O" + correctMessage; break;
            case "06": correctMessage = "U" + correctMessage; break;
            case "10": correctMessage = "A" + correctMessage; break;
            case "11": correctMessage = "B" + correctMessage; break;
            case "12": correctMessage = "C" + correctMessage; break;
            case "13": correctMessage = "D" + correctMessage; break;
            case "14": correctMessage = "F" + correctMessage; break;
            case "15": correctMessage = "G" + correctMessage; break;
            case "16": correctMessage = "H" + correctMessage; break;
            case "20": correctMessage = "E" + correctMessage; break;
            case "21": correctMessage = "C" + correctMessage; break;
            case "22": correctMessage = "J" + correctMessage; break;
            case "23": correctMessage = "K" + correctMessage; break;
            case "24": correctMessage = "L" + correctMessage; break;
            case "25": correctMessage = "M" + correctMessage; break;
            case "26": correctMessage = "N" + correctMessage; break;
            case "30": correctMessage = "E" + correctMessage; break;
            case "31": correctMessage = "D" + correctMessage; break;
            case "32": correctMessage = "K" + correctMessage; break;
            case "33": correctMessage = "P" + correctMessage; break;
            case "34": correctMessage = "Q" + correctMessage; break;
            case "35": correctMessage = "R" + correctMessage; break;
            case "36": correctMessage = "S" + correctMessage; break;
            case "40": correctMessage = "I" + correctMessage; break;
            case "41": correctMessage = "F" + correctMessage; break;
            case "42": correctMessage = "L" + correctMessage; break;
            case "43": correctMessage = "Q" + correctMessage; break;
            case "44": correctMessage = "T" + correctMessage; break;
            case "45": correctMessage = "V" + correctMessage; break;
            case "46": correctMessage = "W" + correctMessage; break;
            case "50": correctMessage = "O" + correctMessage; break;
            case "51": correctMessage = "G" + correctMessage; break;
            case "52": correctMessage = "M" + correctMessage; break;
            case "53": correctMessage = "R" + correctMessage; break;
            case "54": correctMessage = "V" + correctMessage; break;
            case "55": correctMessage = "X" + correctMessage; break;
            case "56": correctMessage = "Y" + correctMessage; break;
            case "60": correctMessage = "U" + correctMessage; break;
            case "61": correctMessage = "H" + correctMessage; break;
            case "62": correctMessage = "N" + correctMessage; break;
            case "63": correctMessage = "S" + correctMessage; break;
            case "64": correctMessage = "W" + correctMessage; break;
            case "65": correctMessage = "Y" + correctMessage; break;
            case "66": correctMessage = "Z" + correctMessage; break;
            default: correctMessage = "A" + correctMessage; break;
            }
        }

        Debug.LogFormat("[100 Levels of Defusal #{0}] The answer to submit is: {1}", moduleId, correctMessage);

        // Starts the screen display
        StartCoroutine(ShowLetters(true));
    }


    // Converts from base 10 to base 6
    private string BaseConvert(int no) {
        string str = "";
        int pos = 7;

        while (pos >= 0) {
            int counter = 0;

            while (no >= Math.Pow(6, pos)) {
                no -= (int)Math.Pow(6, pos);
                counter++;
            }

            str += counter;
            pos--;
        }

        // Removes leading zeros
        char[] noArray = str.ToCharArray();
        str = "";
        bool foundStart = false;

        for (int i = 0; i < noArray.Length; i++) {
            if (foundStart == false) {
                if (noArray[i] != '0')
                    foundStart = true;
            }

            if (foundStart == true)
                str += noArray[i];
        }

        return str;
    }

    // Calculates the number that will be used for the calculation
    private string DetermineUsedNumber(char[] noArray) {
        string str = "";
        int counter = 0;

        for (int i = noArray.Length - 1; i >= 0 && counter < letterSlotsUsed + 1; i--) {
            if (noArray[i] == '0')
                str = 6 + str;

            else
                str = noArray[i] + str;

            counter++;
        }

        while (counter < letterSlotsUsed + 1) {
            str = 0 + str;
            counter++;
        }

        return str;
    }


    // Obfuscates the next
    private IEnumerator Obfuscate(int index, int func, bool sound) {
        float waitTime = 1.5f / 52.0f;

        if (sound == true)
            Audio.PlaySoundAtTransform("100Levels_Letter", transform);

        for (int i = 0; i < 52; i++) {
            Increment(index);
            yield return new WaitForSeconds(waitTime);
        }

        if (sound == true && func != 1)
            Audio.PlaySoundAtTransform("100Levels_LetterStop", transform);

        /* 0 = Do nothing
         * 1 = Disappear
         * 2 = Show correct letter
         */

        // Removes the letter
        if (func == 1) {
            letterIndexes[index] = 0;
            letterDisplays[index] = "A";
            Letters[index].gameObject.SetActive(false);
            LetterTexts[index].text = "";
            LetterButtons[index].enabled = false;
        }

        // Shows the correct letter
        else if (func == 2) {
            ColorText(2);

            char[] correctAnsArray = correctMessage.ToCharArray();
            for (int i = 0; i < letterSlotsUsed; i++)
                LetterTexts[lettersUsed[i]].text = correctAnsArray[i].ToString();
        }
    }

    // Colors each text
    private void ColorText(int colorIndex) {
        /* 0 = White
         * 1 = Red
         * 2 = Orange
         * 3 = Yellow
         * 4 = Green
         * 5 = Cyan
         */

        for (int i = 0; i < LetterTexts.Length; i++) {
            LetterTexts[i].color = TextColors[colorIndex];
        }
    }

    // Shows the letters on the screen one by one
    private IEnumerator ShowLetters(bool unlock) {
        float waitTime = 3.0f / letterSlotsUsed;

        for (int i = 0; i < letterSlotsUsed; i++) {
            LetterTexts[lettersUsed[i]].text = letterDisplays[lettersUsed[i]];
            Letters[lettersUsed[i]].gameObject.SetActive(true);
            LetterButtons[lettersUsed[i]].enabled = true;
            StartCoroutine(Obfuscate(lettersUsed[i], 0, true));
            yield return new WaitForSeconds(waitTime);
        }

        if (unlock == true) {
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(UnlockButtons());
        }
    }

    // Unlocks the buttons
    private IEnumerator UnlockButtons() {
        Audio.PlaySoundAtTransform("100Levels_Switch", transform);
        SubmitBtn.gameObject.SetActive(true);
        SubmitText.text = "SUBMIT";
        SubmitBtnModel.enabled = true;
        ToggleBtn.gameObject.SetActive(true);
        ToggleText.text = "TOGGLE";
        ToggleBtnModel.enabled = true;

        ColorText(5);
        yield return new WaitForSeconds(0.1f);
        ColorText(0);
        yield return new WaitForSeconds(0.1f);
        ColorText(5);
        yield return new WaitForSeconds(0.1f);
        ColorText(0);
        yield return new WaitForSeconds(0.1f);
        ColorText(5);
        yield return new WaitForSeconds(0.1f);
        ColorText(0);
        lockButtons = false;
    }


    // Correct answer
    private IEnumerator CorrectAnswer() {
        ColorText(4);
        Debug.LogFormat("[100 Levels of Defusal #{0}] That was correct!", moduleId);
        yield return new WaitForSeconds(3.0f);
        for (int i = 0; i < letterSlotsUsed; i++) {
            if (i == 0)
                StartCoroutine(Obfuscate(lettersUsed[i], 1, true));

            else
                StartCoroutine(Obfuscate(lettersUsed[i], 1, false));
        }
    }

    // Incorrect answer
    private IEnumerator IncorrectAnswer() {
        ColorText(1);
        yield return new WaitForSeconds(1.5f);
        for (int i = 0; i < letterSlotsUsed; i++) {
            if (i == 0)
                StartCoroutine(Obfuscate(lettersUsed[i], 2, true));

            else
                StartCoroutine(Obfuscate(lettersUsed[i], 2, false));
        }

        yield return new WaitForSeconds(5.5f);
        for (int i = 0; i < letterSlotsUsed; i++) {
            if (i == 0)
                StartCoroutine(Obfuscate(lettersUsed[i], 1, true));

            else
                StartCoroutine(Obfuscate(lettersUsed[i], 1, false));
        }

        yield return new WaitForSeconds(2.0f);
        Debug.LogFormat("[100 Levels of Defusal #{0}] Generating new cipher...", moduleId);
        GenerateCipher();
    }

    // Delayed generation
    private IEnumerator Delay() {
        yield return new WaitForSeconds(2.5f);
        GenerateCipher();
    }

    // Displays "SOLVED" on the screen
    private IEnumerator ShowSolveText() {
        yield return new WaitForSeconds(5.0f);
        letterSlotsUsed = 6;

        lettersUsed[0] = 6; lettersUsed[1] = 7; lettersUsed[2] = 8; lettersUsed[3] = 9; lettersUsed[4] = 10; lettersUsed[5] = 11;
        lettersUsed[6] = -1; lettersUsed[7] = -1; lettersUsed[8] = -1; lettersUsed[9] = -1; lettersUsed[10] = -1; lettersUsed[11] = -1;

        letterIndexes[6] = 18; letterIndexes[7] = 14; letterIndexes[8] = 11; letterIndexes[9] = 21; letterIndexes[10] = 4; letterIndexes[11] = 3;

        for (int i = 0; i < letterSlotsUsed; i++) {
            letterDisplays[lettersUsed[i]] = LETTERS[letterIndexes[lettersUsed[i]]];
            screenDisplay += LETTERS[letterIndexes[lettersUsed[i]]];
        }

        StartCoroutine(ShowLetters(false));
    }


    // Determine level
    private void DetermineLevel() {
        var modules = Bomb.GetSolvableModuleNames();
        moduleCount = modules.Count();

        // Gets the level from the present modules
        if (moduleCount == 11 && modules.Count(x => x.Contains("Colored Squares")) == 1 && modules.Count(x => x.Contains("Flashing Lights")) == 1 && modules.Count(x => x.Contains("Mastermind Simple")) == 1 && modules.Count(x => x.Contains("Nonogram")) == 1 && modules.Count(x => x.Contains("Probing")) == 1 && modules.Count(x => x.Contains("The Rule")) == 1 && modules.Count(x => x.Contains("Simon Screams")) == 1 && modules.Count(x => x.Contains("Symbolic Coordinates")) == 1 && modules.Count(x => x.Contains("Two Bits")) == 1 && modules.Count(x => x.Contains("Visual Impairment")) == 1) level = 1;
        else if (moduleCount == 11 && modules.Count(x => x.Contains("Algebra")) == 1 && modules.Count(x => x.Contains("Caesar Cycle")) == 1 && modules.Count(x => x.Contains("Chinese Counting")) == 1 && modules.Count(x => x.Contains("Crazy Talk")) == 1 && modules.Count(x => x.Contains("Identity Parade")) == 1 && modules.Count(x => x.Contains("Maze")) == 1 && modules.Count(x => x.Contains("Pictionary")) == 1 && modules.Count(x => x.Contains("T-Words")) == 1 && modules.Count(x => x.Contains("Wire Sequence")) == 1 && modules.Count(x => x.Contains("Zoni")) == 1) level = 2;
        else if (moduleCount == 11 && modules.Count(x => x.Contains("Addition")) == 1 && modules.Count(x => x.Contains("Catchphrase")) == 1 && modules.Count(x => x.Contains("The Code")) == 1 && modules.Count(x => x.Contains("Countdown")) == 1 && modules.Count(x => x.Contains("Cruel Digital Root")) == 1 && modules.Count(x => x.Contains("DetoNATO")) == 1 && modules.Count(x => x.Contains("Green Arrows")) == 1 && modules.Count(x => x.Contains("LED Math")) == 1 && modules.Count(x => x.Contains("Safety Safe")) == 1 && modules.Count(x => x.Contains("The Screw")) == 1) level = 3;
        else if (moduleCount == 12 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Astrology")) == 1 && modules.Count(x => x.Contains("Blind Alley")) == 1 && modules.Count(x => x.Contains("Colo(u)r Talk")) == 1 && modules.Count(x => x.Contains("Double Color")) == 1 && modules.Count(x => x.Contains("Faulty Digital Root")) == 1 && modules.Count(x => x.Contains("Keypad Lock")) == 1 && modules.Count(x => x.Contains("Modules Against Humanity")) == 1 && modules.Count(x => x.Contains("Piano Keys")) == 1 && modules.Count(x => x.Contains("Switches")) == 1 && modules.Count(x => x.Contains("Wires")) == 1) level = 4;
        else if (moduleCount == 12 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("The Bulb")) == 1 && modules.Count(x => x.Contains("Caesar Cipher")) == 1 && modules.Count(x => x.Contains("Corners")) == 1 && modules.Count(x => x.Contains("LED Encryption")) == 1 && modules.Count(x => x.Contains("Number Pad")) == 1 && modules.Count(x => x.Contains("Orientation Cube")) == 1 && modules.Count(x => x.Contains("Password")) == 1 && modules.Count(x => x.Contains("Semaphore")) == 1 && modules.Count(x => x.Contains("The Simpleton")) == 1 && modules.Count(x => x.Contains("Unrelated Anagrams")) == 1) level = 5;
        else if (moduleCount == 12 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Battleship")) == 1 && modules.Count(x => x.Contains("Broken Buttons")) == 1 && modules.Count(x => x.Contains("Cooking")) == 1 && modules.Count(x => x.Contains("The Digit")) == 1 && modules.Count(x => x.Contains("Reverse Morse")) == 1 && modules.Count(x => x.Contains("Rhythms")) == 1 && modules.Count(x => x.Contains("Rock-Paper-Scissors-L.-Sp.")) == 1 && modules.Count(x => x.Contains("Simon Says")) == 1 && modules.Count(x => x.Contains("Simon Simons")) == 1 && modules.Count(x => x.Contains("Timezone")) == 1) level = 6;
        else if (moduleCount == 12 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Boolean Keypad")) == 1 && modules.Count(x => x.Contains("The Button")) == 1 && modules.Count(x => x.Contains("Complicated Buttons")) == 1 && modules.Count(x => x.Contains("Going Backwards")) == 1 && modules.Count(x => x.Contains("Listening")) == 1 && modules.Count(x => x.Contains("Lucky Dice")) == 1 && modules.Count(x => x.Contains("The Number")) == 1 && modules.Count(x => x.Contains("Purple Arrows")) == 1 && modules.Count(x => x.Contains("Simon Scrambles")) == 1 && modules.Count(x => x.Contains("Yellow Arrows")) == 1) level = 7;
        else if (moduleCount == 13 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("The Block")) == 1 && modules.Count(x => x.Contains("Color Addition")) == 1 && modules.Count(x => x.Contains("English Test")) == 1 && modules.Count(x => x.Contains("Fast Math")) == 1 && modules.Count(x => x.Contains("Grid Matching")) == 1 && modules.Count(x => x.Contains("Morse-A-Maze")) == 1 && modules.Count(x => x.Contains("Murder")) == 1 && modules.Count(x => x.Contains("Orange Arrows")) == 1 && modules.Count(x => x.Contains("Radiator")) == 1 && modules.Count(x => x.Contains("Simon's On First")) == 1 && modules.Count(x => x.Contains("Wire Placement")) == 1) level = 8;
        else if (moduleCount == 13 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Alchemy")) == 1 && modules.Count(x => x.Contains("Alphabetical Ruling")) == 1 && modules.Count(x => x.Contains("Daylight Directions")) == 1 && modules.Count(x => x.Contains("Equations X")) == 1 && modules.Count(x => x.Contains("Ice Cream")) == 1 && modules.Count(x => x.Contains("Morse Code")) == 1 && modules.Count(x => x.Contains("Morsematics")) == 1 && modules.Count(x => x.Contains("Only Connect")) == 1 && modules.Count(x => x.Contains("Ordered Keys")) == 1 && modules.Count(x => x.Contains("Simon Sounds")) == 1 && modules.Count(x => x.Contains("Vigenère Cipher")) == 1) level = 9;
        else if (moduleCount == 13 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Game of Life Simple")) == 1 && modules.Count(x => x.Contains("Guitar Chords")) == 1 && modules.Count(x => x.Contains("Human Resources")) == 1 && modules.Count(x => x.Contains("Keypad")) == 2 && modules.Count(x => x.Contains("Kooky Keypad")) == 1 && modules.Count(x => x.Contains("The Number Cipher")) == 1 && modules.Count(x => x.Contains("Number Nimbleness")) == 1 && modules.Count(x => x.Contains("Periodic Table")) == 1 && modules.Count(x => x.Contains("Red Buttons")) == 1 && modules.Count(x => x.Contains("The Triangle")) == 1 && modules.Count(x => x.Contains("Who's on First")) == 1) level = 10;
        else if (moduleCount == 14 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Button Sequence")) == 1 && modules.Count(x => x.Contains("Color Morse")) == 1 && modules.Count(x => x.Contains("Complicated Wires")) == 1 && modules.Count(x => x.Contains("Digital Cipher")) == 1 && modules.Count(x => x.Contains("Encrypted Dice")) == 1 && modules.Count(x => x.Contains("FizzBuzz")) == 1 && modules.Count(x => x.Contains("Logic")) == 1 && modules.Count(x => x.Contains("Module Homework")) == 1 && modules.Count(x => x.Contains("Press X")) == 1 && modules.Count(x => x.Contains("Prime Checker")) == 1 && modules.Count(x => x.Contains("Simon Speaks")) == 1 && modules.Count(x => x.Contains("Spelling Bee")) == 1) level = 11;
        else if (moduleCount == 14 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Digital Dials")) == 1 && modules.Count(x => x.Contains("The Giant's Drink")) == 1 && modules.Count(x => x.Contains("Kanji")) == 1 && modules.Count(x => x.Contains("Loopover")) == 1 && modules.Count(x => x.Contains("Maritime Flags")) == 1 && modules.Count(x => x.Contains("Memory")) == 1 && modules.Count(x => x.Contains("Perplexing Wires")) == 1 && modules.Count(x => x.Contains("Playfair Cipher")) == 1 && modules.Count(x => x.Contains("Question Mark")) == 1 && modules.Count(x => x.Contains("Tasha Squeals")) == 1 && modules.Count(x => x.Contains("Turn The Key")) == 1 && modules.Count(x => x.Contains("Web Design")) == 1) level = 12;
        else if (moduleCount == 14 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Bob Barks")) >= 1 && modules.Count(x => x.Contains("Cheap Checkout")) >= 1 && modules.Count(x => x.Contains("Follow the Leader")) >= 1 && modules.Count(x => x.Contains("Light Cycle")) >= 1 && modules.Count(x => x.Contains("Mad Memory")) >= 1 && modules.Count(x => x.Contains("Rock-Paper-Scissors-L.-Sp.")) >= 1 && modules.Count(x => x.Contains("Simon Samples")) >= 1 && modules.Count(x => x.Contains("Tasha Squeals")) >= 1 && modules.Count(x => x.Contains("Word Search")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("Keypad")) + modules.Count(x => x.Contains("The Gamepad")) + modules.Count(x => x.Contains("Zoni")) + modules.Count(x => x.Contains("Double Color")) >= 3) level = 13;
        else if (moduleCount == 14 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Arithmelogic")) >= 1 && modules.Count(x => x.Contains("Blind Alley")) >= 1 && modules.Count(x => x.Contains("Creation")) >= 1 && modules.Count(x => x.Contains("Double-Oh")) >= 1 && modules.Count(x => x.Contains("Homophones")) >= 1 && modules.Count(x => x.Contains("Ingredients")) >= 1 && modules.Count(x => x.Contains("The London Underground")) >= 1 && modules.Count(x => x.Contains("Perspective Pegs")) >= 1 && modules.Count(x => x.Contains("Westeros")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("Round Keypad")) + modules.Count(x => x.Contains("Mortal Kombat")) + modules.Count(x => x.Contains("Digital Root")) + modules.Count(x => x.Contains("Keypad Lock")) >= 3) level = 14;
        else if (moduleCount == 15 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Boolean Maze")) >= 1 && modules.Count(x => x.Contains("Dimension Disruption")) >= 1 && modules.Count(x => x.Contains("FizzBuzz")) >= 1 && modules.Count(x => x.Contains("Functions")) >= 1 && modules.Count(x => x.Contains("Gridlock")) >= 1 && modules.Count(x => x.Contains("Neutralization")) >= 1 && modules.Count(x => x.Contains("Placeholder Talk")) >= 1 && modules.Count(x => x.Contains("Reverse Morse")) >= 1 && modules.Count(x => x.Contains("Skewed Slots")) >= 1 && modules.Count(x => x.Contains("Unfair Cipher")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("Caesar Cycle")) + modules.Count(x => x.Contains("Lucky Dice")) + modules.Count(x => x.Contains("egg")) + modules.Count(x => x.Contains("Letter Keys")) >= 3) level = 15;
        else if (moduleCount == 15 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Alphabet Numbers")) >= 1 && modules.Count(x => x.Contains("Boolean Wires")) >= 1 && modules.Count(x => x.Contains("Cryptic Password")) >= 1 && modules.Count(x => x.Contains("Laundry")) >= 1 && modules.Count(x => x.Contains("Monsplode, Fight!")) >= 1 && modules.Count(x => x.Contains("The Screw")) >= 1 && modules.Count(x => x.Contains("S.E.T.")) >= 1 && modules.Count(x => x.Contains("Simon's Star")) >= 1 && modules.Count(x => x.Contains("USA Maze")) >= 1 && modules.Count(x => x.Contains("Wire Placement")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("The Bulb")) + modules.Count(x => x.Contains("Listening")) + modules.Count(x => x.Contains("N&Ms")) + modules.Count(x => x.Contains("The Switch")) >= 3) level = 16;
        else if (moduleCount == 15 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("3D Maze")) >= 1 && modules.Count(x => x.Contains("Adventure Game")) >= 1 && modules.Count(x => x.Contains("A-maze-ing Buttons")) >= 1 && modules.Count(x => x.Contains("Burglar Alarm")) >= 1 && modules.Count(x => x.Contains("Color Decoding")) >= 1 && modules.Count(x => x.Contains("Colorful Madness")) >= 1 && modules.Count(x => x.Contains("Maritime Flags")) >= 1 && modules.Count(x => x.Contains("Tangrams")) >= 1 && modules.Count(x => x.Contains("T-Words")) >= 1 && modules.Count(x => x.Contains("Unown Cipher")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("Memory")) + modules.Count(x => x.Contains("Purple Arrows")) + modules.Count(x => x.Contains("Sink")) + modules.Count(x => x.Contains("Addition")) >= 3) level = 17;
        else if (moduleCount == 15 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Curriculum")) >= 1 && modules.Count(x => x.Contains("The Dealmaker")) >= 1 && modules.Count(x => x.Contains("Fast Math")) >= 1 && modules.Count(x => x.Contains("Ice Cream")) >= 1 && modules.Count(x => x.Contains("Kanji")) >= 1 && modules.Count(x => x.Contains("Morse Buttons")) >= 1 && modules.Count(x => x.Contains("Morse War")) >= 1 && modules.Count(x => x.Contains("Point of Order")) >= 1 && modules.Count(x => x.Contains("Tennis")) >= 1 && modules.Count(x => x.Contains("The Wire")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("1000 Words")) + modules.Count(x => x.Contains("Bone Apple Tea")) + modules.Count(x => x.Contains("Faulty Digital Root")) + modules.Count(x => x.Contains("Pictionary")) >= 3) level = 18;
        else if (moduleCount == 16 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Big Circle")) >= 1 && modules.Count(x => x.Contains("Binary Grid")) >= 1 && modules.Count(x => x.Contains("Christmas Presents")) >= 1 && modules.Count(x => x.Contains("Morsematics")) >= 1 && modules.Count(x => x.Contains("Number Nimbleness")) >= 1 && modules.Count(x => x.Contains("Plumbing")) >= 1 && modules.Count(x => x.Contains("Prime Encryption")) >= 1 && modules.Count(x => x.Contains("Purgatory")) == 1 && modules.Count(x => x.Contains("Simon Speaks")) >= 1 && modules.Count(x => x.Contains("Superlogic")) >= 1 && modules.Count(x => x.Contains("TetraVex")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("Piano Keys")) + modules.Count(x => x.Contains("Switches")) + modules.Count(x => x.Contains("Flavor Text")) + modules.Count(x => x.Contains("Modulo")) >= 3) level = 19;
        else if (moduleCount == 16 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Graffiti Numbers")) >= 1 && modules.Count(x => x.Contains("Horrible Memory")) >= 1 && modules.Count(x => x.Contains("Human Resources")) >= 1 && modules.Count(x => x.Contains("The iPhone")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Morse-A-Maze")) >= 1 && modules.Count(x => x.Contains("Periodic Table")) >= 1 && modules.Count(x => x.Contains("Rubik's Cube")) >= 1 && modules.Count(x => x.Contains("Silly Slots")) >= 1 && modules.Count(x => x.Contains("Spinning Buttons")) >= 1 && modules.Count(x => x.Contains("Tower of Hanoi")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("Character Codes")) + modules.Count(x => x.Contains("LED Grid")) + modules.Count(x => x.Contains("The Colored Maze")) + modules.Count(x => x.Contains("Orange Arrows")) >= 3) level = 20;
        else if (moduleCount == 16 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Alphabetize")) >= 1 && modules.Count(x => x.Contains("Battleship")) >= 1 && modules.Count(x => x.Contains("Blind Maze")) >= 1 && modules.Count(x => x.Contains("Brush Strokes")) >= 1 && modules.Count(x => x.Contains("Lion’s Share")) >= 1 && modules.Count(x => x.Contains("Memorable Buttons")) >= 1 && modules.Count(x => x.Contains("Mortal Kombat")) >= 1 && modules.Count(x => x.Contains("Periodic Table")) >= 1 && modules.Count(x => x.Contains("Semaphore")) >= 1 && modules.Count(x => x.Contains("Third Base")) >= 1 && modules.Count(x => x.Contains("X-Ray")) >= 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("Anagrams")) + modules.Count(x => x.Contains("Mashematics")) + modules.Count(x => x.Contains("The Code")) + modules.Count(x => x.Contains("The Number Cipher")) + modules.Count(x => x.Contains("Switches")) >= 3) level = 21;
        else if (moduleCount == 17 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Accumulation")) >= 1 && modules.Count(x => x.Contains("Binary Grid")) >= 1 && modules.Count(x => x.Contains("Binary Tree")) >= 1 && modules.Count(x => x.Contains("Blockbusters")) >= 1 && modules.Count(x => x.Contains("English Test")) >= 1 && modules.Count(x => x.Contains("Footnotes")) >= 1 && modules.Count(x => x.Contains("Only Connect")) >= 1 && modules.Count(x => x.Contains("Painting")) >= 1 && modules.Count(x => x.Contains("Round Keypad")) >= 1 && modules.Count(x => x.Contains("Simon Sings")) >= 1 && modules.Count(x => x.Contains("Treasure Hunt")) >= 1 && modules.Count(x => x.Contains("Zoo")) >= 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("Emoji Math")) + modules.Count(x => x.Contains("Flash Memory")) + modules.Count(x => x.Contains("Orientation Cube")) + modules.Count(x => x.Contains("Simon Scrambles")) + modules.Count(x => x.Contains("Wire Placement")) >= 3) level = 22;
        else if (moduleCount == 17 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Binary LEDs")) >= 1 && modules.Count(x => x.Contains("Challenge & Contact")) >= 1 && modules.Count(x => x.Contains("Fast Math")) >= 1 && modules.Count(x => x.Contains("Lasers")) >= 1 && modules.Count(x => x.Contains("LED Grid")) >= 1 && modules.Count(x => x.Contains("Minesweeper")) >= 1 && modules.Count(x => x.Contains("Perspective Pegs")) >= 1 && modules.Count(x => x.Contains("Roger")) >= 1 && modules.Count(x => x.Contains("Square Button")) >= 1 && modules.Count(x => x.Contains("Unown Cipher")) >= 1 && modules.Count(x => x.Contains("Valves")) >= 1 && modules.Count(x => x.Contains("The World's Largest Button")) >= 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("Digital Root")) + modules.Count(x => x.Contains("Word Scramble")) + modules.Count(x => x.Contains("Encrypted Dice")) + modules.Count(x => x.Contains("Letter Keys")) + modules.Count(x => x.Contains("Poetry")) >= 3) level = 23;
        else if (moduleCount == 17 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Color Morse")) >= 1 && modules.Count(x => x.Contains("Connection Device")) >= 1 && modules.Count(x => x.Contains("Cryptography")) >= 1 && modules.Count(x => x.Contains("Dr. Doctor")) >= 1 && modules.Count(x => x.Contains("Flags")) >= 1 && modules.Count(x => x.Contains("The Gamepad")) >= 1 && modules.Count(x => x.Contains("Jack Attack")) >= 1 && modules.Count(x => x.Contains("Morsematics")) >= 1 && modules.Count(x => x.Contains("Mouse In The Maze")) >= 1 && modules.Count(x => x.Contains("The Radio")) >= 1 && modules.Count(x => x.Contains("Stained Glass")) >= 1 && modules.Count(x => x.Contains("Tennis")) >= 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("Divisible Numbers")) + modules.Count(x => x.Contains("Going Backwards")) + modules.Count(x => x.Contains("Backgrounds")) + modules.Count(x => x.Contains("Identity Parade")) + modules.Count(x => x.Contains("Keypad Lock")) >= 3) level = 24;
        else if (moduleCount == 18 && modules.Count(x => x.Contains("Forget Everything")) == 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("Blue Arrows")) >= 1 && modules.Count(x => x.Contains("Cryptic Password")) >= 1 && modules.Count(x => x.Contains("The Cube")) >= 1 && modules.Count(x => x.Contains("Font Select")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Kanji")) >= 1 && modules.Count(x => x.Contains("Melody Sequencer")) >= 1 && modules.Count(x => x.Contains("Quaternions")) >= 1 && modules.Count(x => x.Contains("Seven Deadly Sins")) >= 1 && modules.Count(x => x.Contains("Simon Spins")) >= 1 && modules.Count(x => x.Contains("The Switch")) >= 1 && modules.Count(x => x.Contains("Tap Code")) >= 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("Green Arrows")) + modules.Count(x => x.Contains("Boolean Keypad")) + modules.Count(x => x.Contains("Color Addition")) + modules.Count(x => x.Contains("Keypad Combinations")) + modules.Count(x => x.Contains("Alphabet")) >= 3) level = 25;
        else if (moduleCount == 18 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("...?")) >= 1 && modules.Count(x => x.Contains("Alpha-Bits")) >= 1 && modules.Count(x => x.Contains("Catchphrase")) >= 1 && modules.Count(x => x.Contains("Character Codes")) >= 1 && modules.Count(x => x.Contains("The High Score")) >= 1 && modules.Count(x => x.Contains("Maintenance")) >= 1 && modules.Count(x => x.Contains("Manometers")) >= 1 && modules.Count(x => x.Contains("Mazematics")) >= 1 && modules.Count(x => x.Contains("Polyhedral Maze")) >= 1 && modules.Count(x => x.Contains("Skewed Slots")) >= 1 && modules.Count(x => x.Contains("Synchronization")) >= 1 && modules.Count(x => x.Contains("The Triangle")) >= 1 && modules.Count(x => x.Contains("The Triangle Button")) >= 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("Countdown")) + modules.Count(x => x.Contains("Light Bulbs")) + modules.Count(x => x.Contains("The Bulb")) + modules.Count(x => x.Contains("Insane Talk")) + modules.Count(x => x.Contains("Word Search")) >= 3) level = 26;
        else if (moduleCount == 18 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("% Grey")) >= 1 && modules.Count(x => x.Contains("Bone Apple Tea")) >= 1 && modules.Count(x => x.Contains("Burglar Alarm")) >= 1 && modules.Count(x => x.Contains("Hexamaze")) >= 1 && modules.Count(x => x.Contains("The Labyrinth")) >= 1 && modules.Count(x => x.Contains("Left and Right")) >= 1 && modules.Count(x => x.Contains("Mystic Square")) >= 1 && modules.Count(x => x.Contains("Neutralization")) >= 1 && modules.Count(x => x.Contains("Schlag den Bomb")) >= 1 && modules.Count(x => x.Contains("Simon Stops")) >= 1 && modules.Count(x => x.Contains("Snooker")) >= 1 && modules.Count(x => x.Contains("Ternary Converter")) >= 1 && modules.Count(x => x.Contains("The Wire")) >= 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("Fruits")) + modules.Count(x => x.Contains("The Jukebox")) + modules.Count(x => x.Contains("Alphabetical Ruling")) + modules.Count(x => x.Contains("Double-Oh")) + modules.Count(x => x.Contains("Timezone")) >= 3) level = 27;
        else if (moduleCount == 18 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("1D Maze")) >= 1 && modules.Count(x => x.Contains("Blackjack")) >= 1 && modules.Count(x => x.Contains("Colored Keys")) >= 1 && modules.Count(x => x.Contains("Flavor Text")) >= 1 && modules.Count(x => x.Contains("Friendship")) >= 1 && modules.Count(x => x.Contains("Horrible Memory")) >= 1 && modules.Count(x => x.Contains("IKEA")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Logical Buttons")) >= 1 && modules.Count(x => x.Contains("The London Underground")) >= 1 && modules.Count(x => x.Contains("Password Generator")) >= 1 && modules.Count(x => x.Contains("Quiz Buzz")) >= 1 && modules.Count(x => x.Contains("Splitting The Loot")) >= 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("Combination Lock")) + modules.Count(x => x.Contains("Seven Wires")) + modules.Count(x => x.Contains("Crazy Talk")) + modules.Count(x => x.Contains("Piano Keys")) + modules.Count(x => x.Contains("Rhythms")) >= 3) level = 28;
        else if (moduleCount == 19 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Chord Qualities")) >= 1 && modules.Count(x => x.Contains("The Colored Maze")) >= 1 && modules.Count(x => x.Contains("Greek Calculus")) >= 1 && modules.Count(x => x.Contains("The Hexabutton")) >= 1 && modules.Count(x => x.Contains("The iPhone")) >= 1 && modules.Count(x => x.Contains("Microphone")) >= 1 && modules.Count(x => x.Contains("Minecraft Cipher")) >= 1 && modules.Count(x => x.Contains("The Modkit")) >= 1 && modules.Count(x => x.Contains("Risky Wires")) >= 1 && modules.Count(x => x.Contains("Roman Art")) >= 1 && modules.Count(x => x.Contains("Rubik's Cube")) >= 1 && modules.Count(x => x.Contains("Scavenger Hunt")) >= 1 && modules.Count(x => x.Contains("The Sun")) >= 1 && modules.Count(x => x.Contains("Web Design")) >= 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("LED Encryption")) + modules.Count(x => x.Contains("Numbers")) + modules.Count(x => x.Contains("Digital Cipher")) + modules.Count(x => x.Contains("The Giant's Drink")) + modules.Count(x => x.Contains("Label Priorities")) >= 3) level = 29;
        else if (moduleCount == 19 && modules.Count(x => x.Contains("Simon's Stages")) == 1 && modules.Count(x => x.Contains("Adjacent Letters")) >= 1 && modules.Count(x => x.Contains("Backgrounds")) >= 1 && modules.Count(x => x.Contains("Benedict Cumberbatch")) >= 1 && modules.Count(x => x.Contains("Cruel Keypads")) >= 1 && modules.Count(x => x.Contains("Extended Password")) >= 1 && modules.Count(x => x.Contains("Gridlock")) >= 1 && modules.Count(x => x.Contains("Ingredients")) >= 1 && modules.Count(x => x.Contains("Not Simaze")) >= 1 && modules.Count(x => x.Contains("Point of Order")) >= 1 && modules.Count(x => x.Contains("Shell Game")) >= 1 && modules.Count(x => x.Contains("Simon Selects")) >= 1 && modules.Count(x => x.Contains("Skyrim")) >= 1 && modules.Count(x => x.Contains("Synchronization")) >= 1 && modules.Count(x => x.Contains("Tax Returns")) >= 1 && modules.Count(x => x.Contains("LED Grid")) + modules.Count(x => x.Contains("The Plunger Button")) + modules.Count(x => x.Contains("Catchphrase")) + modules.Count(x => x.Contains("Listening")) + modules.Count(x => x.Contains("Mad Memory")) >= 3) level = 30;
        else if (moduleCount == 20 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Bitmaps")) >= 1 && modules.Count(x => x.Contains("Cheep Checkout")) >= 1 && modules.Count(x => x.Contains("Decolored Squares")) >= 1 && modules.Count(x => x.Contains("Double Arrows")) >= 1 && modules.Count(x => x.Contains("Game of Life Simple")) >= 1 && modules.Count(x => x.Contains("Hieroglyphics")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Light Bulbs")) >= 1 && modules.Count(x => x.Contains("Minecraft Parody")) >= 1 && modules.Count(x => x.Contains("Not Keypad")) >= 1 && modules.Count(x => x.Contains("Polyhedral Maze")) >= 1 && modules.Count(x => x.Contains("Regular Crazy Talk")) >= 1 && modules.Count(x => x.Contains("Roger")) >= 1 && modules.Count(x => x.Contains("The Stopwatch")) >= 1 && modules.Count(x => x.Contains("Who's on First Translated")) >= 1 && modules.Count(x => x.Contains("Big Button Translated")) + modules.Count(x => x.Contains("Insane Talk")) + modules.Count(x => x.Contains("Keypad Lock")) + modules.Count(x => x.Contains("Label Priorities")) + modules.Count(x => x.Contains("Numbers")) >= 3) level = 31;
        else if (moduleCount == 20 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("Curriculum")) >= 1 && modules.Count(x => x.Contains("Equations")) >= 1 && modules.Count(x => x.Contains("Fruits")) >= 1 && modules.Count(x => x.Contains("Mega Man 2")) >= 1 && modules.Count(x => x.Contains("Mineseeker")) >= 1 && modules.Count(x => x.Contains("Modulus Manipulation")) >= 1 && modules.Count(x => x.Contains("Not Wiresword")) >= 1 && modules.Count(x => x.Contains("Playfair Cipher")) >= 1 && modules.Count(x => x.Contains("Reverse Polish Notation")) >= 1 && modules.Count(x => x.Contains("Scripting")) >= 1 && modules.Count(x => x.Contains("Sorting")) >= 1 && modules.Count(x => x.Contains("The Triangle Button")) >= 1 && modules.Count(x => x.Contains("Unicode")) >= 1 && modules.Count(x => x.Contains("Vectors")) >= 1 && modules.Count(x => x.Contains("Password")) + modules.Count(x => x.Contains("The Code")) + modules.Count(x => x.Contains("The Festive Jukebox")) + modules.Count(x => x.Contains("Piano Keys")) + modules.Count(x => x.Contains("Zoni")) >= 3) level = 32;
        else if (moduleCount == 20 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("1000 Words")) >= 1 && modules.Count(x => x.Contains("Answering Can Be Fun")) >= 1 && modules.Count(x => x.Contains("Challenge & Contact")) >= 1 && modules.Count(x => x.Contains("Crackbox")) >= 1 && modules.Count(x => x.Contains("The Dealmaker")) >= 1 && modules.Count(x => x.Contains("Harmony Sequence")) >= 1 && modules.Count(x => x.Contains("The Matrix")) >= 1 && modules.Count(x => x.Contains("Not Memory")) >= 1 && modules.Count(x => x.Contains("Pigpen Cycle")) >= 1 && modules.Count(x => x.Contains("Raiding Temples")) >= 1 && modules.Count(x => x.Contains("Simon Shrieks")) >= 1 && modules.Count(x => x.Contains("Simon's Star")) >= 1 && modules.Count(x => x.Contains("Tangrams")) >= 1 && modules.Count(x => x.Contains("Uncolored Squares")) >= 1 && modules.Count(x => x.Contains("Vexillology")) >= 1 && modules.Count(x => x.Contains("Going Backwards")) + modules.Count(x => x.Contains("Seven Wires")) + modules.Count(x => x.Contains("Countdown")) + modules.Count(x => x.Contains("Switches")) + modules.Count(x => x.Contains("The Witness")) >= 3) level = 33;
        else if (moduleCount == 21 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Chess")) >= 1 && modules.Count(x => x.Contains("Coffeebucks")) >= 1 && modules.Count(x => x.Contains("Colour Flash")) >= 1 && modules.Count(x => x.Contains("Cruel Piano Keys")) >= 1 && modules.Count(x => x.Contains("Cryptography")) >= 1 && modules.Count(x => x.Contains("Discolored Squares")) >= 1 && modules.Count(x => x.Contains("Marble Tumble")) >= 1 && modules.Count(x => x.Contains("Not the Button")) >= 1 && modules.Count(x => x.Contains("Odd One Out")) >= 1 && modules.Count(x => x.Contains("Orientation Cube")) >= 1 && modules.Count(x => x.Contains("Plumbing")) >= 1 && modules.Count(x => x.Contains("Round Keypad")) >= 1 && modules.Count(x => x.Contains("Simon Stages")) >= 1 && modules.Count(x => x.Contains("Sink")) >= 1 && modules.Count(x => x.Contains("Turn The Keys")) >= 1 && modules.Count(x => x.Contains("X01")) >= 1 && modules.Count(x => x.Contains("Keypad")) + modules.Count(x => x.Contains("Who's on First")) + modules.Count(x => x.Contains("Dominoes")) + modules.Count(x => x.Contains("Green Arrows")) + modules.Count(x => x.Contains("Wire Placement")) >= 3) level = 34;
        else if (moduleCount == 21 && modules.Count(x => x.Contains("Tallordered Keys")) == 1 && modules.Count(x => x.Contains("Blinkstop")) >= 1 && modules.Count(x => x.Contains("Bordered Keys")) >= 1 && modules.Count(x => x.Contains("Colored Keys")) >= 1 && modules.Count(x => x.Contains("Creation")) >= 1 && modules.Count(x => x.Contains("Cruel Countdown")) >= 1 && modules.Count(x => x.Contains("Fast Math")) >= 1 && modules.Count(x => x.Contains("Five Letter Words")) >= 1 && modules.Count(x => x.Contains("Guess Who?")) >= 1 && modules.Count(x => x.Contains("Hold Ups")) >= 1 && modules.Count(x => x.Contains("Mahjong")) >= 1 && modules.Count(x => x.Contains("Meter")) >= 1 && modules.Count(x => x.Contains("Module Listening")) >= 1 && modules.Count(x => x.Contains("Not Morse Code")) >= 1 && modules.Count(x => x.Contains("Poetry")) >= 1 && modules.Count(x => x.Contains("Reordered Keys")) >= 1 && modules.Count(x => x.Contains("Seven Deadly Sins")) >= 1 && modules.Count(x => x.Contains("Foreign Exchange Rates")) + modules.Count(x => x.Contains("T-Words")) + modules.Count(x => x.Contains("Astrology")) + modules.Count(x => x.Contains("Color Addition")) + modules.Count(x => x.Contains("Extended Password")) >= 3) level = 35;
        else if (moduleCount == 21 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Font Select")) >= 1 && modules.Count(x => x.Contains("Hunting")) >= 1 && modules.Count(x => x.Contains("Kanji")) >= 1 && modules.Count(x => x.Contains("Logic Gates")) >= 1 && modules.Count(x => x.Contains("Mashematics")) >= 1 && modules.Count(x => x.Contains("Module Maze")) >= 1 && modules.Count(x => x.Contains("Monsplode Trading Cards")) >= 1 && modules.Count(x => x.Contains("Not Maze")) >= 1 && modules.Count(x => x.Contains("Pigpen Rotations")) >= 1 && modules.Count(x => x.Contains("Rubik's Cube")) >= 1 && modules.Count(x => x.Contains("Sueet Wall")) >= 1 && modules.Count(x => x.Contains("Tic Tac Toe")) >= 1 && modules.Count(x => x.Contains("Transmitted Morse")) >= 1 && modules.Count(x => x.Contains("Unordered Keys")) >= 1 && modules.Count(x => x.Contains("Vcrcs")) >= 1 && modules.Count(x => x.Contains("Zoo")) >= 1 && modules.Count(x => x.Contains("Caesar Cipher")) + modules.Count(x => x.Contains("Natures")) + modules.Count(x => x.Contains("Blind Alley")) + modules.Count(x => x.Contains("English Test")) + modules.Count(x => x.Contains("Symbolic Password")) >= 3) level = 36;
        else if (moduleCount == 22 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("ASCII Art")) >= 1 && modules.Count(x => x.Contains("Blockbusters")) >= 1 && modules.Count(x => x.Contains("Bomb Diffusal")) >= 1 && modules.Count(x => x.Contains("Braille")) >= 1 && modules.Count(x => x.Contains("Digit String")) >= 1 && modules.Count(x => x.Contains("Dr. Doctor")) >= 1 && modules.Count(x => x.Contains("Greek Letter Grid")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("The Jukebox")) >= 1 && modules.Count(x => x.Contains("Know Your Way")) >= 1 && modules.Count(x => x.Contains("Melody Sequencer")) >= 1 && modules.Count(x => x.Contains("The Moon")) >= 1 && modules.Count(x => x.Contains("Not Complicated Wires")) >= 1 && modules.Count(x => x.Contains("Party Time")) >= 1 && modules.Count(x => x.Contains("Reverse Morse")) >= 1 && modules.Count(x => x.Contains("Topsy Turvy")) >= 1 && modules.Count(x => x.Contains("Wavetapping")) >= 1 && modules.Count(x => x.Contains("N&Ms")) + modules.Count(x => x.Contains("Prime Checker")) + modules.Count(x => x.Contains("Broken Buttons")) + modules.Count(x => x.Contains("The Jack-O'-Lantern")) + modules.Count(x => x.Contains("S.E.T.")) >= 3) level = 37;
        else if (moduleCount == 22 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Adjacent Letters (Russian)")) >= 1 && modules.Count(x => x.Contains("Bases")) >= 1 && modules.Count(x => x.Contains("Big Circle")) >= 1 && modules.Count(x => x.Contains("Binary Puzzle")) >= 1 && modules.Count(x => x.Contains("Boolean Maze")) >= 1 && modules.Count(x => x.Contains("Broken Guitar Chords")) >= 1 && modules.Count(x => x.Contains("Color Decoding")) >= 1 && modules.Count(x => x.Contains("Colorful Insanity")) >= 1 && modules.Count(x => x.Contains("The Crystal Maze")) >= 1 && modules.Count(x => x.Contains("Divisible Numbers")) >= 1 && modules.Count(x => x.Contains("Gadgetron Vendor")) >= 1 && modules.Count(x => x.Contains("Garfield Kart")) >= 1 && modules.Count(x => x.Contains("Geometry Dash")) >= 1 && modules.Count(x => x.Contains("Mastermind Cruel")) >= 1 && modules.Count(x => x.Contains("Not Password")) >= 1 && modules.Count(x => x.Contains("Red Arrows")) >= 1 && modules.Count(x => x.Contains("Simon Sings")) >= 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("The Digit")) + modules.Count(x => x.Contains("Color Generator")) + modules.Count(x => x.Contains("Crazy Talk")) + modules.Count(x => x.Contains("Simon Scrambles")) >= 3) level = 38;
        else if (moduleCount == 23 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Colored Switches")) >= 1 && modules.Count(x => x.Contains("Connection Device")) >= 1 && modules.Count(x => x.Contains("Coordinates")) >= 1 && modules.Count(x => x.Contains("Encrypted Equations")) >= 1 && modules.Count(x => x.Contains("Game of Life Cruel")) >= 1 && modules.Count(x => x.Contains("The Hangover")) >= 1 && modules.Count(x => x.Contains("Hexamaze")) >= 1 && modules.Count(x => x.Contains("Jenga")) >= 1 && modules.Count(x => x.Contains("Lying Indicators")) >= 1 && modules.Count(x => x.Contains("Modern Cipher")) >= 1 && modules.Count(x => x.Contains("Modulo")) >= 1 && modules.Count(x => x.Contains("Morse Code Translated")) >= 1 && modules.Count(x => x.Contains("Not Wire Sequence")) >= 1 && modules.Count(x => x.Contains("Safety Square")) >= 1 && modules.Count(x => x.Contains("Semamorse")) >= 1 && modules.Count(x => x.Contains("Simon Speaks")) >= 1 && modules.Count(x => x.Contains("Symbolic Colouring")) >= 1 && modules.Count(x => x.Contains("Unown Cipher")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("Addition")) + modules.Count(x => x.Contains("Combination Lock")) + modules.Count(x => x.Contains("The Switch")) + modules.Count(x => x.Contains("Text Field")) >= 3) level = 39;
        else if (moduleCount == 23 && modules.Count(x => x.Contains("Forget Enigma")) == 1 && modules.Count(x => x.Contains("101 Dalmatians")) >= 1 && modules.Count(x => x.Contains("Green Cipher")) >= 1 && modules.Count(x => x.Contains("Hinges")) >= 1 && modules.Count(x => x.Contains("Left and Right")) >= 1 && modules.Count(x => x.Contains("Microcontroller")) >= 1 && modules.Count(x => x.Contains("Minesweeper")) >= 1 && modules.Count(x => x.Contains("Morsematics")) >= 1 && modules.Count(x => x.Contains("Not Who's on First")) >= 1 && modules.Count(x => x.Contains("NumberWang")) >= 1 && modules.Count(x => x.Contains("Pie")) >= 1 && modules.Count(x => x.Contains("Risky Wires")) >= 1 && modules.Count(x => x.Contains("Seven Wires")) >= 1 && modules.Count(x => x.Contains("Shikaku")) >= 1 && modules.Count(x => x.Contains("Signals")) >= 1 && modules.Count(x => x.Contains("Sonic the Hedgehog")) >= 1 && modules.Count(x => x.Contains("The Stare")) >= 1 && modules.Count(x => x.Contains("Thinking Wires")) >= 1 && modules.Count(x => x.Contains("Waste Management")) >= 1 && modules.Count(x => x.Contains("Simon Says")) + modules.Count(x => x.Contains("egg")) + modules.Count(x => x.Contains("❖")) + modules.Count(x => x.Contains("Colo(u)r Talk")) + modules.Count(x => x.Contains("Fruits")) >= 3) level = 40;
        else if (moduleCount == 24 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("15 Mystic Lights")) >= 1 && modules.Count(x => x.Contains("Anagrams")) >= 1 && modules.Count(x => x.Contains("Button Grid")) >= 1 && modules.Count(x => x.Contains("Character Shift")) >= 1 && modules.Count(x => x.Contains("Constellations")) >= 1 && modules.Count(x => x.Contains("Find The Date")) >= 1 && modules.Count(x => x.Contains("Genetic Sequence")) >= 1 && modules.Count(x => x.Contains("Gryphons")) >= 1 && modules.Count(x => x.Contains("Hidden Colors")) >= 1 && modules.Count(x => x.Contains("Insane Talk")) >= 1 && modules.Count(x => x.Contains("Instructions")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Logical Buttons")) >= 1 && modules.Count(x => x.Contains("Mafia")) >= 1 && modules.Count(x => x.Contains("Morse-A-Maze")) >= 1 && modules.Count(x => x.Contains("Passwords Translated")) >= 1 && modules.Count(x => x.Contains("Spinning Buttons")) >= 1 && modules.Count(x => x.Contains("Sticky Notes")) >= 1 && modules.Count(x => x.Contains("Yahtzee")) >= 1 && modules.Count(x => x.Contains("Boot Too Big")) + modules.Count(x => x.Contains("Unrelated Anagrams")) + modules.Count(x => x.Contains("Bone Apple Tea")) + modules.Count(x => x.Contains("Word Scramble")) + modules.Count(x => x.Contains("Yellow Arrows")) >= 3) level = 41;
        else if (moduleCount == 24 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Accumulation")) >= 1 && modules.Count(x => x.Contains("Alliances")) >= 1 && modules.Count(x => x.Contains("Burglar Alarm")) >= 1 && modules.Count(x => x.Contains("Caesar Cycle")) >= 1 && modules.Count(x => x.Contains("Calculus")) >= 1 && modules.Count(x => x.Contains("Emoji Math")) >= 1 && modules.Count(x => x.Contains("Faulty Backgrounds")) >= 1 && modules.Count(x => x.Contains("Flavor Text EX")) >= 1 && modules.Count(x => x.Contains("Flower Patch")) >= 1 && modules.Count(x => x.Contains("Lines of Code")) >= 1 && modules.Count(x => x.Contains("Polygons")) >= 1 && modules.Count(x => x.Contains("Recolored Switches")) >= 1 && modules.Count(x => x.Contains("Reordered Keys")) >= 1 && modules.Count(x => x.Contains("Rubik’s Clock")) >= 1 && modules.Count(x => x.Contains("Symbol Cycle")) >= 1 && modules.Count(x => x.Contains("SYNC-125 [3]")) >= 1 && modules.Count(x => x.Contains("Treasure Hunt")) >= 1 && modules.Count(x => x.Contains("V")) >= 1 && modules.Count(x => x.Contains("Word Scramble")) >= 1 && modules.Count(x => x.Contains("Weird Al Yankovic")) + modules.Count(x => x.Contains("Press X")) + modules.Count(x => x.Contains("The Gamepad")) + modules.Count(x => x.Contains("Rhythms")) + modules.Count(x => x.Contains("Square Button")) >= 3) level = 42;
        else if (moduleCount == 24 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Alphabet")) >= 1 && modules.Count(x => x.Contains("Blind Maze")) >= 1 && modules.Count(x => x.Contains("Blue Cipher")) >= 1 && modules.Count(x => x.Contains("Codenames")) >= 1 && modules.Count(x => x.Contains("Double Expert")) >= 1 && modules.Count(x => x.Contains("Fencing")) >= 1 && modules.Count(x => x.Contains("Flash Memory")) >= 1 && modules.Count(x => x.Contains("Graphic Memory")) >= 1 && modules.Count(x => x.Contains("The High Score")) >= 1 && modules.Count(x => x.Contains("Horrible Memory")) >= 1 && modules.Count(x => x.Contains("Lightspeed")) >= 1 && modules.Count(x => x.Contains("Planets")) >= 1 && modules.Count(x => x.Contains("Retirement")) >= 1 && modules.Count(x => x.Contains("Sea Shells")) >= 1 && modules.Count(x => x.Contains("Shapes And Bombs")) >= 1 && modules.Count(x => x.Contains("Simon Sends")) >= 1 && modules.Count(x => x.Contains("Stack'em")) >= 1 && modules.Count(x => x.Contains("Tower of Hanoi")) >= 1 && modules.Count(x => x.Contains("Varicolored Squares")) >= 1 && modules.Count(x => x.Contains("Digital Root")) + modules.Count(x => x.Contains("Radiator")) + modules.Count(x => x.Contains("...?")) + modules.Count(x => x.Contains("Binary LEDs")) + modules.Count(x => x.Contains("Error Codes")) >= 3) level = 43;
        else if (moduleCount == 25 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("64")) >= 1 && modules.Count(x => x.Contains("Adventure Game")) >= 1 && modules.Count(x => x.Contains("Boxing")) >= 1 && modules.Count(x => x.Contains("Colour Flash PL")) >= 1 && modules.Count(x => x.Contains("The Deck of Many Things")) >= 1 && modules.Count(x => x.Contains("Decolored Squares")) >= 1 && modules.Count(x => x.Contains("Error Codes")) >= 1 && modules.Count(x => x.Contains("Flags")) >= 1 && modules.Count(x => x.Contains("Free Parking")) >= 1 && modules.Count(x => x.Contains("Guess Who?")) >= 1 && modules.Count(x => x.Contains("Horrible Memory")) >= 1 && modules.Count(x => x.Contains("Logic Gates")) >= 1 && modules.Count(x => x.Contains("Maze³")) >= 1 && modules.Count(x => x.Contains("Only Connect")) >= 1 && modules.Count(x => x.Contains("Orange Arrows")) >= 1 && modules.Count(x => x.Contains("Schlag den Bomb")) >= 1 && modules.Count(x => x.Contains("Skewed Slots")) >= 1 && modules.Count(x => x.Contains("Subscribe to Pewdiepie")) >= 1 && modules.Count(x => x.Contains("Uncolored Squares")) >= 1 && modules.Count(x => x.Contains("X-Ray")) >= 1 && modules.Count(x => x.Contains("Color Addition")) + modules.Count(x => x.Contains("Flavor Text")) + modules.Count(x => x.Contains("Hexamaze")) + modules.Count(x => x.Contains("Keypad Lock")) + modules.Count(x => x.Contains("Murder")) + modules.Count(x => x.Contains("Sea Shells")) >= 3) level = 44;
        else if (moduleCount == 25 && modules.Count(x => x.Contains("Forget Me Later")) == 1 && modules.Count(x => x.Contains("3D Tunnels")) >= 1 && modules.Count(x => x.Contains("Affine Cycle")) >= 1 && modules.Count(x => x.Contains("Braille")) >= 1 && modules.Count(x => x.Contains("Cheep Checkout")) >= 1 && modules.Count(x => x.Contains("Chord Progressions")) >= 1 && modules.Count(x => x.Contains("Creation")) >= 1 && modules.Count(x => x.Contains("Festive Piano Keys")) >= 1 && modules.Count(x => x.Contains("Flash Memory")) >= 1 && modules.Count(x => x.Contains("Following Orders")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Mahjong")) >= 1 && modules.Count(x => x.Contains("Not Password")) >= 1 && modules.Count(x => x.Contains("Object Shows")) >= 1 && modules.Count(x => x.Contains("Passport Control")) >= 1 && modules.Count(x => x.Contains("Radiator")) >= 1 && modules.Count(x => x.Contains("The Radio")) >= 1 && modules.Count(x => x.Contains("Regular Crazy Talk")) >= 1 && modules.Count(x => x.Contains("Risky Wires")) >= 1 && modules.Count(x => x.Contains("Siffron")) >= 1 && modules.Count(x => x.Contains("The Stare")) >= 1 && modules.Count(x => x.Contains("Backgrounds")) + modules.Count(x => x.Contains("Big Circle")) + modules.Count(x => x.Contains("The Number")) + modules.Count(x => x.Contains("S.E.T.")) + modules.Count(x => x.Contains("The Stopwatch")) + modules.Count(x => x.Contains("V")) >= 3) level = 45;
        else if (moduleCount == 25 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Answering Can Be Fun")) >= 1 && modules.Count(x => x.Contains("Bitwise Operations")) >= 1 && modules.Count(x => x.Contains("Boot Too Big")) >= 1 && modules.Count(x => x.Contains("Brush Strokes")) >= 1 && modules.Count(x => x.Contains("The Bulb")) >= 1 && modules.Count(x => x.Contains("Color Math")) >= 1 && modules.Count(x => x.Contains("Double Expert")) >= 1 && modules.Count(x => x.Contains("Etterna")) >= 1 && modules.Count(x => x.Contains("Faulty Sink")) >= 1 && modules.Count(x => x.Contains("Friendship")) >= 1 && modules.Count(x => x.Contains("The High Score")) >= 1 && modules.Count(x => x.Contains("Numbered Buttons")) >= 1 && modules.Count(x => x.Contains("Safety Safe")) >= 1 && modules.Count(x => x.Contains("Shape Shift")) >= 1 && modules.Count(x => x.Contains("Tangrams")) >= 1 && modules.Count(x => x.Contains("Tap Code")) >= 1 && modules.Count(x => x.Contains("Unordered Keys")) >= 1 && modules.Count(x => x.Contains("Web Design")) >= 1 && modules.Count(x => x.Contains("White Cipher")) >= 1 && modules.Count(x => x.Contains("Word Search (PL)")) >= 1 && modules.Count(x => x.Contains("Bone Apple Tea")) + modules.Count(x => x.Contains("Caesar Cycle")) + modules.Count(x => x.Contains("Not Simaze")) + modules.Count(x => x.Contains("Number Nimbleness")) + modules.Count(x => x.Contains("Sorting")) + modules.Count(x => x.Contains("Who's on First Translated")) >= 3) level = 46;
        else if (moduleCount == 26 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("British Slang")) >= 1 && modules.Count(x => x.Contains("The Code")) >= 1 && modules.Count(x => x.Contains("Co-op Harmony Sequence")) >= 1 && modules.Count(x => x.Contains("Coordinates")) >= 1 && modules.Count(x => x.Contains("Cryptography")) >= 1 && modules.Count(x => x.Contains("Dr. Doctor")) >= 1 && modules.Count(x => x.Contains("Equations")) >= 1 && modules.Count(x => x.Contains("Foreign Exchange Rates")) >= 1 && modules.Count(x => x.Contains("Functions")) >= 1 && modules.Count(x => x.Contains("The Hypercube")) >= 1 && modules.Count(x => x.Contains("Ice Cream")) >= 1 && modules.Count(x => x.Contains("LED Math")) >= 1 && modules.Count(x => x.Contains("Light Cycle")) >= 1 && modules.Count(x => x.Contains("Loopover")) >= 1 && modules.Count(x => x.Contains("Masyu")) >= 1 && modules.Count(x => x.Contains("Module Homework")) >= 1 && modules.Count(x => x.Contains("Pattern Cube")) >= 1 && modules.Count(x => x.Contains("Playfair Cipher")) >= 1 && modules.Count(x => x.Contains("Stars")) >= 1 && modules.Count(x => x.Contains("Ten-Button Color Code")) >= 1 && modules.Count(x => x.Contains("Unown Cipher")) >= 1 && modules.Count(x => x.Contains("Maze")) + modules.Count(x => x.Contains("Binary LEDs")) + modules.Count(x => x.Contains("Blind Alley")) + modules.Count(x => x.Contains("Simon's Star")) + modules.Count(x => x.Contains("Superlogic")) + modules.Count(x => x.Contains("Text Field")) >= 3) level = 47;
        else if (moduleCount == 26 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("1000 Words")) >= 1 && modules.Count(x => x.Contains("A Message")) >= 1 && modules.Count(x => x.Contains("Chord Qualities")) >= 1 && modules.Count(x => x.Contains("Connection Check")) >= 1 && modules.Count(x => x.Contains("Cruel Piano Keys")) >= 1 && modules.Count(x => x.Contains("Exoplanets")) >= 1 && modules.Count(x => x.Contains("Insane Talk PL")) >= 1 && modules.Count(x => x.Contains("Lucky Dice")) >= 1 && modules.Count(x => x.Contains("Mega Man 2")) >= 1 && modules.Count(x => x.Contains("Module Movements")) >= 1 && modules.Count(x => x.Contains("Morse Buttons")) >= 1 && modules.Count(x => x.Contains("Natures")) >= 1 && modules.Count(x => x.Contains("Partial Derivatives")) >= 1 && modules.Count(x => x.Contains("Plumbing")) >= 1 && modules.Count(x => x.Contains("Poker")) >= 1 && modules.Count(x => x.Contains("Simon Sings")) >= 1 && modules.Count(x => x.Contains("Simon Sounds")) >= 1 && modules.Count(x => x.Contains("The Sun")) >= 1 && modules.Count(x => x.Contains("Varicolored Squares")) >= 1 && modules.Count(x => x.Contains("Waste Management")) >= 1 && modules.Count(x => x.Contains("Yellow Cipher")) >= 1 && modules.Count(x => x.Contains("Complicated Buttons")) + modules.Count(x => x.Contains("Faulty Digital Root")) + modules.Count(x => x.Contains("Listening")) + modules.Count(x => x.Contains("Morse Code Translated")) + modules.Count(x => x.Contains("Morse War")) + modules.Count(x => x.Contains("Red Arrows")) >= 3) level = 48;
        else if (moduleCount == 26 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Bartending")) >= 1 && modules.Count(x => x.Contains("Cruel Boolean Maze")) >= 1 && modules.Count(x => x.Contains("Digit String")) >= 1 && modules.Count(x => x.Contains("Dominoes")) >= 1 && modules.Count(x => x.Contains("Dragon Energy")) >= 1 && modules.Count(x => x.Contains("Grocery Store")) >= 1 && modules.Count(x => x.Contains("Homophones")) >= 1 && modules.Count(x => x.Contains("Identity Parade")) >= 1 && modules.Count(x => x.Contains("Kooky Keypad")) >= 1 && modules.Count(x => x.Contains("Mortal Kombat")) >= 1 && modules.Count(x => x.Contains("Not Complicated Wires")) >= 1 && modules.Count(x => x.Contains("Not Memory")) >= 1 && modules.Count(x => x.Contains("Playfair Cycle")) >= 1 && modules.Count(x => x.Contains("Resistors")) >= 1 && modules.Count(x => x.Contains("Safety Square")) >= 1 && modules.Count(x => x.Contains("Sequences")) >= 1 && modules.Count(x => x.Contains("Simon Samples")) >= 1 && modules.Count(x => x.Contains("Simon States")) >= 1 && modules.Count(x => x.Contains("SYNC-125 [3]")) >= 1 && modules.Count(x => x.Contains("Ternary Converter")) >= 1 && modules.Count(x => x.Contains("USA Maze")) >= 1 && modules.Count(x => x.Contains("Astrology")) + modules.Count(x => x.Contains("Battleship")) + modules.Count(x => x.Contains("Not Wiresword")) + modules.Count(x => x.Contains("Painting")) + modules.Count(x => x.Contains("Party Time")) + modules.Count(x => x.Contains("Unrelated Anagrams")) >= 3) level = 49;
        else if (moduleCount == 27 && modules.Count(x => x.Contains("Forget Us Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Answering Can Be Fun")) >= 1 && modules.Count(x => x.Contains("Boggle")) >= 1 && modules.Count(x => x.Contains("Challenge & Contact")) >= 1 && modules.Count(x => x.Contains("Curriculum")) >= 1 && modules.Count(x => x.Contains("Divided Squares")) == 1 && modules.Count(x => x.Contains("Equations X")) >= 1 && modules.Count(x => x.Contains("Flower Patch")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Left and Right")) >= 1 && modules.Count(x => x.Contains("Letter Keys")) >= 1 && modules.Count(x => x.Contains("Lying Indicators")) >= 1 && modules.Count(x => x.Contains("Maintenance")) >= 1 && modules.Count(x => x.Contains("Ordered Keys")) >= 1 && modules.Count(x => x.Contains("Simon Selects")) >= 1 && modules.Count(x => x.Contains("Simon Stages")) >= 1 && modules.Count(x => x.Contains("Skinny Wires")) >= 1 && modules.Count(x => x.Contains("Skyrim")) >= 1 && modules.Count(x => x.Contains("The Sphere")) >= 1 && modules.Count(x => x.Contains("Square Button")) >= 1 && modules.Count(x => x.Contains("Violet Cipher")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("Black Hole")) + modules.Count(x => x.Contains("Braille")) + modules.Count(x => x.Contains("Chess")) + modules.Count(x => x.Contains("Not Keypad")) + modules.Count(x => x.Contains("Yahtzee")) >= 4) level = 50;
        else if (moduleCount == 27 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Binary")) >= 1 && modules.Count(x => x.Contains("Blockbusters")) >= 1 && modules.Count(x => x.Contains("Boolean Maze")) >= 1 && modules.Count(x => x.Contains("Complex Keypad")) >= 1 && modules.Count(x => x.Contains("English Test")) >= 1 && modules.Count(x => x.Contains("FizzBuzz")) >= 1 && modules.Count(x => x.Contains("Flavor Text EX")) >= 1 && modules.Count(x => x.Contains("Game of Life Simple")) >= 1 && modules.Count(x => x.Contains("The Hidden Value")) >= 1 && modules.Count(x => x.Contains("Keypad Combinations")) >= 1 && modules.Count(x => x.Contains("Kilo Talk")) >= 1 && modules.Count(x => x.Contains("Meter")) >= 1 && modules.Count(x => x.Contains("Microcontroller")) >= 1 && modules.Count(x => x.Contains("Neutrinos")) >= 1 && modules.Count(x => x.Contains("Polygons")) >= 1 && modules.Count(x => x.Contains("Reverse Alphabetize")) >= 1 && modules.Count(x => x.Contains("Shell Game")) >= 1 && modules.Count(x => x.Contains("The Stock Market")) >= 1 && modules.Count(x => x.Contains("Street Fighter")) >= 1 && modules.Count(x => x.Contains("Symbol Cycle")) >= 1 && modules.Count(x => x.Contains("Algebra")) + modules.Count(x => x.Contains("Colour Flash")) + modules.Count(x => x.Contains("Double Color")) + modules.Count(x => x.Contains("Faulty Backgrounds")) + modules.Count(x => x.Contains("Mastermind Simple")) + modules.Count(x => x.Contains("Not the Button")) >= 4) level = 51;
        else if (moduleCount == 27 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Alphabet")) >= 1 && modules.Count(x => x.Contains("Art Appreciation")) >= 1 && modules.Count(x => x.Contains("Binary Tree")) >= 1 && modules.Count(x => x.Contains("Button Order")) >= 1 && modules.Count(x => x.Contains("Countdown")) >= 1 && modules.Count(x => x.Contains("Crackbox")) >= 1 && modules.Count(x => x.Contains("The Crystal Maze")) >= 1 && modules.Count(x => x.Contains("Discolored Squares")) >= 1 && modules.Count(x => x.Contains("Human Resources")) >= 1 && modules.Count(x => x.Contains("Hunting")) >= 1 && modules.Count(x => x.Contains("Jaden Smith Talk")) >= 1 && modules.Count(x => x.Contains("Jukebox.WAV")) >= 1 && modules.Count(x => x.Contains("N&Ms")) >= 1 && modules.Count(x => x.Contains("osu!")) >= 1 && modules.Count(x => x.Contains("Simon Simons")) >= 1 && modules.Count(x => x.Contains("Stock Images")) >= 1 && modules.Count(x => x.Contains("Symbolic Password")) >= 1 && modules.Count(x => x.Contains("Unfair Cipher")) >= 1 && modules.Count(x => x.Contains("Weird Al Yankovic")) >= 1 && modules.Count(x => x.Contains("Wire Ordering")) >= 1 && modules.Count(x => x.Contains("Fencing")) + modules.Count(x => x.Contains("Flashing Lights")) + modules.Count(x => x.Contains("LED Encryption")) + modules.Count(x => x.Contains("Mashematics")) + modules.Count(x => x.Contains("The Witness")) + modules.Count(x => x.Contains("Zoni")) >= 4) level = 52;
        else if (moduleCount == 27 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Atbash Cipher")) >= 1 && modules.Count(x => x.Contains("Bob Barks")) >= 1 && modules.Count(x => x.Contains("Burger Alarm")) >= 1 && modules.Count(x => x.Contains("Chinese Counting")) >= 1 && modules.Count(x => x.Contains("DetoNATO")) >= 1 && modules.Count(x => x.Contains("The Digit")) >= 1 && modules.Count(x => x.Contains("Directional Button")) >= 1 && modules.Count(x => x.Contains("European Travel")) >= 1 && modules.Count(x => x.Contains("Five Letter Words")) >= 1 && modules.Count(x => x.Contains("The Jack-O'-Lantern")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Misordered Keys")) >= 1 && modules.Count(x => x.Contains("Monsplode Trading Cards")) >= 1 && modules.Count(x => x.Contains("Neutralization")) >= 1 && modules.Count(x => x.Contains("Not Maze")) >= 1 && modules.Count(x => x.Contains("Orientation Cube")) >= 1 && modules.Count(x => x.Contains("Passcodes")) >= 1 && modules.Count(x => x.Contains("Role Reversal")) >= 1 && modules.Count(x => x.Contains("State of Aggregation")) >= 1 && modules.Count(x => x.Contains("Topsy Turvy")) >= 1 && modules.Count(x => x.Contains("The Colored Maze")) + modules.Count(x => x.Contains("Extended Password")) + modules.Count(x => x.Contains("Grid Matching")) + modules.Count(x => x.Contains("Keypad Combinations")) + modules.Count(x => x.Contains("Simon Screams")) + modules.Count(x => x.Contains("Word Scramble")) >= 4) level = 53;
        else if (moduleCount == 27 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Alchemy")) >= 1 && modules.Count(x => x.Contains("A-maze-ing Buttons")) >= 1 && modules.Count(x => x.Contains("Antichamber")) >= 1 && modules.Count(x => x.Contains("Arrow Talk")) >= 1 && modules.Count(x => x.Contains("Boolean Venn Diagram")) >= 1 && modules.Count(x => x.Contains("Character Codes")) >= 1 && modules.Count(x => x.Contains("Elder Password")) >= 1 && modules.Count(x => x.Contains("The Festive Jukebox")) >= 1 && modules.Count(x => x.Contains("Game of Life Cruel")) >= 1 && modules.Count(x => x.Contains("Heraldry")) >= 1 && modules.Count(x => x.Contains("Instructions")) >= 1 && modules.Count(x => x.Contains("Know Your Way")) >= 1 && modules.Count(x => x.Contains("Logical Buttons")) >= 1 && modules.Count(x => x.Contains("Mad Memory")) >= 1 && modules.Count(x => x.Contains("Quick Arithmetic")) >= 1 && modules.Count(x => x.Contains("Semamorse")) >= 1 && modules.Count(x => x.Contains("Simon Speaks")) >= 1 && modules.Count(x => x.Contains("Snooker")) >= 1 && modules.Count(x => x.Contains("Symbolic Tasha")) >= 1 && modules.Count(x => x.Contains("Conditional Buttons")) >= 1 && modules.Count(x => x.Contains("Memory")) + modules.Count(x => x.Contains("...?")) + modules.Count(x => x.Contains("Colored Squares")) + modules.Count(x => x.Contains("Digital Cipher")) + modules.Count(x => x.Contains("Green Arrows")) + modules.Count(x => x.Contains("Kanji")) >= 4) level = 54;
        else if (moduleCount == 28 && modules.Count(x => x.Contains("The Twin")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("The Black Page")) >= 1 && modules.Count(x => x.Contains("BoozleTalk")) >= 1 && modules.Count(x => x.Contains("Burglar Alarm")) >= 1 && modules.Count(x => x.Contains("Cheap Checkout")) >= 1 && modules.Count(x => x.Contains("Cookie Jars")) == 1 && modules.Count(x => x.Contains("Cruel Digital Root")) >= 1 && modules.Count(x => x.Contains("Divisible Numbers")) >= 1 && modules.Count(x => x.Contains("The iPhone")) >= 1 && modules.Count(x => x.Contains("The Labyrinth")) >= 1 && modules.Count(x => x.Contains("Ladder Lottery")) >= 1 && modules.Count(x => x.Contains("Pattern Lock")) >= 1 && modules.Count(x => x.Contains("Red Cipher")) >= 1 && modules.Count(x => x.Contains("Rock-Paper-Scissors-L.-Sp.")) >= 1 && modules.Count(x => x.Contains("Roger")) >= 1 && modules.Count(x => x.Contains("Rubik's Cube")) >= 1 && modules.Count(x => x.Contains("Seven Wires")) >= 1 && modules.Count(x => x.Contains("Simon Stops")) >= 1 && modules.Count(x => x.Contains("Simon's On First")) >= 1 && modules.Count(x => x.Contains("Tic Tac Toe")) >= 1 && modules.Count(x => x.Contains("Toon Enough")) >= 1 && modules.Count(x => x.Contains("Widdershins")) >= 1 && modules.Count(x => x.Contains("101 Dalmatians")) + modules.Count(x => x.Contains("Bases")) + modules.Count(x => x.Contains("Memorable Buttons")) + modules.Count(x => x.Contains("Probing")) + modules.Count(x => x.Contains("Simon Scrambles")) + modules.Count(x => x.Contains("Uncolored Squares")) >= 4) level = 55;
        else if (moduleCount == 28 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Baba Is Who?")) >= 1 && modules.Count(x => x.Contains("The Block")) >= 1 && modules.Count(x => x.Contains("Colour Code")) >= 1 && modules.Count(x => x.Contains("The Festive Jukebox")) >= 1 && modules.Count(x => x.Contains("Funny Numbers")) >= 1 && modules.Count(x => x.Contains("The Gamepad")) >= 1 && modules.Count(x => x.Contains("Gatekeeper")) >= 1 && modules.Count(x => x.Contains("Going Backwards")) >= 1 && modules.Count(x => x.Contains("Hyperactive Numbers")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Kudosudoku")) >= 1 && modules.Count(x => x.Contains("Lasers")) >= 1 && modules.Count(x => x.Contains("Laundry")) >= 1 && modules.Count(x => x.Contains("Mastermind Cruel")) >= 1 && modules.Count(x => x.Contains("Maze Scrambler")) >= 1 && modules.Count(x => x.Contains("Morsematics")) >= 1 && modules.Count(x => x.Contains("Mortal Kombat")) >= 1 && modules.Count(x => x.Contains("Mystic Square")) >= 1 && modules.Count(x => x.Contains("Name Changer")) >= 1 && modules.Count(x => x.Contains("Roman Art")) >= 1 && modules.Count(x => x.Contains("Round Keypad")) >= 1 && modules.Count(x => x.Contains("Boolean Wires")) + modules.Count(x => x.Contains("Boot Too Big")) + modules.Count(x => x.Contains("Caesar Cipher")) + modules.Count(x => x.Contains("Monsplode, Fight!")) + modules.Count(x => x.Contains("The Rule")) + modules.Count(x => x.Contains("T-Words")) >= 4) level = 56;
        else if (moduleCount == 28 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("3D Maze")) >= 1 && modules.Count(x => x.Contains("Alphabet Numbers")) >= 1 && modules.Count(x => x.Contains("Benedict Cumberbatch")) >= 1 && modules.Count(x => x.Contains("Calendar")) >= 1 && modules.Count(x => x.Contains("Colorful Madness")) >= 1 && modules.Count(x => x.Contains("Crazy Talk With A K")) >= 1 && modules.Count(x => x.Contains("Cryptic Password")) >= 1 && modules.Count(x => x.Contains("Insanagrams")) >= 1 && modules.Count(x => x.Contains("Just Numbers")) >= 1 && modules.Count(x => x.Contains("Lines of Code")) >= 1 && modules.Count(x => x.Contains("Mahjong")) >= 1 && modules.Count(x => x.Contains("Matchematics")) >= 1 && modules.Count(x => x.Contains("Not Wire Sequence")) >= 1 && modules.Count(x => x.Contains("The Number Cipher")) >= 1 && modules.Count(x => x.Contains("Prime Checker")) >= 1 && modules.Count(x => x.Contains("Shikaku")) >= 1 && modules.Count(x => x.Contains("Silly Slots")) >= 1 && modules.Count(x => x.Contains("Sonic & Knuckles")) >= 1 && modules.Count(x => x.Contains("Strike Solve")) >= 1 && modules.Count(x => x.Contains("Text Field")) >= 1 && modules.Count(x => x.Contains("Wonder Cipher")) >= 1 && modules.Count(x => x.Contains("Who's on First")) + modules.Count(x => x.Contains("Corners")) + modules.Count(x => x.Contains("Crazy Talk")) + modules.Count(x => x.Contains("Passwords Translated")) + modules.Count(x => x.Contains("Sonic the Hedgehog")) + modules.Count(x => x.Contains("Symbolic Coordinates")) >= 4) level = 57;
        else if (moduleCount == 28 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("❖")) >= 1 && modules.Count(x => x.Contains("Boolean Keypad")) >= 1 && modules.Count(x => x.Contains("The Button")) >= 1 && modules.Count(x => x.Contains("Chess")) >= 1 && modules.Count(x => x.Contains("Colored Switches")) >= 1 && modules.Count(x => x.Contains("Encrypted Dice")) >= 1 && modules.Count(x => x.Contains("Encrypted Morse")) >= 1 && modules.Count(x => x.Contains("Keywords")) >= 1 && modules.Count(x => x.Contains("Light Bulbs")) >= 1 && modules.Count(x => x.Contains("Lucky Dice")) >= 1 && modules.Count(x => x.Contains("Maritime Flags")) >= 1 && modules.Count(x => x.Contains("Mouse In The Maze")) >= 1 && modules.Count(x => x.Contains("The Necronomicon")) >= 1 && modules.Count(x => x.Contains("Password Generator")) >= 1 && modules.Count(x => x.Contains("Press X")) >= 1 && modules.Count(x => x.Contains("Rainbow Arrows")) >= 1 && modules.Count(x => x.Contains("Red Herring")) >= 1 && modules.Count(x => x.Contains("Standard Crazy Talk")) >= 1 && modules.Count(x => x.Contains("Synonyms")) >= 1 && modules.Count(x => x.Contains("Time Signatures")) >= 1 && modules.Count(x => x.Contains("The Triangle Button")) >= 1 && modules.Count(x => x.Contains("Color Morse")) + modules.Count(x => x.Contains("The Dealmaker")) + modules.Count(x => x.Contains("Modulo")) + modules.Count(x => x.Contains("Rhythms")) + modules.Count(x => x.Contains("The Triangle")) + modules.Count(x => x.Contains("Visual Impairment")) >= 4) level = 58;
        else if (moduleCount == 28 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Abstract Sequences")) >= 1 && modules.Count(x => x.Contains("Big Button Translated")) >= 1 && modules.Count(x => x.Contains("Braille")) >= 1 && modules.Count(x => x.Contains("The Clock")) >= 1 && modules.Count(x => x.Contains("Daylight Directions")) >= 1 && modules.Count(x => x.Contains("Fast Math")) >= 1 && modules.Count(x => x.Contains("Free Parking")) >= 1 && modules.Count(x => x.Contains("Ingredients")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Micro-Modules")) >= 1 && modules.Count(x => x.Contains("Morse War")) >= 1 && modules.Count(x => x.Contains("Mortal Kombat")) >= 1 && modules.Count(x => x.Contains("Old Fogey")) >= 1 && modules.Count(x => x.Contains("Piano Keys")) >= 1 && modules.Count(x => x.Contains("Polyhedral Maze")) >= 1 && modules.Count(x => x.Contains("Quiz Buzz")) >= 1 && modules.Count(x => x.Contains("Reordered Keys")) >= 1 && modules.Count(x => x.Contains("Subways")) >= 1 && modules.Count(x => x.Contains("Switching Maze")) >= 1 && modules.Count(x => x.Contains("Training Text")) >= 1 && modules.Count(x => x.Contains("Triamonds")) >= 1 && modules.Count(x => x.Contains("Wire Sequence")) + modules.Count(x => x.Contains("Addition")) + modules.Count(x => x.Contains("Mega Man 2")) + modules.Count(x => x.Contains("Modules Against Humanity")) + modules.Count(x => x.Contains("The Plunger Button")) + modules.Count(x => x.Contains("Two Bits")) >= 4) level = 59;
        else if (moduleCount == 29 && modules.Count(x => x.Contains("Forget Them All")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Baccarat")) >= 1 && modules.Count(x => x.Contains("Broken Guitar Chords")) >= 1 && modules.Count(x => x.Contains("Colo(u)r Talk")) >= 1 && modules.Count(x => x.Contains("Color Generator")) >= 1 && modules.Count(x => x.Contains("Cursed Double-Oh")) >= 1 && modules.Count(x => x.Contains("Digital Root")) >= 1 && modules.Count(x => x.Contains("Encryption Bingo")) == 1 && modules.Count(x => x.Contains("Follow the Leader")) >= 1 && modules.Count(x => x.Contains("Footnotes")) >= 1 && modules.Count(x => x.Contains("Forget Me Now")) >= 1 && modules.Count(x => x.Contains("Harmony Sequence")) >= 1 && modules.Count(x => x.Contains("Hereditary Base Notation")) >= 1 && modules.Count(x => x.Contains("The Hyperlink")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("KayMazey Talk")) >= 1 && modules.Count(x => x.Contains("Module Maze")) >= 1 && modules.Count(x => x.Contains("Periodic Table")) >= 1 && modules.Count(x => x.Contains("Pie")) >= 1 && modules.Count(x => x.Contains("Placeholder Talk")) >= 1 && modules.Count(x => x.Contains("Poetry")) >= 1 && modules.Count(x => x.Contains("Quaternions")) >= 1 && modules.Count(x => x.Contains("Wire Spaghetti")) >= 1 && modules.Count(x => x.Contains("N&Ms")) + modules.Count(x => x.Contains("Nonogram")) + modules.Count(x => x.Contains("Number Pad")) + modules.Count(x => x.Contains("Numbers")) + modules.Count(x => x.Contains("Switches")) + modules.Count(x => x.Contains("TetraVex")) >= 4) level = 60;
        else if (moduleCount == 29 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Forget It Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("64")) >= 1 && modules.Count(x => x.Contains("3D Tunnels")) >= 1 && modules.Count(x => x.Contains("Alchemy")) >= 1 && modules.Count(x => x.Contains("Codenames")) >= 1 && modules.Count(x => x.Contains("Cruel Digital Root")) >= 1 && modules.Count(x => x.Contains("The Crystal Maze")) >= 1 && modules.Count(x => x.Contains("Dr. Doctor")) >= 1 && modules.Count(x => x.Contains("Follow the Leader")) >= 1 && modules.Count(x => x.Contains("Gridlock")) >= 1 && modules.Count(x => x.Contains("Minecraft Parody")) >= 1 && modules.Count(x => x.Contains("Modulus Manipulation")) >= 1 && modules.Count(x => x.Contains("Morse War")) >= 1 && modules.Count(x => x.Contains("Morse-A-Maze")) >= 1 && modules.Count(x => x.Contains("Partial Derivatives")) >= 1 && modules.Count(x => x.Contains("Pigpen Cycle")) >= 1 && modules.Count(x => x.Contains("Playfair Cycle")) >= 1 && modules.Count(x => x.Contains("Quintuples")) >= 1 && modules.Count(x => x.Contains("Reordered Keys")) >= 1 && modules.Count(x => x.Contains("Simon Speaks")) >= 1 && modules.Count(x => x.Contains("SYNC-125 [3]")) >= 1 && modules.Count(x => x.Contains("Type Racer")) >= 1 && modules.Count(x => x.Contains("Label Priorities")) >= 1 && modules.Count(x => x.Contains("The Clock")) + modules.Count(x => x.Contains("Dimension Disruption")) + modules.Count(x => x.Contains("Flash Memory")) + modules.Count(x => x.Contains("Not the Button")) + modules.Count(x => x.Contains("Shape Shift")) >= 3) level = 61;
        else if (moduleCount == 29 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Forget It Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("15 Mystic Lights")) >= 1 && modules.Count(x => x.Contains("Benedict Cumberbatch")) >= 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("Complicated Buttons")) >= 1 && modules.Count(x => x.Contains("Equations")) >= 1 && modules.Count(x => x.Contains("Fast Math")) >= 1 && modules.Count(x => x.Contains("Four-Card Monte")) == 1 && modules.Count(x => x.Contains("Green Cipher")) >= 1 && modules.Count(x => x.Contains("Left and Right")) >= 1 && modules.Count(x => x.Contains("Lightspeed")) >= 1 && modules.Count(x => x.Contains("Maritime Flags")) >= 1 && modules.Count(x => x.Contains("Not Who's on First")) >= 1 && modules.Count(x => x.Contains("Number Nimbleness")) >= 1 && modules.Count(x => x.Contains("Numbers")) >= 1 && modules.Count(x => x.Contains("Ordered Keys")) >= 1 && modules.Count(x => x.Contains("Pattern Cube")) >= 1 && modules.Count(x => x.Contains("Perplexing Wires")) >= 1 && modules.Count(x => x.Contains("Question Mark")) >= 1 && modules.Count(x => x.Contains("Reverse Morse")) >= 1 && modules.Count(x => x.Contains("Scripting")) >= 1 && modules.Count(x => x.Contains("Switching Maze")) >= 1 && modules.Count(x => x.Contains("Tetriamonds")) >= 1 && modules.Count(x => x.Contains("Double-Oh")) + modules.Count(x => x.Contains("Insane Talk")) + modules.Count(x => x.Contains("Ladder Lottery")) + modules.Count(x => x.Contains("Modulo")) + modules.Count(x => x.Contains("Not Simaze")) >= 3) level = 62;
        else if (moduleCount == 29 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Forget It Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Audio Morse")) >= 1 && modules.Count(x => x.Contains("Blue Arrows")) >= 1 && modules.Count(x => x.Contains("Button Sequence")) >= 1 && modules.Count(x => x.Contains("Cheep Checkout")) >= 1 && modules.Count(x => x.Contains("Colour Code")) >= 1 && modules.Count(x => x.Contains("Cruel Piano Keys")) >= 1 && modules.Count(x => x.Contains("The Deck of Many Things")) >= 1 && modules.Count(x => x.Contains("Disordered Keys")) >= 1 && modules.Count(x => x.Contains("Flashing Lights")) >= 1 && modules.Count(x => x.Contains("Functions")) >= 1 && modules.Count(x => x.Contains("Know Your Way")) >= 1 && modules.Count(x => x.Contains("The Labyrinth")) >= 1 && modules.Count(x => x.Contains("Masyu")) >= 1 && modules.Count(x => x.Contains("Minesweeper")) >= 1 && modules.Count(x => x.Contains("Neutralization")) >= 1 && modules.Count(x => x.Contains("Neutrinos")) >= 1 && modules.Count(x => x.Contains("The Plunger Button")) >= 1 && modules.Count(x => x.Contains("Prime Encryption")) >= 1 && modules.Count(x => x.Contains("The Radio")) >= 1 && modules.Count(x => x.Contains("Siffron")) >= 1 && modules.Count(x => x.Contains("Silly Slots")) >= 1 && modules.Count(x => x.Contains("Timing is Everything")) == 1 && modules.Count(x => x.Contains("Maze")) + modules.Count(x => x.Contains("The Code")) + modules.Count(x => x.Contains("Matchematics")) + modules.Count(x => x.Contains("Word Scramble")) + modules.Count(x => x.Contains("Word Search")) >= 3) level = 63;
        else if (moduleCount == 29 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Forget It Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("FizzBuzz")) >= 1 && modules.Count(x => x.Contains("Flags")) >= 1 && modules.Count(x => x.Contains("Horrible Memory")) >= 1 && modules.Count(x => x.Contains("Hunting")) >= 1 && modules.Count(x => x.Contains("IKEA")) >= 1 && modules.Count(x => x.Contains("The Jack-O'-Lantern")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Mastermind Cruel")) >= 1 && modules.Count(x => x.Contains("Melody Sequencer")) >= 1 && modules.Count(x => x.Contains("The Modkit")) >= 1 && modules.Count(x => x.Contains("Morsematics")) >= 1 && modules.Count(x => x.Contains("Mystic Square")) >= 1 && modules.Count(x => x.Contains("Not Complicated Wires")) >= 1 && modules.Count(x => x.Contains("Recolored Switches")) >= 1 && modules.Count(x => x.Contains("RGB Maze")) >= 1 && modules.Count(x => x.Contains("Simon Samples")) >= 1 && modules.Count(x => x.Contains("Simon Shrieks")) >= 1 && modules.Count(x => x.Contains("Stars")) >= 1 && modules.Count(x => x.Contains("Synchronization")) >= 1 && modules.Count(x => x.Contains("Turtle Robot")) >= 1 && modules.Count(x => x.Contains("V")) >= 1 && modules.Count(x => x.Contains("Varicolored Squares")) >= 1 && modules.Count(x => x.Contains("Funny Numbers")) + modules.Count(x => x.Contains("Not Keypad")) + modules.Count(x => x.Contains("Not Maze")) + modules.Count(x => x.Contains("Prime Checker")) + modules.Count(x => x.Contains("Simon States")) >= 3) level = 64;
        else if (moduleCount == 30 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Forget Everything")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("The Black Page")) >= 1 && modules.Count(x => x.Contains("Boolean Wires")) >= 1 && modules.Count(x => x.Contains("Chord Progressions")) >= 1 && modules.Count(x => x.Contains("Color Morse")) >= 1 && modules.Count(x => x.Contains("Colorful Dials")) >= 1 && modules.Count(x => x.Contains("Decolored Squares")) >= 1 && modules.Count(x => x.Contains("Digital Dials")) >= 1 && modules.Count(x => x.Contains("Discolored Squares")) >= 1 && modules.Count(x => x.Contains("Extended Password")) >= 1 && modules.Count(x => x.Contains("Gray Cipher")) >= 1 && modules.Count(x => x.Contains("Kudosudoku")) >= 1 && modules.Count(x => x.Contains("Light Cycle")) >= 1 && modules.Count(x => x.Contains("Minecraft Cipher")) >= 1 && modules.Count(x => x.Contains("Monsplode Trading Cards")) >= 1 && modules.Count(x => x.Contains("Risky Wires")) >= 1 && modules.Count(x => x.Contains("Snooker")) >= 1 && modules.Count(x => x.Contains("Spinning Buttons")) >= 1 && modules.Count(x => x.Contains("Splitting The Loot")) >= 1 && modules.Count(x => x.Contains("TetraVex")) >= 1 && modules.Count(x => x.Contains("Third Base")) >= 1 && modules.Count(x => x.Contains("Westeros")) >= 1 && modules.Count(x => x.Contains("Zoni")) >= 1 && modules.Count(x => x.Contains("Rhythms")) >= 1 && modules.Count(x => x.Contains("S.E.T.")) + modules.Count(x => x.Contains("Simon Scrambles")) + modules.Count(x => x.Contains("Square Button")) + modules.Count(x => x.Contains("Stars")) + modules.Count(x => x.Contains("Switches")) >= 3) level = 65;
        else if (moduleCount == 30 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Übermodule")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Adjacent Letters")) >= 1 && modules.Count(x => x.Contains("Black Cipher")) >= 1 && modules.Count(x => x.Contains("Brush Strokes")) >= 1 && modules.Count(x => x.Contains("Christmas Presents")) >= 1 && modules.Count(x => x.Contains("Color Addition")) >= 1 && modules.Count(x => x.Contains("Colorful Madness")) >= 1 && modules.Count(x => x.Contains("Colour Flash PL")) >= 1 && modules.Count(x => x.Contains("Cruel Boolean Maze")) >= 1 && modules.Count(x => x.Contains("Daylight Directions")) >= 1 && modules.Count(x => x.Contains("The Dealmaker")) >= 1 && modules.Count(x => x.Contains("Double Expert")) >= 1 && modules.Count(x => x.Contains("egg")) >= 1 && modules.Count(x => x.Contains("Hinges")) >= 1 && modules.Count(x => x.Contains("The Hyperlink")) >= 1 && modules.Count(x => x.Contains("Module Listening")) >= 1 && modules.Count(x => x.Contains("The Necronomicon")) >= 1 && modules.Count(x => x.Contains("Orange Cipher")) >= 1 && modules.Count(x => x.Contains("Painting")) >= 1 && modules.Count(x => x.Contains("Periodic Table")) >= 1 && modules.Count(x => x.Contains("Plumbing")) >= 1 && modules.Count(x => x.Contains("Tap Code")) >= 1 && modules.Count(x => x.Contains("Thinking Wires")) >= 1 && modules.Count(x => x.Contains("Turn The Key")) == 1 && modules.Count(x => x.Contains("Big Button Translated")) + modules.Count(x => x.Contains("Blind Alley")) + modules.Count(x => x.Contains("Colour Flash")) + modules.Count(x => x.Contains("Corners")) + modules.Count(x => x.Contains("Crazy Talk")) >= 3) level = 66;
        else if (moduleCount == 30 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Übermodule")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Affine Cycle")) >= 1 && modules.Count(x => x.Contains("Burglar Alarm")) >= 1 && modules.Count(x => x.Contains("Character Codes")) >= 1 && modules.Count(x => x.Contains("Combination Lock")) >= 1 && modules.Count(x => x.Contains("The Cube")) >= 1 && modules.Count(x => x.Contains("DetoNATO")) >= 1 && modules.Count(x => x.Contains("Double Arrows")) >= 1 && modules.Count(x => x.Contains("European Travel")) >= 1 && modules.Count(x => x.Contains("Guess Who?")) >= 1 && modules.Count(x => x.Contains("Jumble Cycle")) >= 1 && modules.Count(x => x.Contains("LED Math")) >= 1 && modules.Count(x => x.Contains("Logic")) >= 1 && modules.Count(x => x.Contains("Modern Cipher")) >= 1 && modules.Count(x => x.Contains("Nonogram")) >= 1 && modules.Count(x => x.Contains("Point of Order")) >= 1 && modules.Count(x => x.Contains("Purple Arrows")) >= 1 && modules.Count(x => x.Contains("Quote Crazy Talk End Quote")) >= 1 && modules.Count(x => x.Contains("Raiding Temples")) >= 1 && modules.Count(x => x.Contains("Rubik's Cube")) >= 1 && modules.Count(x => x.Contains("Schlag den Bomb")) >= 1 && modules.Count(x => x.Contains("Simon Sends")) >= 1 && modules.Count(x => x.Contains("Transmitted Morse")) >= 1 && modules.Count(x => x.Contains("The World's Largest Button")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("The Bulb")) + modules.Count(x => x.Contains("Letter Keys")) + modules.Count(x => x.Contains("Orange Arrows")) + modules.Count(x => x.Contains("Party Time")) >= 3) level = 67;
        else if (moduleCount == 30 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Übermodule")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Big Circle")) >= 1 && modules.Count(x => x.Contains("Bitwise Operations")) >= 1 && modules.Count(x => x.Contains("Color Math")) >= 1 && modules.Count(x => x.Contains("Colorful Insanity")) >= 1 && modules.Count(x => x.Contains("Constellations")) >= 1 && modules.Count(x => x.Contains("Crackbox")) >= 1 && modules.Count(x => x.Contains("Cruel Countdown")) >= 1 && modules.Count(x => x.Contains("Elder Futhark")) >= 1 && modules.Count(x => x.Contains("Factory Maze")) >= 1 && modules.Count(x => x.Contains("The High Score")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Memorable Buttons")) >= 1 && modules.Count(x => x.Contains("Mineseeker")) >= 1 && modules.Count(x => x.Contains("Tasha Squeals")) >= 1 && modules.Count(x => x.Contains("Ten-Button Color Code")) >= 1 && modules.Count(x => x.Contains("Topsy Turvy")) >= 1 && modules.Count(x => x.Contains("Treasure Hunt")) >= 1 && modules.Count(x => x.Contains("Vcrcs")) >= 1 && modules.Count(x => x.Contains("Vigenère Cipher")) >= 1 && modules.Count(x => x.Contains("Waste Management")) >= 1 && modules.Count(x => x.Contains("Widdershins")) >= 1 && modules.Count(x => x.Contains("The Wire")) >= 1 && modules.Count(x => x.Contains("The Witness")) >= 1 && modules.Count(x => x.Contains("❖")) + modules.Count(x => x.Contains("Alphabet")) + modules.Count(x => x.Contains("Colored Squares")) + modules.Count(x => x.Contains("Subways")) + modules.Count(x => x.Contains("Text Field")) >= 3) level = 68;
        else if (moduleCount == 30 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Übermodule")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("A Mistake")) >= 1 && modules.Count(x => x.Contains("Binary Tree")) >= 1 && modules.Count(x => x.Contains("Chess")) >= 1 && modules.Count(x => x.Contains("The Code")) >= 1 && modules.Count(x => x.Contains("The cRule")) >= 1 && modules.Count(x => x.Contains("Etterna")) >= 1 && modules.Count(x => x.Contains("Exoplanets")) >= 1 && modules.Count(x => x.Contains("Friendship")) >= 1 && modules.Count(x => x.Contains("Funny Numbers")) >= 1 && modules.Count(x => x.Contains("Game of Life Simple")) >= 1 && modules.Count(x => x.Contains("Hidden Colors")) >= 1 && modules.Count(x => x.Contains("Hill Cycle")) >= 1 && modules.Count(x => x.Contains("Laundry")) >= 1 && modules.Count(x => x.Contains("Not Morse Code")) >= 1 && modules.Count(x => x.Contains("Numbers")) >= 1 && modules.Count(x => x.Contains("Playfair Cipher")) >= 1 && modules.Count(x => x.Contains("Silly Slots")) >= 1 && modules.Count(x => x.Contains("Simon Screams")) >= 1 && modules.Count(x => x.Contains("Simon Selects")) >= 1 && modules.Count(x => x.Contains("Skewed Slots")) >= 1 && modules.Count(x => x.Contains("The Sun")) >= 1 && modules.Count(x => x.Contains("The Time Keeper")) == 1 && modules.Count(x => x.Contains("Unordered Keys")) >= 1 && modules.Count(x => x.Contains("Password")) + modules.Count(x => x.Contains("Anagrams")) + modules.Count(x => x.Contains("Calendar")) + modules.Count(x => x.Contains("N&Ms")) + modules.Count(x => x.Contains("The Simpleton")) >= 3) level = 69;
        else if (moduleCount == 31 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Forget Me Later")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("...?")) >= 1 && modules.Count(x => x.Contains("Alliances")) >= 1 && modules.Count(x => x.Contains("Alphabet")) >= 1 && modules.Count(x => x.Contains("Answering Can Be Fun")) >= 1 && modules.Count(x => x.Contains("Bamboozling Button")) >= 1 && modules.Count(x => x.Contains("Chinese Counting")) >= 1 && modules.Count(x => x.Contains("Colored Keys")) >= 1 && modules.Count(x => x.Contains("Colored Switches")) >= 1 && modules.Count(x => x.Contains("Faulty Backgrounds")) >= 1 && modules.Count(x => x.Contains("Game of Life Cruel")) >= 1 && modules.Count(x => x.Contains("The iPhone")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Jukebox.WAV")) >= 1 && modules.Count(x => x.Contains("Kooky Keypad")) >= 1 && modules.Count(x => x.Contains("LEGOs")) >= 1 && modules.Count(x => x.Contains("Logic Gates")) >= 1 && modules.Count(x => x.Contains("Maintenance")) >= 1 && modules.Count(x => x.Contains("Mastermind Cruel")) >= 1 && modules.Count(x => x.Contains("Minesweeper")) >= 1 && modules.Count(x => x.Contains("Simon Stages")) >= 1 && modules.Count(x => x.Contains("Simon Stops")) >= 1 && modules.Count(x => x.Contains("Stained Glass")) >= 1 && modules.Count(x => x.Contains("Tax Returns")) >= 1 && modules.Count(x => x.Contains("The Troll")) == 1 && modules.Count(x => x.Contains("Keypad")) + modules.Count(x => x.Contains("Wire Sequence")) + modules.Count(x => x.Contains("Light Bulbs")) + modules.Count(x => x.Contains("The Switch")) + modules.Count(x => x.Contains("Turn The Keys")) >= 3) level = 70;
        else if (moduleCount == 31 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Forget Infinity")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("% Grey")) >= 1 && modules.Count(x => x.Contains("Burger Alarm")) >= 1 && modules.Count(x => x.Contains("Calculus")) >= 1 && modules.Count(x => x.Contains("Color Decoding")) >= 1 && modules.Count(x => x.Contains("Deck Creating")) >= 1 && modules.Count(x => x.Contains("Fast Math")) >= 1 && modules.Count(x => x.Contains("The Hexabutton")) >= 1 && modules.Count(x => x.Contains("Hieroglyphics")) >= 1 && modules.Count(x => x.Contains("Mafia")) >= 1 && modules.Count(x => x.Contains("Marble Tumble")) >= 1 && modules.Count(x => x.Contains("osu!")) >= 1 && modules.Count(x => x.Contains("Pigpen Rotations")) >= 1 && modules.Count(x => x.Contains("Planets")) >= 1 && modules.Count(x => x.Contains("Qwirkle")) >= 1 && modules.Count(x => x.Contains("Rock-Paper-Scissors-L.-Sp.")) >= 1 && modules.Count(x => x.Contains("Roger")) >= 1 && modules.Count(x => x.Contains("Roman Art")) >= 1 && modules.Count(x => x.Contains("Rubik’s Clock")) >= 1 && modules.Count(x => x.Contains("Shapes And Bombs")) >= 1 && modules.Count(x => x.Contains("Sonic & Knuckles")) >= 1 && modules.Count(x => x.Contains("The Stopwatch")) >= 1 && modules.Count(x => x.Contains("Two Bits")) >= 1 && modules.Count(x => x.Contains("Unown Cipher")) >= 1 && modules.Count(x => x.Contains("Creation")) >= 1 && modules.Count(x => x.Contains("Fruits")) + modules.Count(x => x.Contains("Green Arrows")) + modules.Count(x => x.Contains("LED Encryption")) + modules.Count(x => x.Contains("LED Grid")) + modules.Count(x => x.Contains("Red Arrows")) >= 3) level = 71;
        else if (moduleCount == 31 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Forget Infinity")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Adventure Game")) >= 1 && modules.Count(x => x.Contains("Blind Maze")) >= 1 && modules.Count(x => x.Contains("Bordered Keys")) >= 1 && modules.Count(x => x.Contains("Catchphrase")) >= 1 && modules.Count(x => x.Contains("Challenge & Contact")) >= 1 && modules.Count(x => x.Contains("Chord Qualities")) >= 1 && modules.Count(x => x.Contains("Digital Cipher")) >= 1 && modules.Count(x => x.Contains("Dominoes")) >= 1 && modules.Count(x => x.Contains("Dragon Energy")) >= 1 && modules.Count(x => x.Contains("Festive Piano Keys")) >= 1 && modules.Count(x => x.Contains("Five Letter Words")) >= 1 && modules.Count(x => x.Contains("Flower Patch")) >= 1 && modules.Count(x => x.Contains("Greek Calculus")) >= 1 && modules.Count(x => x.Contains("Iconic")) == 1 && modules.Count(x => x.Contains("Krazy Talk")) >= 1 && modules.Count(x => x.Contains("Lying Indicators")) >= 1 && modules.Count(x => x.Contains("The Moon")) >= 1 && modules.Count(x => x.Contains("Mouse In The Maze")) >= 1 && modules.Count(x => x.Contains("Negativity")) >= 1 && modules.Count(x => x.Contains("Recorded Keys")) >= 1 && modules.Count(x => x.Contains("Simon Sings")) >= 1 && modules.Count(x => x.Contains("Sticky Notes")) >= 1 && modules.Count(x => x.Contains("Stock Images")) >= 1 && modules.Count(x => x.Contains("Uncolored Switches")) >= 1 && modules.Count(x => x.Contains("Simon Says")) + modules.Count(x => x.Contains("Astrology")) + modules.Count(x => x.Contains("Colo(u)r Talk")) + modules.Count(x => x.Contains("Divisible Numbers")) + modules.Count(x => x.Contains("Double Color")) >= 3) level = 72;
        else if (moduleCount == 31 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Forget Infinity")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("1D Maze")) >= 1 && modules.Count(x => x.Contains("Alphabetical Ruling")) >= 1 && modules.Count(x => x.Contains("Binary Puzzle")) >= 1 && modules.Count(x => x.Contains("Broken Guitar Chords")) >= 1 && modules.Count(x => x.Contains("Caesar's Maths")) >= 1 && modules.Count(x => x.Contains("Cursed Double-Oh")) >= 1 && modules.Count(x => x.Contains("Flags")) >= 1 && modules.Count(x => x.Contains("Font Select")) >= 1 && modules.Count(x => x.Contains("Greek Letter Grid")) >= 1 && modules.Count(x => x.Contains("Hold Ups")) >= 1 && modules.Count(x => x.Contains("The Hypercube")) >= 1 && modules.Count(x => x.Contains("Instructions")) >= 1 && modules.Count(x => x.Contains("Kudosudoku")) >= 1 && modules.Count(x => x.Contains("Langton's Ant")) >= 1 && modules.Count(x => x.Contains("Module Homework")) >= 1 && modules.Count(x => x.Contains("Murder")) >= 1 && modules.Count(x => x.Contains("Natures")) >= 1 && modules.Count(x => x.Contains("Not Wire Sequence")) >= 1 && modules.Count(x => x.Contains("Pie")) >= 1 && modules.Count(x => x.Contains("Purgatory")) == 1 && modules.Count(x => x.Contains("Unfair Cipher")) >= 1 && modules.Count(x => x.Contains("X01")) >= 1 && modules.Count(x => x.Contains("Yahtzee")) >= 1 && modules.Count(x => x.Contains("Wavetapping")) >= 1 && modules.Count(x => x.Contains("Complicated Wires")) + modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("Wire Sequence")) + modules.Count(x => x.Contains("Skinny Wires")) + modules.Count(x => x.Contains("Wire Placement")) >= 3) level = 73;
        else if (moduleCount == 31 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Forget Infinity")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Boxing")) >= 1 && modules.Count(x => x.Contains("Cheep Checkout")) >= 1 && modules.Count(x => x.Contains("Connection Check")) >= 1 && modules.Count(x => x.Contains("Cooking")) >= 1 && modules.Count(x => x.Contains("Elder Password")) >= 1 && modules.Count(x => x.Contains("Following Orders")) >= 1 && modules.Count(x => x.Contains("Going Backwards")) >= 1 && modules.Count(x => x.Contains("The High Score")) >= 1 && modules.Count(x => x.Contains("Ice Cream")) >= 1 && modules.Count(x => x.Contains("The Legendre Symbol")) >= 1 && modules.Count(x => x.Contains("The Matrix")) >= 1 && modules.Count(x => x.Contains("Not Memory")) >= 1 && modules.Count(x => x.Contains("Old Fogey")) >= 1 && modules.Count(x => x.Contains("Quaternions")) >= 1 && modules.Count(x => x.Contains("Quiz Buzz")) >= 1 && modules.Count(x => x.Contains("Settlers of KTaNE")) >= 1 && modules.Count(x => x.Contains("Shikaku")) >= 1 && modules.Count(x => x.Contains("Signals")) >= 1 && modules.Count(x => x.Contains("Sonic the Hedgehog")) >= 1 && modules.Count(x => x.Contains("Sorting")) >= 1 && modules.Count(x => x.Contains("Superlogic")) >= 1 && modules.Count(x => x.Contains("USA Maze")) >= 1 && modules.Count(x => x.Contains("Vectors")) >= 1 && modules.Count(x => x.Contains("Web Design")) >= 1 && modules.Count(x => x.Contains("Memory")) + modules.Count(x => x.Contains("Complex Keypad")) + modules.Count(x => x.Contains("Faulty Digital Root")) + modules.Count(x => x.Contains("Not Wiresword")) + modules.Count(x => x.Contains("Sink")) >= 3) level = 74;
        else if (moduleCount == 31 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Forget Us Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Algebra")) >= 1 && modules.Count(x => x.Contains("Alpha-Bits")) >= 1 && modules.Count(x => x.Contains("Boolean Maze")) >= 1 && modules.Count(x => x.Contains("The Clock")) >= 1 && modules.Count(x => x.Contains("The Digit")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Left and Right")) >= 1 && modules.Count(x => x.Contains("Lockpick Maze")) >= 1 && modules.Count(x => x.Contains("Mad Memory")) >= 1 && modules.Count(x => x.Contains("Maritime Flags")) >= 1 && modules.Count(x => x.Contains("Maze³")) >= 1 && modules.Count(x => x.Contains("Microcontroller")) >= 1 && modules.Count(x => x.Contains("Modern Cipher")) >= 1 && modules.Count(x => x.Contains("The Number Cipher")) >= 1 && modules.Count(x => x.Contains("Only Connect")) >= 1 && modules.Count(x => x.Contains("Poker")) >= 1 && modules.Count(x => x.Contains("Red Buttons")) >= 1 && modules.Count(x => x.Contains("Red Cipher")) >= 1 && modules.Count(x => x.Contains("Shifting Maze")) >= 1 && modules.Count(x => x.Contains("Skewed Slots")) >= 1 && modules.Count(x => x.Contains("The Sphere")) >= 1 && modules.Count(x => x.Contains("Text Field")) >= 1 && modules.Count(x => x.Contains("Ultimate Custom Night")) == 1 && modules.Count(x => x.Contains("Unicode")) >= 1 && modules.Count(x => x.Contains("Black Hole")) + modules.Count(x => x.Contains("Caesar Cipher")) + modules.Count(x => x.Contains("Emoji Math")) + modules.Count(x => x.Contains("Insanagrams")) + modules.Count(x => x.Contains("Not Complicated Wires")) >= 3) level = 75;
        else if (moduleCount == 32 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Brainf---")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Battleship")) >= 1 && modules.Count(x => x.Contains("British Slang")) >= 1 && modules.Count(x => x.Contains("Caesar Cycle")) >= 1 && modules.Count(x => x.Contains("Cryptography")) >= 1 && modules.Count(x => x.Contains("Gadgetron Vendor")) >= 1 && modules.Count(x => x.Contains("Gryphons")) >= 1 && modules.Count(x => x.Contains("The Jukebox")) >= 1 && modules.Count(x => x.Contains("Malfunctions")) >= 1 && modules.Count(x => x.Contains("Misordered Keys")) >= 1 && modules.Count(x => x.Contains("Round Keypad")) >= 1 && modules.Count(x => x.Contains("Rubik’s Clock")) >= 1 && modules.Count(x => x.Contains("The Rule")) >= 1 && modules.Count(x => x.Contains("Sea Shells")) >= 1 && modules.Count(x => x.Contains("Semamorse")) >= 1 && modules.Count(x => x.Contains("Seven Choose Four")) >= 1 && modules.Count(x => x.Contains("Shell Game")) >= 1 && modules.Count(x => x.Contains("Simon Spins")) >= 1 && modules.Count(x => x.Contains("Skyrim")) >= 1 && modules.Count(x => x.Contains("Street Fighter")) >= 1 && modules.Count(x => x.Contains("Tennis")) >= 1 && modules.Count(x => x.Contains("Ternary Converter")) >= 1 && modules.Count(x => x.Contains("The Triangle Button")) >= 1 && modules.Count(x => x.Contains("Violet Cipher")) >= 1 && modules.Count(x => x.Contains("Yellow Arrows")) >= 1 && modules.Count(x => x.Contains("Morse Code")) + modules.Count(x => x.Contains("1000 Words")) + modules.Count(x => x.Contains("Faulty Sink")) + modules.Count(x => x.Contains("The Festive Jukebox")) + modules.Count(x => x.Contains("Orientation Cube")) + modules.Count(x => x.Contains("Passwords Translated")) >= 4) level = 76;
        else if (moduleCount == 32 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Brainf---")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Encrypted Dice")) >= 1 && modules.Count(x => x.Contains("Encrypted Morse")) >= 1 && modules.Count(x => x.Contains("Friendship")) >= 1 && modules.Count(x => x.Contains("The Gamepad")) >= 1 && modules.Count(x => x.Contains("Graffiti Numbers")) >= 1 && modules.Count(x => x.Contains("The Hangover")) >= 1 && modules.Count(x => x.Contains("Hexamaze")) >= 1 && modules.Count(x => x.Contains("The Hidden Value")) >= 1 && modules.Count(x => x.Contains("Homophones")) >= 1 && modules.Count(x => x.Contains("Indigo Cipher")) >= 1 && modules.Count(x => x.Contains("Keypad Lock")) >= 1 && modules.Count(x => x.Contains("Lion’s Share")) >= 1 && modules.Count(x => x.Contains("Listening")) >= 1 && modules.Count(x => x.Contains("The London Underground")) >= 1 && modules.Count(x => x.Contains("Module Maze")) >= 1 && modules.Count(x => x.Contains("NumberWang")) >= 1 && modules.Count(x => x.Contains("Placeholder Talk")) >= 1 && modules.Count(x => x.Contains("Probing")) >= 1 && modules.Count(x => x.Contains("Robot Programming")) >= 1 && modules.Count(x => x.Contains("Simon Screams")) >= 1 && modules.Count(x => x.Contains("Stack'em")) >= 1 && modules.Count(x => x.Contains("Symbolic Colouring")) >= 1 && modules.Count(x => x.Contains("Symbolic Coordinates")) >= 1 && modules.Count(x => x.Contains("Visual Impairment")) >= 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("Addition")) + modules.Count(x => x.Contains("Etterna")) + modules.Count(x => x.Contains("Flavor Text")) + modules.Count(x => x.Contains("Foreign Exchange Rates")) + modules.Count(x => x.Contains("Switches")) >= 4) level = 77;
        else if (moduleCount == 32 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Brainf---")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("3D Maze")) >= 1 && modules.Count(x => x.Contains("Answering Can Be Fun")) >= 1 && modules.Count(x => x.Contains("Bitmaps")) >= 1 && modules.Count(x => x.Contains("Bomb Diffusal")) >= 1 && modules.Count(x => x.Contains("Chicken Nuggets")) >= 1 && modules.Count(x => x.Contains("The Clock")) >= 1 && modules.Count(x => x.Contains("Complicated Buttons")) >= 1 && modules.Count(x => x.Contains("Cryptic Password")) >= 1 && modules.Count(x => x.Contains("Follow the Leader")) >= 1 && modules.Count(x => x.Contains("Microphone")) >= 1 && modules.Count(x => x.Contains("Module Movements")) >= 1 && modules.Count(x => x.Contains("Morse Buttons")) >= 1 && modules.Count(x => x.Contains("Number Pad")) >= 1 && modules.Count(x => x.Contains("Object Shows")) >= 1 && modules.Count(x => x.Contains("Painting")) >= 1 && modules.Count(x => x.Contains("Palindromes")) >= 1 && modules.Count(x => x.Contains("Polyhedral Maze")) >= 1 && modules.Count(x => x.Contains("Press X")) >= 1 && modules.Count(x => x.Contains("Random Access Memory")) == 1 && modules.Count(x => x.Contains("Reordered Keys")) >= 1 && modules.Count(x => x.Contains("Resistors")) >= 1 && modules.Count(x => x.Contains("Uncolored Squares")) >= 1 && modules.Count(x => x.Contains("Wire Spaghetti")) >= 1 && modules.Count(x => x.Contains("X-Ray")) >= 1 && modules.Count(x => x.Contains("Who's on First")) + modules.Count(x => x.Contains("Digital Root")) + modules.Count(x => x.Contains("Mastermind Simple")) + modules.Count(x => x.Contains("Masyu")) + modules.Count(x => x.Contains("Red Arrows")) + modules.Count(x => x.Contains("The Witness")) >= 4) level = 78;
        else if (moduleCount == 33 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Brainf---")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Badugi")) >= 1 && modules.Count(x => x.Contains("Bartending")) >= 1 && modules.Count(x => x.Contains("Binary LEDs")) >= 1 && modules.Count(x => x.Contains("Blackjack")) >= 1 && modules.Count(x => x.Contains("Blue Arrows")) >= 1 && modules.Count(x => x.Contains("Character Shift")) >= 1 && modules.Count(x => x.Contains("Decolored Squares")) >= 1 && modules.Count(x => x.Contains("Fencing")) >= 1 && modules.Count(x => x.Contains("Forget Me Now")) >= 1 && modules.Count(x => x.Contains("Grocery Store")) >= 1 && modules.Count(x => x.Contains("Ice Cream")) >= 1 && modules.Count(x => x.Contains("Ingredients")) >= 1 && modules.Count(x => x.Contains("Jenga")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Logical Buttons")) >= 1 && modules.Count(x => x.Contains("Lombax Cubes")) >= 1 && modules.Count(x => x.Contains("Mahjong")) >= 1 && modules.Count(x => x.Contains("Mega Man 2")) >= 1 && modules.Count(x => x.Contains("Micro-Modules")) >= 1 && modules.Count(x => x.Contains("Only Connect")) >= 1 && modules.Count(x => x.Contains("Regular Crazy Talk")) >= 1 && modules.Count(x => x.Contains("Reverse Polish Notation")) >= 1 && modules.Count(x => x.Contains("Simon Sends")) >= 1 && modules.Count(x => x.Contains("Simon's On First")) >= 1 && modules.Count(x => x.Contains("Switching Maze")) >= 1 && modules.Count(x => x.Contains("LED Grid")) + modules.Count(x => x.Contains("Listening")) + modules.Count(x => x.Contains("Number Cipher")) + modules.Count(x => x.Contains("Semaphore")) + modules.Count(x => x.Contains("Synonyms")) + modules.Count(x => x.Contains("Widdershins")) >= 4) level = 79;
        else if (moduleCount == 33 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("RPS Judging")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Accelerando")) >= 1 && modules.Count(x => x.Contains("Astrology")) >= 1 && modules.Count(x => x.Contains("Chord Progressions")) >= 1 && modules.Count(x => x.Contains("Dr. Doctor")) >= 1 && modules.Count(x => x.Contains("Elder Futhark")) >= 1 && modules.Count(x => x.Contains("Following Orders")) >= 1 && modules.Count(x => x.Contains("Functions")) >= 1 && modules.Count(x => x.Contains("Graffiti Numbers")) >= 1 && modules.Count(x => x.Contains("Guitar Chords")) >= 1 && modules.Count(x => x.Contains("The Hexabutton")) >= 1 && modules.Count(x => x.Contains("IKEA")) >= 1 && modules.Count(x => x.Contains("Indigo Cipher")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Krazy Talk")) >= 1 && modules.Count(x => x.Contains("Logic Gates")) >= 1 && modules.Count(x => x.Contains("Loopover")) >= 1 && modules.Count(x => x.Contains("Minesweeper")) >= 1 && modules.Count(x => x.Contains("Mystic Square")) >= 1 && modules.Count(x => x.Contains("Nonogram")) >= 1 && modules.Count(x => x.Contains("Purple Arrows")) >= 1 && modules.Count(x => x.Contains("Reordered Keys")) >= 1 && modules.Count(x => x.Contains("Shapes And Bombs")) >= 1 && modules.Count(x => x.Contains("Simon Spins")) >= 1 && modules.Count(x => x.Contains("Simon Stages")) >= 1 && modules.Count(x => x.Contains("The Sun")) >= 1 && modules.Count(x => x.Contains("The Time Keeper")) == 1 && modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("Caesar Cipher")) + modules.Count(x => x.Contains("LED Encryption")) + modules.Count(x => x.Contains("Piano Keys")) + modules.Count(x => x.Contains("Standard Crazy Talk")) + modules.Count(x => x.Contains("Stock Images")) >= 3) level = 80;
        else if (moduleCount == 33 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Forget It Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Bamboozling Button")) >= 1 && modules.Count(x => x.Contains("Colored Keys")) >= 1 && modules.Count(x => x.Contains("Faulty Sink")) >= 1 && modules.Count(x => x.Contains("Flavor Text EX")) >= 1 && modules.Count(x => x.Contains("Green Arrows")) >= 1 && modules.Count(x => x.Contains("Horrible Memory")) >= 1 && modules.Count(x => x.Contains("Hunting")) >= 1 && modules.Count(x => x.Contains("Jaden Smith Talk")) >= 1 && modules.Count(x => x.Contains("KayMazey Talk")) >= 1 && modules.Count(x => x.Contains("Keypad Lock")) >= 1 && modules.Count(x => x.Contains("The Labyrinth")) >= 1 && modules.Count(x => x.Contains("The Legendre Symbol")) >= 1 && modules.Count(x => x.Contains("Logical Buttons")) >= 1 && modules.Count(x => x.Contains("Matrices")) >= 1 && modules.Count(x => x.Contains("Mineseeker")) >= 1 && modules.Count(x => x.Contains("Party Time")) >= 1 && modules.Count(x => x.Contains("Periodic Table")) >= 1 && modules.Count(x => x.Contains("Perspective Pegs")) >= 1 && modules.Count(x => x.Contains("Playfair Cipher")) >= 1 && modules.Count(x => x.Contains("Skewed Slots")) >= 1 && modules.Count(x => x.Contains("Sueet Wall")) >= 1 && modules.Count(x => x.Contains("Symbolic Tasha")) >= 1 && modules.Count(x => x.Contains("Ternary Converter")) >= 1 && modules.Count(x => x.Contains("Unicode")) >= 1 && modules.Count(x => x.Contains("Vexillology")) >= 1 && modules.Count(x => x.Contains("Snowflakes")) >= 1 && modules.Count(x => x.Contains("Keypad Combinations")) + modules.Count(x => x.Contains("Letter Keys")) + modules.Count(x => x.Contains("Light Bulbs")) + modules.Count(x => x.Contains("Simon States")) + modules.Count(x => x.Contains("The Stopwatch")) + modules.Count(x => x.Contains("Wire Placement")) >= 3) level = 81;
        else if (moduleCount == 34 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Forget It Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Adventure Game")) >= 1 && modules.Count(x => x.Contains("Bases")) >= 1 && modules.Count(x => x.Contains("Blind Maze")) >= 1 && modules.Count(x => x.Contains("Blue Arrows")) >= 1 && modules.Count(x => x.Contains("Color Morse")) >= 1 && modules.Count(x => x.Contains("Connection Device")) >= 1 && modules.Count(x => x.Contains("Crazy Talk")) >= 1 && modules.Count(x => x.Contains("Cruel Piano Keys")) >= 1 && modules.Count(x => x.Contains("Disordered Keys")) >= 1 && modules.Count(x => x.Contains("Dr. Doctor")) >= 1 && modules.Count(x => x.Contains("English Test")) >= 1 && modules.Count(x => x.Contains("Find The Date")) >= 1 && modules.Count(x => x.Contains("FizzBuzz")) >= 1 && modules.Count(x => x.Contains("Font Select")) >= 1 && modules.Count(x => x.Contains("Game of Life Simple")) >= 1 && modules.Count(x => x.Contains("Lunchtime")) >= 1 && modules.Count(x => x.Contains("M&Ns")) >= 1 && modules.Count(x => x.Contains("Mastermind Cruel")) >= 1 && modules.Count(x => x.Contains("Microcontroller")) >= 1 && modules.Count(x => x.Contains("Multi-Colored Switches")) >= 1 && modules.Count(x => x.Contains("osu!")) >= 1 && modules.Count(x => x.Contains("Passport Control")) >= 1 && modules.Count(x => x.Contains("Quote Crazy Talk End Quote")) >= 1 && modules.Count(x => x.Contains("Stack'em")) >= 1 && modules.Count(x => x.Contains("Symbolic Coordinates")) >= 1 && modules.Count(x => x.Contains("Tap Code")) >= 1 && modules.Count(x => x.Contains("Yellow Cipher")) >= 1 && modules.Count(x => x.Contains("Art Appreciation")) + modules.Count(x => x.Contains("Binary Tree")) + modules.Count(x => x.Contains("Double Color")) + modules.Count(x => x.Contains("Flash Memory")) + modules.Count(x => x.Contains("Insane Talk")) + modules.Count(x => x.Contains("S.E.T.")) >= 3) level = 82;
        else if (moduleCount == 34 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Forget It Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Benedict Cumberbatch")) >= 1 && modules.Count(x => x.Contains("Big Circle")) >= 1 && modules.Count(x => x.Contains("Binary Grid")) >= 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("Blinkstop")) >= 1 && modules.Count(x => x.Contains("Color Braille")) >= 1 && modules.Count(x => x.Contains("The Cube")) >= 1 && modules.Count(x => x.Contains("Decolored Squares")) >= 1 && modules.Count(x => x.Contains("Digital Cipher")) >= 1 && modules.Count(x => x.Contains("Equations")) >= 1 && modules.Count(x => x.Contains("Functions")) >= 1 && modules.Count(x => x.Contains("Homophones")) >= 1 && modules.Count(x => x.Contains("Identity Parade")) >= 1 && modules.Count(x => x.Contains("Jenga")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Kilo Talk")) >= 1 && modules.Count(x => x.Contains("Know Your Way")) >= 1 && modules.Count(x => x.Contains("Lombax Cubes")) >= 1 && modules.Count(x => x.Contains("The London Underground")) >= 1 && modules.Count(x => x.Contains("Maritime Flags")) >= 1 && modules.Count(x => x.Contains("The Matrix")) >= 1 && modules.Count(x => x.Contains("Module Movements")) >= 1 && modules.Count(x => x.Contains("Morse-A-Maze")) >= 1 && modules.Count(x => x.Contains("The Samsung")) >= 1 && modules.Count(x => x.Contains("Ten-Button Color Code")) >= 1 && modules.Count(x => x.Contains("Timezone")) >= 1 && modules.Count(x => x.Contains("Treasure Hunt")) >= 1 && modules.Count(x => x.Contains("Natures")) + modules.Count(x => x.Contains("Not Complicated Wires")) + modules.Count(x => x.Contains("The Plunger Button")) + modules.Count(x => x.Contains("Red Herring")) + modules.Count(x => x.Contains("Round Keypad")) + modules.Count(x => x.Contains("Seven Wires")) >= 3) level = 83;
        else if (moduleCount == 35 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Forget It Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Alliances")) >= 1 && modules.Count(x => x.Contains("Binary LEDs")) >= 1 && modules.Count(x => x.Contains("Challenge & Contact")) >= 1 && modules.Count(x => x.Contains("Cruel Garfield Kart")) >= 1 && modules.Count(x => x.Contains("Earthbound")) >= 1 && modules.Count(x => x.Contains("Game of Life Cruel")) >= 1 && modules.Count(x => x.Contains("Geometry Dash")) >= 1 && modules.Count(x => x.Contains("Greek Calculus")) >= 1 && modules.Count(x => x.Contains("Jumble Cycle")) >= 1 && modules.Count(x => x.Contains("Kooky Keypad")) >= 1 && modules.Count(x => x.Contains("Lying Indicators")) >= 1 && modules.Count(x => x.Contains("Masyu")) >= 1 && modules.Count(x => x.Contains("Melody Sequencer")) >= 1 && modules.Count(x => x.Contains("Morsematics")) >= 1 && modules.Count(x => x.Contains("Mystery Module")) == 1 && modules.Count(x => x.Contains("Not Keypad")) >= 1 && modules.Count(x => x.Contains("Not Password")) >= 1 && modules.Count(x => x.Contains("Point of Order")) >= 1 && modules.Count(x => x.Contains("Polyhedral Maze")) >= 1 && modules.Count(x => x.Contains("Red Cipher")) >= 1 && modules.Count(x => x.Contains("Resistors")) >= 1 && modules.Count(x => x.Contains("Retirement")) >= 1 && modules.Count(x => x.Contains("Reverse Polish Notation")) >= 1 && modules.Count(x => x.Contains("Roman Art")) >= 1 && modules.Count(x => x.Contains("Simon Selects")) >= 1 && modules.Count(x => x.Contains("Simon's On First")) >= 1 && modules.Count(x => x.Contains("Skewed Slots")) >= 1 && modules.Count(x => x.Contains("Splitting The Loot")) >= 1 && modules.Count(x => x.Contains("Not Simaze")) + modules.Count(x => x.Contains("Poetry")) + modules.Count(x => x.Contains("Superlogic")) + modules.Count(x => x.Contains("Switches")) + modules.Count(x => x.Contains("Word Search")) + modules.Count(x => x.Contains("Yahtzee")) >= 3) level = 84;
        else if (moduleCount == 35 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Simon's Stages")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Affine Cycle")) >= 1 && modules.Count(x => x.Contains("Arithmelogic")) >= 1 && modules.Count(x => x.Contains("Blue Cipher")) >= 1 && modules.Count(x => x.Contains("Chicken Nuggets")) >= 1 && modules.Count(x => x.Contains("Christmas Presents")) >= 1 && modules.Count(x => x.Contains("Coffeebucks")) >= 1 && modules.Count(x => x.Contains("Countdown")) >= 1 && modules.Count(x => x.Contains("Crazy Talk With A K")) >= 1 && modules.Count(x => x.Contains("Cryptography")) >= 1 && modules.Count(x => x.Contains("Daylight Directions")) >= 1 && modules.Count(x => x.Contains("DetoNATO")) >= 1 && modules.Count(x => x.Contains("Extended Password")) >= 1 && modules.Count(x => x.Contains("Genetic Sequence")) >= 1 && modules.Count(x => x.Contains("Hereditary Base Notation")) >= 1 && modules.Count(x => x.Contains("Hidden Colors")) >= 1 && modules.Count(x => x.Contains("Human Resources")) >= 1 && modules.Count(x => x.Contains("Integer Trees")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Light Cycle")) >= 1 && modules.Count(x => x.Contains("Lion’s Share")) >= 1 && modules.Count(x => x.Contains("Malfunctions")) >= 1 && modules.Count(x => x.Contains("Not Wiresword")) >= 1 && modules.Count(x => x.Contains("Numbered Buttons")) >= 1 && modules.Count(x => x.Contains("Only Connect")) >= 1 && modules.Count(x => x.Contains("Party Time")) >= 1 && modules.Count(x => x.Contains("Plumbing")) >= 1 && modules.Count(x => x.Contains("Poker")) >= 1 && modules.Count(x => x.Contains("Spot the Difference")) >= 1 && modules.Count(x => x.Contains("Alphabet")) + modules.Count(x => x.Contains("Blind Alley")) + modules.Count(x => x.Contains("Combination Lock")) + modules.Count(x => x.Contains("Emoji Math")) + modules.Count(x => x.Contains("Encrypted Dice")) + modules.Count(x => x.Contains("Mastermind Simple")) >= 3) level = 85;
        else if (moduleCount == 36 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("1D Maze")) >= 1 && modules.Count(x => x.Contains("3 LEDs")) >= 1 && modules.Count(x => x.Contains("3D Maze")) >= 1 && modules.Count(x => x.Contains("Binary Puzzle")) >= 1 && modules.Count(x => x.Contains("Boolean Wires")) >= 1 && modules.Count(x => x.Contains("Double Arrows")) >= 1 && modules.Count(x => x.Contains("Encrypted Equations")) >= 1 && modules.Count(x => x.Contains("Fast Math")) >= 1 && modules.Count(x => x.Contains("Faulty Digital Root")) >= 1 && modules.Count(x => x.Contains("The Hidden Value")) >= 1 && modules.Count(x => x.Contains("Hold Ups")) >= 1 && modules.Count(x => x.Contains("Langton's Ant")) >= 1 && modules.Count(x => x.Contains("Life Iteration")) >= 1 && modules.Count(x => x.Contains("The Number")) >= 1 && modules.Count(x => x.Contains("Painting")) >= 1 && modules.Count(x => x.Contains("Pigpen Cycle")) >= 1 && modules.Count(x => x.Contains("Planets")) >= 1 && modules.Count(x => x.Contains("The Radio")) >= 1 && modules.Count(x => x.Contains("Reordered Keys")) >= 1 && modules.Count(x => x.Contains("Red Arrows")) >= 1 && modules.Count(x => x.Contains("Rubik's Cube")) >= 1 && modules.Count(x => x.Contains("Shape Shift")) >= 1 && modules.Count(x => x.Contains("The Sphere")) >= 1 && modules.Count(x => x.Contains("Stained Glass")) >= 1 && modules.Count(x => x.Contains("The Swan")) == 1 && modules.Count(x => x.Contains("Third Base")) >= 1 && modules.Count(x => x.Contains("Turtle Robot")) >= 1 && modules.Count(x => x.Contains("Type Racer")) >= 1 && modules.Count(x => x.Contains("The World's Largest Button")) >= 1 && modules.Count(x => x.Contains("Divisible Numbers")) + modules.Count(x => x.Contains("Mafia")) + modules.Count(x => x.Contains("Mashematics")) + modules.Count(x => x.Contains("The Simpleton")) + modules.Count(x => x.Contains("Skinny Wires")) + modules.Count(x => x.Contains("Square Button")) >= 3) level = 86;
        else if (moduleCount == 36 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Answering Can Be Fun")) >= 1 && modules.Count(x => x.Contains("Boolean Maze")) >= 1 && modules.Count(x => x.Contains("Button Sequence")) >= 1 && modules.Count(x => x.Contains("Elder Futhark")) >= 1 && modules.Count(x => x.Contains("Equations X")) >= 1 && modules.Count(x => x.Contains("Festive Piano Keys")) >= 1 && modules.Count(x => x.Contains("Foreign Exchange Rates")) >= 1 && modules.Count(x => x.Contains("Hieroglyphics")) >= 1 && modules.Count(x => x.Contains("The High Score")) >= 1 && modules.Count(x => x.Contains("Ingredients")) >= 1 && modules.Count(x => x.Contains("Mega Man 2")) >= 1 && modules.Count(x => x.Contains("Monsplode, Fight!")) >= 1 && modules.Count(x => x.Contains("Negativity")) >= 1 && modules.Count(x => x.Contains("Pickup Identification")) >= 1 && modules.Count(x => x.Contains("Plant Identification")) >= 1 && modules.Count(x => x.Contains("Playfair Cycle")) >= 1 && modules.Count(x => x.Contains("Press X")) >= 1 && modules.Count(x => x.Contains("Question Mark")) >= 1 && modules.Count(x => x.Contains("Radiator")) >= 1 && modules.Count(x => x.Contains("Raiding Temples")) >= 1 && modules.Count(x => x.Contains("Red Arrows")) >= 1 && modules.Count(x => x.Contains("Resistors")) >= 1 && modules.Count(x => x.Contains("Tangrams")) >= 1 && modules.Count(x => x.Contains("Tic Tac Toe")) >= 1 && modules.Count(x => x.Contains("Valves")) >= 1 && modules.Count(x => x.Contains("Varicolored Squares")) >= 1 && modules.Count(x => x.Contains("Wavetapping")) >= 1 && modules.Count(x => x.Contains("Web Design")) >= 1 && modules.Count(x => x.Contains("Neutralization")) >= 1 && modules.Count(x => x.Contains("1000 Words")) + modules.Count(x => x.Contains("Addition")) + modules.Count(x => x.Contains("Bone Apple Tea")) + modules.Count(x => x.Contains("Double-Oh")) + modules.Count(x => x.Contains("Simon Scrambles")) + modules.Count(x => x.Contains("Sink")) >= 3) level = 87;
        else if (moduleCount == 37 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Audio Morse")) >= 1 && modules.Count(x => x.Contains("Binary LEDs")) >= 1 && modules.Count(x => x.Contains("Blockbusters")) >= 1 && modules.Count(x => x.Contains("Boolean Venn Diagram")) >= 1 && modules.Count(x => x.Contains("Connection Check")) >= 1 && modules.Count(x => x.Contains("Cursed Double-Oh")) >= 1 && modules.Count(x => x.Contains("The Deck of Many Things")) >= 1 && modules.Count(x => x.Contains("Discolored Squares")) >= 1 && modules.Count(x => x.Contains("Divisible Numbers")) >= 1 && modules.Count(x => x.Contains("Double Expert")) >= 1 && modules.Count(x => x.Contains("Free Parking")) >= 1 && modules.Count(x => x.Contains("Green Cipher")) >= 1 && modules.Count(x => x.Contains("The Heart")) == 1 && modules.Count(x => x.Contains("Hexamaze")) >= 1 && modules.Count(x => x.Contains("Kanji")) >= 1 && modules.Count(x => x.Contains("Label Priorities")) >= 1 && modules.Count(x => x.Contains("LED Grid")) >= 1 && modules.Count(x => x.Contains("Light Cycle")) >= 1 && modules.Count(x => x.Contains("Logic")) >= 1 && modules.Count(x => x.Contains("Masher The Bottun")) >= 1 && modules.Count(x => x.Contains("Mortal Kombat")) >= 1 && modules.Count(x => x.Contains("Mouse In The Maze")) >= 1 && modules.Count(x => x.Contains("Not Memory")) >= 1 && modules.Count(x => x.Contains("Quiz Buzz")) >= 1 && modules.Count(x => x.Contains("Red Buttons")) >= 1 && modules.Count(x => x.Contains("Switches")) >= 1 && modules.Count(x => x.Contains("Switching Maze")) >= 1 && modules.Count(x => x.Contains("Tasha Squeals")) >= 1 && modules.Count(x => x.Contains("Thread the Needle")) >= 1 && modules.Count(x => x.Contains("Wires")) >= 1 && modules.Count(x => x.Contains("Keypad")) + modules.Count(x => x.Contains("Anagrams")) + modules.Count(x => x.Contains("The Clock")) + modules.Count(x => x.Contains("The Code")) + modules.Count(x => x.Contains("Corners")) + modules.Count(x => x.Contains("The Rule")) >= 3) level = 88;
        else if (moduleCount == 37 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("101 Dalmatians")) >= 1 && modules.Count(x => x.Contains("Alchemy")) >= 1 && modules.Count(x => x.Contains("Black Cipher")) >= 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("Bordered Keys")) >= 1 && modules.Count(x => x.Contains("Broken Buttons")) >= 1 && modules.Count(x => x.Contains("Catchphrase")) >= 1 && modules.Count(x => x.Contains("Chicken Nuggets")) >= 1 && modules.Count(x => x.Contains("Complicated Buttons")) >= 1 && modules.Count(x => x.Contains("Cruel Keypads")) >= 1 && modules.Count(x => x.Contains("The cRule")) >= 1 && modules.Count(x => x.Contains("The Crystal Maze")) >= 1 && modules.Count(x => x.Contains("Daylight Directions")) >= 1 && modules.Count(x => x.Contains("Discolored Squares")) >= 1 && modules.Count(x => x.Contains("Green Arrows")) >= 1 && modules.Count(x => x.Contains("The Hypercube")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Jukebox.WAV")) >= 1 && modules.Count(x => x.Contains("Microphone")) >= 1 && modules.Count(x => x.Contains("Minecraft Cipher")) >= 1 && modules.Count(x => x.Contains("Module Rick")) >= 1 && modules.Count(x => x.Contains("Morse War")) >= 1 && modules.Count(x => x.Contains("Needlessly Complicated Button")) >= 1 && modules.Count(x => x.Contains("Number Pad")) >= 1 && modules.Count(x => x.Contains("Reflex")) >= 1 && modules.Count(x => x.Contains("Sorting")) >= 1 && modules.Count(x => x.Contains("Symbol Cycle")) >= 1 && modules.Count(x => x.Contains("Valves")) >= 1 && modules.Count(x => x.Contains("Waste Management")) >= 1 && modules.Count(x => x.Contains("Yahtzee")) >= 1 && modules.Count(x => x.Contains("Astrology")) + modules.Count(x => x.Contains("Caesar Cipher")) + modules.Count(x => x.Contains("Orange Arrows")) + modules.Count(x => x.Contains("Orientation Cube")) + modules.Count(x => x.Contains("Party Time")) + modules.Count(x => x.Contains("Word Scramble")) >= 3) level = 89;
        else if (moduleCount == 38 && modules.Count(x => x.Contains("Forget Everything")) == 1 && modules.Count(x => x.Contains("Forget Infinity")) == 1 && modules.Count(x => x.Contains("Dungeon 2nd Floor")) >= 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("Christmas Presents")) >= 1 && modules.Count(x => x.Contains("Connection Device")) >= 1 && modules.Count(x => x.Contains("Cryptography")) >= 1 && modules.Count(x => x.Contains("Divided Squares")) == 1 && modules.Count(x => x.Contains("Free Parking")) >= 1 && modules.Count(x => x.Contains("Grocery Store")) >= 1 && modules.Count(x => x.Contains("The Hexabutton")) >= 1 && modules.Count(x => x.Contains("Hinges")) >= 1 && modules.Count(x => x.Contains("The Hypercube")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Mastermind Cruel")) >= 1 && modules.Count(x => x.Contains("Mastermind Simple")) >= 1 && modules.Count(x => x.Contains("Mega Man 2")) >= 1 && modules.Count(x => x.Contains("Microphone")) >= 1 && modules.Count(x => x.Contains("Minesweeper")) >= 1 && modules.Count(x => x.Contains("Morse Buttons")) >= 1 && modules.Count(x => x.Contains("Morse War")) >= 1 && modules.Count(x => x.Contains("Not Wire Sequence")) >= 1 && modules.Count(x => x.Contains("Not Wiresword")) >= 1 && modules.Count(x => x.Contains("Partial Derivatives")) >= 1 && modules.Count(x => x.Contains("Pow")) >= 1 && modules.Count(x => x.Contains("Shell Game")) >= 1 && modules.Count(x => x.Contains("Simon Screams")) >= 1 && modules.Count(x => x.Contains("Simon Sends")) >= 1 && modules.Count(x => x.Contains("Simon Stores")) >= 1 && modules.Count(x => x.Contains("Square Button")) >= 1 && modules.Count(x => x.Contains("Unordered Keys")) >= 1 && modules.Count(x => x.Contains("The Wire")) >= 1 && modules.Count(x => x.Contains("Wire Spaghetti")) >= 1 && modules.Count(x => x.Contains("Microcontroller")) >= 1 && modules.Count(x => x.Contains("The Button")) + modules.Count(x => x.Contains("1D Maze")) + modules.Count(x => x.Contains("3D Tunnels")) + modules.Count(x => x.Contains("Etterna")) + modules.Count(x => x.Contains("Going Backwards")) + modules.Count(x => x.Contains("Mazematics")) + modules.Count(x => x.Contains("Not Maze")) + modules.Count(x => x.Contains("Not Simaze")) + modules.Count(x => x.Contains("Orientation Cube")) + modules.Count(x => x.Contains("Plumbing")) >= 3) level = 90;
        else if (moduleCount == 40 && modules.Count(x => x.Contains("Forget Everything")) == 1 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon 2nd Floor")) >= 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("Blue Cipher")) >= 1 && modules.Count(x => x.Contains("Boolean Venn Diagram")) >= 1 && modules.Count(x => x.Contains("Christmas Presents")) >= 1 && modules.Count(x => x.Contains("Colored Switches")) >= 1 && modules.Count(x => x.Contains("The Cube")) >= 1 && modules.Count(x => x.Contains("Extended Password")) >= 1 && modules.Count(x => x.Contains("Friendship")) >= 1 && modules.Count(x => x.Contains("Game of Life Cruel")) >= 1 && modules.Count(x => x.Contains("Goofy's Game")) >= 1 && modules.Count(x => x.Contains("Greek Calculus")) >= 1 && modules.Count(x => x.Contains("Grid Matching")) >= 1 && modules.Count(x => x.Contains("Guitar Chords")) >= 1 && modules.Count(x => x.Contains("Kilo Talk")) >= 1 && modules.Count(x => x.Contains("Kooky Keypad")) >= 1 && modules.Count(x => x.Contains("Module Listening")) >= 1 && modules.Count(x => x.Contains("Monsplode Trading Cards")) >= 1 && modules.Count(x => x.Contains("Morsematics")) >= 1 && modules.Count(x => x.Contains("Mouse In The Maze")) >= 1 && modules.Count(x => x.Contains("Negativity")) >= 1 && modules.Count(x => x.Contains("Polyhedral Maze")) >= 1 && modules.Count(x => x.Contains("Round Keypad")) >= 1 && modules.Count(x => x.Contains("Simon Sings")) >= 1 && modules.Count(x => x.Contains("The Sphere")) >= 1 && modules.Count(x => x.Contains("Third Base")) >= 1 && modules.Count(x => x.Contains("White Cipher")) >= 1 && modules.Count(x => x.Contains("Who's on First")) >= 1 && modules.Count(x => x.Contains("Wire Placement")) >= 1 && modules.Count(x => x.Contains("The World's Largest Button")) >= 1 && modules.Count(x => x.Contains("Yellow Cipher")) >= 1 && modules.Count(x => x.Contains("Yes and No")) >= 1 && modules.Count(x => x.Contains("Zoo")) >= 1 && modules.Count(x => x.Contains("Maze")) + modules.Count(x => x.Contains("The Colored Maze")) + modules.Count(x => x.Contains("Digital Cipher")) + modules.Count(x => x.Contains("Faulty Sink")) + modules.Count(x => x.Contains("Going Backwards")) + modules.Count(x => x.Contains("Hunting")) + modules.Count(x => x.Contains("Iconic")) + modules.Count(x => x.Contains("Ingredients")) + modules.Count(x => x.Contains("Red Herring")) + modules.Count(x => x.Contains("The Simpleton")) >= 4) level = 91;
        else if (moduleCount == 41 && modules.Count(x => x.Contains("Forget Everything")) == 1 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Dungeon 2nd Floor")) >= 1 && modules.Count(x => x.Contains("3 LEDs")) >= 1 && modules.Count(x => x.Contains("3D Maze")) >= 1 && modules.Count(x => x.Contains("Addition")) >= 1 && modules.Count(x => x.Contains("Affine Cycle")) >= 1 && modules.Count(x => x.Contains("Arrow Talk")) >= 1 && modules.Count(x => x.Contains("Blind Maze")) >= 1 && modules.Count(x => x.Contains("Boxing")) >= 1 && modules.Count(x => x.Contains("Broken Buttons")) >= 1 && modules.Count(x => x.Contains("Button Grid")) >= 1 && modules.Count(x => x.Contains("Caesar Cipher")) >= 1 && modules.Count(x => x.Contains("Caesar Cycle")) >= 1 && modules.Count(x => x.Contains("The Crystal Maze")) >= 1 && modules.Count(x => x.Contains("Decolored Squares")) >= 1 && modules.Count(x => x.Contains("Elder Password")) >= 1 && modules.Count(x => x.Contains("English Test")) >= 1 && modules.Count(x => x.Contains("Faulty RGB Maze")) >= 1 && modules.Count(x => x.Contains("Graffiti Numbers")) >= 1 && modules.Count(x => x.Contains("Hogwarts")) == 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Logic Gates")) >= 1 && modules.Count(x => x.Contains("Lying Indicators")) >= 1 && modules.Count(x => x.Contains("Module Rick")) >= 1 && modules.Count(x => x.Contains("Morse Code Translated")) >= 1 && modules.Count(x => x.Contains("Mystery Module")) == 1 && modules.Count(x => x.Contains("Recorded Keys")) >= 1 && modules.Count(x => x.Contains("Simon Spins")) >= 1 && modules.Count(x => x.Contains("Square Button")) >= 1 && modules.Count(x => x.Contains("The Stare")) >= 1 && modules.Count(x => x.Contains("The Swan")) == 1 && modules.Count(x => x.Contains("The Triangle Button")) >= 1 && modules.Count(x => x.Contains("Unown Cipher")) >= 1 && modules.Count(x => x.Contains("Vigenère Cipher")) >= 1 && modules.Count(x => x.Contains("Wonder Cipher")) >= 1 && modules.Count(x => x.Contains("BoozleTalk")) + modules.Count(x => x.Contains("Color Generator")) + modules.Count(x => x.Contains("Countdown")) + modules.Count(x => x.Contains("Crazy Talk With A K")) + modules.Count(x => x.Contains("Cruel Keypads")) + modules.Count(x => x.Contains("Cruel Piano Keys")) + modules.Count(x => x.Contains("Dimension Disruption")) + modules.Count(x => x.Contains("The Jack-O'-Lantern")) + modules.Count(x => x.Contains("Listening")) + modules.Count(x => x.Contains("The Triangle")) >= 4) level = 92;
        else if (moduleCount == 42 && modules.Count(x => x.Contains("Forget Everything")) == 1 && modules.Count(x => x.Contains("Übermodule")) == 1 && modules.Count(x => x.Contains("Dungeon 2nd Floor")) >= 1 && modules.Count(x => x.Contains("3D Tunnels")) >= 1 && modules.Count(x => x.Contains("A-maze-ing Buttons")) >= 1 && modules.Count(x => x.Contains("Astrology")) >= 1 && modules.Count(x => x.Contains("Blind Maze")) >= 1 && modules.Count(x => x.Contains("Boolean Maze")) >= 1 && modules.Count(x => x.Contains("Bordered Keys")) >= 1 && modules.Count(x => x.Contains("Crazy Talk")) >= 1 && modules.Count(x => x.Contains("The Deck of Many Things")) >= 1 && modules.Count(x => x.Contains("The Digit")) >= 1 && modules.Count(x => x.Contains("Echolocation")) >= 1 && modules.Count(x => x.Contains("Encrypted Hangman")) >= 1 && modules.Count(x => x.Contains("Guess Who?")) >= 1 && modules.Count(x => x.Contains("Hieroglyphics")) >= 1 && modules.Count(x => x.Contains("Hunting")) >= 1 && modules.Count(x => x.Contains("Indigo Cipher")) >= 1 && modules.Count(x => x.Contains("Life Iteration")) >= 1 && modules.Count(x => x.Contains("Logical Buttons")) >= 1 && modules.Count(x => x.Contains("Mazematics")) >= 1 && modules.Count(x => x.Contains("Modern Cipher")) >= 1 && modules.Count(x => x.Contains("Module Maze")) >= 1 && modules.Count(x => x.Contains("Morse Code Translated")) >= 1 && modules.Count(x => x.Contains("Morse-A-Maze")) >= 1 && modules.Count(x => x.Contains("The Number Cipher")) >= 1 && modules.Count(x => x.Contains("Periodic Table")) >= 1 && modules.Count(x => x.Contains("Plumbing")) >= 1 && modules.Count(x => x.Contains("Poetry")) >= 1 && modules.Count(x => x.Contains("Red Buttons")) >= 1 && modules.Count(x => x.Contains("Regular Crazy Talk")) >= 1 && modules.Count(x => x.Contains("Shell Game")) >= 1 && modules.Count(x => x.Contains("Simon's Star")) >= 1 && modules.Count(x => x.Contains("Treasure Hunt")) >= 1 && modules.Count(x => x.Contains("USA Maze")) >= 1 && modules.Count(x => x.Contains("Varicolored Squares")) >= 1 && modules.Count(x => x.Contains("Waste Management")) >= 1 && modules.Count(x => x.Contains("Maze")) + modules.Count(x => x.Contains("A Mistake")) + modules.Count(x => x.Contains("Alphabetical Ruling")) + modules.Count(x => x.Contains("Blind Alley")) + modules.Count(x => x.Contains("Emoji Math")) + modules.Count(x => x.Contains("Fencing")) + modules.Count(x => x.Contains("Going Backwards")) + modules.Count(x => x.Contains("Not Maze")) + modules.Count(x => x.Contains("Switches")) + modules.Count(x => x.Contains("Tap Code")) >= 4) level = 93;
        else if (moduleCount == 44 && modules.Count(x => x.Contains("Forget Everything")) == 1 && modules.Count(x => x.Contains("Brainf---")) == 1 && modules.Count(x => x.Contains("Dungeon 2nd Floor")) >= 1 && modules.Count(x => x.Contains("❖")) >= 1 && modules.Count(x => x.Contains("Alliances")) >= 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("Cheat Checkout")) >= 1 && modules.Count(x => x.Contains("Colored Keys")) >= 1 && modules.Count(x => x.Contains("Colored Squares")) >= 1 && modules.Count(x => x.Contains("Colorful Insanity")) >= 1 && modules.Count(x => x.Contains("Cruel Keypads")) >= 1 && modules.Count(x => x.Contains("Flashing Lights")) >= 1 && modules.Count(x => x.Contains("Green Cipher")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Jukebox.WAV")) >= 1 && modules.Count(x => x.Contains("The Labyrinth")) >= 1 && modules.Count(x => x.Contains("Laundry")) >= 1 && modules.Count(x => x.Contains("Mastermind Simple")) >= 1 && modules.Count(x => x.Contains("Maze³")) >= 1 && modules.Count(x => x.Contains("Morsematics")) >= 1 && modules.Count(x => x.Contains("Nonogram")) >= 1 && modules.Count(x => x.Contains("Perplexing Wires")) >= 1 && modules.Count(x => x.Contains("Probing")) >= 1 && modules.Count(x => x.Contains("Red Cipher")) >= 1 && modules.Count(x => x.Contains("Reverse Morse")) >= 1 && modules.Count(x => x.Contains("Risky Wires")) >= 1 && modules.Count(x => x.Contains("The Rule")) >= 1 && modules.Count(x => x.Contains("Simon Screams")) >= 1 && modules.Count(x => x.Contains("Simon Selects")) >= 1 && modules.Count(x => x.Contains("Simon Shrieks")) >= 1 && modules.Count(x => x.Contains("Simon Speaks")) >= 1 && modules.Count(x => x.Contains("Switching Maze")) >= 1 && modules.Count(x => x.Contains("Symbolic Coordinates")) >= 1 && modules.Count(x => x.Contains("Ten-Button Color Code")) >= 1 && modules.Count(x => x.Contains("Two Bits")) >= 1 && modules.Count(x => x.Contains("The Ultracube")) >= 1 && modules.Count(x => x.Contains("Visual Impairment")) >= 1 && modules.Count(x => x.Contains("Who's on First")) >= 1 && modules.Count(x => x.Contains("The Wire")) >= 1 && modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("The Button")) + modules.Count(x => x.Contains("Keypad")) + modules.Count(x => x.Contains("Simon Says")) + modules.Count(x => x.Contains("Memory")) + modules.Count(x => x.Contains("Morse Code")) + modules.Count(x => x.Contains("Complicated Wires")) + modules.Count(x => x.Contains("Wire Sequence")) + modules.Count(x => x.Contains("Maze")) + modules.Count(x => x.Contains("Password")) >= 4) level = 94;
        else if (moduleCount == 47 && modules.Count(x => x.Contains("Forget Everything")) == 2 && modules.Count(x => x.Contains("Dungeon 2nd Floor")) >= 1 && modules.Count(x => x.Contains("101 Dalmatians")) >= 1 && modules.Count(x => x.Contains("Art Appreciation")) >= 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("Blind Alley")) >= 1 && modules.Count(x => x.Contains("The Button")) >= 1 && modules.Count(x => x.Contains("Color Generator")) >= 1 && modules.Count(x => x.Contains("Cryptic Cycle")) >= 1 && modules.Count(x => x.Contains("The Cube")) >= 1 && modules.Count(x => x.Contains("Double-Oh")) >= 1 && modules.Count(x => x.Contains("Dragon Energy")) >= 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Elder Password")) >= 1 && modules.Count(x => x.Contains("Fast Math")) >= 1 && modules.Count(x => x.Contains("Festive Piano Keys")) >= 1 && modules.Count(x => x.Contains("Flower Patch")) >= 1 && modules.Count(x => x.Contains("Friendship")) >= 1 && modules.Count(x => x.Contains("The Heart")) >= 1 && modules.Count(x => x.Contains("The Hexabutton")) >= 1 && modules.Count(x => x.Contains("Hunting")) >= 1 && modules.Count(x => x.Contains("Keypad")) >= 1 && modules.Count(x => x.Contains("Langton's Ant")) >= 1 && modules.Count(x => x.Contains("Left and Right")) >= 1 && modules.Count(x => x.Contains("LEGOs")) >= 1 && modules.Count(x => x.Contains("Life Iteration")) >= 1 && modules.Count(x => x.Contains("Marble Tumble")) >= 1 && modules.Count(x => x.Contains("Mouse In The Maze")) >= 1 && modules.Count(x => x.Contains("Not Wire Sequence")) >= 1 && modules.Count(x => x.Contains("Partial Derivatives")) >= 1 && modules.Count(x => x.Contains("Plant Identification")) >= 1 && modules.Count(x => x.Contains("Rhythms")) >= 1 && modules.Count(x => x.Contains("Scavenger Hunt")) >= 1 && modules.Count(x => x.Contains("Simon Stages")) >= 1 && modules.Count(x => x.Contains("Skyrim")) >= 1 && modules.Count(x => x.Contains("The Sun")) >= 1 && modules.Count(x => x.Contains("Thinking Wires")) >= 1 && modules.Count(x => x.Contains("Violet Cipher")) >= 1 && modules.Count(x => x.Contains("The Wire")) >= 1 && modules.Count(x => x.Contains("Wires")) >= 1 && modules.Count(x => x.Contains("The Witness")) >= 1 && modules.Count(x => x.Contains("Complicated Wires")) + modules.Count(x => x.Contains("Morse Code")) + modules.Count(x => x.Contains("Memory")) + modules.Count(x => x.Contains("Colored Squares")) + modules.Count(x => x.Contains("Emoji Math")) + modules.Count(x => x.Contains("Green Arrows")) + modules.Count(x => x.Contains("The Jukebox")) + modules.Count(x => x.Contains("Red Arrows")) + modules.Count(x => x.Contains("Wire Placement")) + modules.Count(x => x.Contains("Yellow Arrows")) >= 4) level = 95;
        else if (moduleCount == 55 && modules.Count(x => x.Contains("Forget Everything")) == 1 && modules.Count(x => x.Contains("Simon's Stages")) == 1 && modules.Count(x => x.Contains("Dungeon 2nd Floor")) >= 1 && modules.Count(x => x.Contains("3 LEDs")) >= 1 && modules.Count(x => x.Contains("3D Maze")) >= 1 && modules.Count(x => x.Contains("Accumulation")) >= 1 && modules.Count(x => x.Contains("Adventure Game")) >= 1 && modules.Count(x => x.Contains("Bamboozling Button")) >= 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("Blue Cipher")) >= 1 && modules.Count(x => x.Contains("Chess")) >= 1 && modules.Count(x => x.Contains("The Clock")) >= 1 && modules.Count(x => x.Contains("Constellations")) >= 1 && modules.Count(x => x.Contains("Creation")) >= 1 && modules.Count(x => x.Contains("Cryptography")) >= 1 && modules.Count(x => x.Contains("Factory Maze")) >= 1 && modules.Count(x => x.Contains("Flower Patch")) >= 1 && modules.Count(x => x.Contains("Follow the Leader")) >= 1 && modules.Count(x => x.Contains("Game of Life Simple")) >= 1 && modules.Count(x => x.Contains("Gray Cipher")) >= 1 && modules.Count(x => x.Contains("Harmony Sequence")) >= 1 && modules.Count(x => x.Contains("Iconic")) == 1 && modules.Count(x => x.Contains("The Jack-O'-Lantern")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Maze Scrambler")) >= 1 && modules.Count(x => x.Contains("Maze³")) >= 1 && modules.Count(x => x.Contains("The Moon")) >= 1 && modules.Count(x => x.Contains("Mystery Module")) == 1 && modules.Count(x => x.Contains("Natures")) >= 1 && modules.Count(x => x.Contains("The Number")) >= 1 && modules.Count(x => x.Contains("Partial Derivatives")) >= 1 && modules.Count(x => x.Contains("Password Generator")) >= 1 && modules.Count(x => x.Contains("Plant Identification")) >= 1 && modules.Count(x => x.Contains("Point of Order")) >= 1 && modules.Count(x => x.Contains("Purgatory")) == 1 && modules.Count(x => x.Contains("Quote Crazy Talk End Quote")) >= 1 && modules.Count(x => x.Contains("Seven Deadly Sins")) >= 1 && modules.Count(x => x.Contains("Simon Stops")) >= 1 && modules.Count(x => x.Contains("Spelling Bee")) >= 1 && modules.Count(x => x.Contains("The Stare")) >= 1 && modules.Count(x => x.Contains("The Sun")) >= 1 && modules.Count(x => x.Contains("Type Racer")) >= 1 && modules.Count(x => x.Contains("Ultimate Cycle")) >= 1 && modules.Count(x => x.Contains("Westeros")) >= 1 && modules.Count(x => x.Contains("The Wire")) >= 1 && modules.Count(x => x.Contains("X-Ray")) >= 1 && modules.Count(x => x.Contains("Yahtzee")) >= 1 && modules.Count(x => x.Contains("Yes and No")) >= 1 && modules.Count(x => x.Contains("Zoo")) >= 1 && modules.Count(x => x.Contains("Complicated Wires")) + modules.Count(x => x.Contains("Maze")) + modules.Count(x => x.Contains("Simon Says")) + modules.Count(x => x.Contains("Wires")) + modules.Count(x => x.Contains("Wire Sequence")) + modules.Count(x => x.Contains("Alphabet")) + modules.Count(x => x.Contains("Bases")) + modules.Count(x => x.Contains("Colour Flash")) + modules.Count(x => x.Contains("Going Backwards")) + modules.Count(x => x.Contains("Listening")) >= 5) level = 96;
        else if (moduleCount == 63 && modules.Count(x => x.Contains("Forget Infinity")) == 1 && modules.Count(x => x.Contains("Forget The Colors")) == 1 && modules.Count(x => x.Contains("Dungeon 2nd Floor")) >= 1 && modules.Count(x => x.Contains("Souvenir")) == 1 && modules.Count(x => x.Contains("❖")) >= 1 && modules.Count(x => x.Contains("3D Tunnels")) >= 1 && modules.Count(x => x.Contains("Basic Morse")) >= 1 && modules.Count(x => x.Contains("Bitmaps")) >= 1 && modules.Count(x => x.Contains("Blind Maze")) >= 1 && modules.Count(x => x.Contains("Blinkstop")) >= 1 && modules.Count(x => x.Contains("Bloxx")) >= 1 && modules.Count(x => x.Contains("Caesar Cipher")) >= 1 && modules.Count(x => x.Contains("Character Shift")) >= 1 && modules.Count(x => x.Contains("Audio Keypad")) >= 1 && modules.Count(x => x.Contains("Daylight Directions")) >= 1 && modules.Count(x => x.Contains("Dr. Doctor")) >= 1 && modules.Count(x => x.Contains("Dragon Energy")) >= 1 && modules.Count(x => x.Contains("Dreamcipher")) >= 1 && modules.Count(x => x.Contains("European Travel")) >= 1 && modules.Count(x => x.Contains("Free Parking")) >= 1 && modules.Count(x => x.Contains("Fruits")) >= 1 && modules.Count(x => x.Contains("Green Arrows")) >= 1 && modules.Count(x => x.Contains("Hieroglyphics")) >= 1 && modules.Count(x => x.Contains("The Arena")) >= 1 && modules.Count(x => x.Contains("Human Resources")) >= 1 && modules.Count(x => x.Contains("Ingredients")) >= 1 && modules.Count(x => x.Contains("Keypad Combinations")) >= 1 && modules.Count(x => x.Contains("Lasers")) >= 1 && modules.Count(x => x.Contains("Maze³")) >= 1 && modules.Count(x => x.Contains("Minecraft Cipher")) >= 1 && modules.Count(x => x.Contains("Minecraft Survival")) >= 1 && modules.Count(x => x.Contains("More Code")) >= 1 && modules.Count(x => x.Contains("Morse Buttons")) >= 1 && modules.Count(x => x.Contains("Not Keypad")) >= 1 && modules.Count(x => x.Contains("Not Morse Code")) >= 1 && modules.Count(x => x.Contains("Not Who's on First")) >= 1 && modules.Count(x => x.Contains("Numbered Buttons")) >= 1 && modules.Count(x => x.Contains("Numbers")) >= 1 && modules.Count(x => x.Contains("Cookie Clicker")) == 1 && modules.Count(x => x.Contains("Qwirkle")) >= 1 && modules.Count(x => x.Contains("Remote Math")) >= 1 && modules.Count(x => x.Contains("Emotiguy Identification")) >= 1 && modules.Count(x => x.Contains("S.E.T.")) >= 1 && modules.Count(x => x.Contains("Shortcuts")) >= 1 && modules.Count(x => x.Contains("Simon Selects")) >= 1 && modules.Count(x => x.Contains("Simon Speaks")) >= 1 && modules.Count(x => x.Contains("Spot the Difference")) >= 1 && modules.Count(x => x.Contains("Mental Math")) >= 1 && modules.Count(x => x.Contains("Standard Crazy Talk")) >= 1 && modules.Count(x => x.Contains("IPA")) >= 1 && modules.Count(x => x.Contains("Dictation")) >= 1 && modules.Count(x => x.Contains("Ten-Button Color Code")) >= 1 && modules.Count(x => x.Contains("The Button")) >= 1 && modules.Count(x => x.Contains("Timezone")) >= 1 && modules.Count(x => x.Contains("Waste Management")) >= 1 && modules.Count(x => x.Contains("Wavetapping")) >= 1 && modules.Count(x => x.Contains("Maze")) + modules.Count(x => x.Contains("Password")) + modules.Count(x => x.Contains("1000 Words")) + modules.Count(x => x.Contains("Bone Apple Tea")) + modules.Count(x => x.Contains("Broken Guitar Chords")) + modules.Count(x => x.Contains("Module Maze")) + modules.Count(x => x.Contains("Not Maze")) + modules.Count(x => x.Contains("Not Password")) + modules.Count(x => x.Contains("Question Mark")) + modules.Count(x => x.Contains("Widdershins")) >= 6) level = 97;
        else if (moduleCount == 75 && modules.Count(x => x.Contains("14")) == 1 && modules.Count(x => x.Contains("Forget This")) == 1 && modules.Count(x => x.Contains("Dungeon 2nd Floor")) >= 1 && modules.Count(x => x.Contains("Souvenir")) == 1 && modules.Count(x => x.Contains("7")) >= 1 && modules.Count(x => x.Contains("Astrology")) >= 1 && modules.Count(x => x.Contains("Backgrounds")) >= 1 && modules.Count(x => x.Contains("Bartending")) >= 1 && modules.Count(x => x.Contains("Big Circle")) >= 1 && modules.Count(x => x.Contains("Bitwise Operations")) >= 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("The Block")) >= 1 && modules.Count(x => x.Contains("Blue Cipher")) >= 1 && modules.Count(x => x.Contains("Bomb Diffusal")) >= 1 && modules.Count(x => x.Contains("BoozleTalk")) >= 1 && modules.Count(x => x.Contains("Brawler Database")) >= 1 && modules.Count(x => x.Contains("Burglar Alarm")) >= 1 && modules.Count(x => x.Contains("Button Sequence")) >= 1 && modules.Count(x => x.Contains("Colorful Madness")) >= 1 && modules.Count(x => x.Contains("Crazy Talk")) >= 1 && modules.Count(x => x.Contains("Cryptic Password")) >= 1 && modules.Count(x => x.Contains("Daylight Directions")) >= 1 && modules.Count(x => x.Contains("The Digit")) >= 1 && modules.Count(x => x.Contains("Double-Oh")) >= 1 && modules.Count(x => x.Contains("The Festive Jukebox")) >= 1 && modules.Count(x => x.Contains("Flower Patch")) >= 1 && modules.Count(x => x.Contains("Goofy's Game")) >= 1 && modules.Count(x => x.Contains("Graffiti Numbers")) >= 1 && modules.Count(x => x.Contains("Gryphons")) >= 1 && modules.Count(x => x.Contains("Hexamaze")) >= 1 && modules.Count(x => x.Contains("The Hidden Value")) >= 1 && modules.Count(x => x.Contains("The Hypercube")) >= 1 && modules.Count(x => x.Contains("Kilo Talk")) >= 1 && modules.Count(x => x.Contains("Kyudoku")) >= 1 && modules.Count(x => x.Contains("Lasers")) >= 1 && modules.Count(x => x.Contains("Left and Right")) >= 1 && modules.Count(x => x.Contains("Lightspeed")) >= 1 && modules.Count(x => x.Contains("Logic Gates")) >= 1 && modules.Count(x => x.Contains("Logical Buttons")) >= 1 && modules.Count(x => x.Contains("Lousy Chess")) >= 1 && modules.Count(x => x.Contains("Mafia")) >= 1 && modules.Count(x => x.Contains("Mastermind Cruel")) >= 1 && modules.Count(x => x.Contains("The Matrix")) >= 1 && modules.Count(x => x.Contains("Mega Man 2")) >= 1 && modules.Count(x => x.Contains("The Modkit")) >= 1 && modules.Count(x => x.Contains("Morse-A-Maze")) >= 1 && modules.Count(x => x.Contains("Morsematics")) >= 1 && modules.Count(x => x.Contains("Number Pad")) >= 1 && modules.Count(x => x.Contains("Organization")) == 1 && modules.Count(x => x.Contains("Orientation Cube")) >= 1 && modules.Count(x => x.Contains("Pow")) >= 1 && modules.Count(x => x.Contains("Red Cipher")) >= 1 && modules.Count(x => x.Contains("Regular Crazy Talk")) >= 1 && modules.Count(x => x.Contains("Rhythms")) >= 1 && modules.Count(x => x.Contains("Role Reversal")) >= 1 && modules.Count(x => x.Contains("Roman Art")) >= 1 && modules.Count(x => x.Contains("Semamorse")) >= 1 && modules.Count(x => x.Contains("Simon Samples")) >= 1 && modules.Count(x => x.Contains("Simon Sings")) >= 1 && modules.Count(x => x.Contains("Skinny Wires")) >= 1 && modules.Count(x => x.Contains("Square Button")) >= 1 && modules.Count(x => x.Contains("The Stare")) >= 1 && modules.Count(x => x.Contains("The Triangle")) >= 1 && modules.Count(x => x.Contains("Turn The Key")) == 1 && modules.Count(x => x.Contains("Unfair Cipher")) >= 1 && modules.Count(x => x.Contains("Unicode")) >= 1 && modules.Count(x => x.Contains("Unown Cipher")) >= 1 && modules.Count(x => x.Contains("Audio Morse")) + modules.Count(x => x.Contains("Colour Flash")) + modules.Count(x => x.Contains("Constellations")) + modules.Count(x => x.Contains("Dimension Disruption")) + modules.Count(x => x.Contains("LED Grid")) + modules.Count(x => x.Contains("Logic")) + modules.Count(x => x.Contains("Numbers")) + modules.Count(x => x.Contains("Periodic Table")) + modules.Count(x => x.Contains("Semaphore")) + modules.Count(x => x.Contains("Symbolic Password")) >= 7) level = 98;
        else if (moduleCount == 89 && modules.Count(x => x.Contains("Forget Everything")) == 1 && modules.Count(x => x.Contains("Forget Me Not")) == 1 && modules.Count(x => x.Contains("Dungeon 2nd Floor")) >= 1 && modules.Count(x => x.Contains("Souvenir")) == 1 && modules.Count(x => x.Contains("3D Maze")) >= 1 && modules.Count(x => x.Contains("Addition")) >= 1 && modules.Count(x => x.Contains("Alchemy")) >= 1 && modules.Count(x => x.Contains("Algebra")) >= 1 && modules.Count(x => x.Contains("Bamboozling Button Grid")) >= 1 && modules.Count(x => x.Contains("Benedict Cumberbatch")) >= 1 && modules.Count(x => x.Contains("Binary")) >= 1 && modules.Count(x => x.Contains("Black Hole")) >= 3 && modules.Count(x => x.Contains("Blind Alley")) >= 1 && modules.Count(x => x.Contains("Broken Buttons")) >= 1 && modules.Count(x => x.Contains("Caesar Cycle")) >= 1 && modules.Count(x => x.Contains("Cheap Checkout")) >= 1 && modules.Count(x => x.Contains("Color Decoding")) >= 1 && modules.Count(x => x.Contains("Color Generator")) >= 1 && modules.Count(x => x.Contains("Colour Flash")) >= 1 && modules.Count(x => x.Contains("Cooking")) >= 1 && modules.Count(x => x.Contains("Countdown")) >= 1 && modules.Count(x => x.Contains("Cruel Piano Keys")) >= 1 && modules.Count(x => x.Contains("Cryptography")) >= 1 && modules.Count(x => x.Contains("The Crystal Maze")) >= 1 && modules.Count(x => x.Contains("hexOS")) >= 1 && modules.Count(x => x.Contains("Decolored Squares")) >= 1 && modules.Count(x => x.Contains("Digital Root")) >= 1 && modules.Count(x => x.Contains("Double Color")) >= 1 && modules.Count(x => x.Contains("Double-Oh")) >= 1 && modules.Count(x => x.Contains("Fast Math")) >= 1 && modules.Count(x => x.Contains("Fencing")) >= 1 && modules.Count(x => x.Contains("Flashing Lights")) >= 1 && modules.Count(x => x.Contains("Flavor Text")) >= 1 && modules.Count(x => x.Contains("Following Orders")) >= 1 && modules.Count(x => x.Contains("The Cube")) >= 1 && modules.Count(x => x.Contains("Functions")) >= 1 && modules.Count(x => x.Contains("Game of Life Simple")) >= 1 && modules.Count(x => x.Contains("Gridlock")) >= 1 && modules.Count(x => x.Contains("The Heart")) == 1 && modules.Count(x => x.Contains("The High Score")) >= 1 && modules.Count(x => x.Contains("The iPhone")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Kudosudoku")) >= 1 && modules.Count(x => x.Contains("Laundry")) >= 1 && modules.Count(x => x.Contains("Letter Keys")) >= 1 && modules.Count(x => x.Contains("Listening")) >= 1 && modules.Count(x => x.Contains("Logic Gates")) >= 1 && modules.Count(x => x.Contains("Mastermind Simple")) >= 1 && modules.Count(x => x.Contains("Minesweeper")) >= 1 && modules.Count(x => x.Contains("Misery Squares")) >= 1 && modules.Count(x => x.Contains("Module Maze")) >= 1 && modules.Count(x => x.Contains("Modulo")) >= 1 && modules.Count(x => x.Contains("Modulus Manipulation")) >= 1 && modules.Count(x => x.Contains("Monsplode Trading Cards")) >= 1 && modules.Count(x => x.Contains("Mystery Module")) == 1 && modules.Count(x => x.Contains("Number Pad")) >= 1 && modules.Count(x => x.Contains("NumberWang")) >= 1 && modules.Count(x => x.Contains("Ordered Keys")) >= 1 && modules.Count(x => x.Contains("Pattern Cube")) >= 1 && modules.Count(x => x.Contains("Pie")) >= 1 && modules.Count(x => x.Contains("Point of Order")) >= 1 && modules.Count(x => x.Contains("Press X")) >= 1 && modules.Count(x => x.Contains("Probing")) >= 1 && modules.Count(x => x.Contains("Purgatory")) == 1 && modules.Count(x => x.Contains("Rainbow Arrows")) >= 1 && modules.Count(x => x.Contains("Red Arrows")) >= 1 && modules.Count(x => x.Contains("Red Herring")) >= 1 && modules.Count(x => x.Contains("Rubik's Cube")) >= 1 && modules.Count(x => x.Contains("Schlag den Bomb")) >= 1 && modules.Count(x => x.Contains("Subscribe to Pewdiepie")) >= 1 && modules.Count(x => x.Contains("Switches")) >= 1 && modules.Count(x => x.Contains("The Time Keeper")) == 1 && modules.Count(x => x.Contains("Treasure Hunt")) >= 1 && modules.Count(x => x.Contains("The Triangle Button")) >= 1 && modules.Count(x => x.Contains("Two Bits")) >= 1 && modules.Count(x => x.Contains("T-Words")) >= 1 && modules.Count(x => x.Contains("Uncolored Squares")) >= 1 && modules.Count(x => x.Contains("Who's on First")) >= 1 && modules.Count(x => x.Contains("Memory")) + modules.Count(x => x.Contains("Boolean Venn Diagram")) + modules.Count(x => x.Contains("Boot Too Big")) + modules.Count(x => x.Contains("Complicated Buttons")) + modules.Count(x => x.Contains("Fruits")) + modules.Count(x => x.Contains("Homophones")) + modules.Count(x => x.Contains("Mashematics")) + modules.Count(x => x.Contains("The Number Cipher")) + modules.Count(x => x.Contains("Shape Shift")) + modules.Count(x => x.Contains("Morse Code")) >= 8) level = 99;
        else if (moduleCount == 105 && modules.Count(x => x.Contains("Forget Everything")) == 1 && modules.Count(x => x.Contains("Forget Enigma")) == 1 && modules.Count(x => x.Contains("Dungeon 2nd Floor")) >= 1 && modules.Count(x => x.Contains("Souvenir")) == 1 && modules.Count(x => x.Contains("...?")) >= 1 && modules.Count(x => x.Contains("3 LEDs")) >= 1 && modules.Count(x => x.Contains("Adjacent Letters")) >= 1 && modules.Count(x => x.Contains("Adventure Game")) >= 1 && modules.Count(x => x.Contains("Alliances")) >= 1 && modules.Count(x => x.Contains("Art Appreciation")) >= 1 && modules.Count(x => x.Contains("Astrology")) >= 1 && modules.Count(x => x.Contains("Bases")) >= 1 && modules.Count(x => x.Contains("Big Circle")) >= 1 && modules.Count(x => x.Contains("Binary Puzzle")) >= 1 && modules.Count(x => x.Contains("Bitmaps")) >= 1 && modules.Count(x => x.Contains("Black Hole")) >= 1 && modules.Count(x => x.Contains("Blind Alley")) >= 1 && modules.Count(x => x.Contains("Braille")) >= 1 && modules.Count(x => x.Contains("Chess")) >= 1 && modules.Count(x => x.Contains("Christmas Presents")) >= 1 && modules.Count(x => x.Contains("The Clock")) >= 1 && modules.Count(x => x.Contains("The Colored Maze")) >= 1 && modules.Count(x => x.Contains("Constellations")) >= 1 && modules.Count(x => x.Contains("Coordinates")) >= 1 && modules.Count(x => x.Contains("Creation")) >= 1 && modules.Count(x => x.Contains("Cruel Keypads")) >= 1 && modules.Count(x => x.Contains("Double-Oh")) >= 1 && modules.Count(x => x.Contains("Dungeon")) >= 1 && modules.Count(x => x.Contains("Elder Futhark")) >= 1 && modules.Count(x => x.Contains("Encrypted Morse")) >= 1 && modules.Count(x => x.Contains("Extended Password")) >= 1 && modules.Count(x => x.Contains("The Festive Jukebox")) >= 1 && modules.Count(x => x.Contains("Festive Piano Keys")) >= 1 && modules.Count(x => x.Contains("Find The Date")) >= 1 && modules.Count(x => x.Contains("Follow the Leader")) >= 1 && modules.Count(x => x.Contains("Friendship")) >= 1 && modules.Count(x => x.Contains("Functions")) >= 1 && modules.Count(x => x.Contains("The Giant's Drink")) >= 1 && modules.Count(x => x.Contains("Goofy's Game")) >= 1 && modules.Count(x => x.Contains("Gridlock")) >= 1 && modules.Count(x => x.Contains("The Hexabutton")) >= 1 && modules.Count(x => x.Contains("The High Score")) >= 1 && modules.Count(x => x.Contains("Homophones")) >= 1 && modules.Count(x => x.Contains("Horrible Memory")) >= 1 && modules.Count(x => x.Contains("The Jewel Vault")) >= 1 && modules.Count(x => x.Contains("Know Your Way")) >= 1 && modules.Count(x => x.Contains("The Labyrinth")) >= 1 && modules.Count(x => x.Contains("Left and Right")) >= 1 && modules.Count(x => x.Contains("Life Iteration")) >= 1 && modules.Count(x => x.Contains("Light Cycle")) >= 1 && modules.Count(x => x.Contains("Lockpick Maze")) >= 1 && modules.Count(x => x.Contains("The Modkit")) >= 1 && modules.Count(x => x.Contains("Module Listening")) >= 1 && modules.Count(x => x.Contains("Monsplode, Fight!")) >= 1 && modules.Count(x => x.Contains("Monsplode Trading Cards")) >= 1 && modules.Count(x => x.Contains("The Moon")) >= 1 && modules.Count(x => x.Contains("Mystery Module")) == 1 && modules.Count(x => x.Contains("Only Connect")) >= 1 && modules.Count(x => x.Contains("Painting")) >= 1 && modules.Count(x => x.Contains("Password Destroyer")) == 1 && modules.Count(x => x.Contains("Password Generator")) >= 1 && modules.Count(x => x.Contains("Periodic Table")) >= 1 && modules.Count(x => x.Contains("Perplexing Wires")) >= 1 && modules.Count(x => x.Contains("Piano Keys")) >= 1 && modules.Count(x => x.Contains("Planets")) >= 1 && modules.Count(x => x.Contains("Rhythms")) >= 1 && modules.Count(x => x.Contains("Roger")) >= 1 && modules.Count(x => x.Contains("Round Keypad")) >= 1 && modules.Count(x => x.Contains("The Rule")) >= 1 && modules.Count(x => x.Contains("Shape Shift")) >= 1 && modules.Count(x => x.Contains("Signals")) >= 1 && modules.Count(x => x.Contains("Simon Screams")) >= 1 && modules.Count(x => x.Contains("Simon Stages")) >= 1 && modules.Count(x => x.Contains("The Sphere")) >= 1 && modules.Count(x => x.Contains("The Stare")) >= 1 && modules.Count(x => x.Contains("Stars")) >= 1 && modules.Count(x => x.Contains("The Stopwatch")) >= 1 && modules.Count(x => x.Contains("The Sun")) >= 1 && modules.Count(x => x.Contains("Switches")) >= 1 && modules.Count(x => x.Contains("Symbol Cycle")) >= 1 && modules.Count(x => x.Contains("Symbolic Coordinates")) >= 1 && modules.Count(x => x.Contains("Synonyms")) >= 1 && modules.Count(x => x.Contains("Thinking Wires")) >= 1 && modules.Count(x => x.Contains("Third Base")) >= 1 && modules.Count(x => x.Contains("Tower of Hanoi")) >= 1 && modules.Count(x => x.Contains("Ultimate Cipher")) >= 1 && modules.Count(x => x.Contains("Unown Cipher")) >= 1 && modules.Count(x => x.Contains("Vigenère Cipher")) >= 1 && modules.Count(x => x.Contains("The Wire")) >= 1 && modules.Count(x => x.Contains("Wires")) >= 1 && modules.Count(x => x.Contains("The Witness")) >= 1 && modules.Count(x => x.Contains("Wonder Cipher")) >= 1 && modules.Count(x => x.Contains("Yes and No")) >= 1 && modules.Count(x => x.Contains("Zoo")) >= 1 && modules.Count(x => x.Contains("1000 Words")) + modules.Count(x => x.Contains("Algebra")) + modules.Count(x => x.Contains("Binary LEDs")) + modules.Count(x => x.Contains("Colour Flash PL")) + modules.Count(x => x.Contains("English Test")) + modules.Count(x => x.Contains("Flavor Text")) + modules.Count(x => x.Contains("Genetic Sequence")) + modules.Count(x => x.Contains("Polygons")) + modules.Count(x => x.Contains("Superlogic")) + modules.Count(x => x.Contains("Yahtzee")) >= 10) level = 100;


        // Determines the number of solves needed and the letters used in the cipher
        switch (level) {
        case 1: solvesNeeded = 1; letters = 3; break;
        case 2: solvesNeeded = 2; letters = 3; break;
        case 3: solvesNeeded = 3; letters = 3; break;
        case 4: solvesNeeded = 4; letters = 4; break;
        case 5: solvesNeeded = 5; letters = 4; break;
        case 6: solvesNeeded = 6; letters = 4; break;
        case 7: solvesNeeded = 7; letters = 4; break;
        case 8: solvesNeeded = 8; letters = 5; break;
        case 9: solvesNeeded = 8; letters = 5; break;
        case 10: solvesNeeded = 9; letters = 5; break;
        case 11: solvesNeeded = 9; letters = 5; break;
        case 12: solvesNeeded = 10; letters = 5; break;
        case 13: solvesNeeded = 10; letters = 6; break;
        case 14: solvesNeeded = 11; letters = 6; break;
        case 15: solvesNeeded = 11; letters = 6; break;
        case 16: solvesNeeded = 12; letters = 6; break;
        case 17: solvesNeeded = 12; letters = 6; break;
        case 18: solvesNeeded = 13; letters = 6; break;
        case 19: solvesNeeded = 13; letters = 6; break;
        case 20: solvesNeeded = 14; letters = 6; break;
        case 21: solvesNeeded = 14; letters = 7; break;
        case 22: solvesNeeded = 15; letters = 7; break;
        case 23: solvesNeeded = 15; letters = 7; break;
        case 24: solvesNeeded = 15; letters = 7; break;
        case 25: solvesNeeded = 16; letters = 7; break;
        case 26: solvesNeeded = 16; letters = 7; break;
        case 27: solvesNeeded = 16; letters = 7; break;
        case 28: solvesNeeded = 16; letters = 7; break;
        case 29: solvesNeeded = 17; letters = 7; break;
        case 30: solvesNeeded = 17; letters = 8; break;
        case 31: solvesNeeded = 18; letters = 8; break;
        case 32: solvesNeeded = 18; letters = 8; break;
        case 33: solvesNeeded = 18; letters = 8; break;
        case 34: solvesNeeded = 19; letters = 8; break;
        case 35: solvesNeeded = 19; letters = 8; break;
        case 36: solvesNeeded = 19; letters = 8; break;
        case 37: solvesNeeded = 20; letters = 8; break;
        case 38: solvesNeeded = 20; letters = 8; break;
        case 39: solvesNeeded = 21; letters = 8; break;
        case 40: solvesNeeded = 21; letters = 8; break;
        case 41: solvesNeeded = 22; letters = 8; break;
        case 42: solvesNeeded = 22; letters = 8; break;
        case 43: solvesNeeded = 22; letters = 8; break;
        case 44: solvesNeeded = 23; letters = 9; break;
        case 45: solvesNeeded = 23; letters = 9; break;
        case 46: solvesNeeded = 23; letters = 9; break;
        case 47: solvesNeeded = 24; letters = 9; break;
        case 48: solvesNeeded = 24; letters = 9; break;
        case 49: solvesNeeded = 24; letters = 9; break;
        case 50: solvesNeeded = 24; letters = 9; break;
        case 51: solvesNeeded = 24; letters = 9; break;
        case 52: solvesNeeded = 24; letters = 9; break;
        case 53: solvesNeeded = 24; letters = 9; break;
        case 54: solvesNeeded = 24; letters = 9; break;
        case 55: solvesNeeded = 25; letters = 9; break;
        case 56: solvesNeeded = 25; letters = 9; break;
        case 57: solvesNeeded = 25; letters = 9; break;
        case 58: solvesNeeded = 25; letters = 9; break;
        case 59: solvesNeeded = 25; letters = 9; break;
        case 60: solvesNeeded = 25; letters = 9; break;
        case 61: solvesNeeded = 25; letters = 10; break;
        case 62: solvesNeeded = 25; letters = 10; break;
        case 63: solvesNeeded = 25; letters = 10; break;
        case 64: solvesNeeded = 25; letters = 10; break;
        case 65: solvesNeeded = 26; letters = 10; break;
        case 66: solvesNeeded = 26; letters = 10; break;
        case 67: solvesNeeded = 26; letters = 10; break;
        case 68: solvesNeeded = 26; letters = 10; break;
        case 69: solvesNeeded = 26; letters = 10; break;
        case 70: solvesNeeded = 26; letters = 10; break;
        case 71: solvesNeeded = 26; letters = 10; break;
        case 72: solvesNeeded = 26; letters = 10; break;
        case 73: solvesNeeded = 26; letters = 10; break;
        case 74: solvesNeeded = 26; letters = 10; break;
        case 75: solvesNeeded = 26; letters = 10; break;
        case 76: solvesNeeded = 26; letters = 10; break;
        case 77: solvesNeeded = 26; letters = 10; break;
        case 78: solvesNeeded = 26; letters = 10; break;
        case 79: solvesNeeded = 27; letters = 10; break;
        case 80: solvesNeeded = 27; letters = 11; break;
        case 81: solvesNeeded = 27; letters = 11; break;
        case 82: solvesNeeded = 27; letters = 11; break;
        case 83: solvesNeeded = 28; letters = 11; break;
        case 84: solvesNeeded = 28; letters = 11; break;
        case 85: solvesNeeded = 28; letters = 11; break;
        case 86: solvesNeeded = 28; letters = 11; break;
        case 87: solvesNeeded = 29; letters = 11; break;
        case 88: solvesNeeded = 29; letters = 11; break;
        case 89: solvesNeeded = 29; letters = 11; break;
        case 90: solvesNeeded = 29; letters = 11; break;
        case 91: solvesNeeded = 30; letters = 11; break;
        case 92: solvesNeeded = 30; letters = 11; break;
        case 93: solvesNeeded = 30; letters = 11; break;
        case 94: solvesNeeded = 30; letters = 11; break;
        case 95: solvesNeeded = 30; letters = 11; break;
        case 96: solvesNeeded = 30; letters = 11; break;
        case 97: solvesNeeded = 30; letters = 12; break;
        case 98: solvesNeeded = 30; letters = 12; break;
        case 99: solvesNeeded = 30; letters = 12; break;
        case 100: solvesNeeded = 30; letters = 12; break;
        default: solvesNeeded = 1; letters = FIXLETTERS; levelFound = false; break; // This doesn't actually require 1 solve
        }

        if (levelFound == true)
            Debug.LogFormat("[100 Levels of Defusal #{0}] Initiating Level {1}. Number of solves needed to unlock cipher: {2}", moduleId, level, solvesNeeded);
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} submit <ans> [Submits an answer of 'ans'] | Valid answers have only letters";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (!solvesReached && levelFound)
            {
                yield return "sendtochaterror The module must unlock before an answer can be submitted!";
                yield break;
            }
            if (lockButtons)
            {
                yield return "sendtochaterror An answer cannot be submitted while the letters are not white!";
                yield break;
            }
            if (letterSlotsUsed != parameters[1].Length)
            {
                yield return "sendtochaterror An answer of length '" + parameters[1].Length + "' cannot be submitted!";
                yield break;
            }
            for (int i = 0; i < parameters[1].Length; i++)
            {
                if (!LETTERS.Contains(parameters[1][i].ToString().ToUpper()))
                {
                    yield return "sendtochaterror The specified answer to submit '" + parameters[1] + "' is invalid!";
                    yield break;
                }
            }
            for (int i = 0; i < parameters[1].Length; i++)
            {
                int forct = 0;
                int backct = 0;
                int counter = letterIndexes[lettersUsed[i]];
                while (counter != Array.IndexOf(LETTERS, parameters[1][i].ToString().ToUpper()))
                {
                    counter++;
                    if (counter == 26)
                        counter = 0;
                    forct++;
                }
                counter = letterIndexes[lettersUsed[i]];
                while (counter != Array.IndexOf(LETTERS, parameters[1][i].ToString().ToUpper()))
                {
                    counter--;
                    if (counter == -1)
                        counter = 25;
                    backct++;
                }
                if (forct > backct)
                {
                    if (direction == true)
                    {
                        ToggleBtn.OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                    for (int j = 0; j < backct; j++)
                    {
                        Letters[lettersUsed[i]].OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                else if (forct < backct)
                {
                    if (direction == false)
                    {
                        ToggleBtn.OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                    for (int j = 0; j < forct; j++)
                    {
                        Letters[lettersUsed[i]].OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                else
                {
                    if (UnityEngine.Random.Range(0, 2) == 0)
                    {
                        ToggleBtn.OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                    for (int j = 0; j < forct; j++)
                    {
                        Letters[lettersUsed[i]].OnInteract();
                        yield return new WaitForSeconds(0.05f);
                    }
                }
            }
            SubmitBtn.OnInteract();
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (levelFound)
        {
            while (!solvesReached) { yield return true; yield return new WaitForSeconds(0.1f); }
        }
        while (lockButtons) { yield return true; yield return new WaitForSeconds(0.1f); }
        yield return ProcessTwitchCommand("submit " + correctMessage);
        while (!moduleSolved) { yield return true; yield return new WaitForSeconds(0.1f); }
    }
}