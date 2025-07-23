using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text; // For Encoding.UTF8 and StringBuilder
using SimpleJSON; // Make sure SimpleJSON.cs is in your project Assets

public class OpenAITester : MonoBehaviour
{
    // !!! 重要警告: 此 API Key 将在 WebGL 构建中暴露。仅用于测试目的。 !!!
    // 请替换为你的 OpenAI API Key
    [SerializeField] private string openaiApiKey = "sk-YOUR_OPENAI_API_KEY_HERE";

    private string openaiEndpoint = "https://api.openai.com/v1/chat/completions";

    [TextArea(3, 10)]
    public string inputPrompt = "Hello, what is your name?";

    [TextArea(5, 20)]
    public string responseText = "Waiting for response...";

    // --- UI 调用入口 ---
    public void CallOpenAIApiButton()
    {
        if (string.IsNullOrEmpty(openaiApiKey) || openaiApiKey == "sk-YOUR_OPENAI_API_KEY_HERE")
        {
            Debug.LogError("请在 Inspector 中设置正确的 OpenAI API Key！");
            responseText = "错误: 请设置API Key。";
            return;
        }
        if (string.IsNullOrEmpty(inputPrompt))
        {
            Debug.LogError("请输入提示！");
            responseText = "错误: 请输入提示。";
            return;
        }
        StartCoroutine(SendChatRequest(inputPrompt));
    }

    // --- 网络请求协程 ---
    IEnumerator SendChatRequest(string prompt)
    {
        responseText = "发送请求中...";
        Debug.Log("向OpenAI发送请求: " + prompt);

        // 使用内部定义的辅助方法安全地转义 prompt 字符串
        string escapedPrompt = EscapeJsonString(prompt);

        // 手动构建 JSON 请求体
        string jsonBody = "{" +
                          "\"model\": \"gpt-3.5-turbo\"," + // 或者 "gpt-4o", "gpt-4", etc.
                          "\"messages\": [" +
                            "{\"role\": \"user\", \"content\": \"" + escapedPrompt + "\"}" +
                          "]," +
                          "\"temperature\": 0.7," + // 控制生成文本的随机性
                          "\"max_tokens\": 150" +    // 控制生成文本的最大长度
                          "}";

        Debug.Log("Generated JSON Body: " + jsonBody); // 打印生成的 JSON，方便调试

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        // 创建并发送 UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(openaiEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);

        yield return request.SendWebRequest(); // 等待请求完成

        // --- 处理响应 ---
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("OpenAI API 请求失败: " + request.error);
            responseText = "API请求失败: " + request.error;
            // 打印更多调试信息
            Debug.LogError("HTTP Status Code: " + request.responseCode);
            Debug.LogError("Response Headers: " + request.GetResponseHeaders());
            Debug.LogError("原始响应文本 (如果存在): " + request.downloadHandler.text);
        }
        else
        {
            string rawResponse = request.downloadHandler.text;
            Debug.Log("OpenAI API 原始响应: " + rawResponse);

            // 尝试解析 JSON 响应
            try
            {
                JSONNode jsonResponse = JSON.Parse(rawResponse);
                // 提取助手回复内容
                string assistantMessage = jsonResponse["choices"][0]["message"]["content"].Value;
                responseText = "LLM 回复: " + assistantMessage;
            }
            catch (System.Exception e)
            {
                Debug.LogError("JSON 解析错误: " + e.Message);
                responseText = "JSON 解析失败，原始响应: " + rawResponse;
            }
        }
    }

    // --- 辅助方法：安全转义 JSON 字符串 ---
    // 这个方法负责将字符串中的特殊字符（如引号、反斜杠、换行符等）进行转义，
    // 以确保它们在 JSON 字符串中是合法的。
    private string EscapeJsonString(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        foreach (char c in text)
        {
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '/': // 正斜杠也可以转义，但非强制
                    sb.Append("\\/");
                    break;
                default:
                    // 对于控制字符（ASCII < 32），JSON 要求使用 \uXXXX 转义
                    // 这里我们假设输入是可打印的，或者由 OpenAITester 的用户负责。
                    // 对于更严格的 Unicode 处理，可能需要更复杂的逻辑。
                    if (c < 32 || c > 126) // 如果是控制字符或非基本ASCII，使用Unicode转义
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("X4")); // 转换为四位十六进制
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }
}