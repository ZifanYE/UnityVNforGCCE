using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using static CharacterManager;
using System.Runtime.CompilerServices;
using System.Linq;

public class Testing : MonoBehaviour
{
    public Characters Anna;
    public Characters Uru;
    public BackgroundManager BKM;
    public GameObject Choices;
    public ButtonManager Buttons;
   // public Button TouchArea;
   // public Button ButtonA;
    //public Button ButtonB;
   // public Button AutoBtn;
   // public GameObject Auto;
    public bool A = false;
    bool allowInteractions = true;
    DialogueSystem1 dialogue;
    //BCFC controller;
    // public Texture tex;
    // public VideoClip mov;
    // public float speed;
    //public bool smooth;
    // Start is called before the first frame update
    void Start()
    {

        Anna = CharacterManager.instance.GetCharacter("Anna", enableCreatedCharacterOnStart: false);
        Uru = CharacterManager.instance.GetCharacter("Uru", enableCreatedCharacterOnStart: false);
        dialogue = DialogueSystem1.instance;
        //场景,先启动初始背景（静态）
        //  controller = BCFC.instance;
        Say(s[0]);
        BKM.SetBK(1);
       // AutoBtn.onClick.AddListener(auto);
        //TouchArea.onClick.AddListener(TouchAreaClick);
    }

    public string[] s = new string[]
    {
       /* 
        * 0侦探安娜和助手安德森经营着街角的一间小事务所，两人平稳的生活和这座宁静的小镇一样，仿佛毫无涟漪。:  
        * 1直到一封突然出现的邀请信，打破了日常的循环。
        * 2打开台灯，昏黄的灯光打在了一封陌生的信上。
        * 3安娜难掩疲惫的双眸盯着那封信
        * 4信封上暗刻的花纹在光下若隐若现
        * 5=这东西怎么会突然出现在她桌上呢？
        * 6"安德森？":Anna
        * 7安娜下意识地四处寻找起她的助手，但一眼望尽的事务所显然没人其他人在了。: 
        * 8安德森最近都没来事务所了，没了助手的协助，安娜比以前辛苦了不少
        * 9她撩了一把鬓边散乱的碎发，打开信封。
        * 10"亲爱的安娜：明晚在男爵庄园将举行春季舞会，本地年轻的贵族小姐、贵族公子们借时均会到场...":Anna
        * 11您这样美丽的女子也不应当缺席。尼科尔·查理男爵诚邀您一同前往参加欢宴。艾琳"
        * 12春季舞会？男爵庄园？这是什么恶作剧吗？"
        * 13尽管心中满是疑惑，但安娜还是将邀请函收入包中，侦探的直觉告诉她不该轻视任何奇怪的线索，也许这封奇怪的信件正是委托人的请求。: 
        * 14"也没有写报酬是多少，明天去看一下吧，希望委托人不要太小气就好。":Anna
        * 15--第二天早晨--:  
        * 
        * 
        * 
        * 
       */

    };
    int index = 1;
    int i = 0;
    // Update is called once per frame
    void Update()
    {
        //设置动态视频 BKM.SetMv(1);
        //设置静态图片 BKM.SetBK(5);
        //切换立绘 Yuki.SetBody(1);
        //移动  Yuki.MoveTo(characterposition.bottomLeft, 2f, true);
        //Uru.SetAlpha(0);//让人物消失
        /*有选项的时候
         * Choices.SetActive(true);
                    ButtonA.onClick.AddListener(choiceA);
                    ButtonB.onClick.AddListener(choiceB);
                    allowInteractions = false;
                    //有选项的时候，生成其他Dialogu eTesting,连续下去
         * */

        if (Input.GetKeyDown("space"))
        {
            Buttons.cancelInvoke();
            Story();

        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            //改颜色·AutoBtn.colors;
            Buttons.AutoBtn.onClick.Invoke();
        }

    }
    
    public void Story()
    {
        
        if ((!dialogue.isSpeaking || dialogue.isWaitingForUserInput) && allowInteractions)
        {
            if (index >= s.Length)
            {
                return;
            }

            Say(s[index]);
            if (index == 2)
            {
                BKM.SetBK(2);
            }
            if (index == 9)
            {
                Anna.SetAlpha(0, true);
            }
            if (index == 10)
            {
                Anna.SetAlpha(1, true);
            }
            if (index == 15)
            {
                BKM.SetBK(5);
            }


            if (index == 12)
            {
                Anna.SetBody(4);
            }
            if (index == 14)
            {
                Anna.SetBody(7);
            }
            if (index == 15)
            {

                Anna.SetAlpha(0, true);
            }



            index++;
        }
    }
    
    void choiceA()
    {
        string[] s1 =
        {
            ".....",
            "=哈哈哈哈",
            "你选了A选项呢",
        };
        choiceInsert(s1);
        A = true;
        choiceClear();

    }
    void choiceClear()
    {
        Choices.SetActive(false);
    }
    void choiceB()
    {
        string[] s1 =
        {
            "我们出发吧:Uru",
            "就这样，选择了B的我们开始了旅程",
            "真可惜"
        };
        choiceInsert(s1);
        A = true;
        choiceClear();
    }
    void choiceInsert(string[] s1)
    {
        if (index >= 0 && index <= s.Length)
        {
            // 将s1数组插入到s数组的指定位置
            s = s.Take(index).Concat(s1).Concat(s.Skip(index)).ToArray();
        }
        allowInteractions = true;
    }

    void Say(string s)
    {
        string[] parts = s.Split(':');
        string speech = parts[0];
        string speaker = (parts.Length >= 2) ? parts[1] : "";
        if (speaker != null)
        {
            if (speaker == "Uru")
            {
                Anna.SetAlpha(0.9f);
                Uru.SetAlpha(1f);
                string[] partsadd = speaker.Split('=');
                if (partsadd.Length >= 2)
                {
                    Uru.Say(speech, true);
                }
                else
                    Uru.Say(speech, false);
            }
            else if (speaker == "Anna")
            {
                Uru.SetAlpha(0.9f);
                Anna.SetAlpha(1f);
                string[] partsadd = speaker.Split('=');
                if (partsadd.Length >= 2)
                {
                    Anna.Say(speech, true);
                }
                else
                    Anna.Say(speech, false);
            }
            else
            {
                //新加的
                Uru.SetAlpha(1f);
                Anna.SetAlpha(1f);
                string[] parts2 = speech.Split('=');
                if (parts2.Length >= 2)
                {
                    speech = "\n" + parts2[1]; //换行显示
                    dialogue.SayAdd(speech, speaker);
                }
                else
                    dialogue.Say(speech, speaker);
            }
        }

    }
}
