using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BeatmapImporter : EditorWindow
{
    private TextAsset jsonFile;
    private AudioClip audioClip;

    [MenuItem("Tools/Import Beatmap JSON")]
    static void Init()
    {
        GetWindow<BeatmapImporter>("Beatmap Importer");
    }

    private void OnGUI()
    {
        jsonFile = (TextAsset)EditorGUILayout.ObjectField("Beatmap JSON", jsonFile, typeof(TextAsset), false);
        audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false);

        if (GUILayout.Button("Import and Create Asset"))
        {
            if (jsonFile == null || audioClip == null)
            {
                Debug.LogError("Provide both JSON and AudioClip");
                return;
            }

            ImportJson(jsonFile, audioClip);
        }
    }

    private void ImportJson(TextAsset json, AudioClip clip)
    {
        // Parse the JSON into intermediate structure
        var parsed = JsonUtility.FromJson<BeatmapDataJsonRoot>(json.text);
        if (parsed == null || parsed.notes == null)
        {
            Debug.LogError("Failed to parse beatmap JSON. Check format.");
            return;
        }

        // Create a new BeatmapData asset
        BeatmapData asset = ScriptableObject.CreateInstance<BeatmapData>();
        asset.musicTrack = clip;
        asset.approachTime = parsed.approachTime;
        asset.notes = new List<BeatNoteData>();

        foreach (var n in parsed.notes)
        {
            asset.notes.Add(new BeatNoteData
            {
                hitTime = (float)n.hitTime,
                type = 0,
                spawnPointIndex = n.spawnPointIndex
            });
        }

        // Suggest default name from the JSON filename
        string defaultName = "NewBeatmap";
        string assetPath = AssetDatabase.GetAssetPath(json);
        if (!string.IsNullOrEmpty(assetPath))
        {
            defaultName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Beatmap Asset",
            defaultName,
            "asset",
            "Save beatmap as asset"
        );
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Debug.Log("Beatmap imported successfully.");
        }
    }

    // Classes for JSON parsing
    [System.Serializable]
    private class BeatmapDataJsonRoot
    {
        public float approachTime;
        public List<BeatNoteDataJson> notes;
    }

    [System.Serializable]
    private class BeatNoteDataJson
    {
        public float hitTime;
        public int type;
        public int spawnPointIndex;
    }
}
