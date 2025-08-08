using System;
using System.Collections.Generic;
using UnityEngine;
using Rhythm.UI;

namespace Rhythm.GamePlay.OSU.Aimless
{

    /// <summary>
    /// Singleton
    /// </summary>
    public class RhythmManagerOSUAimless : MonoBehaviour, INoteVisualSettings
    {

        public static RhythmManagerOSUAimless Instance
        {
            get; private set;
        }

        public bool AutoPlay = false;

        public float AudioStartDelay = 3f;
        [Header("Beatmap Settings")]
        [SerializeField] private BeatmapData beatmap;

        [SerializeField] private OSUBeatNote notePrefab;
        [SerializeField] private RectTransform noteParentCanvas;
        [SerializeField] private float audioOffset = 0.0f;
        
        [Header("Spawn Settings")]
        [SerializeField] private bool useCanvasSize = true;
        [SerializeField] private Vector2 spawnRangeOffset = new(50, 50);
        [SerializeField] private Vector2 customSpawnRange = new(800, 440);
        [Tooltip("Distance range for spawning enemies")]
        [SerializeField] private Vector2 distanceRange = new(10f, 50f);
        [SerializeField] private int virtualSpawnPointCount = 16;

        public SFXResource DryShot;
        [SerializeField] private Camera worldCamera = null;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject notificationTextPrefab;

        [SerializeField] private AimIndicator indicatorPrefab;
        [Header("Gameplay Options")]
        public bool showIndicator = true;
        public bool showApproachRing = true;

        public bool ShowApproachRing => showApproachRing;
        public bool ShowIndicator => showIndicator;
        public float indicatorLeadInMultiplier = 1.0f;

        private Canvas canvasComponent;
        private Vector2 spawnRange;
        private double dspSongStartTime;
        private int spawnIndex = 0;


        
        // --- Pooling fields ---
        [Header("Pooling")]
        [SerializeField] private int poolSize = 8;
        private readonly Queue<OSUBeatNote> notePool = new();
        private readonly Queue<GameObject> enemyPool = new();
        private readonly Queue<GameObject> textPool = new();

        private readonly List<OSUBeatNote> activeNotes = new();
        private AimIndicator indicator;
        public OSUBeatNote CurrentIndicatorTarget {get; private set;}

        private void Awake()
        {

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Calculate spawn range based on settings
            if (useCanvasSize && noteParentCanvas != null)
            {
                spawnRange = new Vector2(
                    noteParentCanvas.rect.width - spawnRangeOffset.x,
                    noteParentCanvas.rect.height - spawnRangeOffset.y
                );
            }
            else
            {
                spawnRange = customSpawnRange;
            }

            // Pre-instantiate note pool objects and hide them
            for (int i = 0; i < poolSize; i++)
            {
                var note = Instantiate(notePrefab, noteParentCanvas);
                note.gameObject.SetActive(false);
                notePool.Enqueue(note);

                var enemy = Instantiate(enemyPrefab);
                enemy.SetActive(false);
                enemyPool.Enqueue(enemy);

                var text = Instantiate(notificationTextPrefab, noteParentCanvas);
                text.SetActive(false);
                textPool.Enqueue(text);
            }

        }

