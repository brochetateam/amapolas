using UnityEngine;
using Amapolas.Managers;
using System.Collections.Generic;

namespace Amapolas.Gameplay
{
    public class HandBlockingManager : MonoBehaviour
    {
        public static HandBlockingManager Instance { get; private set; }

        [Header("Shield System Settings")]
        public GameObject shieldPrefab; // Reference to the big shield prefab
        public float blockHeightThreshold = 0.4f; // Normalized Y (0-1). > 0.4 means hands are "up"
        public float shieldDistance = 1.5f; // Increased buffer to avoid double triggers
        
        [Header("Visuals (Auto-applied if no prefab)")]
        [Range(0f, 1f)] public float shieldOpacity = 0.5f; 
        public Color shieldColor = new Color(0, 0.5f, 1f);
        public bool showAsHollowCircle = true;

        public bool IsBlocking { get; private set; } = false;

        private Camera _mainCamera;
        private GameObject _globalShield;
        private UnityEngine.UI.Image _shieldImage;
        private HandShield _handShieldComponent;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            _mainCamera = Camera.main;

            if (DetectionManager.Instance != null)
            {
                DetectionManager.Instance.OnHandsUpdated += UpdateBlockingState;
            }
            
            CreateGlobalShield();
        }

        private void Update()
        {
            if (_globalShield != null && _mainCamera != null)
            {
                // Ensure the shield follows the camera
                _globalShield.transform.position = _mainCamera.transform.position + _mainCamera.transform.forward * shieldDistance;
                _globalShield.transform.rotation = _mainCamera.transform.rotation;

                // Sync visuals if using the auto-generated shield
                if (_shieldImage != null)
                {
                    _shieldImage.enabled = IsBlocking && (shieldOpacity > 0.01f);
                    Color c = shieldColor;
                    c.a = IsBlocking ? shieldOpacity : 0f;
                    _shieldImage.color = c;
                }
            }
        }

        private void UpdateBlockingState(Vector2[] handPositions)
        {
            bool anyHandDetected = handPositions != null && handPositions.Length > 0;
            bool anyHandBlocking = false;

            if (anyHandDetected)
            {
                foreach (var pos in handPositions)
                {
                    // MediaPipe Y is 0 at top, 1 at bottom. 
                    // Threshold is distance from bottom. 
                    // Example: threshold 0.4 means blocking if Y is in top 60% (Y < 0.6)
                    float triggerY = 1f - blockHeightThreshold;
                    if (pos.y < triggerY) 
                    {
                        anyHandBlocking = true;
                        break;
                    }
                }
            }

            // IsBlocking update follows logic...
            IsBlocking = anyHandBlocking;

            if (_globalShield != null)
            {
                _globalShield.SetActive(IsBlocking);
            }
        }

        private void CreateGlobalShield()
        {
            if (shieldPrefab != null)
            {
                _globalShield = Instantiate(shieldPrefab, transform);
            }
            else
            {
                // 1. Create the Physics Controller (Invisible Cube)
                _globalShield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _globalShield.name = "GlobalBlockingShield_Physics";
                _globalShield.transform.SetParent(transform);
                _globalShield.transform.localScale = new Vector3(8f, 8f, 1f); 
                _globalShield.tag = "Shield";
                
                var collider = _globalShield.GetComponent<Collider>();
                collider.isTrigger = true;
                
                var renderer = _globalShield.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false; // INVISIBLE

                // 2. Create the World-Space UI Visuals
                GameObject canvasGO = new GameObject("Shield_UI_Canvas", typeof(Canvas));
                canvasGO.transform.SetParent(_globalShield.transform, false);
                
                Canvas canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                
                // Adjust size to match the 8x8 collider roughly
                RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
                canvasRT.sizeDelta = new Vector2(800, 800); 
                canvasRT.localScale = Vector3.one * 0.01f; // Back to world units
                
                GameObject imageGO = new GameObject("Shield_Image", typeof(UnityEngine.UI.Image));
                imageGO.transform.SetParent(canvasGO.transform, false);
                
                _shieldImage = imageGO.GetComponent<UnityEngine.UI.Image>();
                RectTransform imageRT = imageGO.GetComponent<RectTransform>();
                imageRT.anchorMin = Vector2.zero;
                imageRT.anchorMax = Vector2.one;
                imageRT.sizeDelta = Vector2.zero;

                // Simple hollow effect: If a sprite is not available, we can use a ring-like layout
                // or just a solid color. For now, solid color with alpha.
                _shieldImage.color = new Color(shieldColor.r, shieldColor.g, shieldColor.b, 0); 
                
                _globalShield.AddComponent<HandShield>();
            }

            _handShieldComponent = _globalShield.GetComponent<HandShield>();
            if (_handShieldComponent != null)
            {
                _handShieldComponent.OnKnifeBlocked += HandleBlock;
            }

            _globalShield.SetActive(false);
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
                DetectionManager.Instance.OnHandsUpdated -= UpdateBlockingState;
            }
        }
    }
}
