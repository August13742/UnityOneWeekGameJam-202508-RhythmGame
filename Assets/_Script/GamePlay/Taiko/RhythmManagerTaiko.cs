using System.Collections.Generic;
using UnityEngine;
using Rhythm.Core;

namespace Rhythm.GamePlay.Taiko
{
    public class RhythmManagerTaiko : MonoBehaviour
    {
        [Header("External")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform taikoHitBar;
        [SerializeField] private OSU.Aimless.RhythmManagerOSUAimless osu;

        [Header("Prefabs")]
        [SerializeField] private TaikoNote prefabA;
        [SerializeField] private TaikoNote prefabB;

        [Header("Lane Layout")]
        [SerializeField] private float laneYOffsetFraction = 0f; // -0.25 is lower quarter, 0 no change
        [SerializeField] private float rightMargin = 80f;       // px from right edge (visibility)
        [SerializeField] private float pixelsPerSecond = 400f;

        [Header("Behaviour")]
        [SerializeField] private bool enableTaikoVisuals = true;
        [SerializeField] private float postHitGrace = 0.15f;

        // runtime
        private float xHit, laneY, xRightVisible;

        // FIFO container (explicit)
        private readonly Queue<TaikoNote> fifo = new();
        private readonly List<TaikoNote> active = new();

        // separate pools so A/B don't get mixed (currently only using one type though)
        private readonly Queue<TaikoNote> poolA = new();
        private readonly Queue<TaikoNote> poolB = new();

        [SerializeField] private RectTransform scrolllane;

        void Awake()
        {
            if (!osu)
                osu = OSU.Aimless.RhythmManagerOSUAimless.Instance;
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

        void OnEnable()
        {
            Rhythm.GamePlay.OSU.Aimless.RhythmManagerOSUAimless.OnGameStarted += OnGameStarted;
            Rhythm.GamePlay.OSU.Aimless.RhythmManagerOSUAimless.OnGameFinished += OnGameFinished;
        }
        void OnDisable()
        {
            Rhythm.GamePlay.OSU.Aimless.RhythmManagerOSUAimless.OnGameStarted -= OnGameStarted;
            Rhythm.GamePlay.OSU.Aimless.RhythmManagerOSUAimless.OnGameFinished -= OnGameFinished;

            // Prevent cross-scene stale refs
            HardClearAll();
        }
        private void OnGameStarted() => ResetTaikoState();
        private void OnGameFinished() => ResetTaikoState();
        private void Start()
        {
            JudgementSystem.Instance.OnJudgement += OnOSUJudgement;
            Rhythm.GamePlay.OSU.Aimless.RhythmManagerOSUAimless.OnGameStarted += OnGameStarted;
            Rhythm.GamePlay.OSU.Aimless.RhythmManagerOSUAimless.OnGameFinished += OnGameFinished;

        }
        private void OnDestroy()
        {
            var osu = OSU.Aimless.RhythmManagerOSUAimless.Instance;
            if (osu != null)
            {
                OSU.Aimless.RhythmManagerOSUAimless.OnGameStarted -= OnGameStarted;
                OSU.Aimless.RhythmManagerOSUAimless.OnGameFinished -= OnGameFinished;
            }
        }


        void Update()
        {
            if (!enableTaikoVisuals || osu == null || osu.CurrentState != OSU.Aimless.GameState.Playing)
                return;

            double songNow = osu.SongTimeNow();
            MirrorOSUSpawns();
            for (int i = active.Count - 1; i >= 0; --i)
            {
                var n = active[i];
                n.UpdatePosition(songNow);
                if (n.HasPassedHitZone(songNow, postHitGrace))
                {
                    ReturnToPool(n);
                    active.RemoveAt(i);
                    if (fifo.Count > 0 && ReferenceEquals(fifo.Peek(), n))
                        fifo.Dequeue();
                }
            }
        }


        void MirrorOSUSpawns()
        {
            var bm = osu.CurrentBeatmap;
            if (bm == null || bm.notes == null)
                return;

            while (NeedsTaikoSpawn(osu))
            {
                var data = bm.notes[GetTaikoSpawnIndex()];

                double lead = (xRightVisible - xHit) / pixelsPerSecond;
                double songNow = osu.SongTimeNow();
                if (songNow + 0.001 < (data.hitTime - lead))
                    break;

                SpawnTaiko(data);
                IncTaikoSpawnIndex();
            }
        }


        int taikoSpawnIndex = 0;
        int GetTaikoSpawnIndex() => taikoSpawnIndex;
        void IncTaikoSpawnIndex() => taikoSpawnIndex++;

        bool NeedsTaikoSpawn(OSU.Aimless.RhythmManagerOSUAimless o)
        {
            var bm = o.CurrentBeatmap;
            if (bm == null)
                return false;
            if (taikoSpawnIndex >= bm.notes.Count)
                return false;

            double songTime = o.SongTimeNow();
            var nd = bm.notes[taikoSpawnIndex];
            return (nd.hitTime - bm.approachTime) <= songTime;
        }

        void SpawnTaiko(BeatNoteData data)
        {
            var note = GetFromPool((NoteType)data.type);
            note.transform.SetParent(scrolllane, false);

            note.Initialise(
                data.hitTime,         
                (NoteType)data.type,
                pixelsPerSecond,
                xHit,
                laneY
            );
            active.Add(note);
            fifo.Enqueue(note);
        }

        void OnOSUJudgement(string result, int __)
        {
            if (fifo.Count == 0)
                return;

            // Only pop on successful hits
            if (result != "Miss")
            {
                var n = fifo.Dequeue();
                // Check if note is still active before triggering effect
                if (n != null && n.gameObject.activeInHierarchy)
                {
                    n.TriggerHitEffect();
                }
                ReturnToPool(n);
                active.Remove(n);
            }
        }
        public void ResetTaikoState()
        {
            // Drop destroyed and stop effects safely
            for (int i = active.Count - 1; i >= 0; --i)
            {
                var n = active[i];
                if (n == null)
                {
                    active.RemoveAt(i);
                    continue;
                }
                n.ResetNoteSafe(); // see extension below
            }
            active.Clear();
            fifo.Clear();
            taikoSpawnIndex = 0;

            // Purge pools of destroyed objects
            PurgePool(poolA);
            PurgePool(poolB);
        }

        private static void PurgePool(Queue<TaikoNote> q)
        {
            if (q.Count == 0)
                return;
            var tmp = new Queue<TaikoNote>(q.Count);
            while (q.Count > 0)
            {
                var n = q.Dequeue();
                if (n != null)
                    tmp.Enqueue(n);
            }
            while (tmp.Count > 0)
                q.Enqueue(tmp.Dequeue());
        }

        // For safety across scene unloads (called from OnDisable)
        private void HardClearAll()
        {
            active.Clear();
            fifo.Clear();
            poolA.Clear();
            poolB.Clear();
            taikoSpawnIndex = 0;
        }

        void ReturnToPool(TaikoNote n)
        {
            if (n == null)
                return; // destroyed already
            n.ResetNoteSafe();
            var q = (n.Type == 0) ? poolA : poolB;
            q.Enqueue(n);
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


        public void SetVisualsEnabled(bool enabled)
        {
            enableTaikoVisuals = enabled;
            if (!enabled)
            {
                foreach (var n in active)
                    n.ResetNote();
                active.Clear();
                fifo.Clear();
                // Reset spawn index when disabling
                taikoSpawnIndex = 0;
            }
        }

    }
}
