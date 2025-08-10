using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rhythm.GamePlay.OSU.Aimless;
using Rhythm.Core;
using UnityEngine.Audio;

namespace Rhythm.UI
{
    public class SongRowController : MonoBehaviour
    {
        [Header("Refs")]
        public SongRowView view;

        [Header("UI Feedback")]
        public Color selectedColor = Color.yellow;
        public Color defaultColor = Color.white;


        public string SongKey
        {
            get; private set;
        }
        private Dictionary<Difficulty, BeatmapData> diffs;
        private SongRecordsDB recordsDB;
        public Difficulty? SelectedDifficulty
        {
            get; private set;
        }
        public BeatmapData SelectedBeatmap
        {
            get; private set;
        }
        public System.Action<SongRowController> OnExpandRequested;
        public System.Action<BeatmapData, string, Difficulty> OnStartRequested;

        public System.Action PlayingSongSample;
        private void WireDifficulty(Button btn, Difficulty d)
        {
            if (!btn)
                return;

            if (diffs != null && diffs.TryGetValue(d, out var bm))
            {
                btn.gameObject.SetActive(true);
                SetButtonTextColor(btn, d);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {

                    OnExpandRequested?.Invoke(this);
                    SelectedDifficulty = d;
                    SelectedBeatmap = bm;
                    view.SetExpanded(true);
                    RefreshStats();
                    UpdateDifficultyButtonColors();
                });
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }


        public void Init(string songKey, Dictionary<Difficulty, BeatmapData> diffs, SongRecordsDB recordsDB, JukeboxUIController jukeboxController, bool startCollapsed = true) // MODIFIED
        {
            SongKey = songKey;
            this.diffs = diffs;
            this.recordsDB = recordsDB;

            if (view == null)
            {
                Debug.LogError("[SongRowController] view not assigned.");
                return;
            }

            WireUpToggles(jukeboxController);

            view.songName.text = songKey.Replace("_", " ");
            WireDifficulty(view.easyBtn, Difficulty.Easy);
            WireDifficulty(view.normalBtn, Difficulty.Normal);
            WireDifficulty(view.hardBtn, Difficulty.Hard);
            WireDifficulty(view.insaneBtn, Difficulty.Insane);

            if (startCollapsed)
            {
                view.SetExpanded(false);
            }
            else
            {
                // If starting expanded, ensure colors are correct
                UpdateDifficultyButtonColors();
            }

            if (view.rowSelectBtn)
            {
                view.rowSelectBtn.onClick.RemoveAllListeners();
                view.rowSelectBtn.onClick.AddListener(() => ExpandRow(autoPickDifficulty: true));
            }

            if (view.startButton)
            {
                view.startButton.onClick.RemoveAllListeners();
                view.startButton.onClick.AddListener(() =>
                {
                    if (SelectedDifficulty.HasValue && SelectedBeatmap != null)
                        OnStartRequested?.Invoke(SelectedBeatmap, SongKey, SelectedDifficulty.Value);
                });
            }
        }
        private void WireUpToggles(JukeboxUIController jukebox)
        {
            if (jukebox == null)
                return;

            if (view.autoPlayToggle)
            {
                view.autoPlayToggle.onValueChanged.RemoveAllListeners();
                // seed UI from controller so prefab/defaults don’t fight
                view.autoPlayToggle.SetIsOnWithoutNotify(jukebox.AutoPlayState);
                view.autoPlayToggle.onValueChanged.AddListener(jukebox.SetAutoPlay);
                // push current UI state into controller once
                jukebox.SetAutoPlay(view.autoPlayToggle.isOn);
            }

            if (view.indicatorToggle)
            {
                view.indicatorToggle.onValueChanged.RemoveAllListeners();
                view.indicatorToggle.SetIsOnWithoutNotify(jukebox.ShowIndicatorState);  
                view.indicatorToggle.onValueChanged.AddListener(jukebox.SetShowIndicator);
                jukebox.SetShowIndicator(view.indicatorToggle.isOn);
            }

            if (view.approachRingToggle)
            {
                view.approachRingToggle.onValueChanged.RemoveAllListeners();
                view.approachRingToggle.SetIsOnWithoutNotify(jukebox.ShowApproachRingState);
                view.approachRingToggle.onValueChanged.AddListener(jukebox.SetShowApproachRing);
                jukebox.SetShowApproachRing(view.approachRingToggle.isOn);
            }
            if (view.perfectSFXToggle)
            {
                view.perfectSFXToggle.onValueChanged.RemoveAllListeners();
                view.perfectSFXToggle.SetIsOnWithoutNotify(jukebox.ShowPerfectSFXState);
                view.perfectSFXToggle.onValueChanged.AddListener(jukebox.SetPerfectSFXState);
                jukebox.SetShowIndicator(view.perfectSFXToggle.isOn);

            }
        }

