using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using static CharacterManager;
using UnityEditor;

public class AIStoryBrancher : MonoBehaviour
{
    public Characters Aoi;
    // --- OpenAI API Configuration ---
    [SerializeField] private string openaiApiKey = "sk-YOUR_OPENAI_API_KEY_HERE";
    private string openaiEndpoint = "https://api.openai.com/v1/chat/completions";

    // --- Story Text Source ---
    public TextAsset rawStoryTextFile;

    // --- UI Component References ---
    public TextMeshProUGUI storyTextDisplay;
    public TextMeshProUGUI speakerNameDisplay;
    public TextMeshProUGUI statusTextDisplay; // Used to display loading or error status

    // --- Option UI References ---
    public GameObject optionsPanel; // Options panel (contains all buttons)
    public GameObject optionButtonPrefab; // Your option button prefab
    public ButtonManager bm;

    // --- Internal State Variables ---
    private string[] processedDialogueLines; // AI generated story text
    private int currentLineIndex = 0;
    private bool isProcessingAI = false;
    private bool isTypingText = false;
    private bool isWaitingForUserInput = false; // Indicates waiting for user to click screen to advance dialogue
    public bool isAuto = false;
    private bool isShowingOptions = false;     // Indicates currently displaying options, waiting for user selection
    private List<GameObject> currentOptionButtons = new List<GameObject>();

    public float typeSpeed = 0.05f;
    [SerializeField] private float optionButtonSpacing = 20f; // Spacing between option buttons

    // History
    private List<string> DialogueHistory = new List<string>();
    [SerializeField] private int maxSimpleHistoryLines = 5; // Maximum number of history lines, settable in Inspector

    void Start()
    {
        // Check if UI components and option components are assigned
        Aoi = CharacterManager.instance.GetCharacter("Aoi", enableCreatedCharacterOnStart: false);
        if (storyTextDisplay == null || speakerNameDisplay == null || statusTextDisplay == null ||
            optionsPanel == null || optionButtonPrefab == null)
        {
            Debug.LogError("VNStoryLoader: Some UI components or option prefab not assigned! Please check Inspector.");
            if (statusTextDisplay != null) statusTextDisplay.text = "Error: UI/Option components not assigned.";
            return;
        }

        // Bind option button click events (dynamically added in ShowOptions)
        optionsPanel.SetActive(false); // Ensure options panel is initially hidden

        // Check raw story text file
        if (rawStoryTextFile == null)
        {
            statusTextDisplay.text = "Raw Story Text File (TextAsset) is not assigned!";
            Debug.LogError("VNStoryLoader: Raw Story Text File (TextAsset) is not assigned!");
            return;
        }

        // Start AI processing flow
        StartCoroutine(ProcessStoryWithAI());
    }

