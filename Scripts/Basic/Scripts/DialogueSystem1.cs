using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.UI; // <-- 移除此行，因为我们不再使用旧版 Text 组件

using TMPro; // <-- 添加此行，用于 TextMeshPro 组件

public class DialogueSystem1 : MonoBehaviour
{
    public static DialogueSystem1 instance;
    public ELEMENTS elements;

    void Awake()
    {
        instance = this;
    }

    public void Say(string speech, string speaker = "", bool additive = false)
    {
        StopSpeaking();

        if (additive)
        {
            // 注意：这里需要访问 TextMeshProUGUI 的 .text 属性
            speechText.text = targetSpeech;
        }
        speaking = StartCoroutine(Speaking(speech, additive, speaker));
    }

    public void SayAdd(string speech, string speaker = "")
    {
        StopSpeaking();
        // 注意：这里需要访问 TextMeshProUGUI 的 .text 属性
        speechText.text = targetSpeech;

        speaking = StartCoroutine(Speaking(speech, true, speaker));
    }

    public void StopSpeaking()
    {
        if (isSpeaking)
        {
            StopCoroutine(speaking);
            speaking = null;
        }
    }

    public bool isSpeaking { get { return speaking != null; } }
    [HideInInspector] public bool isWaitingForUserInput = false;

    string targetSpeech = "";

    Coroutine speaking = null;
    IEnumerator Speaking(string speech, bool additive, string speaker)
    {
        speechPanel.SetActive(true);
        targetSpeech = speech;
        if (!additive)
        {
            // 注意：这里需要访问 TextMeshProUGUI 的 .text 属性
            speechText.text = "";
        }
        else
        {
            // 注意：这里需要访问 TextMeshProUGUI 的 .text 属性
            targetSpeech = speechText.text + targetSpeech;
        }

        if (speaker == "Anna")
        {
            // 注意：这里需要访问 TextMeshProUGUI 的 .text 属性
            speakerNameText.text = "安娜";
        }
        else
        {
            // 注意：这里需要访问 TextMeshProUGUI 的 .text 属性
            speakerNameText.text = DetermineSpeaker(speaker); //temporary
        }

        isWaitingForUserInput = false;

        // 逐字显示逻辑
        while (speechText.text != targetSpeech)
        {
            // 确保 targetSpeech 足够长，防止索引越界
            if (speechText.text.Length < targetSpeech.Length)
            {
                speechText.text += targetSpeech[speechText.text.Length];
            }
            else // 如果意外地 text 长度超过或等于 targetSpeech，就跳出循环
            {
                break;
            }
            yield return new WaitForSeconds(0.05f);
            // yield return new WaitForEndOfFrame();
        }

        // 文本显示完毕
        isWaitingForUserInput = true;
        while (isWaitingForUserInput)
        {
            yield return new WaitForEndOfFrame();
        }
        StopSpeaking();
    }

    string DetermineSpeaker(string s)
    {
        // 注意：这里需要访问 TextMeshProUGUI 的 .text 属性
        string retVal = speakerNameText.text;
        if (s != speakerNameText.text && s != "")
            retVal = (s.ToLower().Contains("narrator")) ? "" : s;
        // else if (s == "Anna") return "安娜";

        return retVal;
    }

    public void Close()
    {
        StopSpeaking();
        speechPanel.SetActive(false);
    }

    [System.Serializable]
    public class ELEMENTS
    {
        public GameObject speechPanel;
        // <-- 将 Text 类型改为 TextMeshProUGUI
        public TextMeshProUGUI speakerNameText;
        // <-- 将 Text 类型改为 TextMeshProUGUI
        public TextMeshProUGUI speechText;
    }

    public GameObject speechPanel { get { return elements.speechPanel; } }
    // <-- 将 Text 类型改为 TextMeshProUGUI
    public TextMeshProUGUI speakerNameText { get { return elements.speakerNameText; } }
    // <-- 将 Text 类型改为 TextMeshProUGUI
    public TextMeshProUGUI speechText { get { return elements.speechText; } }
}