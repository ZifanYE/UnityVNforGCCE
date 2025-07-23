using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class Characters
{
    public string characterName;
    [HideInInspector] public RectTransform root;

    //public bool isMultiLayerCharacter { get { return renderers.renderer == null; } }

    public bool enabled { get { return root.gameObject.activeInHierarchy; } set { root.gameObject.SetActive(value); } }

    public Vector2 anchorPadding { get { return root.anchorMax - root.anchorMin; } }
    DialogueSystem1 dialogue;
    public void Say(string speech, bool add)
    {

        if (!enabled)
            enabled = true;
        if (add == false)
            dialogue.Say(speech, characterName);
        else
            dialogue.SayAdd(speech, characterName);
    }

    Vector2 targetPosition;
    Coroutine moving;
    bool isMoving { get { return moving != null; } }
    public void MoveTo(Vector2 Target, float speed, bool smooth = true)
    {
        StopMoving();
        moving = CharacterManager.instance.StartCoroutine(Moving(Target, speed, smooth));
    }
    public void StopMoving(bool arriveAtTargetPositionImmediately = false)
    {
        if (isMoving)
        {
            CharacterManager.instance.StopCoroutine(moving);
        }
        moving = null;
    }
    public void SetAlpha(float alpha)
    {
        //Image image = GameObject.Instantiate(renderers.bodyRenderer).GetComponent<Image>();
        Image image = renderers.bodyRenderer.GetComponent<Image>();
        image.color = GlobalF.SetAlpha(image.color, alpha);
    }
    public void SetAlpha(float alpha, bool smooth = true)
    {
        CharacterManager.instance.StartCoroutine(SettingAlpha(alpha));
    }
    private IEnumerator SettingAlpha(float alpha)
    {
        // 获取渲染器的 Image 组件
        Image image = renderers.bodyRenderer.GetComponent<Image>();

        // 获取当前颜色
        Color currentColor = image.color;

        // 逐渐改变 alpha 值
        while (Mathf.Abs(currentColor.a - alpha) > 0.01f)
        {
            currentColor.a = Mathf.Lerp(currentColor.a, alpha, Time.deltaTime * 5f);
            image.color = currentColor;
            yield return null;
        }

        // 最终设定目标 alpha 值
        currentColor.a = alpha;
        image.color = currentColor;
    }
    public void SetPosition(Vector2 target)
    {
        Vector2 padding = anchorPadding;
        float maxX = 1f - padding.x;
        float maxY = 1f - padding.y;
        Vector2 minAnchorTarget = new Vector2(maxX * targetPosition.x, maxY * targetPosition.y);

        root.anchorMin = minAnchorTarget;
        root.anchorMax = root.anchorMin + padding;
    }
    IEnumerator Moving(Vector2 Target, float speed, bool smooth)
    {
        targetPosition = Target;
        Vector2 padding = anchorPadding;
        float maxX = 1f - padding.x;
        float maxY = 1f - padding.y;

        Vector2 minAnchorTarget = new Vector2(maxX * targetPosition.x, maxY * targetPosition.y);
        speed *= Time.deltaTime;

        while (root.anchorMin != minAnchorTarget)
        {
            root.anchorMin = (!smooth) ? Vector2.MoveTowards(root.anchorMin, minAnchorTarget, speed) : Vector2.Lerp(root.anchorMin, minAnchorTarget, speed);
            root.anchorMax = root.anchorMin + padding;
            yield return new WaitForEndOfFrame();
        }
    }
    //begin Transitioning Images
    public Sprite GetSprite(int index = 0)
    {
        // Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Characters/Ange/Ange Ushiromiya (Witch)");
        Debug.Log(characterName);
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Characters/" + characterName);
        Debug.Log(sprites.Length);
        Debug.Log(sprites);
        Debug.Log(index);
        Debug.Log(sprites[index]);
        return sprites[index];
    }

    public void SetBody(int index)
    {
        Debug.Log("现在要展现：" + index);
        renderers.bodyRenderer.sprite = GetSprite(index);
    }
    public void SetBody(Sprite sprite)
    {
        renderers.bodyRenderer.sprite = sprite;
    }

    public void SetExpression(int index)
    {
        renderers.expressionRenderer.sprite = GetSprite(index);
    }
    public void SetExpression(Sprite sprite)
    {
        renderers.expressionRenderer.sprite = sprite;
    }

    bool isTransitioningBody { get { return transitioningBody != null; } }
    //bool isTransitioningBody {get{transitioningBody != null; } }
    Coroutine transitioningBody = null;

    public void TransitionBody(Sprite sprite, float speed, bool smooth)
    {
        if (renderers.bodyRenderer.sprite == sprite)
            return;

        StopTransitioningBody();
        transitioningBody = CharacterManager.instance.StartCoroutine(TransitioningBody(sprite, speed, smooth));
    }
    void StopTransitioningBody()
    {
        if (isTransitioningBody)
            CharacterManager.instance.StopCoroutine(transitioningBody);
        transitioningBody = null;
    }

    public IEnumerator TransitioningBody(Sprite sprite, float speed, bool smooth)
    {
        for (int i = 0; i < renderers.allBodyRenderers.Count; i++)
        {
            Image image = renderers.allBodyRenderers[i];
            if (image.sprite == sprite)
            {
                renderers.bodyRenderer = image;
                break;
            }
        }

        if (renderers.bodyRenderer.sprite != sprite)
        {
            Image image = GameObject.Instantiate(renderers.bodyRenderer.gameObject, renderers.bodyRenderer.transform.parent).GetComponent<Image>();
            renderers.allBodyRenderers.Add(image);
            renderers.bodyRenderer = image;
            image.color = GlobalF.SetAlpha(image.color, 0f);
            image.sprite = sprite;
        }
        while (GlobalF.TransitionImages(ref renderers.bodyRenderer, ref renderers.allBodyRenderers, speed, smooth))
            yield return new WaitForEndOfFrame();

        StopTransitioningBody();
    }
    public void Activate()
    {
        if (!enabled)
        {
            Debug.Log($"{characterName} 被激活了");
            enabled = true;
        }
    }

    //End
    //create a new character..
    public Characters(string _name, bool enableOnstart = true)
    {
        CharacterManager cm = CharacterManager.instance;
        GameObject prefab = Resources.Load("Characters/Character[" + _name + "]") as GameObject;
        GameObject ob = GameObject.Instantiate(prefab, cm.characterPanel);

        root = ob.GetComponent<RectTransform>();
        characterName = _name;

        renderers.bodyRenderer = ob.transform.Find("bodyLayer").GetComponent<Image>();
        renderers.expressionRenderer = ob.transform.Find("expressionLayer").GetComponent<Image>();
        //get the renderer(s)

        dialogue = DialogueSystem1.instance;

        enabled = enableOnstart;
    }
    [System.Serializable]
    class Renderers
    {
        public Image bodyRenderer;
        public Image expressionRenderer;

        public List<Image> allBodyRenderers = new();
        public List<Image> allExpressionRenderers = new();
    }
    Renderers renderers = new();
}
