using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Il2CppInterop.Runtime.Injection;

namespace CombatAnalytics.UI
{
    public class StandaloneUIManager
    {
        private static StandaloneUIManager _instance;
        public static StandaloneUIManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new StandaloneUIManager();
                return _instance;
            }
        }

        private GameObject _uiRoot;
        private Canvas _canvas;
        private GameObject _dpsPanelGameObject; // Store GameObject instead of DpsPanel component
        private DpsPanel _dpsPanel;
        private bool _initialized;

        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                Plugin.Instance.Log.LogInfo("Initializing Standalone UI Manager...");

                // Ensure EventSystem exists (required for UI interaction)
                EnsureEventSystem();

                // Register IL2CPP types
                ClassInjector.RegisterTypeInIl2Cpp<DpsPanel>();
                ClassInjector.RegisterTypeInIl2Cpp<DraggablePanel>();
                ClassInjector.RegisterTypeInIl2Cpp<UICommandQueue>();

                // Initialize the command queue (creates the MonoBehaviour for Update loop)
                var _ = UICommandQueue.Instance;
                Plugin.Instance.Log.LogInfo("UICommandQueue initialized.");

                // Create UI Root (just a container, no Canvas on it)
                _uiRoot = new GameObject("CombatAnalytics_UI");
                _uiRoot.layer = 5; // UI layer
                _uiRoot.SetActive(true); // Ensure it's active
                _uiRoot.hideFlags |= HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(_uiRoot);

                Plugin.Instance.Log.LogInfo($"UI Root created. Active: {_uiRoot.activeInHierarchy}");

                // Note: We don't add Canvas to the root, we add it to individual panels
                // This matches ZUI's approach

                Plugin.Instance.Log.LogInfo("CanvasScaler configured.");

                Plugin.Instance.Log.LogInfo("GraphicRaycaster added.");

                _initialized = true;
                Plugin.Instance.Log.LogInfo("Standalone UI Manager initialized successfully.");
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"Failed to initialize Standalone UI Manager: {ex}");
            }
        }

        private void EnsureEventSystem()
        {
            try
            {
                var existingEventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
                if (existingEventSystem != null)
                {
                    Plugin.Instance.Log.LogInfo("EventSystem already exists in scene.");
                    return;
                }

                Plugin.Instance.Log.LogInfo("No EventSystem found, creating one...");
                var eventSystemObj = new GameObject("CombatAnalytics_EventSystem");
                UnityEngine.Object.DontDestroyOnLoad(eventSystemObj);
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Plugin.Instance.Log.LogInfo("EventSystem created.");
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogWarning($"Failed to ensure EventSystem: {ex}");
            }
        }

        public void ToggleDpsPanel()
        {
            UnityEngine.Debug.Log("[CombatAnalytics] StandaloneUIManager.ToggleDpsPanel called");
            
            if (!_initialized)
            {
                UnityEngine.Debug.LogWarning("[CombatAnalytics] Not initialized, initializing now");
                Initialize();
            }

            try
            {
                UnityEngine.Debug.Log($"[CombatAnalytics] Panel state: {(_dpsPanelGameObject != null ? "exists" : "null")}");
                
                if (_dpsPanelGameObject == null)
                {
                    UnityEngine.Debug.Log("[CombatAnalytics] Creating panel...");
                    CreateDpsPanel();
                    // Don't toggle after creation - leave it visible
                    UnityEngine.Debug.Log("[CombatAnalytics] Panel created and left visible");
                    return;
                }
                
                // Panel exists, toggle it
                bool currentState = _dpsPanelGameObject.activeSelf;
                bool newState = !currentState;
                UnityEngine.Debug.Log($"[CombatAnalytics] Toggling from {currentState} to {newState}");
                _dpsPanelGameObject.SetActive(newState);
                UnityEngine.Debug.Log($"[CombatAnalytics] Toggled to: {newState}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CombatAnalytics] Toggle failed: {ex}");
            }
        }

        private UnityEngine.UI.Text _dpsTextComponent; // Store text component for updates

        private void CreateDpsPanel()
        {
            try
            {
                UnityEngine.Debug.Log("[CombatAnalytics] Creating professional DPS Panel...");
                
                // Create canvas
                var canvasObj = new GameObject("DpsPanelCanvas");
                UnityEngine.Object.DontDestroyOnLoad(canvasObj);
                canvasObj.layer = 5;
                
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 32767;
                _canvas.overrideSorting = true;
                
                canvasObj.AddComponent<GraphicRaycaster>();
                
                // Panel background
                var panelObj = new GameObject("DpsPanel");
                panelObj.transform.SetParent(canvasObj.transform, false);
                panelObj.layer = 5;
                
                var rt = panelObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(300, 200); // Top right area
                rt.sizeDelta = new Vector2(300, 200);
                
                var bg = panelObj.AddComponent<Image>();
                bg.color = new Color(0.05f, 0.05f, 0.05f, 0.92f); // Very dark, slightly transparent
                
                // Title bar (with drag functionality)
                var titleObj = new GameObject("Title");
                titleObj.transform.SetParent(panelObj.transform, false);
                titleObj.layer = 5;
                
                var titleRect = titleObj.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0, 1);
                titleRect.anchorMax = new Vector2(1, 1);
                titleRect.pivot = new Vector2(0.5f, 1);
                titleRect.anchoredPosition = Vector2.zero;
                titleRect.sizeDelta = new Vector2(0, 30);
                
                var titleBg = titleObj.AddComponent<Image>();
                titleBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
                
                // Make entire panel draggable (attach to panel, not title bar, to avoid IL2CPP issues)
                var dragger = panelObj.AddComponent<DraggablePanel>();
                
                // Title TEXT as a CHILD (can't have Image and Text on same GameObject)
                var titleTextObj = new GameObject("TitleText");
                titleTextObj.transform.SetParent(titleObj.transform, false);
                titleTextObj.layer = 5;
                
                var titleTextRect = titleTextObj.AddComponent<RectTransform>();
                titleTextRect.anchorMin = new Vector2(0, 0);
                titleTextRect.anchorMax = new Vector2(1, 1);
                titleTextRect.anchoredPosition = Vector2.zero;
                titleTextRect.sizeDelta = Vector2.zero;
                titleTextRect.offsetMax = new Vector2(-30, 0); // Leave space for close button
                
                var titleText = titleTextObj.AddComponent<UnityEngine.UI.Text>();
                titleText.text = "DPS METER";
                titleText.fontSize = 14;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = new Color(0.9f, 0.9f, 1f, 1f);
                titleText.fontStyle = FontStyle.Bold;
                titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                
                // Close button (X) in top-right corner
                var closeBtn = new GameObject("CloseButton");
                closeBtn.transform.SetParent(titleObj.transform, false);
                closeBtn.layer = 5;
                
                var closeBtnRect = closeBtn.AddComponent<RectTransform>();
                closeBtnRect.anchorMin = new Vector2(1, 0.5f);
                closeBtnRect.anchorMax = new Vector2(1, 0.5f);
                closeBtnRect.pivot = new Vector2(1, 0.5f);
                closeBtnRect.anchoredPosition = new Vector2(-5, 0);
                closeBtnRect.sizeDelta = new Vector2(20, 20);
                
                var closeBtnImage = closeBtn.AddComponent<Image>();
                closeBtnImage.color = new Color(0.8f, 0.3f, 0.3f, 0.8f);
                
                var closeBtnButton = closeBtn.AddComponent<Button>();
                closeBtnButton.targetGraphic = closeBtnImage;
                closeBtnButton.onClick.AddListener((UnityEngine.Events.UnityAction)OnCloseClick);
                
                var closeBtnTextObj = new GameObject("X");
                closeBtnTextObj.transform.SetParent(closeBtn.transform, false);
                closeBtnTextObj.layer = 5;
                
                var closeBtnTextRect = closeBtnTextObj.AddComponent<RectTransform>();
                closeBtnTextRect.anchorMin = Vector2.zero;
                closeBtnTextRect.anchorMax = Vector2.one;
                closeBtnTextRect.anchoredPosition = Vector2.zero;
                closeBtnTextRect.sizeDelta = Vector2.zero;
                
                var closeBtnText = closeBtnTextObj.AddComponent<UnityEngine.UI.Text>();
                closeBtnText.text = "X";
                closeBtnText.fontSize = 14;
                closeBtnText.alignment = TextAnchor.MiddleCenter;
                closeBtnText.color = Color.white;
                closeBtnText.fontStyle = FontStyle.Bold;
                closeBtnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                
                // Stats text
                var textObj = new GameObject("DpsText");
                textObj.transform.SetParent(panelObj.transform, false);
                textObj.layer = 5;
                
                var textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.05f, 0);
                textRect.anchorMax = new Vector2(0.95f, 1);
                textRect.anchoredPosition = Vector2.zero;
                textRect.sizeDelta = new Vector2(0, -35); // Leave space for title bar (30px + 5px margin)
                textRect.offsetMin = new Vector2(10, 35); // Left and bottom padding
                textRect.offsetMax = new Vector2(-10, -35); // Right and top padding
                
                _dpsTextComponent = textObj.AddComponent<UnityEngine.UI.Text>();
                _dpsTextComponent.text = "Damage: 0\nDPS: 0.0\nTime: 0s\n\nPress F9 to toggle";
                _dpsTextComponent.fontSize = 16;
                _dpsTextComponent.alignment = TextAnchor.UpperLeft;
                _dpsTextComponent.color = new Color(1f, 1f, 1f, 0.95f);
                _dpsTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                
                // Button container
                var buttonContainer = new GameObject("Buttons");
                buttonContainer.transform.SetParent(panelObj.transform, false);
                buttonContainer.layer = 5;
                
                var btnContainerRect = buttonContainer.AddComponent<RectTransform>();
                btnContainerRect.anchorMin = new Vector2(0, 0);
                btnContainerRect.anchorMax = new Vector2(1, 0);
                btnContainerRect.pivot = new Vector2(0.5f, 0);
                btnContainerRect.anchoredPosition = Vector2.zero;
                btnContainerRect.sizeDelta = new Vector2(0, 30);
                
                // Reset button
                CreateButton(buttonContainer, "Reset", new Vector2(0.05f, 0.5f), new Vector2(0.47f, 0.5f), null);
                
                // Close button
                CreateButton(buttonContainer, "Close", new Vector2(0.53f, 0.5f), new Vector2(0.95f, 0.5f), null);
                
                _dpsPanelGameObject = panelObj;
                
                UnityEngine.Debug.Log("[CombatAnalytics] Professional DPS panel created!");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CombatAnalytics] CreateDpsPanel failed: {ex}");
                UnityEngine.Debug.LogError($"[CombatAnalytics] Stack: {ex.StackTrace}");
            }
        }

        private void CreateButton(GameObject parent, string label, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
        {
            var btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent.transform, false);
            btnObj.layer = 5;
            
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = anchorMin;
            btnRect.anchorMax = anchorMax;
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = Vector2.zero;
            
            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            
            var button = btnObj.AddComponent<Button>();
            button.targetGraphic = btnImage;
            
            // Store callback reference
            if (label == "Reset")
            {
                button.onClick.AddListener((UnityEngine.Events.UnityAction)OnResetClick);
            }
            else if (label == "Close")
            {
                button.onClick.AddListener((UnityEngine.Events.UnityAction)OnCloseClick);
            }
            
            var btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            btnTextObj.layer = 5;
            
            var btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.anchoredPosition = Vector2.zero;
            btnTextRect.sizeDelta = Vector2.zero;
            
            var btnText = btnTextObj.AddComponent<UnityEngine.UI.Text>();
            btnText.text = label;
            btnText.fontSize = 12;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void OnResetClick()
        {
            Services.DpsTracker.Reset();
        }

        private void OnCloseClick()
        {
            if (_dpsPanelGameObject != null)
                _dpsPanelGameObject.SetActive(false);
        }

        public void UpdateDpsText(string text)
        {
            if (_dpsTextComponent != null)
            {
                _dpsTextComponent.text = text;
            }
            // Silently skip if component is null (panel is toggled off or not created yet)
        }

        public string GetDebugInfo()
        {
            var info = "";
            info += $"- Canvas exists: {_canvas != null}\n";
            info += $"- Canvas GameObject: {(_canvas != null ? _canvas.gameObject.name : "null")}\n";
            info += $"- Canvas active: {(_canvas != null ? _canvas.gameObject.activeInHierarchy.ToString() : "null")}\n";
            info += $"- DPS Panel exists: {_dpsPanel != null}\n";
            info += $"- DPS Panel GameObject: {(_dpsPanel != null ? _dpsPanel.gameObject.name : "null")}\n";
            info += $"- DPS Panel active: {(_dpsPanel != null ? _dpsPanel.gameObject.activeInHierarchy.ToString() : "null")}\n";
            info += $"- UI Root exists: {_uiRoot != null}\n";
            info += $"- UI Root active: {(_uiRoot != null ? _uiRoot.activeInHierarchy.ToString() : "null")}\n";
            return info;
        }

        public void Cleanup()
        {
            if (_uiRoot != null)
            {
                UnityEngine.Object.Destroy(_uiRoot);
            }
            _initialized = false;
            _dpsPanel = null;
        }
    }
}
