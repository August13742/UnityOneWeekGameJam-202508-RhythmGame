using System.Collections.Generic;
using UnityEngine;

namespace Rhythm.GamePlay.OSU.Aimless
{

    /// <summary>
    /// Singleton
    /// </summary>
    public class RhythmManagerOSUAimless : MonoBehaviour
    {

        public static RhythmManagerOSUAimless Instance
        {
            get; private set;
        }


        public float AudioStartDelay = 3f;
        [SerializeField] private BeatmapData beatmap;
        [SerializeField] private OSUBeatNote notePrefab;
        [SerializeField] private RectTransform noteParentCanvas;
        [SerializeField] private float audioOffset = 0.0f;
        [SerializeField] private Vector2 spawnRangeOffset = new(50, 50);

        [SerializeField] private Camera worldCamera = null;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private GameObject enemyPrefab;

        [SerializeField] private AimIndicator indicatorPrefab;

        private Canvas canvasComponent;
        private Vector2 spawnRange = new(800, 440);
        private double dspSongStartTime;
        private int spawnIndex = 0;


        float minX;
        float maxX;
        float minY;
        float maxY;
        // --- Pooling fields ---
        [Header("Pooling")]
        [SerializeField] private int poolSize = 8;
        private readonly Queue<OSUBeatNote> notePool = new();
        private readonly Queue<GameObject> enemyPool = new();


        private readonly List<OSUBeatNote> activeNotes = new();
        private AimIndicator indicator;
        private OSUBeatNote currentIndicatorTarget = null;

        private void Awake()
        {

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;


            spawnRange = new Vector2(
                noteParentCanvas.rect.width - spawnRangeOffset.x,
                noteParentCanvas.rect.height - spawnRangeOffset.y
            );


            // Pre-instantiate note pool objects and hide them
            for (int i = 0; i < poolSize; i++)
            {
                var note = Instantiate(notePrefab, noteParentCanvas);
                note.gameObject.SetActive(false);
                notePool.Enqueue(note);
            }

            // Pre-instantiate enemy pool objects and hide them
            for (int i = 0; i < poolSize; i++)
            {
                var enemy = Instantiate(enemyPrefab);
                enemy.SetActive(false);
                enemyPool.Enqueue(enemy);
            }
        }

        private void Start()
        {

            canvasComponent = noteParentCanvas.GetComponentInParent<Canvas>();
            // Start audio after delay
            AudioSource audioSource = GetComponent<AudioSource>();
            double startTime = AudioSettings.dspTime + AudioStartDelay;
            audioSource.clip = beatmap.musicTrack;
            audioSource.PlayScheduled(startTime);
            dspSongStartTime = startTime;

            // Synchronise countdown
            var countdownText = FindFirstObjectByType<Rhythm.UI.CountDownText>();
            if (countdownText != null)
                countdownText.SetScheduledStartTime(dspSongStartTime);

            minX = -spawnRange.x / 2f;
            maxX = spawnRange.x / 2f;
            minY = -spawnRange.y / 2f;
            maxY = spawnRange.y / 2f;

            // If no spawn points, create virtual ones in front of the camera
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                int virtualCount = 10;
                float distMin = 10f;   // near plane
                float distMax = 30f;   // far plane
                float halfWidth = 10f;   // +-X spread
                float heightOffset = 6f;    // +-Y spread

                enemySpawnPoints = new Transform[virtualCount];

                for (int i = 0; i < virtualCount; ++i)
                {
                    Vector3 localPos = new(
                        Random.Range(-halfWidth, halfWidth),   // X
                        Random.Range(0, heightOffset),  // Y
                        Random.Range(distMin, distMax));    // Z (forward)

                    Vector3 worldPos = worldCamera.transform.TransformPoint(localPos);

                    var go = new GameObject($"VirtualSpawnPoint_{i}");
                    go.transform.position = worldPos;

                    // face the camera
                    Vector3 dirToCam = (worldCamera.transform.position - worldPos).normalized;
                    go.transform.rotation = Quaternion.LookRotation(dirToCam, Vector3.up);

                    go.transform.SetParent(transform);          // housekeeping
                    enemySpawnPoints[i] = go.transform;
                }
            }

            indicator = Instantiate(indicatorPrefab, noteParentCanvas);
            indicator.gameObject.SetActive(false);
        }

