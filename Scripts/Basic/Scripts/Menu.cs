using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    // Start is called before the first frame update
    GameObject m_Menu;
    private void Start()
    {
       // m_Menu = GetComponent<GameObject>();
        this.SetActive(false);
    }
    public void SetActive(bool active)
    {
        if (m_Menu != null)
        {
            if(active!=true)
            {
                m_Menu.SetActive(false);
                Debug.Log("ÏûÁËÂð");
            }
            else
            {
                m_Menu.SetActive(true);
            }
        }
    }
    
}
