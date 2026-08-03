using UnityEngine;


[CreateAssetMenu(fileName = "NewDialogue", menuName = "NPC Dialogue")]
public class Dialogue : ScriptableObject
{
    public string npcName;
    public Sprite npcPortrait;
    public string[] dialogueLines;
    public bool[] autoProgressLines;
    public float autoProgressDelay = 1.5f;
    public float typingSpeed = 0.5f;
    public AudioClip voiceSound;
    public float voicePitch = 1f;
}
