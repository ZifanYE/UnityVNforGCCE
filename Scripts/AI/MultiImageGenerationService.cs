using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class MultiImageGenerationService : MonoBehaviour
{
    public static MultiImageGenerationService instance { get; private set; }

    // 回调：生成的文件名列表
    public System.Action<List<string>> OnImagesGenerated;
    // 回调：生成的纹理列表
    public System.Action<List<Texture2D>> OnTexturesGenerated;

    [SerializeField] private string imageGenApiKey = "YOUR_OPENAI_API_KEY_HERE";
    private string chatCompletionsEndpoint = "https://api.openai.com/v1/chat/completions";
    private string imageGenEndpoint = "https://api.openai.com/v1/images/generations";

    [Header("场景设置")]
    public TextAsset rawStoryTextFile;
    //public TextAsset scenesTextFile; // 包含6个场景的txt文件
    public BackgroundManager backgroundManager;

    [Header("生成设置")]
    [SerializeField] private string outputFileNamePrefix = "scene";
    [SerializeField] private float imageTransitionSpeed = 0.5f;
    [SerializeField] private bool imageSmoothTransition = true;
    [SerializeField] private string dalleImageSize = "1024x1024";
    [SerializeField] private string dalleImageQuality = "standard";

    // 系统提示语
    private const string SYSTEM_PROMPT = "あなたはビジュアルノベルの背景画像を生成する専門家です。" +
                                        "以下の場景描述に基づいて、詳細で写実的かつ映画のようなビジュアルノベルの背景画像を生成してください。" +
                                        "アスペクト比は16:9でお願いします。" +
                                        "日本語アニメーションスタイル, Please add black bars to the top and bottom to create a 16:9 aspect ratio. " +
                                        "Do not add black bars to the sides。";
    //キャラクターは含めないでください。
    private const string SCENE_EXTRACTION_PROMPT =
       "あなたはプロのシナリオライターです。以下の長い物語のテキストを読み、ビジュアルノベルの背景画像を生成するのに最も適した、視覚的に重要な場面を6つ抽出してください。" +
       "各場面は、場所、時間、雰囲気、そして重要な視覚的要素を簡潔に記述してください。" +
       "必ず以下のフォーマットを厳守し、場面ごとに改行して出力してください。\n" +
       "フォーマット：\n" +
       "場面1：[ここに場面1の説明]\n" +
       "場面2：[ここに場面2の説明]\n" +
       "場面3：[ここに場面3の説明]\n" +
       "場面4：[ここに場面4の説明]\n" +
       "場面5：[ここに場面5の説明]\n" +
       "場面6：[ここに場面6の説明]";
    private const string GENERATED_BG_SUBFOLDER = "GeneratedBackgrounds";
    private const string SCENE_SEPARATOR = "---"; // 场景分隔符

    void Awake()
    {
        if (instance != null && instance != this)
            Destroy(gameObject);
        else
            instance = this;
    }

    void Start()
    {
        StartCoroutine(ExtractScenesAndGenerateImages());


        //StartCoroutine(GenerateImagesForAllScenes());
    }
    public IEnumerator ExtractScenesAndGenerateImages()
    {
        string storyContent = rawStoryTextFile.text;
        Debug.Log("物語から場面の抽出を開始します...");

        // --- APIリクエストの作成 ---
        // JSONボディの構築
        string jsonBody = "{" +
            "\"model\": \"gpt-4o\"," + // 最新で高性能なモデルを推奨
            "\"messages\": [" +
                "{\"role\": \"system\", \"content\": \"" + EscapeJsonString(SCENE_EXTRACTION_PROMPT) + "\"}," +
                "{\"role\": \"user\", \"content\": \"" + EscapeJsonString(storyContent) + "\"}" +
            "]," +
            "\"temperature\": 0.5" +
        "}";

        UnityWebRequest request = new UnityWebRequest(chatCompletionsEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + imageGenApiKey);

        // --- APIリクエストの送信と待機 ---
        yield return request.SendWebRequest();

        // --- 結果の処理 ---
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"場面抽出に失敗しました: {request.error}\n{request.downloadHandler.text}");
            yield break;
        }

        JSONNode jsonResponse = JSON.Parse(request.downloadHandler.text);
        // AIが生成した場面テキストを取得
        string extractedScenesText = jsonResponse["choices"][0]["message"]["content"];

        if (string.IsNullOrEmpty(extractedScenesText))
        {
            Debug.LogError("AIからの応答が空でした。");
            yield break;
        }


        StartCoroutine(GenerateImagesForAllScenes(extractedScenesText));
        // --- 画像生成サービスに結果を渡す ---
    }

    public IEnumerator GenerateImagesForAllScenes(string ParseStory)
    {


        // 解析场景文本
        List<string> scenes = ParseScenesFromText(ParseStory);

        if (scenes.Count == 0)
        {
            Debug.LogError("MultiImageGenerationService: 未找到任何场景描述！");
            yield break;
        }

        Debug.Log($"MultiImageGenerationService: 发现 {scenes.Count} 个场景");

        List<string> generatedImageNames = new List<string>();
        List<Texture2D> textureList = new List<Texture2D>();

        for (int i = 0; i < scenes.Count; i++)
        {
            string sceneDescription = scenes[i].Trim();
            if (string.IsNullOrEmpty(sceneDescription))
            {
                Debug.LogWarning($"场景 {i + 1} 为空，跳过");
                continue;
            }

            Debug.Log($"正在生成场景 {i + 1}/{scenes.Count}: {sceneDescription.Substring(0, Mathf.Min(50, sceneDescription.Length))}...");

            string fileName = $"{outputFileNamePrefix}_{i + 1}";
            string fullSaveDirPath = Path.Combine(Application.persistentDataPath, GENERATED_BG_SUBFOLDER);
            string fullSaveFilePath = Path.Combine(fullSaveDirPath, fileName + ".png");

            // 构建完整的提示语：系统提示 + 场景描述
            string fullPrompt = SYSTEM_PROMPT + "\n\n场景描述：\n" + sceneDescription;

            string jsonBody = "{" +
                              "\"prompt\": \"" + EscapeJsonString(fullPrompt) + "\"," +
                              "\"model\": \"dall-e-3\"," +
                              "\"n\": 1," +
                              "\"size\": \"" + dalleImageSize + "\"," +
                              "\"quality\": \"" + dalleImageQuality + "\"" +
                              "}";

            UnityWebRequest request = new UnityWebRequest(imageGenEndpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + imageGenApiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"场景 {i + 1} 图片生成失败：{request.error}");
                Debug.LogError($"响应内容：{request.downloadHandler.text}");
                continue;
            }

            JSONNode jsonResponse = SimpleJSON.JSON.Parse(request.downloadHandler.text);
            string imageUrl = jsonResponse["data"][0]["url"];

            if (string.IsNullOrEmpty(imageUrl))
            {
                Debug.LogError($"场景 {i + 1} 无法解析图片 URL");
                continue;
            }

            UnityWebRequest imageRequest = UnityWebRequest.Get(imageUrl);
            imageRequest.downloadHandler = new DownloadHandlerBuffer();
            yield return imageRequest.SendWebRequest();

            if (imageRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"场景 {i + 1} 图片下载失败：{imageRequest.error}");
                continue;
            }

            byte[] imageBytes = imageRequest.downloadHandler.data;
            Directory.CreateDirectory(Path.GetDirectoryName(fullSaveFilePath));
            File.WriteAllBytes(fullSaveFilePath, imageBytes);
            generatedImageNames.Add(fileName + ".png");

            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            tex.name = fileName;
            textureList.Add(tex);

            // 自动调用替换背景（自动显示）
            SetBackgroundTexture(tex);

            Debug.Log($"场景 {i + 1} 生成完成：{fileName}.png");

            // 给API一些休息时间，避免请求过快
            yield return new WaitForSeconds(3f);
        }

        OnImagesGenerated?.Invoke(generatedImageNames);
        OnTexturesGenerated?.Invoke(textureList);

        Debug.Log("MultiImageGenerationService: 所有场景图片生成完毕！");
        Debug.Log($"成功生成 {generatedImageNames.Count} 张图片");
    }
    //public IEnumerator GenerateImagesForAllScenes()
    //{
    //    if (backgroundManager == null || scenesTextFile == null)
    //    {
    //        Debug.LogError("MultiImageGenerationService: 缺少依赖组件！");
    //        yield break;
    //    }
    //    if (string.IsNullOrEmpty(imageGenApiKey) || imageGenApiKey == "YOUR_OPENAI_API_KEY_HERE")
    //    {
    //        Debug.LogError("MultiImageGenerationService: API Key 未设置！");
    //        yield break;
    //    }

    //    // 解析场景文本
    //    List<string> scenes = ParseScenesFromText(scenesTextFile.text);

    //    if (scenes.Count == 0)
    //    {
    //        Debug.LogError("MultiImageGenerationService: 未找到任何场景描述！");
    //        yield break;
    //    }

    //    Debug.Log($"MultiImageGenerationService: 发现 {scenes.Count} 个场景");

    //    List<string> generatedImageNames = new List<string>();
    //    List<Texture2D> textureList = new List<Texture2D>();

    //    for (int i = 0; i < scenes.Count; i++)
    //    {
    //        string sceneDescription = scenes[i].Trim();
    //        if (string.IsNullOrEmpty(sceneDescription))
    //        {
    //            Debug.LogWarning($"场景 {i + 1} 为空，跳过");
    //            continue;
    //        }

    //        Debug.Log($"正在生成场景 {i + 1}/{scenes.Count}: {sceneDescription.Substring(0, Mathf.Min(50, sceneDescription.Length))}...");

    //        string fileName = $"{outputFileNamePrefix}_{i + 1}";
    //        string fullSaveDirPath = Path.Combine(Application.persistentDataPath, GENERATED_BG_SUBFOLDER);
    //        string fullSaveFilePath = Path.Combine(fullSaveDirPath, fileName + ".png");

    //        // 构建完整的提示语：系统提示 + 场景描述
    //        string fullPrompt = SYSTEM_PROMPT + "\n\n场景描述：\n" + sceneDescription;

    //        string jsonBody = "{" +
    //                          "\"prompt\": \"" + EscapeJsonString(fullPrompt) + "\"," +
    //                          "\"model\": \"dall-e-3\"," +
    //                          "\"n\": 1," +
    //                          "\"size\": \"" + dalleImageSize + "\"," +
    //                          "\"quality\": \"" + dalleImageQuality + "\"" +
    //                          "}";

    //        UnityWebRequest request = new UnityWebRequest(imageGenEndpoint, "POST");
    //        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
    //        request.downloadHandler = new DownloadHandlerBuffer();
    //        request.SetRequestHeader("Content-Type", "application/json");
    //        request.SetRequestHeader("Accept", "application/json");
    //        request.SetRequestHeader("Authorization", "Bearer " + imageGenApiKey);

    //        yield return request.SendWebRequest();

    //        if (request.result != UnityWebRequest.Result.Success)
    //        {
    //            Debug.LogError($"场景 {i + 1} 图片生成失败：{request.error}");
    //            Debug.LogError($"响应内容：{request.downloadHandler.text}");
    //            continue;
    //        }

    //        JSONNode jsonResponse = SimpleJSON.JSON.Parse(request.downloadHandler.text);
    //        string imageUrl = jsonResponse["data"][0]["url"];

    //        if (string.IsNullOrEmpty(imageUrl))
    //        {
    //            Debug.LogError($"场景 {i + 1} 无法解析图片 URL");
    //            continue;
    //        }

    //        UnityWebRequest imageRequest = UnityWebRequest.Get(imageUrl);
    //        imageRequest.downloadHandler = new DownloadHandlerBuffer();
    //        yield return imageRequest.SendWebRequest();

    //        if (imageRequest.result != UnityWebRequest.Result.Success)
    //        {
    //            Debug.LogError($"场景 {i + 1} 图片下载失败：{imageRequest.error}");
    //            continue;
    //        }

    //        byte[] imageBytes = imageRequest.downloadHandler.data;
    //        Directory.CreateDirectory(Path.GetDirectoryName(fullSaveFilePath));
    //        File.WriteAllBytes(fullSaveFilePath, imageBytes);
    //        generatedImageNames.Add(fileName + ".png");

    //        Texture2D tex = new Texture2D(2, 2);
    //        tex.LoadImage(imageBytes);
    //        tex.name = fileName;
    //        textureList.Add(tex);

    //        // 自动调用替换背景（自动显示）
    //        SetBackgroundTexture(tex);

    //        Debug.Log($"场景 {i + 1} 生成完成：{fileName}.png");

    //        // 给API一些休息时间，避免请求过快
    //        yield return new WaitForSeconds(3f);
    //    }

    //    OnImagesGenerated?.Invoke(generatedImageNames);
    //    OnTexturesGenerated?.Invoke(textureList);

    //    Debug.Log("MultiImageGenerationService: 所有场景图片生成完毕！");
    //    Debug.Log($"成功生成 {generatedImageNames.Count} 张图片");
    //}

    /// <summary>
    /// 从文本中解析场景列表
    /// 支持两种格式：
    /// 1. 使用 "---" 分隔符分隔场景
    /// 2. 使用 "场景1:", "场景2:" 等标记分隔场景
    /// </summary>
    private List<string> ParseScenesFromText(string text)
    {
        List<string> scenes = new List<string>();

        if (string.IsNullOrEmpty(text))
            return scenes;

        // 首先尝试使用 "---" 分隔符
        if (text.Contains(SCENE_SEPARATOR))
        {
            string[] parts = text.Split(new string[] { SCENE_SEPARATOR }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    scenes.Add(trimmed);
                }
            }
        }
        // 尝试使用 "场景" 标记分隔
        else if (text.Contains("场景"))
        {
            string[] lines = text.Split('\n');
            string currentScene = "";

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // 检查是否是场景标题行
                if (trimmedLine.StartsWith("场景") && (trimmedLine.Contains(":") || trimmedLine.Contains("：")))
                {
                    // 保存前一个场景
                    if (!string.IsNullOrEmpty(currentScene))
                    {
                        scenes.Add(currentScene.Trim());
                    }
                    // 开始新场景，去掉场景标题
                    int colonIndex = Mathf.Max(trimmedLine.IndexOf(":"), trimmedLine.IndexOf("："));
                    currentScene = colonIndex >= 0 ? trimmedLine.Substring(colonIndex + 1).Trim() : "";
                }
                else if (!string.IsNullOrEmpty(trimmedLine))
                {
                    // 添加到当前场景描述
                    if (!string.IsNullOrEmpty(currentScene))
                        currentScene += "\n";
                    currentScene += trimmedLine;
                }
            }

            // 添加最后一个场景
            if (!string.IsNullOrEmpty(currentScene))
            {
                scenes.Add(currentScene.Trim());
            }
        }
        // 如果没有特殊分隔符，尝试按行分割（假设每行是一个场景）
        else
        {
            string[] lines = text.Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    scenes.Add(trimmed);
                }
            }
        }

        return scenes;
    }

    // 自动替换背景纹理方法
    public void SetBackgroundTexture(Texture2D tex)
    {
        if (backgroundManager != null && tex != null)
        {
            backgroundManager.SetGeneratedBackground(tex, imageTransitionSpeed, imageSmoothTransition);
            Debug.Log($"MultiImageGenerationService: 替换背景纹理成功 -> {tex.name}");
        }
        else
        {
            Debug.LogError("MultiImageGenerationService: 替换背景失败，backgroundManager 或纹理为空");
        }
    }

    private string EscapeJsonString(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
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
                        sb.Append("\\u" + ((int)c).ToString("X4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// 手动生成指定场景的图片
    /// </summary>
    //public void GenerateImageForScene(int sceneIndex)
    //{
    //    if (scenesTextFile == null) return;

    //    List<string> scenes = ParseScenesFromText(scenesTextFile.text);
    //    if (sceneIndex >= 0 && sceneIndex < scenes.Count)
    //    {
    //        StartCoroutine(GenerateSingleSceneImage(scenes[sceneIndex], sceneIndex));
    //    }
    //}

    private IEnumerator GenerateSingleSceneImage(string sceneDescription, int sceneIndex)
    {
        string fileName = $"{outputFileNamePrefix}_{sceneIndex + 1}";
        string fullSaveDirPath = Path.Combine(Application.persistentDataPath, GENERATED_BG_SUBFOLDER);
        string fullSaveFilePath = Path.Combine(fullSaveDirPath, fileName + ".png");

        string fullPrompt = SYSTEM_PROMPT + "\n\n场景描述：\n" + sceneDescription;

        string jsonBody = "{" +
                          "\"prompt\": \"" + EscapeJsonString(fullPrompt) + "\"," +
                          "\"model\": \"dall-e-3\"," +
                          "\"n\": 1," +
                          "\"size\": \"" + dalleImageSize + "\"," +
                          "\"quality\": \"" + dalleImageQuality + "\"" +
                          "}";

        UnityWebRequest request = new UnityWebRequest(imageGenEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + imageGenApiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            JSONNode jsonResponse = SimpleJSON.JSON.Parse(request.downloadHandler.text);
            string imageUrl = jsonResponse["data"][0]["url"];

            if (!string.IsNullOrEmpty(imageUrl))
            {
                UnityWebRequest imageRequest = UnityWebRequest.Get(imageUrl);
                imageRequest.downloadHandler = new DownloadHandlerBuffer();
                yield return imageRequest.SendWebRequest();

                if (imageRequest.result == UnityWebRequest.Result.Success)
                {
                    byte[] imageBytes = imageRequest.downloadHandler.data;
                    Directory.CreateDirectory(Path.GetDirectoryName(fullSaveFilePath));
                    File.WriteAllBytes(fullSaveFilePath, imageBytes);

                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(imageBytes);
                    tex.name = fileName;

                    SetBackgroundTexture(tex);
                    Debug.Log($"单个场景 {sceneIndex + 1} 生成完成：{fileName}.png");
                }
            }
        }
    }
}