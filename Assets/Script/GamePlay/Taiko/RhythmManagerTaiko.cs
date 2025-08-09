using System.Collections.Generic;
using UnityEngine;

namespace Rhythm.GamePlay.Taiko
{
    public class RhythmManagerTaiko : MonoBehaviour
    {
        [Header("External")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform taikoHitBar;   // assign in inspector
        [SerializeField] private Rhythm.GamePlay.OSU.Aimless.RhythmManagerOSUAimless osu; // link if not using Instance

        [Header("Prefabs")]
        [SerializeField] private TaikoNote prefabA;
        [SerializeField] private TaikoNote prefabB;

        [Header("Lane Layout")]
        [SerializeField] private float laneYOffsetFraction = -0.25f; // -0.25 => lower quarter
        [SerializeField] private float rightMargin = 80f;            // px from right edge (visibility)
        [SerializeField] private float pixelsPerSecond = 400f;

        [Header("Behaviour")]
        [SerializeField] private bool enableTaikoVisuals = true;
        [SerializeField] private float postHitGrace = 0.15f;

        // runtime
        private float xHit, laneY, xRightVisible;

        // FIFO container (explicit)
        private readonly Queue<TaikoNote> fifo = new();
        private readonly List<TaikoNote> active = new();

        // separate pools so A/B don’t get mixed
        private readonly Queue<TaikoNote> poolA = new();
        private readonly Queue<TaikoNote> poolB = new();

        [SerializeField] private RectTransform scrolllane; // assign in inspector

        void Awake()
        {
            if (!osu)
                osu = Rhythm.GamePlay.OSU.Aimless.RhythmManagerOSUAimless.Instance;
            CacheLaneGeometry();
        }

        void OnRectTransformDimensionsChange() => CacheLaneGeometry();

        void CacheLaneGeometry()
        {
            var cr = canvas.GetComponent<RectTransform>().rect;
            // anchored space centered at (0,0)
            xHit = taikoHitBar.anchoredPosition.x;
            laneY = cr.height * laneYOffsetFraction;
            xRightVisible = (cr.width * 0.5f) - rightMargin;
        }

        //void OnEnable()
        //{
        //    // subscribe once to OSU Judgements if you want to pop FIFO on any Judgement
        //    Rhythm.GamePlay.JudgementSystem.Instance.OnJudgement += OnOSUJudgement;
        //}

        void OnDisable()
        {
            if (Rhythm.GamePlay.JudgementSystem.Instance != null)
                Rhythm.GamePlay.JudgementSystem.Instance.OnJudgement -= OnOSUJudgement;
        }
        private void Start()
        {
            Rhythm.GamePlay.JudgementSystem.Instance.OnJudgement += OnOSUJudgement;
        }
        void Update()
        {
            if (!enableTaikoVisuals || osu == null || osu.CurrentState != Rhythm.GamePlay.OSU.Aimless.GameState.Playing)
                return;

            double now = AudioSettings.dspTime;

            // spawn to mirror OSU (read from its beatmap/indices)
            MirrorOSUSpawns(now);

            // move + cleanup
            for (int i = active.Count - 1; i >= 0; --i)
            {
                var n = active[i];
                n.UpdatePosition(now);
                if (n.HasPassedHitZone(now, postHitGrace))
                {
                    ReturnToPool(n);
                    active.RemoveAt(i);
                    // If it’s still in fifo (e.g., no Judgement happened), drop it:
                    if (fifo.Count > 0 && ReferenceEquals(fifo.Peek(), n))
                        fifo.Dequeue();
                }
            }
        }

        void MirrorOSUSpawns(double _)
        {
            var bm = osu.CurrentBeatmap;
            if (bm == null || bm.notes == null)
                return;

            // track local spawn index separately from OSU if needed
            // simplest approach: use OSU’s CurrentSpawnIndex as the same point
            // and spawn any note whose spawn condition is met.
            while (NeedsTaikoSpawn(osu))
            {
                var data = bm.notes[GetTaikoSpawnIndex()];
                double absHit = osu.CurrentDSPSongStartTime + data.hitTime + osu.AudioOffset;

                // Optional: spawn exactly when note becomes visible at right edge
                double tSpawn = absHit - (xRightVisible - xHit) / pixelsPerSecond;
                if (AudioSettings.dspTime + 0.001 < tSpawn)
                    break; // not yet time to show

                SpawnTaiko(data, absHit);
                IncTaikoSpawnIndex();
            }
        }

        // --- implement these however you track Taiko’s own spawn index ---
        int taikoSpawnIndex = 0;
        int GetTaikoSpawnIndex() => taikoSpawnIndex;
        void IncTaikoSpawnIndex() => taikoSpawnIndex++;

        bool NeedsTaikoSpawn(Rhythm.GamePlay.OSU.Aimless.RhythmManagerOSUAimless o)
        {
            var bm = o.CurrentBeatmap;
            if (bm == null)
                return false;
            if (taikoSpawnIndex >= bm.notes.Count)
                return false;

            // same gate OSU uses: note becomes “active” when approach starts
            double songTime = AudioSettings.dspTime - o.CurrentDSPSongStartTime;
            var nd = bm.notes[taikoSpawnIndex];
            return (nd.hitTime - bm.approachTime) <= songTime;
        }

        void SpawnTaiko(BeatNoteData data, double absHit)
        {
            var note = GetFromPool((NoteType)data.type);
            note.transform.SetParent(scrolllane, false);

            note.Initialise(
                absHit,
                (NoteType)data.type,
                pixelsPerSecond,
                xHit,
                laneY
            );

            active.Add(note);
            fifo.Enqueue(note);

#if UNITY_EDITOR
            // sanity: ensure append-order is FIFO even when hitTimes tie
            if (active.Count >= 2)
            {
                var a = active[^2].HitTime;
                var b = active[^1].HitTime;
                if (b < a - 1e-6)
                    Debug.LogWarning("Taiko FIFO: spawn order vs HitTime mismatch");
            }
#endif
        }

        void OnOSUJudgement(string result, int __)
        {
            if (fifo.Count == 0)
                return;

            // Only pop on successful hits
            if (result == "Perfect" || result == "Good")
            {
                var n = fifo.Dequeue();
                n.TriggerHitEffect();
                ReturnToPool(n);
                active.Remove(n);
            }
        }

        TaikoNote GetFromPool(NoteType t)
        {
            var q = (t == 0) ? poolA : poolB;
            if (q.Count > 0)
            {
                var n = q.Dequeue();
                n.gameObject.SetActive(true);
                return n;
            }
            var prefab = (t == 0) ? prefabA : prefabB;
            return Instantiate(prefab, transform);
        }

        void ReturnToPool(TaikoNote n)
        {
            n.ResetNote();
            var q = (n.Type == 0) ? poolA : poolB;
            q.Enqueue(n);
        }

        // optional public toggles
        public void SetVisualsEnabled(bool enabled)
        {
            enableTaikoVisuals = enabled;
            if (!enabled)
            {
                foreach (var n in active)
                    n.ResetNote();
                active.Clear();
                fifo.Clear();
            }
        }
    }
}
