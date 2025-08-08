#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class GenerateBeatmapWindow : EditorWindow
{
    AudioClip clip;
    enum Difficulty
    {
        EASY, NORMAL, HARD
    }
    Difficulty level = Difficulty.NORMAL;
    const string PY = "python";                // or absolute path
    string projectRoot;
    string scriptPath;

    // Add a toggle for saving the JSON file
    bool keepJsonFile = false;

    void OnEnable()
    {
        projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        scriptPath = Path.Combine(projectRoot, "Tools", "generate_beatmap.py");
    }

    [MenuItem("Tools/Beatmap/Auto-Generate")]
    static void Open()
    {
        GetWindow<GenerateBeatmapWindow>("Auto Beatmap");
    }

    void OnGUI()
    {
        clip = (AudioClip)EditorGUILayout.ObjectField("AudioClip", clip, typeof(AudioClip), false);
        level = (Difficulty)EditorGUILayout.EnumPopup("Difficulty", level);

        // Draw the toggle for saving JSON
        keepJsonFile = EditorGUILayout.Toggle("Save JSON to Project", keepJsonFile);

        if (GUILayout.Button("Generate ▶"))
        {
            if (!clip)
            {
                UnityEngine.Debug.LogError("Assign AudioClip");
                return;
            }

            // Debug script path existence
            if (!File.Exists(scriptPath))
            {
                UnityEngine.Debug.LogError($"Python script not found at: {scriptPath}");
                return;
            }

            string wav = SaveTempWav(clip);
            string jsonPath = Path.ChangeExtension(wav, $".{level}.json");

            UnityEngine.Debug.Log($"Temp WAV file: {wav}");
            UnityEngine.Debug.Log($"Output JSON path: {jsonPath}");
            UnityEngine.Debug.Log($"Python script: {scriptPath}");
            UnityEngine.Debug.Log($"Project root: {projectRoot}");

            string arguments = $"\"{scriptPath}\" \"{wav}\" \"{jsonPath}\" --difficulty {level} --diag";

            UnityEngine.Debug.Log($"Running command: {PY} {arguments}");

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = PY,
                    Arguments = arguments,
                    WorkingDirectory = projectRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            string output = "";
            string error = "";

            p.OutputDataReceived += (sender, args) => {
                if (args.Data != null)
                    output += args.Data + "\n";
            };
            p.ErrorDataReceived += (sender, args) => {
                if (args.Data != null)
                    error += args.Data + "\n";
            };

            try
            {
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();

                UnityEngine.Debug.Log($"Python process exit code: {p.ExitCode}");
                if (p.ExitCode != 0)
                {
                    UnityEngine.Debug.LogError($"Python non-zero exit: {p.ExitCode}\n{error}");
                    return;
                }

                if (!string.IsNullOrEmpty(output))
                    UnityEngine.Debug.Log($"Python output: {output}");

                if (!string.IsNullOrEmpty(error))
                    UnityEngine.Debug.LogError($"Python error: {error}");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to start or run Python process: {ex.Message}");
                return;
            }

            if (!File.Exists(jsonPath))
            {
                UnityEngine.Debug.LogError("Python failed to generate JSON output file");
                return;
            }

            // Only move and import the JSON if the toggle is enabled
            if (keepJsonFile)
            {
                var jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(MoveIntoProject(jsonPath));
                Import(jsonAsset, clip);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Done", "Beatmap generated (JSON saved to project).", "OK");
            }
            else
            {
                // Create the ScriptableObject directly from the temp file
                ImportFromPath(jsonPath, clip);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Done", "Beatmap generated", "OK");

                // Delete the temporary JSON file
                try
                {
                    File.Delete(jsonPath);
                    UnityEngine.Debug.Log($"Deleted temporary JSON file: {jsonPath}");
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to delete temporary JSON file: {ex.Message}");
                }
            }

            // Always delete the temporary WAV file
            try
            {
                if (File.Exists(wav))
                {
                    File.Delete(wav);
                    UnityEngine.Debug.Log($"Deleted temporary WAV file: {wav}");
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to delete temporary WAV file: {ex.Message}");
            }
        }
    }

    static string SaveTempWav(AudioClip clip)
    {
        var path = Path.Combine(Path.GetTempPath(), clip.name + ".wav");
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        try
        {
            using var f = new FileStream(path, FileMode.Create);
            // Write WAV header
            WriteWavHeader(f, clip);
            // Write sample data
            WriteSampleData(f, samples);
            UnityEngine.Debug.Log($"WAV file saved successfully at: {path}");
            return path;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to save WAV file: {ex.Message}");
            return path; // Return path anyway to allow process to continue for debugging
        }
    }

    static void WriteWavHeader(FileStream stream, AudioClip clip)
    {
        int headerSize = 44;
        int fileSize = headerSize + (clip.samples * clip.channels * 2); // 2 bytes per sample (16 bit)

        // RIFF header
        WriteString(stream, "RIFF");
        WriteInt(stream, fileSize - 8); // File size - 8 bytes
        WriteString(stream, "WAVE");

        // Format chunk
        WriteString(stream, "fmt ");
        WriteInt(stream, 16); // Chunk size
        WriteShort(stream, 1); // Audio format (1 = PCM)
        WriteShort(stream, (short)clip.channels); // Channels
        WriteInt(stream, clip.frequency); // Sample rate
        WriteInt(stream, clip.frequency * clip.channels * 2); // Byte rate
        WriteShort(stream, (short)(clip.channels * 2)); // Block align
        WriteShort(stream, 16); // Bits per sample

        // Data chunk
        WriteString(stream, "data");
        WriteInt(stream, clip.samples * clip.channels * 2); // Chunk size
    }

    static void WriteSampleData(FileStream stream, float[] samples)
    {
        for (int i = 0; i < samples.Length; i++)
        {
            // Convert float to 16-bit PCM
            short value = (short)(samples[i] * 32767);
            byte[] bytes = System.BitConverter.GetBytes(value);
            stream.Write(bytes, 0, 2);
        }
    }

    static void WriteString(FileStream stream, string value)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    static void WriteInt(FileStream stream, int value)
    {
        byte[] bytes = System.BitConverter.GetBytes(value);
        stream.Write(bytes, 0, 4);
    }

    static void WriteShort(FileStream stream, short value)
    {
        byte[] bytes = System.BitConverter.GetBytes(value);
        stream.Write(bytes, 0, 2);
    }

    static string MoveIntoProject(string jsonPath)
    {
        string target = "Assets/Beatmaps/" + Path.GetFileName(jsonPath);
        try
        {
            Directory.CreateDirectory("Assets/Beatmaps");
            File.Copy(jsonPath, target, true);
            AssetDatabase.ImportAsset(target);
            UnityEngine.Debug.Log($"JSON file moved to project at: {target}");
            return target;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to move JSON file to project: {ex.Message}");
            return target; // Return target path anyway to allow process to continue for debugging
        }
    }

    // New method to import from a file path directly, not a TextAsset
    void ImportFromPath(string jsonPath, AudioClip clip)
    {
        try
        {
            if (!File.Exists(jsonPath))
            {
                UnityEngine.Debug.LogError("JSON file not found at path");
                return;
            }

            string jsonContent = File.ReadAllText(jsonPath);
            UnityEngine.Debug.Log($"Importing JSON content: {jsonContent}");
            var parsed = JsonUtility.FromJson<BeatmapDataJsonRoot>(jsonContent);

            if (parsed == null)
            {
                UnityEngine.Debug.LogError("Failed to parse JSON data");
                return;
            }

            var asset = ScriptableObject.CreateInstance<BeatmapData>();
            asset.musicTrack = clip;
            asset.approachTime = parsed.approachTime;
            asset.notes = new List<BeatNoteData>();

            foreach (var n in parsed.notes)
                asset.notes.Add(new BeatNoteData
                {
                    hitTime = n.hitTime,
                    type = n.type,
                    spawnPointIndex = n.spawnPointIndex
                });

            string assetPath = $"Assets/Beatmaps/{clip.name}_{level}.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"Beatmap asset created at: {assetPath}");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to import beatmap data: {ex.Message}");
        }
    }

    void Import(TextAsset json, AudioClip clip)
    {
        try
        {
            if (json == null)
            {
                UnityEngine.Debug.LogError("JSON asset is null");
                return;
            }

            UnityEngine.Debug.Log($"Importing JSON content: {json.text}");
            var parsed = JsonUtility.FromJson<BeatmapDataJsonRoot>(json.text);

            if (parsed == null)
            {
                UnityEngine.Debug.LogError("Failed to parse JSON data");
                return;
            }

            var asset = ScriptableObject.CreateInstance<BeatmapData>();
            asset.musicTrack = clip;
            asset.approachTime = parsed.approachTime;
            asset.notes = new List<BeatNoteData>();

            foreach (var n in parsed.notes)
                asset.notes.Add(new BeatNoteData
                {
                    hitTime = n.hitTime,
                    type = n.type,
                    spawnPointIndex = n.spawnPointIndex
                });

            string assetPath = $"Assets/Beatmaps/{clip.name}_{level}.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"Beatmap asset created at: {assetPath}");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to import beatmap data: {ex.Message}");
        }
    }
}
#endif
