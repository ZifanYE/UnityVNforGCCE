using UnityEngine;
using static CharacterManager;

public class CharacterTesting : MonoBehaviour
{
    public Characters Anji;
    public Characters Uru;
    // Start is called before the first frame update
    void Start()
    {
        Anji = CharacterManager.instance.GetCharacter("Anji", enableCreatedCharacterOnStart: false);
        Uru = CharacterManager.instance.GetCharacter("Uru", enableCreatedCharacterOnStart: false);
        //Avira = CharacterManager.instance.GetCharacter("Avira", enableCreatedCharacterOnStart: false);

    }

    public string[] speech;
    int i = 0;

    public Vector2 moveTarget;
    public float moveSpeed;
    public bool smooth;


    public int bodyIndex, expressionIndex = 0;
    public float speed = 5f;
    public bool smoothtransitions = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (i < speech.Length)
            {
                string[] parts = speech[i].Split('=');//say()与sayAdd()的区别
                if (parts.Length >= 2)
                {
                    Anji.Say(parts[1], true);
                }
                else
                {
                    Anji.Say(speech[i], false);
                }
            }
            else
                DialogueSystem1.instance.Close();

            i++;
        }
        if (Input.GetKeyDown(KeyCode.A))//Avira讲话
        {
            if (i < speech.Length)
            {
                string[] parts = speech[i].Split('=');
                if (parts.Length >= 2)
                {
                    Uru.Say(parts[1], true);
                }
                else
                {
                    Uru.Say(speech[i], false);
                }
            }
            else
                DialogueSystem1.instance.Close();

            i++;
        }
        if (Input.GetKey(KeyCode.M))//移动角色
        {
            Anji.MoveTo(characterposition.bottomLeft, moveSpeed, smooth);

            Uru.MoveTo(characterposition.bottomRight, moveSpeed, smooth);
        }

        if (Input.GetKey(KeyCode.S))//停止移动
        {
            Anji.StopMoving(true);
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            Anji.TransitionBody(Anji.GetSprite(bodyIndex), speed, smoothtransitions);


        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            Anji.SetBody(bodyIndex);
        }
        /*
        if (Input.GetKeyDown(KeyCode.E))
        {
            Anji.SetExpression(expressionIndex);
        }
        */
    }
}
