using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "NewMusicResource", menuName = "Audio/Music Resource")]
public class MusicResource : ScriptableObject
{
    public AudioClip clip;
}
