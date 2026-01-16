using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveMusic
{
    /// <summary>
    ///     Simple UI notification system for music system errors.
    ///     Displays a temporary message panel when music generation fails.
    /// </summary>
    public class MusicErrorNotification : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private Text messageText;
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private bool autoHide = true;
        
        private static MusicErrorNotification instance;
        private Coroutine hideCoroutine;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Ensure panel starts hidden
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[MusicErrorNotification] Notification panel not assigned, creating default UI");
                CreateDefaultUI();
            }
        }
        
        /// <summary>
        ///     Show an error notification.
        /// </summary>
        public static void ShowError(string message)
        {
            if (instance == null)
            {
                Debug.LogWarning("[MusicErrorNotification] No instance available");
                return;
            }
            
            instance.DisplayError(message);
        }
        
        /// <summary>
        ///     Display the error message.
        /// </summary>
        private void DisplayError(string message)
        {
            if (notificationPanel == null || messageText == null)
            {
                Debug.LogError($"[MusicErrorNotification] UI components missing. Error: {message}");
                return;
            }
            
            // Stop any existing hide coroutine
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }
            
            // Update message and show panel
            messageText.text = $"Music System Error: {message}";
            notificationPanel.SetActive(true);
            
            Debug.Log($"[MusicErrorNotification] Displayed error: {message}");
            
            // Auto-hide after duration
            if (autoHide)
            {
                hideCoroutine = StartCoroutine(HideAfterDelay());
            }
        }
        
        /// <summary>
        ///     Hide the notification panel after a delay.
        /// </summary>
        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            Hide();
        }
        
        /// <summary>
        ///     Manually hide the notification.
        /// </summary>
        public void Hide()
        {
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }
            
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
        }
        
        /// <summary>
        ///     Create a default UI if none is assigned.
        /// </summary>
        private void CreateDefaultUI()
        {
            // Create canvas if needed
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("ErrorNotificationCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            
            // Create notification panel
            var panelGO = new GameObject("NotificationPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            
            var rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0, -20);
            rectTransform.sizeDelta = new Vector2(400, 80);
            
            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f); // Red background
            
            // Create text
            var textGO = new GameObject("MessageText");
            textGO.transform.SetParent(panelGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            
            var text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "Music Error";
            
            notificationPanel = panelGO;
            messageText = text;
            notificationPanel.SetActive(false);
            
            Debug.Log("[MusicErrorNotification] Created default UI");
        }
    }
}
