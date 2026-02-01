using UnityEngine;
using UnityEngine.UI;
using TMPro; // Assuming TextMeshPro is available, fallback to legacy if not
using System.Collections;

namespace Amapolas.Managers
{
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        [Header("UI Elements")]
        public GameObject messagePanel;
        public TextMeshProUGUI statusText;
        public Image hitOverlay;
        public Image blockOverlay;
        public Slider healthSlider;
        public TextMeshProUGUI wordText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            
            // Try to find TMP if not assigned
            if (statusText == null) statusText = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Start()
        {
            if (DetectionManager.Instance != null)
            {
                DetectionManager.Instance.OnFaceDetected += ShowMaskMessage;
                DetectionManager.Instance.OnMaskPutOn += HideMessages;
            }

            ShowInitialMessage();
        }

        public void ShowInitialMessage()
        {
            if (statusText != null)
            {
                statusText.text = "PONTE DELANTE DE LA CAMARA";
                messagePanel.SetActive(true);
            }
        }

        public void ShowMaskMessage()
        {
            if (statusText != null)
            {
                statusText.text = "PONTE LA MASCARA";
                messagePanel.SetActive(true);
                StartCoroutine(BlinkEffect());
            }
        }

        public void HideMessages()
        {
            if (messagePanel != null) messagePanel.SetActive(false);
        }

        public void DisplayWord(string word, Color color)
        {
            if (wordText != null)
            {
                wordText.text = word;
                wordText.color = color;
                wordText.gameObject.SetActive(true);
            }
        }

        public void ClearWord()
        {
            if (wordText != null)
            {
                wordText.gameObject.SetActive(false);
            }
        }

        public void TriggerHitFeedback()
        {
            if (hitOverlay != null) StartCoroutine(FlashOverlay(hitOverlay, new Color(1, 0, 0, 0.5f)));
        }

        public void TriggerHealFeedback()
        {
            if (hitOverlay != null) StartCoroutine(FlashOverlay(hitOverlay, new Color(1, 0.8f, 0, 0.3f))); // Golden flash for heal
        }

        public void TriggerBlockFeedback()
        {
            if (blockOverlay != null) StartCoroutine(FlashOverlay(blockOverlay, new Color(0, 1, 0, 0.5f)));
        }

        public void UpdateHealth(float current, float max)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = max;
                healthSlider.value = current;
            }
        }

        IEnumerator FlashOverlay(Image img, Color color)
        {
            img.color = color;
            img.gameObject.SetActive(true);
            float duration = 0.2f;
            float elapsed = 0f;
            float startAlpha = color.a;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(startAlpha, 0f, elapsed / duration));
                yield return null;
            }
            img.gameObject.SetActive(false);
        }

        IEnumerator BlinkEffect()
        {
            while (DetectionManager.Instance.CurrentState == DetectionState.WaitingForMask)
            {
                statusText.alpha = 1f;
                yield return new WaitForSeconds(0.5f);
                statusText.alpha = 0.3f;
                yield return new WaitForSeconds(0.5f);
            }
            statusText.alpha = 1f;
        }
    }
}
