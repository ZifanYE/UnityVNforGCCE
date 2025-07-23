using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class VNStoryLoader : MonoBehaviour
{
    // --- OpenAI API 配置 ---
    [SerializeField] private string openaiApiKey = "sk-YOUR_OPENAI_API_KEY_HERE";
    private string openaiEndpoint = "https://api.openai.com/v1/chat/completions";

    // --- 故事文本源 ---
    public TextAsset rawStoryTextFile; 

    // --- UI 组件引用 ---
    public TextMeshProUGUI storyTextDisplay;
    public TextMeshProUGUI speakerNameDisplay;
    public TextMeshProUGUI statusTextDisplay; // 用于显示加载或错误状态

    // --- 选项 UI 引用 ---
    public GameObject optionsPanel; // 选项面板（包含所有按钮）
    public GameObject optionButtonPrefab; // 你的选项按钮预制体
    public ButtonManager bm;

    // --- 内部状态变量 ---
    private string[] processedDialogueLines; //ai生成的故事文本
    private int currentLineIndex = 0;
    private bool isProcessingAI = false;
    private bool isTypingText = false;
    private bool isWaitingForUserInput = false; // 表示等待用户点击屏幕推进对话
    public bool isAuto = false;
    private bool isShowingOptions = false;     // 表示当前正在显示选项，等待用户选择
    private List<GameObject> currentOptionButtons = new List<GameObject>();

    public float typeSpeed = 0.05f; 
    [SerializeField] private float optionButtonSpacing = 20f; // 选项按钮之间的间距

    //历史记录
    private List<string> DialogueHistory = new List<string>();
    [SerializeField] private int maxSimpleHistoryLines = 5; // 历史记录的最大行数，可在Inspector中设置
    void Start()
    {
        // 检查UI组件和选项组件是否已赋值
        if (storyTextDisplay == null || speakerNameDisplay == null || statusTextDisplay == null || 
            optionsPanel == null || optionButtonPrefab == null)
        {
            Debug.LogError("VNStoryLoader: 部分UI组件或选项预制体未赋值！请检查Inspector。");
            if (statusTextDisplay != null) statusTextDisplay.text = "错误：UI/选项组件未赋值。";
            return;
        }

        // 绑定选项按钮点击事件 (在 ShowOptions 中动态添加)
        optionsPanel.SetActive(false); // 确保选项面板初始是隐藏的

        // 检查原始故事文本文件
        if (rawStoryTextFile == null)
        {
            statusTextDisplay.text = "Raw Story Text File (TextAsset) is not assigned!";
            Debug.LogError("VNStoryLoader: Raw Story Text File (TextAsset) is not assigned!");
            return;
        }

        // 启动AI处理流程
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
            if (!isProcessingAI && !isTypingText && !isShowingOptions && isWaitingForUserInput && !isAuto)
                DisplayNextLine();
            else if (isAuto)
            {
                bm.cancelInvoke();
            }
        }
    }

    // --- AI 处理故事文本流程 (保持不变) ---
    IEnumerator ProcessStoryWithAI()
    {
        isProcessingAI = true;
        statusTextDisplay.text = "waiting ...";
        speakerNameDisplay.text = "";
        storyTextDisplay.text = "";

        if (string.IsNullOrEmpty(openaiApiKey) || openaiApiKey == "sk-YOUR_OPENAI_API_KEY_HERE")
        {
            string errorMsg = "错误: OpenAI API Key 未设置或不正确！无法处理故事文本。";
            statusTextDisplay.text = errorMsg;
            Debug.LogError("VNStoryLoader: " + errorMsg);
            isProcessingAI = false;
            yield break;
        }

        string storyContent = rawStoryTextFile.text;
        if (string.IsNullOrEmpty(storyContent))
        {
            string errorMsg = "错误: 故事文本文件为空！";
            statusTextDisplay.text = errorMsg;
            Debug.LogError("VNStoryLoader: " + errorMsg);
            isProcessingAI = false;
            yield break;
        }

        Debug.Log("VNStoryLoader: 正在发送故事文本到 OpenAI 进行处理...");

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
            string errorMsg = "AI处理失败：网络或API错误。\n" + request.error + "\n状态码: " + request.responseCode;
            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                errorMsg += "\n响应: " + request.downloadHandler.text;
            }
            statusTextDisplay.text = errorMsg;
            Debug.LogError("VNStoryLoader: " + errorMsg);
        }
        else
        {
            string rawResponse = request.downloadHandler.text;
            Debug.Log("VNStoryLoader: OpenAI原始响应: " + rawResponse);

            try
            {
                JSONNode jsonResponse = SimpleJSON.JSON.Parse(rawResponse);
                string aiFormattedText = jsonResponse["choices"][0]["message"]["content"].Value;
                Debug.Log("VNStoryLoader: AI格式化后的文本:\n" + aiFormattedText);

                processedDialogueLines = aiFormattedText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                statusTextDisplay.text = "";
                storyTextDisplay.text = "Success.Click the screen to start the story.";
                speakerNameDisplay.text = "";

                currentLineIndex = 0;
                isWaitingForUserInput = true;
            }
            catch (System.Exception e)
            {
                string parseError = "AI响应解析错误: " + e.Message + "\n原始响应: " + rawResponse;
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
    // --- 显示下一行对话 ---
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
            storyTextDisplay.text = "";
            speakerNameDisplay.text = "";
            isWaitingForUserInput = false;
            Debug.Log("VNStoryLoader: 对话结束。");
            return;
        }

        string line = processedDialogueLines[currentLineIndex];

        // --- 修正：更精确地识别句尾的 ":[OPTIONS]..." ---
        // 确保 line 已经移除了最外层双引号和可能的末尾冒号（如果AI输出有的话）
        string cleanedLineForOptionsCheck = line.Trim();
        if (cleanedLineForOptionsCheck.StartsWith("\"") && cleanedLineForOptionsCheck.EndsWith("\""))
        {
            cleanedLineForOptionsCheck = cleanedLineForOptionsCheck.Substring(1, cleanedLineForOptionsCheck.Length - 2);
        }
        if (cleanedLineForOptionsCheck.EndsWith(":")) // 如果AI在 OPTIONS 后面也加了冒号
        {
            cleanedLineForOptionsCheck = cleanedLineForOptionsCheck.TrimEnd(':');
        }


        // 检查是否包含 ":[OPTIONS]" 模式
        // StringComparison.Ordinal 确保精确匹配，不受文化因素影响
        int optionsIndex = cleanedLineForOptionsCheck.IndexOf(":[OPTIONS]", StringComparison.Ordinal);

        if (optionsIndex != -1) // 找到了 ":[OPTIONS]"
        {
            Debug.Log($"VNStoryLoader: 检测到句尾选项指令在索引 {optionsIndex} 的行: '{line}'");
            
            // 分割文本内容和选项指令
            string contentBeforeOptions = cleanedLineForOptionsCheck.Substring(0, optionsIndex); // 文本内容
            string optionsData = cleanedLineForOptionsCheck.Substring(optionsIndex + ":".Length); // ":[OPTIONS]..." 之后的部分

            // 处理文本内容 (逐字显示)
            string[] dialogueParts = line.Split(new char[] { ':' }, 2); // 依然用原始的line来解析说话者
            string speaker = (dialogueParts.Length > 1) ? dialogueParts[1].Trim() : "";
            speakerNameDisplay.text = "";
            // speakerNameDisplay.text = speaker;
            StartCoroutine(TypeTextAndShowOptions(contentBeforeOptions, optionsData));

            // 调用 AddSimpleLineToHistory，传入对话内容
            AddSimpleLineToHistory(speaker + ":" + contentBeforeOptions); // 格式化为 "角色名:文本"

            currentLineIndex++; // 推进到下一行，因为当前行已经处理完毕
            return; // 暂停对话流，等待用户选择
        }

        // --- 以下是常规对话和旁白处理（如果上方没有匹配到选项） ---
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("#"))
        {
            Debug.Log($"VNStoryLoader: 跳过空行/注释行: '{line}'");

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
            StartCoroutine(TypeText(regularContent));
        }
        // 调用 AddSimpleLineToHistory，传入对话内容
        AddSimpleLineToHistory("--" + regularContent.Replace("--", "").Trim() + "--:"); // 保持 "--文本--:" 格式
        currentLineIndex++;
    }
    
    // --- 新增：逐字显示文本并显示选项的协程 ---
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
        isWaitingForUserInput = true; // 文本显示完毕

        // 文本显示完毕后，立即显示选项
        ShowOptionsInternal(optionsData); // 内部调用 ShowOptions
    }
    
    // --- 逐字显示文本效果 (保持不变) ---
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

    // --- 选项逻辑：动态创建按钮（从 ShowOptions 拆分出内部方法） ---
    // 这个方法现在只负责处理选项字符串并显示UI，不再由 DisplayNextLine 直接调用
    void ShowOptionsInternal(string optionsDataString)
    {
        isShowingOptions = true; // 标记正在显示选项
        isWaitingForUserInput = false; // 不再等待推进对话，而是等待选项选择
        // storyTextDisplay.text 不再清空，因为文本已经显示了
        // speakerNameDisplay.text 也不清空

        optionsPanel.SetActive(true); // 显示选项面板

        // 清除所有旧的动态创建按钮
        foreach (GameObject btn in currentOptionButtons)
        {
            Destroy(btn);
        }
        currentOptionButtons.Clear();

        // 解析选项文本：optionsDataString 已经包含了 [OPTIONS]...
        string optionsString = optionsDataString.Replace("[OPTIONS]", "").Trim();
        string[] options = optionsString.Split('|');

        if (options.Length == 0)
        {
            Debug.LogWarning("VNStoryLoader: 没有解析到任何选项文本！请检查:[OPTIONS]后的格式。");
            HideOptions(); // 无法创建选项，直接隐藏面板
            return;
        }

        RectTransform panelRect = optionsPanel.GetComponent<RectTransform>();
        float currentYOffset = 0f; // 用于垂直堆叠按钮

        // 动态创建并配置按钮
        for (int i = 0; i < options.Length; i++)
        {
            if (optionButtonPrefab == null)
            {
                Debug.LogError("VNStoryLoader: 选项按钮预制体未赋值！无法创建选项。");
                return;
            }

            GameObject newButtonGO = Instantiate(optionButtonPrefab, optionsPanel.transform);
            newButtonGO.name = $"OptionButton_{i}";
            currentOptionButtons.Add(newButtonGO); // 添加到列表中以便后续管理

            Button newButton = newButtonGO.GetComponent<Button>();
            TextMeshProUGUI buttonText = newButtonGO.GetComponentInChildren<TextMeshProUGUI>();

            if (newButton == null || buttonText == null)
            {
                Debug.LogError($"VNStoryLoader: 预制体 '{optionButtonPrefab.name}' 缺少 Button 或 TextMeshProUGUI 组件。");
                Destroy(newButtonGO);
                continue;
            }

            buttonText.text = options[i].Trim();
            newButton.gameObject.SetActive(true);

            // 设置按钮位置 (简单垂直堆叠)
            RectTransform buttonRect = newButtonGO.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0.5f, 1f); // 锚定在父面板顶部中央
                buttonRect.anchorMax = new Vector2(0.5f, 1f);
                buttonRect.pivot = new Vector2(0.5f, 1f); // 枢轴点在按钮顶部中央

                buttonRect.anchoredPosition = new Vector2(0f, -currentYOffset); 
                currentYOffset += buttonRect.sizeDelta.y + optionButtonSpacing; // 累加下一个按钮的偏移
            }

            // 添加点击事件监听器
            int optionIndex = i; // 捕获循环变量
            newButton.onClick.AddListener(() => OnOptionSelected(optionIndex));
        }
    }

    void HideOptions()
    {
        isShowingOptions = false;
        optionsPanel.SetActive(false);
        // 销毁所有动态创建的按钮
        foreach (GameObject btn in currentOptionButtons)
        {
            Destroy(btn);
        }
        currentOptionButtons.Clear();
    }

    // 当用户点击选项按钮时调用
    void OnOptionSelected(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= currentOptionButtons.Count) return;

        TextMeshProUGUI selectedButtonText = currentOptionButtons[optionIndex].GetComponentInChildren<TextMeshProUGUI>();
        string selectedOptionText = (selectedButtonText != null) ? selectedButtonText.text : "未知选项";

        Debug.Log("VNStoryLoader: 用户选择了: " + selectedOptionText);

        HideOptions(); // 隐藏选项界面

        StartCoroutine(SendOptionToAI(selectedOptionText));
    }

    IEnumerator SendOptionToAI(string selectedOption)
    {
        isProcessingAI = true;
        statusTextDisplay.text = "Now waiting AI reply...";
        speakerNameDisplay.text = "";
        storyTextDisplay.text = selectedOption + "..."; // 显示用户选择的选项
                                                        // --- 构建历史字符串 ---
        StringBuilder historyBuilder = new StringBuilder();
        foreach (string historicalLine in DialogueHistory)
        {
            historyBuilder.AppendLine(historicalLine); // 每行历史记录后加一个换行
        }
        string historyContext = historyBuilder.ToString().Trim(); // 移除末尾多余的换行
        Debug.Log("VNStoryLoader: 之前的故事文本: " + historyContext);
        string prompt = $"用户选择了：\"{selectedOption}\"，之前故事的文本为\"{historyContext}\"。请你根据这个选择，继续视觉小说故事，生成接下来的几句话对话或旁白。请继续保持你之前作为视觉小说脚本作家的格式（\"内容\":角色名 或 \"内容\":）。只生成几句话，不用太长。合理且充满想象力。";
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
            string errorMsg = "获取AI回复失败：网络或API错误。\n" + request.error;
            statusTextDisplay.text = errorMsg;
            Debug.LogError("VNStoryLoader: " + errorMsg);
            storyTextDisplay.text = "AI回复失败。";
        }
        else
        {
            string rawResponse = request.downloadHandler.text;
            Debug.Log("VNStoryLoader: AI回复原始响应: " + rawResponse);

            try
            {
                JSONNode jsonResponse = SimpleJSON.JSON.Parse(rawResponse);
                string aiReplyText = jsonResponse["choices"][0]["message"]["content"].Value;
                Debug.Log("VNStoryLoader: AI回复内容:\n" + aiReplyText);

                InsertAILinesIntoDialogue(aiReplyText);

                statusTextDisplay.text = "";
                isWaitingForUserInput = true;
                DisplayNextLine(); // 显示AI回复的第一行
            }
            catch (System.Exception e)
            {
                string parseError = "AI回复解析错误: " + e.Message + "\n原始响应: " + rawResponse;
                statusTextDisplay.text = parseError;
                Debug.LogError("VNStoryLoader: " + parseError);
                storyTextDisplay.text = "AI回复解析失败。";
            }
        }
        isProcessingAI = false;
    }

    // 将 AI 回复插入到当前对话数组中 (保持不变)
    void InsertAILinesIntoDialogue(string aiReply)
    {
        string[] newLines = aiReply.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (newLines.Length == 0) return;

        List<string> tempDialogueList = new List<string>(processedDialogueLines);
        // 在当前对话行的下一行插入新生成的行
        // currentLineIndex 指向的是选项指令后的下一行，所以在这里插入是合适的
        tempDialogueList.InsertRange(currentLineIndex, newLines); 
        
        processedDialogueLines = tempDialogueList.ToArray();
        Debug.Log($"VNStoryLoader: 插入了 {newLines.Length} 行AI回复。当前对话数组总长度: {processedDialogueLines.Length}");
    }
    // --- 新增：将格式化后的对话行添加到简单历史记录 ---
    void AddSimpleLineToHistory(string formattedLine)
    {
        DialogueHistory.Add(formattedLine);

        // 保持历史记录在最大行数内
        if (DialogueHistory.Count > maxSimpleHistoryLines)
        {
            DialogueHistory.RemoveAt(0); // 移除最旧的行
        }
        Debug.Log($"VNStoryLoader: 历史记录更新 - 添加: '{formattedLine}' (当前总行数: {DialogueHistory.Count})");
    }
    // --- 辅助方法：生成 AI Prompt ---
    private string GenerateAIPrompt(string storyText)
    {
        return $"あなたプロのビジュアルノベル（Visual Novel）スクリプトライター兼テキスト分割の専門家です。\n" +
               $"以下に物語のテキストを提供します。あなたはそれをビジュアルノベルの表示に適した最小単位（通常は句読点、視覚的アクション、シーン切り替えポイントなどに基づいて）に分割する必要があります。并对应故事情节至少插入三个选项,注意语言要与输入保持一致, 通过选项将故事延续下去直到你觉得满意的结局。\n\n" +
               $"分割された各テキストについて、以下のルールに厳密に従ってフォーマットして出力してください：\n\n" +
               $"1.  **ナレーションまたは描写：**\n" +
               $"    * `\"テキスト内容\":` (コロンの後にキャラクター名なし、ナレーションまたはシーン描写を示します)\n" +
               $"2.  **キャラクターのセリフ：**\n" +
               $"    * `\"セリフ内容\":キャラクター名` (コロンの後にキャラクター名、キャラクター名は日本語、中国語、または英語)\n" +
               $"3.  **シーン/時間切り替えポイント：**\n" +
               $"    * `\"--描写--:\"` (例：`\"--翌朝--:\"`、描写の前後に二重ダッシュがあります)\n" +
               $"4.  **选项指令（附加到句尾）：**\n" + // <-- 新增：明确指出附加到句尾
               $"    * `\"文本内容\":[OPTIONS]选项A文本|选项B文本|选项C文本` (紧跟在句子的内容后，用 `:[OPTIONS]` 分隔，接着是选项文本用 `|` 分隔。**这一行必须是独立的，并且不包含额外的双引号或末尾的冒号在整个字符串外面。**)\n" +
               $"5.  **累加文本（以前のテキストに追加）：**\n" +
               $"    * `\"=テキスト内容\":` または `\"=セリフ内容\":キャラクター名` (内容の前に `=` を付けます)\n" +
               $"6.  **コメント行：**\n" +
               $"    * `//` または `#` で始まります。これらの行はコメントと見なされ、物語の内容としては解析されません。\n\n" +
               $"**上記の出力フォーマットに厳密に従ってください。フォーマットされたテキスト以外の余分な説明や誘導的な言葉を出力しないでください。余分な空行を含まないでください。すべてのテキスト内容が二重引用符で囲まれるべき箇所は囲んでください。**\n\n" +
               $"以下は物語のテキスト：\n" +
               $"---\n" +
               $"{storyText}\n" +
               $"---\n\n" +
               $"今すぐフォーマットされたテキストの出力を開始してください：";
    }


    // --- 辅助方法：安全转义 JSON 字符串 (保持不变) ---
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