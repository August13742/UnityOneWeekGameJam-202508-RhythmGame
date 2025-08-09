//using System.Collections.Generic;
//using UnityEngine;
//namespace Rhythm.GamePlay.OSU
//{

//    /// <summary>
//    /// Singleton
//    /// </summary>
//    public class RhythmManagerOSU : RhythmManager, INoteVisualSettings
//    {


//        public static RhythmManagerOSU Instance
//        {
//            get; private set;
//        }
 

//        public float delay = 3f;
//        [SerializeField] private BeatmapData beatmap;
//        [SerializeField] private OSUBeatNote notePrefab;
//        [SerializeField] private RectTransform noteParentCanvas;
//        [SerializeField] private float audioOffset = 0.0f;
//        [SerializeField] private Vector2 spawnRangeOffset = new (50, 50);

//        [SerializeField] private Camera worldCamera = null ;
//        [SerializeField] private Transform[] enemySpawnPoints;
//        [SerializeField] private GameObject enemyPrefab;
//        [Header("Gameplay Options")]
//        public bool showApproachRing = true;

//        public bool ShowApproachRing => showApproachRing;
//        public bool ShowIndicator => false; // OSU does not use indicator
//        private Canvas canvasComponent;
//        private Vector2 spawnRange = new (800, 440);
//        private double dspSongStartTime;
//        private int spawnIndex = 0;


//        float minX;
//        float maxX;
//        float minY;
//        float maxY;
//        // --- Pooling fields ---
//        [Header("Pooling")]
//        [SerializeField] private int poolSize = 8;
//        private readonly Queue<OSUBeatNote> notePool = new();
//        private readonly Queue<GameObject> enemyPool = new();

//        private void Awake()
//        {

//            if (Instance != null && Instance != this)
//            {
//                Destroy(gameObject);
//                return;
//            }
//            Instance = this;


//            spawnRange = new Vector2(
//                noteParentCanvas.rect.width - spawnRangeOffset.x,
//                noteParentCanvas.rect.height - spawnRangeOffset.y
//            );

//            // Pre-instantiate note pool objects and hide them
//            for (int i = 0; i < poolSize; i++)
//            {
//                var note = Instantiate(notePrefab, noteParentCanvas);
//                note.gameObject.SetActive(false);
//                notePool.Enqueue(note);
//            }

//            // Pre-instantiate enemy pool objects and hide them
//            for (int i = 0; i < poolSize; i++)
//            {
//                var enemy = Instantiate(enemyPrefab);
//                enemy.SetActive(false);
//                enemyPool.Enqueue(enemy);
//            }
//        }

//        private void Start()
//        {

//            canvasComponent = noteParentCanvas.GetComponentInParent<Canvas>();
//            // Start audio after delay
//            AudioSource audioSource = GetComponent<AudioSource>();
//            double startTime = AudioSettings.dspTime + delay;
//            audioSource.clip = beatmap.musicTrack;
//            audioSource.PlayScheduled(startTime);
//            dspSongStartTime = startTime;

//            // Synchronise countdown
//            var countdownText = FindFirstObjectByType<Rhythm.UI.CountDownText>();
//            if (countdownText != null)
//                countdownText.SetScheduledStartTime(dspSongStartTime);

//            minX = -spawnRange.x / 2f;
//            maxX = spawnRange.x / 2f;
//            minY = -spawnRange.y / 2f;
//            maxY = spawnRange.y / 2f;

//            // If no spawn points, create virtual ones in front of the camera
//            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
//            {
//                int virtualCount = 10;
//                float distMin = 10f;   // near plane
//                float distMax = 30f;   // far plane
//                float halfWidth = 10f;   // +-X spread
//                float heightOffset = 6f;    // +-Y spread

//                enemySpawnPoints = new Transform[virtualCount];

//                for (int i = 0; i < virtualCount; ++i)
//                {
//                    Vector3 localPos = new (
//                        Random.Range(-halfWidth, halfWidth),   // X
//                        Random.Range(0, heightOffset),  // Y
//                        Random.Range(distMin, distMax));    // Z (forward)

