using UnityEngine;
using System.Collections.Generic;

namespace Amapolas.Gameplay
{
    public class InfiniteCorridor : MonoBehaviour
    {
        public GameObject corridorTilePrefab;
        public int initialTiles = 5;
        public float tileLength = 10f;
        
        private List<GameObject> activeTiles = new List<GameObject>();
        private Transform cameraTransform;

        void Start()
        {
            cameraTransform = Camera.main.transform;
            
            // Create initial tiles
            for (int i = 0; i < initialTiles; i++)
            {
                SpawnTile(i * tileLength);
            }
        }

        void Update()
        {
            if (GameplayManager.Instance == null) return;
            
            float speed = GameplayManager.Instance.gameSpeed;
            
            // Move tiles
            foreach (var tile in activeTiles)
            {
                tile.transform.Translate(Vector3.back * speed * Time.deltaTime);
            }

            // Recycle tiles
            if (activeTiles[0].transform.position.z < -tileLength)
            {
                GameObject tile = activeTiles[0];
                activeTiles.RemoveAt(0);
                
                float lastZ = activeTiles[activeTiles.Count - 1].transform.position.z;
                tile.transform.position = new Vector3(0, 0, lastZ + tileLength);
                activeTiles.Add(tile);
            }
        }

        void SpawnTile(float zPos)
        {
            GameObject tile = null;
            if (corridorTilePrefab != null)
            {
                tile = Instantiate(corridorTilePrefab, new Vector3(0, 0, zPos), Quaternion.identity, transform);
            }
            else
            {
                // Fallback: simple floor
                tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
                tile.transform.position = new Vector3(0, 0, zPos);
                tile.transform.rotation = Quaternion.Euler(0, 0, 0);
                tile.transform.localScale = new Vector3(0.5f, 1, 1f);
                tile.transform.SetParent(transform);
            }
            activeTiles.Add(tile);
        }
    }
}
