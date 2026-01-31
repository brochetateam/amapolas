using UnityEngine;
using UnityEngine.InputSystem;
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
        public event Action OnMaskPutOn; // Triggered when face is lost after detection
        public event Action<Vector2[]> OnHandsUpdated;

        [Header("Debug Settings")]
        public bool useWebcam = true;
        [SerializeField] private bool mockFaceDetected = false;

        private bool faceSeenOnce = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            HandleDebugInput();
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
                        CurrentState = DetectionState.InGame;
                        OnMaskPutOn?.Invoke();
                        Debug.Log("Mask detected (Face lost)! Starting Game...");
                    }
                    break;
                
                case DetectionState.InGame:
                    // Hand tracking logic will go here
                    break;
            }
        }

        private bool GetFaceDetectionStatus()
        {
            if (!useWebcam) return mockFaceDetected;
            
            // TODO: Integrar con MediaPipe FaceLandmarker Task
            // Por ahora devolvemos falso para no romper el flujo
            return false;
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
