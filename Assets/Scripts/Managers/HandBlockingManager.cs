using UnityEngine;
using Amapolas.Managers;
using System.Collections.Generic;

namespace Amapolas.Gameplay
{
    public class HandBlockingManager : MonoBehaviour
    {
        [Header("Shield Settings")]
        public GameObject shieldPrefab; // Reference to a prefab with Shield tag and HandShield script
        public float shieldDistance = 2f;
        public float smoothness = 10f;

        private Camera _mainCamera;
        private List<GameObject> _activeShields = new List<GameObject>();

        private void Start()
        {
            _mainCamera = Camera.main;

            if (DetectionManager.Instance != null)
            {
                DetectionManager.Instance.OnHandsUpdated += UpdateShields;
            }
        }

        private void UpdateShields(Vector2[] handPositions)
        {
            // Manage shield count
            while (_activeShields.Count < handPositions.Length)
            {
                CreateShield();
            }
            while (_activeShields.Count > handPositions.Length)
            {
                var shield = _activeShields[_activeShields.Count - 1];
                _activeShields.RemoveAt(_activeShields.Count - 1);
                Destroy(shield);
            }

            // Update positions
            for (int i = 0; i < handPositions.Length; i++)
            {
                UpdateShieldPosition(_activeShields[i], handPositions[i]);
            }
        }

        private void CreateShield()
        {
            GameObject shield = null;
            if (shieldPrefab != null)
            {
                shield = Instantiate(shieldPrefab, transform);
            }
            else
            {
                // Fallback shield
                shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shield.name = "HandShield_Fallback";
                shield.transform.localScale = new Vector3(0.5f, 0.5f, 0.1f);
                shield.tag = "Shield";
                var collider = shield.GetComponent<Collider>();
                collider.isTrigger = true;
                shield.AddComponent<HandShield>();
            }

            var handShield = shield.GetComponent<HandShield>();
            if (handShield != null)
            {
                handShield.OnKnifeBlocked += HandleBlock;
            }

            _activeShields.Add(shield);
        }

        private void UpdateShieldPosition(GameObject shield, Vector2 normalizedPos)
        {
            // Convert normalized MediaPipe coordinates (0-1) to camera view space
            // NOTE: Flip X to fix mirroring. Map Y directly to fix vertical inversion.
            Vector3 screenPos = new Vector3((1f - normalizedPos.x) * Screen.width, normalizedPos.y * Screen.height, shieldDistance);
            Vector3 targetWorldPos = _mainCamera.ScreenToWorldPoint(screenPos);

            shield.transform.position = Vector3.Lerp(shield.transform.position, targetWorldPos, Time.deltaTime * smoothness);
        }

        private void HandleBlock()
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
                DetectionManager.Instance.OnHandsUpdated -= UpdateShields;
            }
        }
    }
}
