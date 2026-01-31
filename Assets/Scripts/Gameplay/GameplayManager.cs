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

            GameSector sector = (GameSector)Random.Range(0, 4);
            
            // Spawn far away with some variation
            Vector3 spawnPos = knifeSpawnPoint.position;
            spawnPos += GetSectorInWorld(sector, 10f); 

            GameObject knife = Instantiate(knifePrefab, spawnPos, Quaternion.identity);
            Knife projectile = knife.AddComponent<Knife>();
            projectile.speed = gameSpeed * 1.5f;
            
            projectile.SetSectorTarget(sector);
        }

        private Vector3 GetSectorInWorld(GameSector sector, float spread)
        {
            float x = (sector == GameSector.TopLeft || sector == GameSector.BottomLeft) ? -spread : spread;
            float y = (sector == GameSector.TopLeft || sector == GameSector.TopRight) ? spread : -spread;
            return new Vector3(x, y, 0);
        }
    }

    public class Knife : MonoBehaviour
    {
        public float speed;
        private Transform playerCamera;
        private Vector3 direction;
        private bool isDeflected = false;

        public void SetSectorTarget(GameSector sector)
        {
            playerCamera = Camera.main.transform;
            
            float spread = 0.8f; 
            float x = (sector == GameSector.TopLeft || sector == GameSector.BottomLeft) ? -spread : spread;
            float y = (sector == GameSector.TopLeft || sector == GameSector.TopRight) ? spread : -spread;
            
            Vector3 targetPos = playerCamera.position + playerCamera.forward * 2f + playerCamera.right * x + playerCamera.up * (y + 1.6f); 
            
            direction = (targetPos - transform.position).normalized;
            transform.LookAt(targetPos);
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

            if (Vector3.Distance(transform.position, playerCamera.position) > 50f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Shield")) 
            {
                isDeflected = true;
                direction = new Vector3(Random.Range(-1f, 1f), 1f, 1f).normalized;
                speed *= 0.5f;
                
                var manager = FindObjectOfType<HandBlockingManager>();
                if (manager != null) manager.HandleBlock();
                
                Destroy(gameObject, 2f);
            }
            else if (other.CompareTag("Player"))
            {
                if (Managers.GameUI.Instance != null) Managers.GameUI.Instance.TriggerHitFeedback();
                Destroy(gameObject);
            }
        }
    }
}
