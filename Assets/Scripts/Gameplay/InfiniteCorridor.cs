using UnityEngine;
using System.Collections.Generic;

namespace Amapolas.Gameplay
{
    public class InfiniteCorridor : MonoBehaviour
    {
        public enum MovementType { CorridorMovesTowardsPlayer, PlayerMovesThroughCorridor }
        
        [Header("Settings")]
        public MovementType movementMode = MovementType.CorridorMovesTowardsPlayer;
        public GameObject corridorTilePrefab;
        public int initialTiles = 10;
        public float tileLength = 15f;
        
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
            
            if (movementMode == MovementType.CorridorMovesTowardsPlayer)
            {
                // Tiles move towards camera
                foreach (var tile in activeTiles)
                {
                    tile.transform.Translate(Vector3.back * speed * Time.deltaTime);
                }
            }
            else
            {
                // Camera moves forward
                cameraTransform.Translate(Vector3.forward * speed * Time.deltaTime);
                // Parent the shields to the camera so they follow the player
                // (In a real scenario, Hand tracking would be in screen space or world space relative to camera)
            }

            // Recycle tiles
            float playerZ = cameraTransform.position.z;
            if (activeTiles[0].transform.position.z < playerZ - tileLength)
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
                tile.transform.localScale = new Vector3(2f, 2f, 1f); // Make it 2x larger
            }
            else
            {
                // Fallback: simple floor
                tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
                tile.transform.position = new Vector3(0, 0, zPos);
                tile.transform.rotation = Quaternion.Euler(0, 0, 0);
                tile.transform.localScale = new Vector3(4f, 1, 1.5f); // Wider and deeper
                tile.transform.SetParent(transform);
            }
            activeTiles.Add(tile);
        }
    }
}
