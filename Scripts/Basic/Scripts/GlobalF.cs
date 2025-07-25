using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class GlobalF : MonoBehaviour
{
    public static bool TransitionImages(ref Image activeImage, ref List<Image> allImages, float speed,bool smooth)
    {
        bool anyValueChanged = false;

        speed *= Time.deltaTime;
        for (int i = allImages.Count - 1; i >= 0; i--)
        {
            Image image = allImages[i];

            if(image == activeImage)
            {
                if(image.color.a < 1f)
                {
                    image.color = SetAlpha(image.color, smooth ? Mathf.Lerp(image.color.a, 1f, speed) : Mathf.MoveTowards(image.color.a, 1f, speed));
                    anyValueChanged = true;
                } 
            }
            else
            {
                if(image.color.a > 0)
                {
                    image.color = SetAlpha(image.color, smooth ? Mathf.Lerp(image.color.a, 0f, speed) : Mathf.MoveTowards(image.color.a, 0f, speed));
                    anyValueChanged = true;
                }
                else
                {
                    allImages.RemoveAt(i);
                    DestroyImmediate (image.gameObject);
                    continue;
                }
            }
        }

        return anyValueChanged;
    }
    public static bool TransitionRawImages(ref RawImage activeImage, ref List<RawImage> allImages, float speed, bool smooth)
    {
        bool anyValueChanged = false;

        speed *= Time.deltaTime;
        for (int i = allImages.Count - 1; i >= 0; i--)
        {
            RawImage image = allImages[i];

            if (image == activeImage)
            {
                if (image.color.a < 1f)
                {
                    image.color = SetAlpha(image.color, smooth ? Mathf.Lerp(image.color.a, 1f, speed) : Mathf.MoveTowards(image.color.a, 1f, speed));
                    anyValueChanged = true;
                }
            }
            else
            {
                if (image.color.a > 0)
                {
                    image.color = SetAlpha(image.color, smooth ? Mathf.Lerp(image.color.a, 0f, speed) : Mathf.MoveTowards(image.color.a, 0f, speed));
                    anyValueChanged = true;
                }
                else
                {
                    VideoClip mov = activeImage.GetComponent<VideoPlayer>().clip;//?不确定
                    BCFC.instance.cinematic.RemoveVideo(mov);
                    //movie停止？
                    /*
                    if (VideoClip clip != null)
                    {
                        if (activeImage != null)
                        {
                            activeImage.GetComponent<VideoPlayer>().clip = null;
                            activeImage.color = GlobalF.SetAlpha(activeImage.color, 0f);
                            allImages.Remove(activeImage);
                            activeImage = null;
                        }
                        // Destroy(activeImage.gameObject);
                        return;
                    }
                    else
                    {
                        Debug.Log("现在不存在clip");
                    }
                    */
                    allImages.RemoveAt(i);
                    DestroyImmediate(image.gameObject);
                    continue;
                    
                }
            }
        }

        return anyValueChanged;
    }
    public static Color SetAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }
}
