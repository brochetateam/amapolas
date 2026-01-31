using UnityEngine;
using Amapolas.Managers;
using System.Collections.Generic;

namespace Amapolas.Gameplay
{
    public enum GameSector { TopLeft, TopRight, BottomLeft, BottomRight }

    public class HandBlockingManager : MonoBehaviour
    {
        [Header("Sector Shields")]
        public GameObject[] sectorShields = new GameObject[4]; // Order matching GameSector enum
        public float smoothness = 10f;

        private void Start()
        {
            if (DetectionManager.Instance != null)
            {
                DetectionManager.Instance.OnHandsUpdated += UpdateHandSectors;
            }
        }

        private void UpdateHandSectors(Vector2[] handPositions)
        {
            // Reset all sectors for this frame
            bool[] sectorActivity = new bool[4];

            foreach (var pos in handPositions)
            {
                // X: 0 (Right) -> 1 (Left)
                // Y: 0 (Top) -> 1 (Bottom)
                float x = 1f - pos.x;
                float y = pos.y;

                bool isLeft = x < 0.5f;
                bool isTop = y < 0.5f;

                if (isTop && isLeft) sectorActivity[(int)GameSector.TopLeft] = true;
                else if (isTop && !isLeft) sectorActivity[(int)GameSector.TopRight] = true;
                else if (!isTop && isLeft) sectorActivity[(int)GameSector.BottomLeft] = true;
                else if (!isTop && !isLeft) sectorActivity[(int)GameSector.BottomRight] = true;
            }

            // Sync shield gameobjects
            for (int i = 0; i < 4; i++)
            {
                if (sectorShields[i] != null)
                {
                    sectorShields[i].SetActive(sectorActivity[i]);
                }
            }
        }

        public void HandleBlock()
        {
            if (GameUI.Instance != null)
            {
                GameUI.Instance.TriggerBlockFeedback();
            }
        }

        private void OnDestroy()
        {
            if (DetectionManager.Instance != null)
            {
                DetectionManager.Instance.OnHandsUpdated -= UpdateHandSectors;
            }
        }
    }
}