        private void ExpandRow(bool autoPickDifficulty = true)
        {
            // Get music track from any available difficulty
            AudioClip musicTrack = null;
            foreach (var kvp in diffs)
            {
                if (kvp.Value?.musicTrack != null)
                {
                    musicTrack = kvp.Value.musicTrack;
                    break;
                }
            }

            // Play music immediately when row is expanded, regardless of difficulty selection
            if (musicTrack != null)
            {
                GameEvents.Instance?.PlayMusicScheduled(musicTrack, AudioSettings.dspTime);
                PlayingSongSample?.Invoke();
            }

            OnExpandRequested?.Invoke(this);
            view.SetExpanded(true);

            if (!autoPickDifficulty)
                return;

            if (!SelectedDifficulty.HasValue)
            {
                var pref = GetPreferredAvailableDifficulty();
                if (pref.HasValue && diffs.TryGetValue(pref.Value, out var bm))
                {
                    SelectedDifficulty = pref.Value;
                    SelectedBeatmap = bm;
                    UpdateDifficultyButtonColors();
                }
            }

            if (SelectedDifficulty.HasValue)
                RefreshStats();
        }

        private void UpdateDifficultyButtonColors()
        {
            SetButtonTextColor(view.easyBtn, Difficulty.Easy);
            SetButtonTextColor(view.normalBtn, Difficulty.Normal);
            SetButtonTextColor(view.hardBtn, Difficulty.Hard);
            SetButtonTextColor(view.insaneBtn, Difficulty.Insane);
        }

        private void SetButtonTextColor(Button btn, Difficulty d)
        {
            if (!btn)
                return;
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt)
            {
                // Is this button's difficulty the currently selected one?
                txt.color = (SelectedDifficulty.HasValue && SelectedDifficulty.Value == d) ? selectedColor : defaultColor;
            }
        }


        private Difficulty? GetPreferredAvailableDifficulty()
        {
            Difficulty[] order = { Difficulty.Normal, Difficulty.Easy, Difficulty.Hard, Difficulty.Insane };
            foreach (var d in order)
                if (diffs.ContainsKey(d))
                    return d;
            return null;
        }

        public void RefreshStats()
        {
            var rec = recordsDB ? recordsDB.GetRecord(SongKey, SelectedDifficulty ?? Difficulty.Normal) : null;
            if (view.bestScore)
                view.bestScore.text = rec != null ? rec.highScore.ToString() : "-";
            if (view.accuracy)
                view.accuracy.text = rec != null ? $"{rec.bestAccuracy:P1}" : "-";
            if (view.maxCombo)
                view.maxCombo.text = rec != null ? rec.maxCombo.ToString() : "-";
            if (view.note)
                view.note.text = rec != null && rec.totalNotes > 0 ? $"{rec.notesHit}/{rec.totalNotes}" : "-";
            if (view.perfect)
                view.perfect.text = rec != null ? rec.bestPerfect.ToString() : "-";
            if (view.good)
                view.good.text = rec != null ? rec.bestGood.ToString() : "-";
            if (view.miss)
                view.miss.text = rec != null ? rec.bestMiss.ToString() : "-";
        }

        public void Collapse() => view.SetExpanded(false);

    }
}