//                    Vector3 worldPos = worldCamera.transform.TransformPoint(localPos);

//                    var go = new GameObject($"VirtualSpawnPoint_{i}");
//                    go.transform.position = worldPos;

//                    // face the camera
//                    Vector3 dirToCam = (worldCamera.transform.position - worldPos).normalized;
//                    go.transform.rotation = Quaternion.LookRotation(dirToCam, Vector3.up);

//                    go.transform.SetParent(transform);          // housekeeping
//                    enemySpawnPoints[i] = go.transform;
//                }
//            }
//        }

//        private void Update()
//        {
//            double dspNow = AudioSettings.dspTime;
//            double songTime = dspNow - dspSongStartTime;

//            // Spawn all notes whose hitTime is within the lead-in window
//            while (spawnIndex < beatmap.notes.Count &&
//                   beatmap.notes[spawnIndex].hitTime - beatmap.approachTime <= songTime)
//            {
//                SpawnNote(beatmap.notes[spawnIndex]);
//                spawnIndex++;
//            }
//        }


//        private void SpawnNote(BeatNoteData data)
//        {
//            double relativeHitTime = data.hitTime; //
//            double absoluteDSPHitTime = dspSongStartTime + relativeHitTime + audioOffset;

//            // pick enemy spawn point
//            int index = (data.spawnPointIndex >= 0 && data.spawnPointIndex < enemySpawnPoints.Length)
//                        ? data.spawnPointIndex
//                        : Random.Range(0, enemySpawnPoints.Length);

//            Transform spawnPoint = enemySpawnPoints[index];

//            // 1. Spawn 3D enemy from pool
//            GameObject enemy = GetEnemyFromPool();
//            enemy.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
//            enemy.SetActive(true);

//            // Init enemy with timing for sync
//            if (enemy.TryGetComponent<EnemyRhythmUnit>(out var rhythmUnit))
//            {
//                rhythmUnit.SetHitTime(absoluteDSPHitTime);
//                rhythmUnit.SetReturnToPoolCallback(ReturnEnemyToPool);
//            }

//            // 2. Spawn UI marker
//            Vector3 screenPos = worldCamera.WorldToScreenPoint(spawnPoint.position);


//            Camera cam = canvasComponent.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera;

//            RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                noteParentCanvas, screenPos, cam, out Vector2 canvasPos);

//            OSUBeatNote note = GetNoteFromPool();
//            var rect = note.GetComponent<RectTransform>();
//            rect.SetParent(noteParentCanvas, false);
//            rect.anchoredPosition = canvasPos;

//            note.Initialise(
//                absoluteDSPHitTime,
//                relativeHitTime,
//                beatmap.approachTime,
//                delta => JudgementSystem.Instance.RegisterHit(delta),
//                () => JudgementSystem.Instance.RegisterMiss(),
//                ReturnNoteToPool,
//                this,
//                spawnPoint.position
//            );
//        }

//        private OSUBeatNote GetNoteFromPool()
//        {
//            if (notePool.Count > 0)
//            {
//                var note = notePool.Dequeue();
//                note.gameObject.SetActive(true);
//                return note;
//            }
//            // If pool is empty, instantiate as fallback (should be rare)
//            var newNote = Instantiate(notePrefab, noteParentCanvas);
//            return newNote;
//        }

//        public void ReturnNoteToPool(OSUBeatNote note)
//        {
//            note.gameObject.SetActive(false);
//            notePool.Enqueue(note);
//        }

//        private GameObject GetEnemyFromPool()
//        {
//            if (enemyPool.Count > 0)
//            {
//                var enemy = enemyPool.Dequeue();
//                enemy.SetActive(true);
//                return enemy;
//            }
//            // If pool is empty, instantiate as fallback (should be rare)
//            var newEnemy = Instantiate(enemyPrefab);
//            return newEnemy;
//        }

//        public void ReturnEnemyToPool(GameObject enemy)
//        {
//            enemy.SetActive(false);
//            enemyPool.Enqueue(enemy);
//        }
//    }
//}
