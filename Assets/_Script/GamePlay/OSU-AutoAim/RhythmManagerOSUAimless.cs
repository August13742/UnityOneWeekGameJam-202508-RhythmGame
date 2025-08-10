using System;
using System.Collections.Generic;
using Rhythm.Core;
using Rhythm.UI;
using UnityEngine;

namespace Rhythm.GamePlay.OSU.Aimless
{
    public enum GameState
    {
        NotStarted,
        CountingDown,
        Playing,
        Paused,
        Finished
    }

    /// <summary>
    /// Singleton
    /// </summary>
    public class RhythmManagerOSUAimless : MonoBehaviour, INoteVisualSettings
    {
        public static RhythmManagerOSUAimless Instance
        {
            get; private set;
        }

        [Header("Game State")]
        public GameState CurrentState { get; private set; } = GameState.NotStarted;
        public bool AutoPlay = false;

        
        [Header("Audio Settings")]
        public float AudioStartDelay = 3f;
        public bool LoopSong = false;

        [Header("Beatmap Settings")]
        [SerializeField] private BeatmapData beatmap;
        [SerializeField] private string songKey = ""; 
        [SerializeField] private Difficulty difficulty = Difficulty.Normal; 
        [SerializeField] private SongRecordsDB recordsDB;

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
        [SerializeField] private SFXResource noteSpawnIndicationSFX;
        [Header("Gameplay Options")]
        public bool showIndicator = true;
        public bool showApproachRing = true;
        public bool showPerfectSFX = true;

        [SerializeField] double leadinWindow = 0f;
        public bool ShowApproachRing => showApproachRing;
        public bool ShowIndicator => showIndicator;
        public bool ShowPerfectSFX => showPerfectSFX;
        public float indicatorLeadInMultiplier = 1.0f;

        private Canvas canvasComponent;
        private Vector2 spawnRange;
        private double dspSongStartTime;
        private int spawnIndex = 0;

        // Pause timing fields
        private double pausedDspTime;
        private double totalPausedDuration;

        // --- Pooling fields ---
        [Header("Pooling")]
        [SerializeField] private int poolSize = 8;
        private readonly Queue<OSUBeatNote> notePool = new();
        private readonly Queue<GameObject> enemyPool = new();
        private readonly Queue<GameObject> textPool = new();

        private readonly List<OSUBeatNote> activeNotes = new();
        private AimIndicator indicator;
        public OSUBeatNote CurrentIndicatorTarget { get; private set; }

        // Events for UI
        public static event Action<GameState> OnGameStateChanged;
        public static event Action<float> OnSongProgressChanged;
        public static event Action OnGameStarted;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action OnGameFinished;
        public Action ShotFired;

        // event for calibration UI updates
        public static event Action<float> OnAudioOffsetChanged;
        public InputSystem_Actions Input
        {
            get; private set;
        }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Check for parameters from jukebox scene
            if (GameStartParameters.TryGetParameters(
                out BeatmapData paramBeatmap,
                out string paramSongKey,
                out Difficulty paramDifficulty,
                out bool paramAutoPlay,
                out bool paramShowIndicator,
                out bool paramShowApproachRing,
                out bool paramShowPerfectSFX))
            {
                beatmap = paramBeatmap;
                songKey = paramSongKey;
                difficulty = paramDifficulty;
                AutoPlay = paramAutoPlay;
                showIndicator = paramShowIndicator;
                showApproachRing = paramShowApproachRing;
                showPerfectSFX = paramShowPerfectSFX;
                
                Debug.Log($"[RhythmManager] Loaded parameters from jukebox: Song='{songKey}', Difficulty='{difficulty}'");
                
                // Clear parameters after use
                GameStartParameters.ClearParameters();
            }
            else
            {
                Debug.LogWarning("[RhythmManager] No parameters from jukebox found, using default beatmap settings");
                
                // Set default song key from beatmap name if empty
                if (string.IsNullOrEmpty(songKey) && beatmap != null)
                {
                    songKey = beatmap.name;
                }
            }

            // Load saved audio offset if present
            if (PlayerPrefs.HasKey("AudioOffset"))
            {
                audioOffset = Mathf.Clamp(PlayerPrefs.GetFloat("AudioOffset"), minAudioOffset, maxAudioOffset);
            }

            Input = new InputSystem_Actions();

            InitialiseSpawnRange();
            InitialisePools();
            InitialiseEnemySpawnPoints();

