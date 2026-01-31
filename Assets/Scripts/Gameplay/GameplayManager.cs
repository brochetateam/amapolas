using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Amapolas.Gameplay
{
    public enum ProjectileType { Knife, Amapola }

    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager Instance { get; private set; }

        [Header("Settings")]
        public float gameSpeed = 5f;
        public GameObject knifePrefab;
        public GameObject amapolaPrefab;
        public Transform knifeSpawnPoint;
        public float timeBetweenSpawns = 1.5f;

        [Header("Player Stats")]
        public float maxHealth = 100f;
        public float currentHealth;

        private bool isGameActive = false;
        private bool _isProjectileActive = false;

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
        }

        public void StartGame()
        {
            isGameActive = true;
            _isProjectileActive = false;
            Debug.Log("Game Started: Corridor moving...");
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
            if (isGameActive)
            {
                // Speed up over time? 
                gameSpeed += Time.deltaTime * 0.1f;
            }
        }

        IEnumerator SpawnProjectilesRoutine()
        {
            while (isGameActive)
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
            bool spawnAmapola = Random.value > 0.7f; // 30% chance for amapola
            GameObject prefab = spawnAmapola ? amapolaPrefab : knifePrefab;
            if (prefab == null) prefab = knifePrefab; // Fallback
            if (prefab == null) return;

            GameObject go = Instantiate(prefab, knifeSpawnPoint.position, Quaternion.identity);
            
            // Ensure Rigidbody exists for reliable Trigger detection
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Projectile projectile = go.AddComponent<Projectile>();
            projectile.type = spawnAmapola ? ProjectileType.Amapola : ProjectileType.Knife;
            projectile.speed = gameSpeed * 1.5f;

            _isProjectileActive = true;
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
        }

        private void OnTriggerEnter(Collider other)
        {
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
