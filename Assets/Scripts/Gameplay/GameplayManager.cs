using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Amapolas.Gameplay
{
    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager Instance { get; private set; }

        [Header("Settings")]
        public float gameSpeed = 5f;
        public GameObject knifePrefab;
        public Transform knifeSpawnPoint;

        private bool isGameActive = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            // Subscribe to DetectionManager events
            if (Managers.DetectionManager.Instance != null)
            {
                Managers.DetectionManager.Instance.OnMaskPutOn += StartGame;
            }
        }

        public void StartGame()
        {
            isGameActive = true;
            Debug.Log("Game Started: Corridor moving...");
            StartCoroutine(SpawnKnivesRoutine());
        }

        private void Update()
        {
            if (isGameActive)
            {
                // Speed up over time? 
                gameSpeed += Time.deltaTime * 0.1f;
            }
        }

        IEnumerator SpawnKnivesRoutine()
        {
            while (isGameActive)
            {
                yield return new WaitForSeconds(Random.Range(2f, 4f));
                SpawnKnife();
            }
        }

        void SpawnKnife()
        {
            if (knifePrefab == null) return;

            GameObject knife = Instantiate(knifePrefab, knifeSpawnPoint.position, Quaternion.identity);
            Knife projectile = knife.AddComponent<Knife>();
            projectile.speed = gameSpeed * 1.5f;
        }
    }

    public class Knife : MonoBehaviour
    {
        public float speed;
        private Transform playerCamera;
        private Vector3 direction;
        private bool isDeflected = false;

        void Start()
        {
            playerCamera = Camera.main.transform;
            // Target the camera position at spawn time
            direction = (playerCamera.position - transform.position).normalized;
        }

        void Update()
        {
            if (!isDeflected)
            {
                // Still moving towards player (even if player moves, we could update direction or keep it linear)
                transform.position += direction * speed * Time.deltaTime;
            }
            else
            {
                transform.Translate(direction * speed * Time.deltaTime);
            }

            // Destroy if passed player or too far
            if (Vector3.Distance(transform.position, playerCamera.position) > 50f && transform.position.z < playerCamera.position.z)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Shield")) 
            {
                Debug.Log("Knife Blocked!");
                isDeflected = true;
                direction = new Vector3(Random.Range(-1f, 1f), 1f, 1f).normalized;
                speed *= 0.5f;
                Destroy(gameObject, 2f);
            }
            else if (other.CompareTag("Player"))
            {
                Debug.Log("Hit Player!");
                Destroy(gameObject);
            }
        }
    }
}
