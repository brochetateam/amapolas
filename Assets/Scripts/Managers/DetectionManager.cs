using UnityEngine;
using UnityEngine.InputSystem;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Amapolas.Managers
{
    public enum DetectionState
    {
        WaitingForFace,
        WaitingForMask, // Face lost after being detected
        InGame
    }

    public class DetectionManager : MonoBehaviour
    {
        public static DetectionManager Instance { get; private set; }

        public DetectionState CurrentState { get; private set; } = DetectionState.WaitingForFace;

        public event Action OnFaceDetected;
        public event Action OnMaskPutOn; 
        public event Action<Vector2[]> OnHandsUpdated;

        [Header("MediaPipe Components")]
        [SerializeField] private Mediapipe.Unity.Screen _screen;
        
        [Header("Detection Settings")]
        public string modelPath = "face_landmarker_v2_with_blendshapes.bytes";
        public float maskDetectionDelay = 1.5f; // Tiempo que debe desaparecer la cara para confirmar máscara
        
        [Header("Debug Settings")]
        public bool useWebcam = true;
        [SerializeField] private bool mockFaceDetected = false;
        
        private ImageSource _imageSource;
        private FaceLandmarker _faceLandmarker;
        private Mediapipe.Unity.Experimental.TextureFramePool _textureFramePool;
        private bool faceSeenOnce = false;
        private bool _isFaceDetectedReal = false;
        private float _faceLostTimer = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private IEnumerator Start()
        {
            if (useWebcam)
            {
                yield return InitializeWebcam();
            }
        }

        private IEnumerator InitializeWebcam()
        {
            // Wait for Bootstrap to finish if it exists
            Bootstrap bootstrap = FindFirstObjectByType<Bootstrap>();
            if (bootstrap != null)
            {
                yield return new WaitUntil(() => bootstrap.isFinished);
            }

            _imageSource = ImageSourceProvider.ImageSource;
            if (_imageSource != null)
            {
                yield return _imageSource.Play();
                
                // CRITICAL: Wait until texture is actually ready
                yield return new WaitUntil(() => _imageSource.textureWidth > 0);

                if (_imageSource.isPrepared && _screen != null)
                {
                    _screen.Initialize(_imageSource);
                }

                // Initialize FaceLandmarker
                yield return InitializeFaceLandmarker();
                
                // Start Processing Loop
                StartCoroutine(ProcessFrames());
            }
        }

        private IEnumerator InitializeFaceLandmarker()
        {
            yield return AssetLoader.PrepareAssetAsync(modelPath);
            
            var options = new FaceLandmarkerOptions(
                new Mediapipe.Tasks.Core.BaseOptions(Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU, modelAssetPath: modelPath),
                runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE,
                numFaces: 1
            );
            
            _faceLandmarker = FaceLandmarker.CreateFromOptions(options);
            _textureFramePool = new Mediapipe.Unity.Experimental.TextureFramePool(_imageSource.textureWidth, _imageSource.textureHeight, TextureFormat.RGBA32, 5);
        }

        private IEnumerator ProcessFrames()
        {
            var waitForEndOfFrame = new WaitForEndOfFrame();
            var result = FaceLandmarkerResult.Alloc(1);

            while (_faceLandmarker != null)
            {
                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return null;
                    continue;
                }

                yield return waitForEndOfFrame;
                textureFrame.ReadTextureOnCPU(_imageSource.GetCurrentTexture());
                var image = textureFrame.BuildCPUImage();
                
                if (_faceLandmarker.TryDetect(image, null, ref result))
                {
                    _isFaceDetectedReal = (result.faceLandmarks != null && result.faceLandmarks.Count > 0);
                }
                else
                {
                    _isFaceDetectedReal = false;
                }

                textureFrame.Release();
                yield return new WaitForSeconds(0.1f); // Reduce CPU load
            }
        }

        private void Update()
        {
            HandleDebugInput();
            
            // Ensure Screen Texture is always up to date (Fallback if Initialize was too early)
            if (_screen != null && _imageSource != null && _imageSource.isPrepared)
            {
                if (_screen.texture == null) _screen.texture = _imageSource.GetCurrentTexture();
            }

            UpdateDetectionLogic();
        }

        private void HandleDebugInput()
        {
            if (!useWebcam && Keyboard.current != null)
            {
                if (Keyboard.current.fKey.wasPressedThisFrame) // Simulate Face Detected
                {
                    mockFaceDetected = true;
                    Debug.Log("[Debug] Face Detected simulated");
                }
                if (Keyboard.current.mKey.wasPressedThisFrame) // Simulate Mask Put On (Face Lost)
                {
                    mockFaceDetected = false;
                    Debug.Log("[Debug] Mask Put On simulated");
                }
            }
        }

        private void UpdateDetectionLogic()
        {
            bool isFaceCurrentlyDetected = GetFaceDetectionStatus();

            switch (CurrentState)
            {
                case DetectionState.WaitingForFace:
                    if (isFaceCurrentlyDetected)
                    {
                        faceSeenOnce = true;
                        CurrentState = DetectionState.WaitingForMask;
                        OnFaceDetected?.Invoke();
                        Debug.Log("Face detected! Now: PONTE LA MASCARA");
                    }
                    break;

                case DetectionState.WaitingForMask:
                    if (!isFaceCurrentlyDetected && faceSeenOnce)
                    {
                        _faceLostTimer += Time.deltaTime;
                        if (_faceLostTimer >= maskDetectionDelay)
                        {
                            CurrentState = DetectionState.InGame;
                            OnMaskPutOn?.Invoke();
                            Debug.Log("Mask confirmed! Starting Game...");
                        }
                    }
                    else
                    {
                        _faceLostTimer = 0f; // Face returned or still seen
                    }
                    break;
                
                case DetectionState.InGame:
                    // Hand tracking logic will go here
                    break;
            }
        }

        private bool GetFaceDetectionStatus()
        {
            if (!useWebcam)
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.fKey.wasPressedThisFrame) mockFaceDetected = true;
                    if (Keyboard.current.mKey.wasPressedThisFrame) mockFaceDetected = false;
                }
                return mockFaceDetected;
            }
            
            return _isFaceDetectedReal; 
        }

        private void OnDestroy()
        {
            _faceLandmarker?.Close();
            _textureFramePool?.Dispose();
        }

        // Method to be called by MediaPipe Hand Task
        public void UpdateHandData(Vector2[] handPositions)
        {
            if (CurrentState == DetectionState.InGame)
            {
                OnHandsUpdated?.Invoke(handPositions);
            }
        }
    }
}
