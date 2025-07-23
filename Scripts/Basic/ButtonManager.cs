using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    public Button TouchArea;
    //public Button ChoiceButton;
    public Button AutoBtn;
    public GameObject Auto;
    public Button SkipBtn;
    public GameObject Skip;
    public Button EndBtn;
    public Testing Dt;
    public VNStoryLoader vNStoryLoader;
    bool isAuto = false;
    bool isSkip = false;

    // Start is called before the first frame update
    void Start()
    {
        EndBtn.onClick.AddListener(EndGame);
        AutoBtn.onClick.AddListener(auto);
        SkipBtn.onClick.AddListener(skip);
        TouchArea.onClick.AddListener(TouchAreaClick);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void EndGame()
    {
        // 调用 Application.Quit() 结束游戏
        Application.Quit();
    }
    void auto()
    {
        if (!isAuto)
        {
            if (isSkip)
            {
                cancelInvoke();
            }
            Debug.Log("开始自动");
            AutoBtn.enabled = false;
            InvokeRepeating("autospeaking", 0f, 1.5f);
            isAuto = true;
        }

    }
    public void skip()
    {
        if (!isSkip)
        {
            if (isAuto)
            {
                cancelInvoke();
            }
            Debug.Log("开始有限自动");
            SkipBtn.enabled = false;
            InvokeRepeating("autoSkip", 0f, 0.2f);
            isSkip = true;
        }
    }
    void autoSkip()
    {
        if (!isSkip)
        {
            return;
        }
        //Dt.Story();
        vNStoryLoader.typeSpeed = 0.01f;

        StartCoroutine(vNStoryLoader.Auto());

    }
    void autospeaking()
    {
        if (!isAuto)
        {
            return;
        }
        //Dt.Story();
        vNStoryLoader.typeSpeed = 0.05f;
        StartCoroutine(vNStoryLoader.Auto());



    }
    public void TouchAreaClick()
    {
        cancelInvoke();
        Dt.Story();
    }
    public void cancelInvoke()
    {
        isAuto = false;
        isSkip = false;
        vNStoryLoader.isAuto = false;

        CancelInvoke("autospeaking");
        Auto.SetActive(false);
        AutoBtn.enabled = true;
        CancelInvoke("autoSkip");
        vNStoryLoader.typeSpeed = 0.05f;

        Skip.SetActive(false);
        SkipBtn.enabled = true;
        /*
        if (Auto.active)
        {
            CancelInvoke("autospeaking");
            Auto.SetActive(false);
            AutoBtn.enabled = true;
        }
        if (Skip.active)
        {
            CancelInvoke("autoSkip");
            Skip.SetActive(false);
            SkipBtn.enabled = true;
        }
        */
    }
}
