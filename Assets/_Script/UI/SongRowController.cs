using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rhythm.GamePlay.OSU.Aimless;
using Rhythm.Core;

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


        public void Init(string songKey, Dictionary<Difficulty, BeatmapData> diffs, SongRecordsDB recordsDB, bool startCollapsed = true)
        {
            SongKey = songKey;
            this.diffs = diffs;
            this.recordsDB = recordsDB;

            if (view == null)
            {
                Debug.LogError("[SongRowController] view not assigned.");
                return;
            }


            LinkToggles();

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
        private void ExpandRow(bool autoPickDifficulty = true)
        {
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

                    GameEvents.Instance?.PlayMusicScheduled(bm.musicTrack, AudioSettings.dspTime);
                    PlayingSongSample?.Invoke();

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

        private void LinkToggles()
        {
            var manager = RhythmManagerOSUAimless.Instance;
            if (manager == null)
                return;

            if (view.autoPlayToggle)
            {
                view.autoPlayToggle.isOn = manager.AutoPlay;
                view.autoPlayToggle.onValueChanged.RemoveAllListeners();
                view.autoPlayToggle.onValueChanged.AddListener(isOn => manager.AutoPlay = isOn);
            }

            if (view.indicatorToggle)
            {
                view.indicatorToggle.isOn = manager.showIndicator;
                view.indicatorToggle.onValueChanged.RemoveAllListeners();
                view.indicatorToggle.onValueChanged.AddListener(isOn => manager.showIndicator = isOn);
            }

            if (view.approachRingToggle)
            {
                view.approachRingToggle.isOn = manager.showApproachRing;
                view.approachRingToggle.onValueChanged.RemoveAllListeners();
                view.approachRingToggle.onValueChanged.AddListener(isOn => manager.showApproachRing = isOn);
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
