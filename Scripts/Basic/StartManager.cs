using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartManager : MonoBehaviour
{
    public Button StartBtn;
    public Button EndBtn;
    // Start is called before the first frame update
    void Start()
    {
        StartBtn.onClick.AddListener(SceneSwitcher);
        EndBtn.onClick.AddListener(EndGame);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SceneSwitcher()
    {
        SceneManager.LoadScene("Story1");
    }
    public void EndGame()
    {
        // 调用 Application.Quit() 结束游戏
        Application.Quit();
    }
}
