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
                // X: 0 (Right in Image) -> 1 (Left in Image)
                // Y: 0 (Top in Image) -> 1 (Bottom in Image)
                // With our fixes: (1-x) is Left-to-Right, y is Top-to-Bottom
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
                    // Visual feedback: simple enable/disable for now
                    // In a more polished version, we could use alpha or scale
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
