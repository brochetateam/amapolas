using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Amapolas.Utils;

namespace Amapolas.Gameplay
{
    public enum ProjectileType { Knife, Amapola }

    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager Instance { get; private set; }

        [Header("Settings")]
        public float gameSpeed = 7f;
        public GameObject knifePrefab;
        public GameObject amapolaPrefab;
        public Transform knifeSpawnPoint;
        public float timeBetweenSpawns = 1.5f;

        [Header("Finish Settings")]
        public string finalMessage = "SOY LIBRE";
        private AudioSource _bgmSource;
        private bool _isGameFinished = false;

        [Header("Player Stats")]
        public float maxHealth = 100f;
        public float currentHealth;

        private bool isGameActive = false;
        private bool _isProjectileActive = false;
        private float _gameStartTime = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            currentHealth = maxHealth;
        }

        private void Start()
        {
            // Subscribe to DetectionManager events
            if (Managers.DetectionManager.Instance != null)
            {
                Managers.DetectionManager.Instance.OnMaskPutOn += StartGame;
            }
            
            UpdateUI();

            // Find the audio source
            GameObject audioGO = GameObject.Find("Audio Source");
            if (audioGO != null) _bgmSource = audioGO.GetComponent<AudioSource>();

            InitializeNarrativeEvents();
            StartCoroutine(SpawnProjectilesRoutine());
        }

        [System.Serializable]
        public struct NarrativeEvent
        {
            public float timestamp;
            public string phrase;
            public bool triggered;
        }

        private List<NarrativeEvent> _narrativeQueue = new List<NarrativeEvent>();

        private void InitializeNarrativeEvents()
        {
            _narrativeQueue.Clear();
            _narrativeQueue.Add(new NarrativeEvent { timestamp = 0f, phrase = "Al principio era fácil...", triggered = false });
            _narrativeQueue.Add(new NarrativeEvent { timestamp = 30f, phrase = "Pero el ruido exterior creció...", triggered = false });
            _narrativeQueue.Add(new NarrativeEvent { timestamp = 60f, phrase = "Me escondí tras un muro...", triggered = false });
            _narrativeQueue.Add(new NarrativeEvent { timestamp = 90f, phrase = "Olvidé mi propia voz...", triggered = false });
            _narrativeQueue.Add(new NarrativeEvent { timestamp = 150f, phrase = "Hoy elijo soltar el peso...", triggered = false });
            _narrativeQueue.Add(new NarrativeEvent { timestamp = 180f, phrase = "Me quito la máscara...", triggered = false });
            _narrativeQueue.Add(new NarrativeEvent { timestamp = 210f, phrase = "Por fin respiro...", triggered = false });
            _narrativeQueue.Add(new NarrativeEvent { timestamp = 230f, phrase = "Soy suficiente.", triggered = false });
        }

        public void StartGame()
        {
            isGameActive = true;
            _isProjectileActive = false;
            _gameStartTime = Time.time;
            Debug.Log("Game Started: Narrative sequence beginning...");
            StartCoroutine(SpawnProjectilesRoutine());
        }

        public void AdjustHealth(float amount)
        {
            currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
            UpdateUI();
            
            if (currentHealth <= 0)
            {
                GameOver();
            }
        }

        public void NotifyProjectileResolved()
        {
            _isProjectileActive = false;
        }

        private void UpdateUI()
        {
            if (Managers.GameUI.Instance != null)
            {
                Managers.GameUI.Instance.UpdateHealth(currentHealth, maxHealth);
            }
        }

        private void GameOver()
        {
            isGameActive = false;
            Debug.Log("GAME OVER");
            // Add more game over logic if needed
        }

        private void Update()
        {
            if (!isGameActive || _isGameFinished) return;

            // Background speed drift
            gameSpeed += Time.deltaTime * 0.05f;

            float currentTime = _bgmSource != null ? _bgmSource.time : (Time.time - _gameStartTime);

            // Trigger narrative events
            for (int i = 0; i < _narrativeQueue.Count; i++)
            {
                var ev = _narrativeQueue[i];
                if (!ev.triggered && currentTime >= ev.timestamp)
                {
                    ev.triggered = true;
                    _narrativeQueue[i] = ev; // Update back in list
                    if (Managers.GameUI.Instance != null)
                    {
                        Managers.GameUI.Instance.DisplayStoryPhrase(ev.phrase);
                    }
                }
            }

            // Check if audio finished (if it was playing)
            if (_bgmSource != null && !_bgmSource.isPlaying && _bgmSource.time == 0 && Time.timeSinceLevelLoad > 10f)
            {
                FinishGame();
            }
            else if (_bgmSource != null && !_bgmSource.loop && _bgmSource.time >= _bgmSource.clip.length - 0.2f)
            {
                FinishGame();
            }
        }

        private void FinishGame()
        {
            if (_isGameFinished) return;
            _isGameFinished = true;
            gameSpeed = 0f; // Stop corridor
            _isProjectileActive = true; 
            StopAllCoroutines(); // Stop spawning
            
            // Clean up scene
            foreach (var p in FindObjectsOfType<Projectile>()) Destroy(p.gameObject);
            
            if (Managers.GameUI.Instance != null)
            {
                Managers.GameUI.Instance.ClearWord();
                Managers.GameUI.Instance.HideMessages();
                Managers.GameUI.Instance.ShowFinalExperienceMessage(finalMessage);
            }
        }

        IEnumerator SpawnProjectilesRoutine()
        {
            yield return new WaitForSeconds(2f); // Initial wait
            
            while (isGameActive && !_isGameFinished)
            {
                // Keep spawning as long as there's no active projectile
                if (!_isProjectileActive)
                {
                    yield return new WaitForSeconds(timeBetweenSpawns);
                    if (isGameActive) SpawnRandomProjectile();
                }
                yield return null; 
            }
        }

        void SpawnRandomProjectile()
        {
            if (_isGameFinished) return;

            float currentTime = _bgmSource != null ? _bgmSource.time : (Time.time - _gameStartTime);
            
            bool spawnAmapola = true;
            float currentSpawnDelay = timeBetweenSpawns;

            // UPDATED NARRATIVE PHASES LOOP (Gameplay difficulty only)
            if (currentTime < 60f) // 0-60s: Innocence / Early Judgment
            {
                // 80% Amapolas initially, decreasing toward 50% near 60s
                spawnAmapola = Random.value > Mathf.Lerp(0.2f, 0.5f, currentTime / 60f);
                currentSpawnDelay = 2.5f;
            }
            else if (currentTime < 150f) // 60-150s: The Mask (Increasing intensity)
            {
                // Mostly knives
                spawnAmapola = Random.value > 0.9f; 
                currentSpawnDelay = Mathf.Lerp(2.0f, 1.2f, (currentTime - 60f) / 90f);
                gameSpeed = Mathf.Max(gameSpeed, 7f);
            }
            else if (currentTime < 200f) // 150-200s: Transition
            {
                // 50/50 mix
                spawnAmapola = Random.value > 0.5f;
                currentSpawnDelay = 1.8f;
            }
            else // 200s to end: The Awakening
            {
                // Pure Amapolas
                spawnAmapola = true;
                currentSpawnDelay = 3.5f;
                gameSpeed = Mathf.Max(gameSpeed * 0.98f, 3.5f);
            }

            GameObject prefab = spawnAmapola ? amapolaPrefab : knifePrefab;
            if (prefab == null) prefab = knifePrefab;

            GameObject go = Instantiate(prefab, knifeSpawnPoint.position, Quaternion.identity);
            
            // Disable visual representation for Amapolas as requested (no art yet)
            if (spawnAmapola)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;
                
                // Also check children if it's a prefab with multiple parts
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.enabled = false;
            }
            
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Projectile projectile = go.AddComponent<Projectile>();
            projectile.type = spawnAmapola ? ProjectileType.Amapola : ProjectileType.Knife;
            projectile.speed = gameSpeed * 1.5f;

            // Word Logic (Only gameplay words here, story phrases are handled in Update)
            if (Managers.GameUI.Instance != null)
            {
                string word = spawnAmapola ? GameWords.GetRandomKind() : GameWords.GetRandomHurtful();
                // User requested Amapolas to be GREEN
                Color wordColor = spawnAmapola ? Color.green : Color.red; 
                Managers.GameUI.Instance.DisplayWord(word, wordColor);
            }

            _isProjectileActive = true;
            timeBetweenSpawns = currentSpawnDelay;
        }
    }

    public class Projectile : MonoBehaviour
    {
        public ProjectileType type;
        public float speed;
        private Transform playerCamera;
        private Vector3 direction;
        private bool isDeflected = false;
        private bool isResolved = false;

        void Start()
        {
            playerCamera = Camera.main.transform;
            // Target the camera position at spawn time
            direction = (playerCamera.position - transform.position).normalized;
            
            // Look towards player
            transform.LookAt(playerCamera);
        }

        void Update()
        {
            if (!isDeflected)
            {
                transform.position += direction * speed * Time.deltaTime;
            }
            else
            {
                transform.Translate(Vector3.forward * speed * Time.deltaTime);
            }

            // Destroy if passed player or too far
            if (Vector3.Distance(transform.position, playerCamera.position) > 50f && transform.position.z < playerCamera.position.z)
            {
                Resolve();
                Destroy(gameObject);
            }
        }

        private void Resolve()
        {
            if (isResolved) return;
            isResolved = true;
            if (GameplayManager.Instance != null) GameplayManager.Instance.NotifyProjectileResolved();
            if (Managers.GameUI.Instance != null) Managers.GameUI.Instance.ClearWord();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isResolved) return;

            if (other.CompareTag("Shield")) 
            {
                isDeflected = true;
                // Move away in a random upward direction
                direction = new Vector3(UnityEngine.Random.Range(-1f, 1f), 1f, 1f).normalized;
                transform.LookAt(transform.position + direction);
                speed *= 0.5f;
                
                if (Managers.GameUI.Instance != null) Managers.GameUI.Instance.TriggerBlockFeedback();
                
                Resolve();
                Destroy(gameObject, 2f);
            }
            else if (other.CompareTag("Player"))
            {
                if (type == ProjectileType.Knife)
                {
                    GameplayManager.Instance.AdjustHealth(-15f);
                    if (Managers.GameUI.Instance != null) Managers.GameUI.Instance.TriggerHitFeedback();
                }
                else
                {
                    GameplayManager.Instance.AdjustHealth(10f);
                    if (Managers.GameUI.Instance != null) Managers.GameUI.Instance.TriggerHealFeedback();
                }
                
                Resolve();
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            Resolve();
        }
    }
}
