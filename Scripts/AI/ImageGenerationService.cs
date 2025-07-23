using SimpleJSON;
using System.Collections;
using System.IO; // For file operations
using System.Text; // For StringBuilder and Encoding
using UnityEngine;
using UnityEngine.Networking; // For UnityWebRequest

public class ImageGenerationService : MonoBehaviour
{
    // --- 单例模式 ---
    public static ImageGenerationService instance { get; private set; }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            // DontDestroyOnLoad(gameObject); // If this service needs to persist across scenes, uncomment
        }
    }

    // --- API 配置 ---
    [SerializeField] private string imageGenApiKey = "YOUR_OPENAI_API_KEY_HERE"; // 这里填入你的 OpenAI API Key
    // --- 核心修正：OpenAI DALL-E 图像生成 API 的正确端点 ---
    private string imageGenEndpoint = "https://api.openai.com/v1/images/generations";

    // --- 图片生成 Prompt 源文件 ---
    public TextAsset imagePromptFile;

    // --- 文件保存目录 ---
    private const string GENERATED_BG_SUBFOLDER = "GeneratedBackgrounds";

    // --- 背景管理器引用 ---
    public BackgroundManager backgroundManager;

    // --- 图片生成参数 (可在 Inspector 中设置) ---
    [SerializeField] private string imageFileName = "generated_default_bg"; // 自动生成图片的文件名
    [SerializeField] private float imageTransitionSpeed = 0.5f; // 背景过渡速度
    [SerializeField] private bool imageSmoothTransition = true; // 是否平滑过渡
    [SerializeField] private string dalleImageSize = "1024x1024"; // 新增：DALL-E 图片尺寸 (例如 "1024x1024", "1792x1024", "1024x1792")
    [SerializeField] private string dalleImageQuality = "standard"; // 新增：DALL-E 图片质量 ("standard" or "hd")

    // --- Start 方法：在脚本激活时立即开始生成图片 ---
    void Start()
    {
        // 1. 基本检查
        if (backgroundManager == null)
        {
            Debug.LogError("ImageGenerationService: BackgroundManager 未赋值！无法设置背景。");
            return;
        }
        if (imagePromptFile == null)
        {
            Debug.LogError("ImageGenerationService: 图片 Prompt 文件 (TextAsset) 未赋值！无法生成图片。");
            return;
        }

        // 2. 启动协程：自动生成并设置背景
        StartCoroutine(GenerateAndSetBackgroundRoutine());
    }

    // --- 私有协程：生成图片并直接设置为背景 ---
    private IEnumerator GenerateAndSetBackgroundRoutine()
    {
        // 1. 基本检查 (部分已在 Start 中完成，但为了协程独立性，这里也检查一次)
        if (string.IsNullOrEmpty(imageGenApiKey) || imageGenApiKey == "YOUR_OPENAI_API_KEY_HERE")
        {
            Debug.LogError("ImageGenerationService: OpenAI API Key 未设置或仍是默认值！");
            yield break;
        }
        string ImagePrompt = "あなたはビジュアルノベルの背景画像を生成する専門家です。" + // 指导语
                                    "以下の説明に基づいて、一つのsceneを選び、詳細で写実的かつ映画のようなビジュアルノベルの背景画像を生成してください。" + // 补充说明
                                    "キャラクターは含めないでください。アスペクト比は16:9でお願いします。日本語アニメーションスタイル, Please add black bars to the top and bottom to create a 16:9 aspect ratio. Do not add black bars to the sides.。" +
                                    "\n\n描写：\n"; // 从文件读取的 Prompt 内容
        ImagePrompt = ImagePrompt + imagePromptFile.text.Trim();
        if (string.IsNullOrEmpty(ImagePrompt))
        {
            Debug.LogError("ImageGenerationService: 图片 Prompt 文件内容为空！无法生成图片。");
            yield break;
        }

        Debug.Log($"ImageGenerationService: 正在使用 OpenAI DALL-E 为 Prompt '{ImagePrompt}' 生成图片 '{imageFileName}'...");

        // 2. 构建文件路径和目录 
        string fullSaveDirPath = Path.Combine(Application.persistentDataPath, GENERATED_BG_SUBFOLDER);
        string fullSaveFilePath = Path.Combine(fullSaveDirPath, imageFileName + ".png");

        if (!Directory.Exists(fullSaveDirPath))
        {
            Directory.CreateDirectory(fullSaveDirPath);
        }

        // --- 核心修正：构建 OpenAI DALL-E API 请求 JSON ---
        string jsonBody = "{" +
                          "\"prompt\": \"" + EscapeJsonString(ImagePrompt) + "\"," + // DALL-E 使用 "prompt" 键
                          "\"model\": \"dall-e-3\"," + // 指定模型，推荐 DALL-E 3
                          "\"n\": 1," + // 生成图片数量 (DALL-E 3 只能是 1)
                          "\"size\": \"" + dalleImageSize + "\"," + // 图片尺寸
                          "\"quality\": \"" + dalleImageQuality + "\"" + // 图片质量
                                                                         // DALL-E 3 还可以有 "style": "vivid" 或 "natural"
                          "}";

        UnityWebRequest request = new UnityWebRequest(imageGenEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        // DALL-E 返回的 JSON 中包含图片 URL，而不是直接图片数据，所以 Accept 为 application/json
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + imageGenApiKey);

        // 3. 发送 API 请求
        yield return request.SendWebRequest();

        // 4. 处理 API 响应
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            string errorDetails = $"OpenAI DALL-E 图片生成失败：{request.error}. HTTP Status: {request.responseCode}. Response: {request.downloadHandler?.text}";
            Debug.LogError($"ImageGenerationService: {errorDetails}");
        }
        else
        {
            // --- 核心修正：解析 DALL-E 响应，获取图片 URL ---
            string rawResponse = request.downloadHandler.text;
            Debug.Log($"ImageGenerationService: DALL-E 原始响应: {rawResponse}");
            string imageUrl = null;
            try
            {
                // DALL-E 响应格式：{"data": [{"url": "..."}]}
                JSONNode jsonResponse = SimpleJSON.JSON.Parse(rawResponse);
                imageUrl = jsonResponse["data"][0]["url"].Value;
                Debug.Log($"ImageGenerationService: DALL-E 生成图片 URL: {imageUrl}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ImageGenerationService: 解析 DALL-E 响应失败：{e.Message}. 原始响应: {rawResponse}");
            }

            if (!string.IsNullOrEmpty(imageUrl))
            {
                // 5. **下载生成的图片** (DALL-E 只返回 URL，需要额外下载)
                yield return DownloadImageFromUrl(imageUrl, fullSaveFilePath, imageFileName);
            }
            else
            {
                Debug.LogError("ImageGenerationService: 未能从 DALL-E 响应中获取图片 URL。");
            }
        }
    }

    // --- 新增：从 URL 下载图片并设置背景的协程 ---
    private IEnumerator DownloadImageFromUrl(string imageUrl, string saveFilePath, string fileName)
    {
        Debug.Log($"ImageGenerationService: 正在从 URL 下载图片: {imageUrl}");
        UnityWebRequest imageDownloadRequest = UnityWebRequest.Get(imageUrl);
        imageDownloadRequest.downloadHandler = new DownloadHandlerBuffer();
        yield return imageDownloadRequest.SendWebRequest();

        if (imageDownloadRequest.result == UnityWebRequest.Result.ConnectionError || imageDownloadRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"ImageGenerationService: 下载图片失败：{imageDownloadRequest.error}");
        }
        else
        {
            byte[] imageBytes = imageDownloadRequest.downloadHandler.data;
            try
            {
                // 6. 保存图片到本地
                Directory.CreateDirectory(Path.GetDirectoryName(saveFilePath));
                File.WriteAllBytes(saveFilePath, imageBytes);
                Debug.Log($"ImageGenerationService: 图片已保存到: {saveFilePath}");

                // 7. 将图片加载为 Texture2D
                Texture2D generatedTexture = new Texture2D(2, 2);
                generatedTexture.LoadImage(imageBytes);
                generatedTexture.name = fileName;

                // 8. **核心：直接调用 BackgroundManager 的 SetGeneratedBackground 方法**
                if (backgroundManager != null)
                {
                    backgroundManager.SetGeneratedBackground(generatedTexture, imageTransitionSpeed, imageSmoothTransition);
                    Debug.Log($"ImageGenerationService: 已成功将 '{fileName}' 设置为背景。");
                }
                else
                {
                    Debug.LogError("ImageGenerationService: BackgroundManager 未赋值！无法设置背景。");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ImageGenerationService: 保存或加载图片失败：{e.Message}");
            }
        }
    }

    // --- 辅助方法：转义 JSON 字符串 (保持不变) ---
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
