using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class LayerTesting : MonoBehaviour
{
    BCFC controller;

    public Texture tex;
    public VideoClip mov;

    public float speed;
    public bool smooth;
    // Start is called before the first frame update
    void Start()
    {
        controller = BCFC.instance;
    }

    // Update is called once per frame
    void Update()
    {
        BCFC.LAYER layer = null;
        if (Input.GetKey(KeyCode.T))
        {
            BCFC.instance.cinematic.RemoveVideo(mov);

            Debug.Log("��ʾbackground");
            //controller.cinematic.
            this.gameObject.SetActive(false);
            layer = controller.background;
            BCFC.instance.background.SetTexture(tex);
            this.gameObject.SetActive(true);
        }
        if(Input.GetKey(KeyCode.R))
        {

            BCFC.instance.cinematic.RemoveVideo(mov);

            Debug.Log("��ʾbackground");
            //controller.cinematic.
            this.gameObject.SetActive(false);
            layer = controller.background;
            layer.TransitionToTexture(tex,speed,smooth);
            this.gameObject.SetActive(true);
        }
        if (Input.GetKey(KeyCode.Q))
        {

            this.gameObject.SetActive(false);
            layer = controller.cinematic;
            BCFC.instance.background.SetTexture(null);//��R��Q��ô��,���Ҳ���Q��Q֮��ת��
            layer.TransitionToClip(mov, speed, smooth);
            this.gameObject.SetActive(true);
        }
        if (Input.GetKey(KeyCode.W))
        {
            this.gameObject.SetActive(false);
            layer = controller.cinematic;
            Debug.Log("��ʾ��Ƶ");
            
            BCFC.instance.cinematic.SetVideo(mov);
            this.gameObject.SetActive(true);

        }
 
    }
}
