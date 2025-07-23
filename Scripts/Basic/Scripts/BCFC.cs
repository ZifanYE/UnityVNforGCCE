using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.Video;

public class BCFC : MonoBehaviour
{
    public static BCFC instance;

    public LAYER background = new LAYER();
    public LAYER foreground = new LAYER();
    public LAYER cinematic = new LAYER();
    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }

    [System.Serializable]
    public class LAYER
    {
        public GameObject root;
        public GameObject newImageObjectReference;
        public RawImage activeImage;
        public List<RawImage> allImages = new List<RawImage>();
        public RenderTexture rendertex;

        public void SetTexture(Texture texture)
        {

            if(texture != null)
            {
                if(activeImage == null)
                {
                    CreateNewActiveImage(); 
                }
                activeImage.texture = texture;
                activeImage.color = GlobalF.SetAlpha(activeImage.color, 1f);
            }
            else
            {
                if (activeImage != null)
                {
                    allImages.Remove(activeImage);
                    Debug.Log("��������");
                    GameObject.DestroyImmediate(activeImage.gameObject);
                    Debug.Log("������");
                    activeImage = null;
                }
            }
        }

        /*
        public void DropVideo(VideoClip clip)
        {
            VideoPlayer mov = new();
            if (clip != null)
            {
                mov.Stop();
            }
        }
        */
        public void SetVideo(VideoClip clip)
        {
            if (clip != null)
            {
                if (activeImage == null)
                {
                    Destroy(activeImage);//
                    CreateNewActiveImage();     
                }
                activeImage.GetComponent<VideoPlayer>().clip = clip;
                activeImage.color = GlobalF.SetAlpha(activeImage.color, 1f);

                //clip.Equals(activeImage);
            }
            else
            {
                if (activeImage != null)
                {
                    allImages.Remove (activeImage);
                    Debug.Log("��������");
                    DestroyImmediate(activeImage.gameObject);
                    Debug.Log("������");
                    activeImage = null;
                }
            }
            //VideoPlayer mov = new();
            //mov.GetComponent<VideoPlayer>().GetComponent<VideoPlayer>().clip = clip;
            //Debug.Log("��ʾ��Ƶ����?");
            //mov.isLooping = ifMovieThenLoop;//ѭ������
            //mov.Play();
            //rendertex.Release();
            //Debug.Log("��ʾ��Ƶ����");
            
        }
        public void RemoveVideo(VideoClip clip)
        {
            if (clip != null)
            {
                if(activeImage != null && (activeImage.GetComponent<VideoPlayer>().clip != null))
                {
                    activeImage.GetComponent<VideoPlayer>().clip = null;
                    activeImage.color = GlobalF.SetAlpha(activeImage.color, 0f);
                    allImages.Remove(activeImage);
                    //activeImage = null;
                    DestroyImmediate(activeImage.gameObject);

                    Debug.Log("��������");
                }
               // Destroy(activeImage.gameObject);
                return;
            }
            else
            {
                Debug.Log("���ڲ�����clip");
                return;
            }
            
        }
        public void TransitionToTexture(Texture texture, float speed = 2f, bool smooth = false,bool ifMovieTheLoop = true)
        {
            
            if(activeImage != null && activeImage.texture == texture)//
            {
                return;
            }
            
            StopTransitioning();
            Debug.Log("��ʼת����");
            transitioning =BCFC.instance.StartCoroutine(Transitioning(texture,speed, smooth, ifMovieTheLoop));
            Debug.Log("ת������");
        }

        void StopTransitioning()
        {
            if(isTransitioning)
            {
                BCFC.instance.StopCoroutine(transitioning);
            }
            transitioning = null;
        }

        public bool isTransitioning { get { return transitioning != null; } }
        Coroutine transitioning = null;

        IEnumerator Transitioning(Texture texture, float speed, bool smooth, bool ifMoveTheLoop)
        {
            if(texture != null)
            {
                for (int i = 0; i < allImages.Count; i++)
                {
                    RawImage image = allImages[i];
                    if (image.texture == texture)
                    {
                        activeImage = image;
                        break;
                    }
                }
                if (activeImage == null || activeImage.texture != texture)
                {
                    CreateNewActiveImage();  
                    activeImage.texture = texture;
                    activeImage.color = GlobalF.SetAlpha(activeImage.color, 0f);
                    
                    //activeImage.GetComponent<VideoPlayer>().clip = clip;
                }
            }
            else
            {
                activeImage = null;
                Debug.Log("??");
            }
            while(GlobalF.TransitionRawImages(ref activeImage, ref allImages, speed, smooth))
            {
                yield return new WaitForEndOfFrame();
            }

            StopTransitioning();
            
        }

        //��Ƶת��
        IEnumerator TransitioningV(VideoClip clip, float speed, bool smooth, bool isLooping)
        {
            if (clip != null)
            {
                for (int i = 0; i < allImages.Count; i++)
                {
                    RawImage image = allImages[i];
                    if (image.GetComponent<VideoPlayer>().clip==clip)
                    {
                        activeImage = image;
                        break;
                    }
                }
                if (activeImage == null || activeImage.GetComponent<VideoPlayer>().clip != clip)
                {
                    CreateNewActiveImage();
                    activeImage.GetComponent<VideoPlayer>().clip = clip;
                    activeImage.color = GlobalF.SetAlpha(activeImage.color, 0f);
                    //activeImage.GetComponent<VideoPlayer>().clip = clip;
                }
            }
            else
            {
                activeImage = null;
                Debug.Log("??");
            }
            while (GlobalF.TransitionRawImages(ref activeImage, ref allImages, speed, smooth))
            {
                yield return new WaitForEndOfFrame();
            }

            StopTransitioning();

        }
        public void TransitionToClip(VideoClip clip, float speed = 2f, bool smooth = false, bool isLooping = true)
        {
            /*
            if(activeImage != null == texture)//activeImage != null && activeImage.texture == texture
            {
                return;
            }
            */
            StopTransitioning();
            Debug.Log("��ʼת����");
            transitioning = BCFC.instance.StartCoroutine(TransitioningV(clip, speed, smooth, isLooping));
            Debug.Log("ת������");
        }
        void CreateNewActiveImage()
        {
            GameObject ob = Instantiate(newImageObjectReference,root.transform) as GameObject;
            ob.SetActive(true);
            RawImage raw = ob.GetComponent<RawImage>();
            activeImage = raw;  
            allImages.Add(raw);
        }
    }
}