            // Initialise records database if not assigned
            if (recordsDB == null)
            {
                // Try to find existing SongRecordsDB in Resources or create one
                recordsDB = Resources.Load<SongRecordsDB>("SongRecordsDB");
                if (recordsDB == null)
                {
                    Debug.LogWarning("No SongRecordsDB found. Records will not be saved.");
                }
            }
        }

        private void OnEnable()
        {
            Input?.Enable();
            AudioManager.OnMusicStartConfirmed += HandleMusicStartConfirmed;
        }
        private void OnDisable()
        {
            Input?.Disable();
            AudioManager.OnMusicStartConfirmed -= HandleMusicStartConfirmed;
        }

        private void HandleMusicStartConfirmed(double actualDspStart)
        {
            dspSongStartTime = actualDspStart; // authoritative
        }

        private void Start()
        {

            if (!beatmap || !beatmap.musicTrack || !noteParentCanvas || !indicatorPrefab || !notePrefab || !enemyPrefab || !notificationTextPrefab || !worldCamera)
            {
                Debug.LogError("[RhythmManager] Missing required references.");
                enabled = false;
                return;
            }

            if (leadinWindow.Equals(0f))
            {
                leadinWindow = JudgementSystem.Instance.PerfectWindow / 2;
            }
            canvasComponent = noteParentCanvas.GetComponentInParent<Canvas>();
            indicator = Instantiate(indicatorPrefab, noteParentCanvas);
            indicator.gameObject.SetActive(false);
            
            SetGameState(GameState.NotStarted);
            
            Debug.Log($"[RhythmManager] Initialised with beatmap: '{beatmap.name}', Song: '{songKey}', Difficulty: '{difficulty}'");
            StartCoroutine(StartGameSequence());
        }
        private System.Collections.IEnumerator StartGameSequence()
        {

            float fadeInDuration = 1.0f;
            CrossfadeManager.Instance.FadeFromBlack();

            yield return new WaitForSeconds(fadeInDuration);

            StartGame();
        }
        private void Update()
        {
            if (CurrentState == GameState.Playing)
            {
                UpdateGameplay();
            }
        }

        #region Game State Management

        public void StartGame()
        {
            if (CurrentState != GameState.NotStarted && CurrentState != GameState.Finished)
            {
                Debug.LogWarning("Cannot start game in current state: " + CurrentState);
                return;
            }

            if (beatmap == null || beatmap.musicTrack == null)
            {
                Debug.LogError("[RhythmManager] Cannot start game: No valid beatmap or music track!");
                return;
            }

            ResetGame();
            SetGameState(GameState.CountingDown);

            double startTime = AudioSettings.dspTime + AudioStartDelay;
            dspSongStartTime = startTime; // provisional; will be overwritten by confirmation
            totalPausedDuration = 0.0;
            GameEvents.Instance.PlayMusicScheduled(beatmap.musicTrack, startTime);

            JudgementSystem.Instance.TotalNotesInSong = beatmap.notes.Count;
            // Synchronise countdown
            var countdownText = FindFirstObjectByType<CountDownText>();
            if (countdownText != null)
                countdownText.SetScheduledStartTime(dspSongStartTime);

            OnGameStarted?.Invoke();

            // Start the game after the countdown
            Invoke(nameof(StartPlayingState), AudioStartDelay);
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing)
            {
                Debug.LogWarning("Cannot pause game in current state: " + CurrentState);
                return;
            }

            pausedDspTime = AudioSettings.dspTime;
            SetGameState(GameState.Paused);
            