        private void Start()
        {

            canvasComponent = noteParentCanvas.GetComponentInParent<Canvas>();

            double startTime = AudioSettings.dspTime + AudioStartDelay;

            // Announce the intent to play scheduled music via the event system.
            GameEvents.Instance.PlayMusicScheduled(beatmap.musicTrack, startTime);

 
            dspSongStartTime = startTime;


            // Synchronise countdown
            var countdownText = FindFirstObjectByType<Rhythm.UI.CountDownText>();
            if (countdownText != null)
                countdownText.SetScheduledStartTime(dspSongStartTime);

            


            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {

                float minX = -spawnRange.x / 2f;
                float maxX = spawnRange.x / 2f;
                float minY = -spawnRange.y / 2f;
                float maxY = spawnRange.y / 2f;


                int virtualCount = virtualSpawnPointCount;
                float distMin = distanceRange.x;
                float distMax = distanceRange.y;
                
                enemySpawnPoints = new Transform[virtualCount];

                // Create a grid-like distribution of points
                int rowCount = Mathf.CeilToInt(Mathf.Sqrt(virtualCount));
                int colCount = Mathf.CeilToInt((float)virtualCount / rowCount);

                Debug.Log($"{Screen.width}, {Screen.height} | " +
                          $"Spawn Range: {spawnRange.x}, {spawnRange.y} | " +
                          $"Virtual Count: {virtualCount} | " +
                          $"Rows: {rowCount}, Cols: {colCount}");
                for (int i = 0; i < virtualCount; ++i)
                {
                    // Calculate grid position (0-1 range)
                    int row = i / colCount;
                    int col = i % colCount;

                    // Add randomness within the cell
                    float marginX = spawnRangeOffset.x / Screen.width;
                    float marginY = spawnRangeOffset.y / Screen.height;


                    marginX = Mathf.Clamp01(marginX);
                    marginY = Mathf.Clamp01(marginY);

                    // Compute normalized grid position WITHIN margins
                    float xPercent = Mathf.Lerp(marginX, 1f - marginX, (float)col / (colCount - 1));
                    float yPercent = Mathf.Lerp(marginY, 1f - marginY, (float)row / (rowCount - 1));


                    xPercent += UnityEngine.Random.Range(-0.02f, 0.02f);
                    yPercent += UnityEngine.Random.Range(-0.02f, 0.02f);

                    // Convert to viewport coordinates (0-1, centered at 0.5,0.5)
                    float viewportX = xPercent;
                    float viewportY = yPercent;
                    
                    // Vary the distance from camera
                    float distance = UnityEngine.Random.Range(distMin, distMax);
                    
                    // Convert viewport point to world position at the desired distance
                    Vector3 viewportPoint = new Vector3(viewportX, viewportY, distance);
                    Vector3 worldPos = worldCamera.ViewportToWorldPoint(viewportPoint);
                    
                    // Create spawn point game object
                    var go = new GameObject($"VirtualSpawnPoint_{i}");
                    go.transform.position = worldPos;
                    
                    // Make the enemy face the camera
                    Vector3 dirToCam = (worldCamera.transform.position - worldPos).normalized;
                    go.transform.rotation = Quaternion.LookRotation(dirToCam, Vector3.up);
                    
                    go.transform.SetParent(transform);  // housekeeping
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
                (beatmap.notes[spawnIndex].hitTime) - beatmap.approachTime <= songTime)
            {
                SpawnNote(beatmap.notes[spawnIndex]);
                spawnIndex++;
            }

            if (AutoPlay)
            {
                foreach (var note in activeNotes)
                {
                    if (!note.HasProcessed && songTime >= note.RelativeHitTime)
                    {
                        ShotFired?.Invoke();
                        note.ProcessHit();
                    }
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    HandleInput();
                }
            }

            activeNotes.RemoveAll(note => note.HasProcessed);

            if (showIndicator) UpdateIndicator();
        }
        private void UpdateIndicator()
        {
            OSUBeatNote nextTarget = null;
            double earliestHitTime = double.MaxValue;
            foreach (var note in activeNotes)
            {
                if (!note.HasProcessed && note.RelativeHitTime < earliestHitTime)
                {
                    earliestHitTime = note.RelativeHitTime;
                    nextTarget = note;
                }
            }

            double songTime = AudioSettings.dspTime - dspSongStartTime;
            bool shouldBeActive = false;
            float timeToHit = 0f;

            if (nextTarget != null)
            {
                timeToHit = (float)(nextTarget.RelativeHitTime - songTime);
                float leadIn = beatmap.approachTime * indicatorLeadInMultiplier;

                if (timeToHit <= leadIn)
                {
                    shouldBeActive = true;
                }
            }

            if (shouldBeActive)
            {
                if (CurrentIndicatorTarget != nextTarget || !indicator.gameObject.activeSelf)
                {
                    indicator.gameObject.SetActive(true);
                    float duration = Mathf.Max(0.1f, timeToHit);
                    indicator.Initialise(nextTarget.GetComponent<RectTransform>(), duration);
                }
            }
            else
            {
                if (indicator.gameObject.activeSelf)
                {
                    indicator.gameObject.SetActive(false);
                }
            }

            CurrentIndicatorTarget = nextTarget;
        }

        public Action ShotFired;
        private void HandleInput()
        {
            ShotFired?.Invoke();
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
                AudioManager.Instance.PlaySFX(DryShot);
                
            }
        }

        private void SpawnNote(BeatNoteData data)
        {

            double relativeHitTime = data.hitTime; //
            double absoluteDSPHitTime = dspSongStartTime + relativeHitTime + audioOffset;


            // pick enemy spawn point
            int index = (data.spawnPointIndex >= 0 && data.spawnPointIndex < enemySpawnPoints.Length)
                        ? data.spawnPointIndex
                        : UnityEngine.Random.Range(0, enemySpawnPoints.Length);

            Transform spawnPoint = enemySpawnPoints[index];

            // 1. Spawn 3D enemy from pool
            GameObject enemy = GetEnemyFromPool();
            enemy.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            enemy.SetActive(true);

            ;
            // Init enemy with timing for sync
            if (enemy.TryGetComponent<EnemyRhythmUnit>(out var rhythmUnit))
            {
                rhythmUnit.SetHitTime(absoluteDSPHitTime);
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

            // Get notification text from pool and position it at the note
            NotificationText notificationText = GetNotificationTextFromPool();
            var notifRect = notificationText.GetComponent<RectTransform>();
            notifRect.SetParent(noteParentCanvas, false);
            notifRect.anchoredPosition = canvasPos;
            notificationText.gameObject.SetActive(false); // Hide until needed

            note.Initialise(
                absoluteDSPHitTime,
                relativeHitTime,
                beatmap.approachTime,
                delta => JudgementSystem.Instance.RegisterHit(delta),
                () => JudgementSystem.Instance.RegisterMiss(),
                ReturnNoteToPool,
                this,
                spawnPoint.position,
                notificationText.gameObject
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

        private NotificationText GetNotificationTextFromPool()
        {
            if (textPool.Count > 0)
            {
                var go = textPool.Dequeue();
                go.SetActive(true);
                return go.GetComponent<NotificationText>();
            }
            var newGo = Instantiate(notificationTextPrefab, noteParentCanvas);
            return newGo.GetComponent<NotificationText>();
        }

        public void ReturnNotificationTextToPool(NotificationText text)
        {
            text.ResetText();
            text.gameObject.SetActive(false);
            textPool.Enqueue(text.gameObject);
        }

        public Vector3 GetCurrentTargetPosition()
        {
            if (CurrentIndicatorTarget != null)
            {
                return CurrentIndicatorTarget.WorldPosition;
            }

            // Fallback if no target exists.
            return worldCamera.transform.position + worldCamera.transform.forward * 20f;
        }
    }
}

