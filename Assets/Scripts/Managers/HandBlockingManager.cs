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
        [Range(0f, 1f)] public float shieldOpacity = 0.02f; 
        public Color shieldColor = new Color(0, 0.5f, 1f);

        public bool IsBlocking { get; private set; } = false;

        private Camera _mainCamera;
        private GameObject _globalShield;
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
                var renderer = _globalShield.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.enabled = IsBlocking && (shieldOpacity > 0.01f);
                    Color c = renderer.material.color;
                    c.a = shieldOpacity;
                    renderer.material.color = c;
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
                // Fallback: A big invisible (but trigger) plane or sphere
                _globalShield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _globalShield.name = "GlobalBlockingShield";
                _globalShield.transform.SetParent(transform);
                _globalShield.transform.localScale = new Vector3(8f, 8f, 1f); // Much larger and thicker
                _globalShield.tag = "Shield";
                
                var collider = _globalShield.GetComponent<Collider>();
                collider.isTrigger = true;
                
                var renderer = _globalShield.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Create a dedicated material to ensure transparency works
                    // Try "Standard" first; if URP is used, you might need to change this in Inspector
                    Material transMat = new Material(Shader.Find("Standard"));
                    if (transMat.shader != null)
                    {
                        transMat.SetFloat("_Mode", 3); // Transparent mode
                        transMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        transMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        transMat.SetInt("_ZWrite", 0);
                        transMat.EnableKeyword("_ALPHABLEND_ON");
                        transMat.renderQueue = 3000;
                        transMat.color = new Color(shieldColor.r, shieldColor.g, shieldColor.b, shieldOpacity);
                        renderer.material = transMat;
                    }
                }

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