            AudioManager.Instance.PauseMusic();
            
            
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused)
            {
                Debug.LogWarning("Cannot resume game in current state: " + CurrentState);
                return;
            }

            // Calculate how long we were paused and adjust timing
            double pauseDuration = AudioSettings.dspTime - pausedDspTime;
            totalPausedDuration += pauseDuration;
            
            SetGameState(GameState.Playing);
            
            AudioManager.Instance.ResumeMusic();
            
            OnGameResumed?.Invoke();
        }

        public void StopGame()
        {
            SetGameState(GameState.Finished);
            AudioManager.Instance.StopMusic();
            OnGameFinished?.Invoke();

            // Save record only if not in AutoPlay mode and we have valid data
            if (!AutoPlay && recordsDB != null && JudgementSystem.Instance != null && !string.IsNullOrEmpty(songKey))
            {
                recordsDB.SaveRecord(
                    songKey, 
                    difficulty,
                    score: JudgementSystem.Instance.Score,
                    accuracy: JudgementSystem.Instance.CurrentAccuracy,
                    combo: JudgementSystem.Instance.MaxComboAchieved
                );
                
                Debug.Log($"Record saved for {songKey} ({difficulty}): Score={JudgementSystem.Instance.Score}, Accuracy={JudgementSystem.Instance.CurrentAccuracy:P2}, Combo={JudgementSystem.Instance.MaxComboAchieved}");
            }
            else if (AutoPlay)
            {
                Debug.Log("AutoPlay mode - record not saved");
            }
            else
            {
                Debug.LogWarning("Could not save record: missing recordsDB, JudgementSystem, or songKey");
            }
        }

        public void RestartGame()
        {
            StopGame();
            JudgementSystem.Instance.ResetStatistics();
            StartGame();
        }

        private void StartPlayingState()
        {
            SetGameState(GameState.Playing);
        }

        private void SetGameState(GameState newState)
        {
            if (CurrentState != newState)
            {
                CurrentState = newState;
                OnGameStateChanged?.Invoke(newState);
            }
        }

        private void ResetGame()
        {
            // Clear all active notes
            foreach (var note in activeNotes)
                note.ProcessMiss();
            activeNotes.Clear();

            // Return all enemies to pool
            foreach (var enemy in FindObjectsByType<EnemyRhythmUnit>(FindObjectsSortMode.None))
                ReturnEnemyToPool(enemy.gameObject);

            spawnIndex = 0;
            totalPausedDuration = 0.0;
            CurrentIndicatorTarget = null;
            
            if (indicator != null)
                indicator.gameObject.SetActive(false);
        }
        
        private void UpdateGameplay()
        {
            double songTime = SongTimeNow();


            // Update song progress for UI
            if (beatmap != null && beatmap.notes.Count > 0)
            {
                float totalSongLength = (float)beatmap.musicTrack.length;
                float progress = Mathf.Clamp01((float)songTime / totalSongLength);
                OnSongProgressChanged?.Invoke(progress);
            }

            // Spawn notes
            while (spawnIndex < beatmap.notes.Count &&
                (beatmap.notes[spawnIndex].hitTime) - beatmap.approachTime <= songTime)
            {
                SpawnNote(beatmap.notes[spawnIndex]);
                spawnIndex++;
            }

            if (showPerfectSFX)
            {
                foreach (var note in activeNotes)
                {
                    if (!note.HasProcessed && !note.IndicatorSoundPlayed)
                    {
                        double timeDelta = songTime - note.RelativeHitTime;
                        if (Mathf.Abs((float)timeDelta) <= leadinWindow)
                        {
                            AudioManager.Instance.PlaySFX(noteSpawnIndicationSFX);
                            note.IndicatorSoundPlayed = true; // Mark as played
                            break;
                        }
                    }
                }
            }

            if (AutoPlay)
            {
                foreach (var note in activeNotes)
                {
                    if (!note.HasProcessed && songTime >= note.RelativeHitTime)
                    {
                        ShotFired?.Invoke();
                        note.ProcessHit();
                        break;
                    }
                }
            }
            else if (Input.Player.HitNote.triggered)
            {
                HandleInput();
            }

            activeNotes.RemoveAll(note => note.HasProcessed);

            if (showIndicator)
                UpdateIndicator(songTime);

            // Check for song end using the actual audio clip length
            if (beatmap != null && beatmap.musicTrack != null)
            {
                float songEndTime = beatmap.musicTrack.length;
                if (songTime > songEndTime + 0.5f) // 0.5s buffer after song ends
                {
                    if (LoopSong)
                    {
                        RestartGame();
                    }
                    else
                    {
                        StopGame();
                    }
                }
            }
        }

        private void HandleInput()
        {
            ShotFired?.Invoke();
            OSUBeatNote noteToHit = null;
            double earliestHitTime = double.MaxValue;

            // Find the oldest note that hasn't been processed yet
            foreach (var note in activeNotes)
            {
                if (!note.HasProcessed && note.RelativeHitTime < earliestHitTime)
                {
                    earliestHitTime = note.RelativeHitTime;
                    noteToHit = note;
                    break;
                }
            }

            if (noteToHit != null)
            {
                noteToHit.ProcessHit();
            }
            else
            {
                // Penalty optional
                AudioManager.Instance.PlaySFX(DryShot);
            }
        }

        #endregion

        #region Save System API

        /// <summary>
        /// Set the song key and difficulty for save system
        /// </summary>
        public void SetSongInfo(string newSongKey, Difficulty newDifficulty)
        {
            songKey = newSongKey;
            difficulty = newDifficulty;
        }

        /// <summary>
        /// Get the current record for this song and difficulty
        /// </summary>
        public SongRecord GetCurrentRecord()
        {
            if (recordsDB != null && !string.IsNullOrEmpty(songKey))
            {
                return recordsDB.GetRecord(songKey, difficulty);
            }
            return null;
        }

        /// <summary>
        /// Check if current score would be a new personal best
        /// </summary>
        public bool IsNewPersonalBest()
        {
            var currentRecord = GetCurrentRecord();
            if (currentRecord == null) return true; // First time playing
            
            return JudgementSystem.Instance != null && 
                   JudgementSystem.Instance.Score > currentRecord.highScore;
        }

        #endregion

        #region Initialisation Methods

        private void InitialiseSpawnRange()
        {
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
        }

        private void InitialisePools()
        {
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

        private void InitialiseEnemySpawnPoints()
        {
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                int virtualCount = virtualSpawnPointCount;
                float distMin = distanceRange.x;
                float distMax = distanceRange.y;
                
                enemySpawnPoints = new Transform[virtualCount];

                if (virtualCount == 1)
                {
                    float centerDistance = (distMin + distMax) * 0.5f;
                    Vector3 centerViewport = new(0.5f, 0.5f, centerDistance);
                    Vector3 worldPos = worldCamera.ViewportToWorldPoint(centerViewport);

                    var go = new GameObject("VirtualSpawnPoint_0");
                    go.transform.position = worldPos;

                    Vector3 dirToCam = (worldCamera.transform.position - worldPos).normalized;
                    go.transform.rotation = Quaternion.LookRotation(dirToCam, Vector3.up);

                    go.transform.SetParent(transform);
                    enemySpawnPoints[0] = go.transform;
                }
                else
                {
                    int rowCount = Mathf.CeilToInt(Mathf.Sqrt(virtualCount));
                    int colCount = Mathf.CeilToInt((float)virtualSpawnPointCount / rowCount);

                    for (int i = 0; i < virtualCount; ++i)
                    {
                        int row = i / colCount;
                        int col = i % colCount;

                        float marginX = spawnRangeOffset.x / Screen.width;
                        float marginY = spawnRangeOffset.y / Screen.height;

                        marginX = Mathf.Clamp01(marginX);
                        marginY = Mathf.Clamp01(marginY);

                        float xPercent = Mathf.Lerp(marginX, 1f - marginX, (float)col / (colCount - 1));
                        float yPercent = Mathf.Lerp(marginY, 1f - marginY, (float)row / (rowCount - 1));

                        xPercent += UnityEngine.Random.Range(-0.02f, 0.02f);
                        yPercent += UnityEngine.Random.Range(-0.02f, 0.02f);

                        float viewportX = xPercent;
                        float viewportY = yPercent;
                        
                        float distance = UnityEngine.Random.Range(distMin, distMax);
                        
                        Vector3 viewportPoint = new(viewportX, viewportY, distance);
                        Vector3 worldPos = worldCamera.ViewportToWorldPoint(viewportPoint);
                        
                        var go = new GameObject($"VirtualSpawnPoint_{i}");
                        go.transform.position = worldPos;
                        
                        Vector3 dirToCam = (worldCamera.transform.position - worldPos).normalized;
                        go.transform.rotation = Quaternion.LookRotation(dirToCam, Vector3.up);
                        
                        go.transform.SetParent(transform);
                        enemySpawnPoints[i] = go.transform;
                    }
                }
            }
        }

        #endregion

        private void UpdateIndicator(double songTime = -1)
        {
            if (songTime < 0)
                songTime = AudioSettings.dspTime - dspSongStartTime - totalPausedDuration;

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

        private void SpawnNote(BeatNoteData data)
        {
            double relativeHitTime = data.hitTime;
            double absoluteDSPHitTime = dspSongStartTime + relativeHitTime + audioOffset + totalPausedDuration;

            int index = (data.spawnPointIndex >= 0 && data.spawnPointIndex < enemySpawnPoints.Length)
                        ? data.spawnPointIndex
                        : UnityEngine.Random.Range(0, enemySpawnPoints.Length);

            Transform spawnPoint = enemySpawnPoints[index];

            GameObject enemy = GetEnemyFromPool();
            enemy.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            enemy.SetActive(true);

            if (enemy.TryGetComponent<EnemyRhythmUnit>(out var rhythmUnit))
            {
                rhythmUnit.SetRelativeHitTime(relativeHitTime + audioOffset);
                rhythmUnit.SetReturnToPoolCallback(ReturnEnemyToPool);
            }

            Vector3 screenPos = worldCamera.WorldToScreenPoint(spawnPoint.position);
            Camera cam = canvasComponent.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                noteParentCanvas, screenPos, cam, out Vector2 canvasPos);

            OSUBeatNote note = GetNoteFromPool();
            var rect = note.GetComponent<RectTransform>();
            rect.SetParent(noteParentCanvas, false);
            rect.anchoredPosition = canvasPos;

            NotificationText notificationText = GetNotificationTextFromPool();
            var notifRect = notificationText.GetComponent<RectTransform>();
            notifRect.SetParent(noteParentCanvas, false);
            notifRect.anchoredPosition = canvasPos;
            notificationText.gameObject.SetActive(false);

            note.Initialise(
                relativeHitTime,                     // ← pass relative
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

            return worldCamera.transform.position + worldCamera.transform.forward * 20f;
        }


        #region Public API for UI

        public bool IsGameActive => CurrentState == GameState.Playing;
        public bool CanStartGame => CurrentState == GameState.NotStarted || CurrentState == GameState.Finished;
        public bool CanPauseGame => CurrentState == GameState.Playing;
        public bool CanResumeGame => CurrentState == GameState.Paused;
        
        public float GetSongProgress()
        {
            if (beatmap == null || beatmap.notes.Count == 0 || CurrentState != GameState.Playing)
                return 0f;
                
            double songTime = AudioSettings.dspTime - dspSongStartTime - totalPausedDuration;
            float totalSongLength = (float)beatmap.notes[^1].hitTime + beatmap.approachTime;
            return Mathf.Clamp01((float)songTime / totalSongLength);
        }

        public string GetCurrentBeatmapName()
        {
            return beatmap != null ? beatmap.name : "No Beatmap";
        }

        [Header("Calibration Settings")]
        [SerializeField] private bool isCalibrationMode = false;
        [SerializeField] private float offsetAdjustmentStep = 0.01f; // 10ms steps
        [SerializeField] private float maxAudioOffset = 0.5f; // 500ms max
        [SerializeField] private float minAudioOffset = -0.5f; // -500ms max

        public bool IsCalibrationMode => isCalibrationMode;
        public float AudioOffset => audioOffset;

        public void StartCalibrationMode(BeatmapData calibrationBeatmap)
        {
            // Set calibration-specific settings
            isCalibrationMode = true;
            LoopSong = true;
            AutoPlay = false;
            beatmap = calibrationBeatmap;
            
            // Start the game
            StartGame();
        }

        public void ExitCalibrationMode()
        {
            isCalibrationMode = false;
            StopGame();
        }

        public void IncreaseAudioOffset()
        {
            audioOffset = Mathf.Clamp(audioOffset + offsetAdjustmentStep, minAudioOffset, maxAudioOffset);
            OnAudioOffsetChanged?.Invoke(audioOffset);
        }

        public void DecreaseAudioOffset()
        {
            audioOffset = Mathf.Clamp(audioOffset - offsetAdjustmentStep, minAudioOffset, maxAudioOffset);
            OnAudioOffsetChanged?.Invoke(audioOffset);
        }

        public void SetAudioOffset(float newOffset)
        {
            audioOffset = Mathf.Clamp(newOffset, minAudioOffset, maxAudioOffset);
            OnAudioOffsetChanged?.Invoke(audioOffset);
        }
        #endregion

        public BeatmapData CurrentBeatmap => beatmap;
        public int CurrentSpawnIndex => spawnIndex;
        public double CurrentDSPSongStartTime => dspSongStartTime;
        public double GetPauseAwareDSPTime()
        {
            return AudioSettings.dspTime - totalPausedDuration;
        }
        public double SongTimeNow() => AudioSettings.dspTime - dspSongStartTime - totalPausedDuration;

        public double TotalPausedDuration => totalPausedDuration;
    }
}

