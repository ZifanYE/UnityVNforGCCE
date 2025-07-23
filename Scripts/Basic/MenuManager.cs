using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public Button EndBtn;
    // Start is called before the first frame update
    void Start()
    {
        EndBtn.onClick.AddListener(EndGame);
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
}
