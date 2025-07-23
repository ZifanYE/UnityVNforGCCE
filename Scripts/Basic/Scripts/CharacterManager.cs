using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager instance;
    public RectTransform characterPanel;
    public List<Characters> characters = new List<Characters>();

    public Dictionary<string, int> characterDictionary = new Dictionary<string, int>();
    private void Awake()
    {
        instance = this;
    }

    public Characters GetCharacter(string characterName, bool createCharacterIfDoesNotExist = true, bool enableCreatedCharacterOnStart = true)
    {
        int index = -1;
        if (characterDictionary.TryGetValue(characterName, out index))
        {
            return characters[index];
        }
        else if (createCharacterIfDoesNotExist)
        {
            return CreateCharacter(characterName, enableCreatedCharacterOnStart);
        }

        return null;
    }

    public Characters CreateCharacter(string characterName, bool enableOnStart = true)
    {
        Characters newCharacter = new(characterName, enableOnStart);

        characterDictionary.Add(characterName, characters.Count);
        characters.Add(newCharacter);

        return newCharacter;
    }

    public class CHARACTERPOSITIONS
    {
        public Vector2 bottomLeft = new Vector2(0, 0);
        public Vector2 topRight = new Vector2(1f, 1f);
        public Vector2 topLeft = new Vector2(0f, 1f);
        public Vector2 center = new Vector2(0.5f, 0.5f);
        public Vector2 bottomcenter = new Vector2(0.5f, 0);
        public Vector2 bottomRight = new Vector2(1f, 0);

        public Vector2 rightdisappear = new Vector2(4f, 0);

    }
    public static CHARACTERPOSITIONS characterposition = new CHARACTERPOSITIONS();

    public class CHARACTEREXPRESSIONS
    {
        public int normal = 0;
        public int sky = 1;
        public int normalAngle = 2;
        public int cojoinedFingers = 3;
    }

}
