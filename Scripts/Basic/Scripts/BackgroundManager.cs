using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class BackgroundManager : MonoBehaviour
{
    // Start is called before the first frame update
    BCFC controller;
    public Texture tex;
    public VideoClip mov;
    public float speed;
    public bool smooth;
    void Start()
    {
        controller = BCFC.instance;
        /*
        //场景启动：河边
        BCFC.LAYER layer = null;
        BCFC.instance.cinematic.RemoveVideo(mov);
        Debug.Log("显示background");
        //controller.cinematic.
        this.gameObject.SetActive(false);
        layer = controller.background;
        layer.TransitionToTexture(tex, speed, smooth);
        this.gameObject.SetActive(true);
        */
    }
    public Texture GetBackground(int index = 0)
    {
        Texture[] textures = Resources.LoadAll<Texture>("Images/Background/still");
        return textures[index];
    }
    //todo:场景列表，静态可以用索引调用
    public void SetBK(int index)
    {
        BCFC.LAYER layer = null;
        if (mov != null)
        {
            BCFC.instance.cinematic.RemoveVideo(mov);
        }
        //
        Debug.Log("显示background");
        //controller.cinematic.
        this.gameObject.SetActive(false);
        layer = controller.background;
        tex = GetBackground(index);
        layer.TransitionToTexture(tex, speed, smooth);
       // layer.activeImage.color = GlobalF.SetAlpha(layer.activeImage.color, 1f);//
        this.gameObject.SetActive(true);
    }
    public VideoClip GetClip(int index=0)
    {
        VideoClip[] clips = Resources.LoadAll<VideoClip>("Images/Background/animated");
        return clips[index];
    }
    public void SetMv(int index)
    {
        //跳出场景
        BCFC.LAYER layer = null;
        this.gameObject.SetActive(false);
        layer = controller.cinematic;
        BCFC.instance.background.SetTexture(null);//先R后Q怎么样,而且不能Q和Q之间转换
        mov = GetClip(index);
        layer.TransitionToClip(mov, speed, smooth);
        this.gameObject.SetActive(true);
        //
    }
    // --- 新增函数：加载并显示动态生成的背景图片 ---
    // 这个函数接收一个 Texture2D，并将其平滑过渡到背景图层
    public void SetGeneratedBackground(Texture2D newBackgroundTexture, float transitionSpeed, bool useSmoothTransition)
    {
        if (newBackgroundTexture == null)
        {
            Debug.LogWarning("BackgroundManager: 尝试设置一个空的生成背景纹理。");
            return;
        }

        BCFC.LAYER layer = controller.background;

        // 移除可能存在的视频背景（如果你的逻辑是这样的）
        if (mov != null)
        {
            BCFC.instance.cinematic.RemoveVideo(mov);
            mov = null; // 清空视频引用
        }

        // 假设 `controller.background` （即你的 `BCFC.LAYER`）能够正确处理旧纹理并过渡到新纹理
        Debug.Log($"BackgroundManager: 正在显示动态生成的背景 '{newBackgroundTexture.name}'.");

        // 确保 BackgroundManager 的 GameObject 是激活的，因为它负责控制层
        this.gameObject.SetActive(true);

        // 使用 BCFC.LAYER 的 TransitionToTexture 方法
        layer.TransitionToTexture(newBackgroundTexture, transitionSpeed, useSmoothTransition);
    }
}
