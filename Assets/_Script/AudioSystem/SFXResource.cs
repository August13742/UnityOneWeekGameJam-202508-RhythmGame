using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "NewSFXResource", menuName = "Audio/SFX Resource")]
public class SFXResource : ScriptableObject
{
    public string eventName;
    public AudioClip clip;
    public bool loop;
    [Range(0f, 1f)] public float volumeLinear = 1f;
    [Range(0.1f, 3f)] public float pitchScale = 1f;
}