    void Update()
    {
        //if (!isProcessingAI && !isTypingText && !isShowingOptions && isWaitingForUserInput && !isAuto)
        //{
        //    if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        //    {
        //        DisplayNextLine();
        //    }
        //}
        //if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        //{
        //    isAuto = false;
        //}


        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (storyTextDisplay.text == "The end" || storyTextDisplay.text == "The end.")
            {
                EditorApplication.isPlaying = false;
            }
            if (!isProcessingAI && !isTypingText && !isShowingOptions && isWaitingForUserInput && !isAuto)
                DisplayNextLine();
            else if (isAuto)
            {
                bm.cancelInvoke();
            }
        }
    }

    // --- AI Story Text Processing Flow (remains unchanged) ---
    IEnumerator ProcessStoryWithAI()
    {
        isProcessingAI = true;
        statusTextDisplay.text = "waiting ...";
        speakerNameDisplay.text = "";
        storyTextDisplay.text = "";

        if (string.IsNullOrEmpty(openaiApiKey) || openaiApiKey == "sk-YOUR_OPENAI_API_KEY_HERE")
        {
            string errorMsg = "Error: OpenAI API Key not set or incorrect! Unable to process story text.";
            statusTextDisplay.text = errorMsg;
            Debug.LogError("VNStoryLoader: " + errorMsg);
            isProcessingAI = false;
            yield break;
        }

        string storyContent = rawStoryTextFile.text;
        if (string.IsNullOrEmpty(storyContent))
        {
            string errorMsg = "Error: Story text file is empty!";
            statusTextDisplay.text = errorMsg;
            Debug.LogError("VNStoryLoader: " + errorMsg);
            isProcessingAI = false;
            yield break;
        }

        Debug.Log("VNStoryLoader: Sending story text to OpenAI for processing...");

        string prompt = GenerateAIPrompt(storyContent);
        string jsonBody = "{" +
                          "\"model\": \"gpt-4o\"," +
                          "\"messages\": [" +
                          "{\"role\": \"system\", \"content\": \"" + EscapeJsonString(prompt) + "\"}" +
                          "]," +
                          "\"temperature\": 0.3," +
                          "\"max_tokens\": 2000" +
                          "}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(openaiEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            string errorMsg = "AI processing failed: Network or API error.\n" + request.error + "\nStatus Code: " + request.responseCode;
            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                errorMsg += "\nResponse: " + request.downloadHandler.text;
            }
            statusTextDisplay.text = errorMsg;
            Debug.LogError("VNStoryLoader: " + errorMsg);
        }
        else
        {
            string rawResponse = request.downloadHandler.text;
            Debug.Log("VNStoryLoader: OpenAI Raw Response: " + rawResponse);

            try
            {
                JSONNode jsonResponse = SimpleJSON.JSON.Parse(rawResponse);
                string aiFormattedText = jsonResponse["choices"][0]["message"]["content"].Value;
                Debug.Log("VNStoryLoader: AI Formatted Text:\n" + aiFormattedText);

                processedDialogueLines = aiFormattedText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                statusTextDisplay.text = "";
                storyTextDisplay.text = "Success.Click the screen to start the story.";
                speakerNameDisplay.text = "";

                currentLineIndex = 0;
                isWaitingForUserInput = true;
            }
            catch (System.Exception e)
            {
                string parseError = "AI response parsing error: " + e.Message + "\nRaw Response: " + rawResponse;
                statusTextDisplay.text = parseError;
                Debug.LogError("VNStoryLoader: " + parseError);
            }
        }
        isProcessingAI = false;
    }

    public IEnumerator Auto()
    {
        isAuto = true;
        if (isTypingText)
        {

        }
        else
        {
            DisplayNextLine();
            yield return new WaitForEndOfFrame();
        }

    }

    // --- Display Next Line ---
    void DisplayNextLine()
    {
        if (isTypingText)
        {
            StopAllCoroutines();
            if (currentLineIndex > 0 && processedDialogueLines != null && currentLineIndex <= processedDialogueLines.Length)
            {
                string prevLine = processedDialogueLines[currentLineIndex - 1];
                string[] parts = prevLine.Split(new char[] { ':' }, 2);
                storyTextDisplay.text = parts[0].Replace("\"", "");
            }
            isTypingText = false;
            isWaitingForUserInput = true;
            return;
        }

        isWaitingForUserInput = false;

        if (processedDialogueLines == null || currentLineIndex >= processedDialogueLines.Length)
        {
            storyTextDisplay.text = " ";
            speakerNameDisplay.text = "";
            isWaitingForUserInput = false;
            Debug.Log("VNStoryLoader: Dialogue ended.");
            return;
        }

        string line = processedDialogueLines[currentLineIndex];

        // --- Correction: More precisely identify ":[OPTIONS]..." at the end of the sentence ---
        // Ensure line has removed outermost double quotes and possible trailing colon (if AI outputs it)
        string cleanedLineForOptionsCheck = line.Trim();
        if (cleanedLineForOptionsCheck.StartsWith("\"") && cleanedLineForOptionsCheck.EndsWith("\""))
        {
            cleanedLineForOptionsCheck = cleanedLineForOptionsCheck.Substring(1, cleanedLineForOptionsCheck.Length - 2);
        }
        if (cleanedLineForOptionsCheck.EndsWith(":")) // If AI also adds a colon after OPTIONS
        {
            cleanedLineForOptionsCheck = cleanedLineForOptionsCheck.TrimEnd(':');
        }

        // Check if it contains ":[OPTIONS]" pattern
        // StringComparison.Ordinal ensures exact match, unaffected by culture
        int optionsIndex = cleanedLineForOptionsCheck.IndexOf(":[OPTIONS]", StringComparison.Ordinal);

        if (optionsIndex != -1) // Found ":[OPTIONS]"
        {
            Debug.Log($"VNStoryLoader: Detected end-of-sentence option command at index {optionsIndex} in line: '{line}'");

            // Split text content and option command
            string contentBeforeOptions = cleanedLineForOptionsCheck.Substring(0, optionsIndex); // Text content
            string optionsData = cleanedLineForOptionsCheck.Substring(optionsIndex + ":".Length); // Part after ":[OPTIONS]..."

            // Process text content (typewriter effect)
            string[] dialogueParts = line.Split(new char[] { ':' }, 2); // Still use original line to parse speaker
            string speaker = (dialogueParts.Length > 1) ? dialogueParts[1].Trim() : "";
            
            speakerNameDisplay.text = "";
            // speakerNameDisplay.text = speaker;
            StartCoroutine(TypeTextAndShowOptions(contentBeforeOptions, optionsData));

            // Call AddSimpleLineToHistory, passing dialogue content
            AddSimpleLineToHistory(speaker + ":" + contentBeforeOptions); // Format as "Character Name:Text"

            currentLineIndex++; // Advance to next line, as current line is processed
            return; // Pause dialogue flow, wait for user selection
        }

        // --- Below is regular dialogue and narration processing (if no options matched above) ---
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("#"))
        {
            Debug.Log($"VNStoryLoader: Skipping empty/comment line: '{line}'");

            currentLineIndex++;
            DisplayNextLine();
            return;
        }

        string[] regularDialogueParts = line.Split(new char[] { ':' }, 2);
        string regularContent = regularDialogueParts[0].Replace("\"", "");
        string regularSpeaker = (regularDialogueParts.Length > 1) ? regularDialogueParts[1].Trim() : "";

        if (regularContent.StartsWith("--") && regularContent.EndsWith("--"))
        {
            speakerNameDisplay.text = "";
            StartCoroutine(TypeText(regularContent.Replace("--", "").Trim()));
        }
        else
        {
            speakerNameDisplay.text = regularSpeaker;
            Debug.Log("Speaker parsed as: '" + regularSpeaker + "'");
            if (regularSpeaker == "Aoi")
            {
                int index = UnityEngine.Random.Range(0, 11);
                Aoi.SetBody(index);
                //Aoi.Activate();
                //Aoi.SetAlpha(0.9f,true);

            }
            StartCoroutine(TypeText(regularContent));
        }
        // Call AddSimpleLineToHistory, passing dialogue content
        AddSimpleLineToHistory("--" + regularContent.Replace("--", "").Trim() + "--:"); // Keep "--Text--:" format
        currentLineIndex++;
    }
    public void ChangeFace()
    {
        int index = UnityEngine.Random.Range(0, 11);
        Aoi.SetBody(index);
    }
    // --- New: Coroutine to type text and show options ---
    IEnumerator TypeTextAndShowOptions(string fullText, string optionsData)
    {
        isTypingText = true;
        storyTextDisplay.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            storyTextDisplay.text += fullText[i];
            yield return new WaitForSeconds(typeSpeed);
        }
        isTypingText = false;
        isWaitingForUserInput = true; // Text finished displaying

        // Once text is displayed, immediately show options
        ShowOptionsInternal(optionsData); // Internal call to ShowOptions
    }

    // --- Typewriter text effect (remains unchanged) ---
    IEnumerator TypeText(string fullText)
    {
        isTypingText = true;
        storyTextDisplay.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            storyTextDisplay.text += fullText[i];
            yield return new WaitForSeconds(typeSpeed);
        }
        isTypingText = false;
        isWaitingForUserInput = true;
    }

    // --- Option Logic: Dynamically create buttons (extracted from ShowOptions into internal method) ---
    // This method is now only responsible for processing the option string and displaying the UI,
    // not directly called by DisplayNextLine.
    void ShowOptionsInternal(string optionsDataString)
    {
        isShowingOptions = true; // Mark as showing options
        isWaitingForUserInput = false; // No longer waiting to advance dialogue, but waiting for option selection
        // storyTextDisplay.text is no longer cleared, as text has already been displayed
        // speakerNameDisplay.text is also not cleared

        optionsPanel.SetActive(true); // Show options panel

        // Clear all old dynamically created buttons
        foreach (GameObject btn in currentOptionButtons)
        {
            Destroy(btn);
        }
        currentOptionButtons.Clear();

        // Parse option text: optionsDataString already contains [OPTIONS]...
        string optionsString = optionsDataString.Replace("[OPTIONS]", "").Trim();
        string[] options = optionsString.Split('|');

        if (options.Length == 0)
        {
            Debug.LogWarning("VNStoryLoader: No option text parsed! Please check format after :[OPTIONS].");
            HideOptions(); // Unable to create options, hide panel directly
            return;
        }

        RectTransform panelRect = optionsPanel.GetComponent<RectTransform>();
        float currentYOffset = 0f; // Used for vertical button stacking

        // Dynamically create and configure buttons
        for (int i = 0; i < options.Length; i++)
        {
            if (optionButtonPrefab == null)
            {
                Debug.LogError("VNStoryLoader: Option button prefab not assigned! Cannot create options.");
                return;
            }

            GameObject newButtonGO = Instantiate(optionButtonPrefab, optionsPanel.transform);
            newButtonGO.name = $"OptionButton_{i}";
            currentOptionButtons.Add(newButtonGO); // Add to list for later management

            Button newButton = newButtonGO.GetComponent<Button>();
            TextMeshProUGUI buttonText = newButtonGO.GetComponentInChildren<TextMeshProUGUI>();

            if (newButton == null || buttonText == null)
            {
                Debug.LogError($"VNStoryLoader: Prefab '{optionButtonPrefab.name}' is missing Button or TextMeshProUGUI component.");
                Destroy(newButtonGO);
                continue;
            }

            buttonText.text = options[i].Trim();
            newButton.gameObject.SetActive(true);

            // Set button position (simple vertical stacking)
            RectTransform buttonRect = newButtonGO.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0.5f, 1f); // Anchor at top-center of parent panel
                buttonRect.anchorMax = new Vector2(0.5f, 1f);
                buttonRect.pivot = new Vector2(0.5f, 1f); // Pivot point at top-center of button

                buttonRect.anchoredPosition = new Vector2(0f, -currentYOffset);
                currentYOffset += buttonRect.sizeDelta.y + optionButtonSpacing; // Accumulate offset for next button
            }

            // Add click event listener
            int optionIndex = i; // Capture loop variable
            newButton.onClick.AddListener(() => OnOptionSelected(optionIndex));
        }
    }

    void HideOptions()
    {
        isShowingOptions = false;
        optionsPanel.SetActive(false);
        // Destroy all dynamically created buttons
        foreach (GameObject btn in currentOptionButtons)
        {
            Destroy(btn);
        }
        currentOptionButtons.Clear();
    }

    // Called when the user clicks an option button
    void OnOptionSelected(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= currentOptionButtons.Count) return;

        TextMeshProUGUI selectedButtonText = currentOptionButtons[optionIndex].GetComponentInChildren<TextMeshProUGUI>();
        string selectedOptionText = (selectedButtonText != null) ? selectedButtonText.text : "Unknown Option";

        Debug.Log("VNStoryLoader: User selected: " + selectedOptionText);

        HideOptions(); // Hide option interface

        StartCoroutine(SendOptionToAI(selectedOptionText));
    }

    IEnumerator SendOptionToAI(string selectedOption)
    {
        isProcessingAI = true;
        statusTextDisplay.text = "Now waiting AI reply...";
        speakerNameDisplay.text = "";
        storyTextDisplay.text = selectedOption + "..."; // Display user selected option
        // --- Build history string ---
        StringBuilder historyBuilder = new StringBuilder();
        foreach (string historicalLine in DialogueHistory)
        {
            historyBuilder.AppendLine(historicalLine); // Add a newline after each history line
        }
        string historyContext = historyBuilder.ToString().Trim(); // Remove trailing newline
        Debug.Log("VNStoryLoader: Previous story text: " + historyContext);
        string prompt = GenerateAIPrompt(rawStoryTextFile.text);
        prompt += $"User selected: \"{selectedOption}\", the previous story text was \"{historyContext}\". Based on this selection, please continue the visual novel story, generating a few lines of dialogue or narration. " +
            $"Only generate a few lines, not too long, but generate a option in the end. Be reasonable and imaginative and based on the format. 假如你认为这个故事应该完结了请生成无选项的结局，根据selection来生成坏结局，结局的话请在末尾新一行加上 The end。";
        string jsonBody = "{" +
                          "\"model\": \"gpt-4o\"," +
                          "\"messages\": [" +
                          "{\"role\": \"system\", \"content\": \"" + EscapeJsonString(prompt) + "\"}" +
                          "]," +
                          "\"temperature\": 0.7," +
                          "\"max_tokens\": 500" +
                          "}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(openaiEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            string errorMsg = "Failed to get AI reply: Network or API error.\n" + request.error;
            statusTextDisplay.text = errorMsg;
            Debug.LogError("VNStoryLoader: " + errorMsg);
            storyTextDisplay.text = "AI reply failed.";
        }
        else
        {
            string rawResponse = request.downloadHandler.text;
            Debug.Log("VNStoryLoader: AI Reply Raw Response: " + rawResponse);

            try
            {
                JSONNode jsonResponse = SimpleJSON.JSON.Parse(rawResponse);
                string aiReplyText = jsonResponse["choices"][0]["message"]["content"].Value;
                Debug.Log("VNStoryLoader: AI Reply Content:\n" + aiReplyText);

                InsertAILinesIntoDialogue(aiReplyText);

                statusTextDisplay.text = "";
                isWaitingForUserInput = true;
                DisplayNextLine(); // Display the first line of AI reply
            }
            catch (System.Exception e)
            {
                string parseError = "AI reply parsing error: " + e.Message + "\nRaw Response: " + rawResponse;
                statusTextDisplay.text = parseError;
                Debug.LogError("VNStoryLoader: " + parseError);
                storyTextDisplay.text = "AI reply parsing failed.";
            }
        }
        isProcessingAI = false;
    }

    // Inserts AI reply into the current dialogue array (remains unchanged)
    void InsertAILinesIntoDialogue(string aiReply)
    {
        string[] newLines = aiReply.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (newLines.Length == 0) return;

        List<string> tempDialogueList = new List<string>(processedDialogueLines);
        // Insert new lines after the current dialogue line
        // currentLineIndex points to the line after the option command, so inserting here is appropriate
        tempDialogueList.InsertRange(currentLineIndex, newLines);

        processedDialogueLines = tempDialogueList.ToArray();
        Debug.Log($"VNStoryLoader: Inserted {newLines.Length} lines of AI reply. Current dialogue array total length: {processedDialogueLines.Length}");
    }

    // --- New: Add formatted dialogue line to simple history ---
    void AddSimpleLineToHistory(string formattedLine)
    {
        DialogueHistory.Add(formattedLine);

        // Keep history within max line count
        if (DialogueHistory.Count > maxSimpleHistoryLines)
        {
            DialogueHistory.RemoveAt(0); // Remove oldest line
        }
        Debug.Log($"VNStoryLoader: History updated - Added: '{formattedLine}' (Current total lines: {DialogueHistory.Count})");
    }

    // --- Helper Method: Generate AI Prompt ---
    private string GenerateAIPrompt(string storyText)
    {
        return $"You are a professional Visual Novel script writer and text segmentation expert.\n" +
               $"Below is the story text. You need to segment it into the smallest units suitable for visual novel display (usually based on punctuation, visual actions, scene change points, etc.). And insert at least three options corresponding to the plot, paying attention to keeping the language consistent with the input.\n\n" +
               $"For each segmented text, please strictly follow the rules below for formatting and output:\n\n" +
               $"1.  **Narration or Description:**\n" +
               $"    * `\"Text Content\":` (no character name after the colon, indicates narration or scene description)\n" +
               $"2.  **Character Dialogue:**\n" +
               $"    * `\"Dialogue Content\":Character Name` (character name after the colon, character name can be in Japanese, Chinese, or English)\n" +
               $"3.  **Scene/Time Change Point:**\n" +
               $"    * `\"--Description--:\"` (e.g., `\"--The next morning--:\"`, with double dashes before and after the description)\n" +
               $"4.  **Option Command (appended to the end of the sentence):**\n" + // <-- New: Explicitly state appended to end of sentence
               $"    * `\"Text Content\":[OPTIONS]Option A Text|Option B Text|Option C Text` (immediately follows the sentence content, separated by `:[OPTIONS]`, followed by option texts separated by `|`. **This line must be self-contained and not contain extra double quotes or trailing colons outside the entire string.**)\n" +
               $"5.  **Accumulative Text (added to previous text):**\n" +
               $"    * `\"=Text Content\":` or `\"=Dialogue Content\":Character Name` (with `=` before the content)\n" +
               $"6.  **Comment Line:**\n" +
               $"    * Starts with `//` or `#`. These lines are treated as comments and are not parsed as story content.\n\n" +
               $"**Strictly follow the output format above. Do not output any extra explanations or guiding words other than the formatted text. Do not include extra blank lines. All text content that should be enclosed in double quotes must be enclosed.**\n\n" +
               $"Below is the story text:\n" +
               $"---\n" +
               $"{storyText}\n" +
               $"---\n\n" +
               $"Now, start outputting the formatted text:";
    }

    // --- Helper Method: Safely escape JSON string (remains unchanged) ---
    private string EscapeJsonString(string text)
    {
        if (string.IsNullOrEmpty(text)) { return ""; }
        StringBuilder sb = new StringBuilder();
        foreach (char c in text)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '/': sb.Append("\\/"); break;
                default:
                    if (c < 32 || (c > 126 && c < 160))
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("X4"));
                    }
                    else { sb.Append(c); }
                    break;
            }
        }
        return sb.ToString();
    }
}