        private void Update()
        {
            double dspNow = AudioSettings.dspTime;
            double songTime = dspNow - dspSongStartTime;

            // Spawn all notes whose hitTime is within the lead-in window
            while (spawnIndex < beatmap.notes.Count &&
                   beatmap.notes[spawnIndex].hitTime - beatmap.approachTime <= songTime)
            {
                SpawnNote(beatmap.notes[spawnIndex]);
                spawnIndex++;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                HandleInput();
            }
            activeNotes.RemoveAll(note => note.HasProcessed);

            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            // 1. Find the next note to target (the one with the earliest HitTime)
            OSUBeatNote nextTarget = null;
            double earliestHitTime = double.MaxValue;

            foreach (var note in activeNotes)
            {
                // The list is cleaned of processed notes, so the first one we find in a sorted
                // list would be the one. This loop finds the one with the minimum HitTime.
                if (note.HitTime < earliestHitTime)
                {
                    earliestHitTime = note.HitTime;
                    nextTarget = note;
                }
            }

            // 2. Check if the target has changed
            if (nextTarget != currentIndicatorTarget)
            {
                if (nextTarget != null)
                {
                    // --- We have a new target ---
                    // Determine the start time for our duration calculation.
                    // If it's the first note, we start from the beginning of the song.
                    // Otherwise, we start from the previous note's hit time.
                    double prevNoteTime = (currentIndicatorTarget == null)
                                        ? dspSongStartTime
                                        : currentIndicatorTarget.HitTime;

                    float duration = (float)(nextTarget.HitTime - prevNoteTime);

                    // Set a minimum duration to prevent instant-teleporting on very fast notes.
                    if (duration < 0.1f)
                        duration = 0.1f;

                    // Command the indicator to move.
                    indicator.Initialise(nextTarget.GetComponent<RectTransform>(), duration);
                }
                else if (currentIndicatorTarget != null)
                {
                    indicator.gameObject.SetActive(false);
                }

                // 3. Update the state to reflect the new target
                currentIndicatorTarget = nextTarget;
            }
        }


        private void HandleInput()
        {
            OSUBeatNote noteToHit = null;
            double earliestHitTime = double.MaxValue;

            // Find the oldest note that hasn't been processed yet.
            foreach (var note in activeNotes)
            {
                if (!note.HasProcessed && note.HitTime < earliestHitTime)
                {
                    earliestHitTime = note.HitTime;
                    noteToHit = note;
                }
            }

            if (noteToHit != null)
            {
                noteToHit.ProcessHit();
            }
            else
            {
                //penalty optional
            }
        }

        private void SpawnNote(BeatNoteData data)
        {
            // pick enemy spawn point
            int index = (data.spawnPointIndex >= 0 && data.spawnPointIndex < enemySpawnPoints.Length)
                        ? data.spawnPointIndex
                        : Random.Range(0, enemySpawnPoints.Length);

            Transform spawnPoint = enemySpawnPoints[index];

            // 1. Spawn 3D enemy from pool
            GameObject enemy = GetEnemyFromPool();
            enemy.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            enemy.SetActive(true);

            // Init enemy with timing for sync
            if (enemy.TryGetComponent<EnemyRhythmUnit>(out var rhythmUnit))
            {
                rhythmUnit.SetHitTime(dspSongStartTime + data.hitTime + audioOffset);
                rhythmUnit.SetReturnToPoolCallback(ReturnEnemyToPool);
            }

            // 2. Spawn UI marker
            Vector3 screenPos = worldCamera.WorldToScreenPoint(spawnPoint.position);


            Camera cam = canvasComponent.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                noteParentCanvas, screenPos, cam, out Vector2 canvasPos);

            OSUBeatNote note = GetNoteFromPool();
            var rect = note.GetComponent<RectTransform>();
            rect.SetParent(noteParentCanvas, false);
            rect.anchoredPosition = canvasPos;

            note.Initialise(
                dspSongStartTime + data.hitTime + audioOffset,
                beatmap.approachTime,
                delta => JudgementSystem.Instance.RegisterHit(delta),
                () => JudgementSystem.Instance.RegisterMiss(),
                ReturnNoteToPool
            );

            activeNotes.Add(note);
        }

        private OSUBeatNote GetNoteFromPool()
        {
            if (notePool.Count > 0)
            {
                var note = notePool.Dequeue();
                note.gameObject.SetActive(true);
                return note;
            }
            // If pool is empty, instantiate as fallback (should be rare)
            var newNote = Instantiate(notePrefab, noteParentCanvas);
            return newNote;
        }

        public void ReturnNoteToPool(OSUBeatNote note)
        {
            note.gameObject.SetActive(false);
            notePool.Enqueue(note);
        }

        private GameObject GetEnemyFromPool()
        {
            if (enemyPool.Count > 0)
            {
                var enemy = enemyPool.Dequeue();
                enemy.SetActive(true);
                return enemy;
            }
            // If pool is empty, instantiate as fallback (should be rare)
            var newEnemy = Instantiate(enemyPrefab);
            return newEnemy;
        }

        public void ReturnEnemyToPool(GameObject enemy)
        {
            enemy.SetActive(false);
            enemyPool.Enqueue(enemy);
        }
    }
}

