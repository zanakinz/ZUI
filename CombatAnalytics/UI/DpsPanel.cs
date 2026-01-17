using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CombatAnalytics.UI
{
    public class DpsPanel : MonoBehaviour
    {
        private TextMeshProUGUI _dpsText;
        private RectTransform _rectTransform;
        private DraggablePanel _draggable;

        public DpsPanel(IntPtr ptr) : base(ptr) { }

        public void Initialize()
        {
            try
            {
                Plugin.Instance.Log.LogInfo("Starting DPS Panel initialization...");

                if (gameObject == null)
                {
                    Plugin.Instance.Log.LogError("GameObject is null in DpsPanel.Initialize!");
                    return;
                }

                // Get the RectTransform (should already exist from GameObject creation)
                _rectTransform = gameObject.GetComponent<RectTransform>();
                if (_rectTransform == null)
                {
                    _rectTransform = gameObject.AddComponent<RectTransform>();
                }

                // Set layer to UI
                gameObject.layer = 5;

                // Setup anchoring and size - MATCH THE WORKING RED BOX PATTERN
                _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _rectTransform.anchoredPosition = Vector2.zero;
                _rectTransform.sizeDelta = new Vector2(400, 300);

                Plugin.Instance.Log.LogInfo("RectTransform configured.");

                // Add background
                var bg = gameObject.AddComponent<Image>();
                if (bg == null)
                {
                    Plugin.Instance.Log.LogError("Failed to add Image component!");
                    return;
                }
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
                bg.raycastTarget = true;

                Plugin.Instance.Log.LogInfo("Background added.");

                // Add vertical layout
                var layout = gameObject.AddComponent<VerticalLayoutGroup>();
                if (layout == null)
                {
                    Plugin.Instance.Log.LogError("Failed to add VerticalLayoutGroup!");
                    return;
                }
                var padding = new RectOffset();
                padding.left = 10;
                padding.right = 10;
                padding.top = 10;
                padding.bottom = 10;
                layout.padding = padding;
                layout.spacing = 5;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlHeight = false;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;

                Plugin.Instance.Log.LogInfo("Layout configured.");

                // Create title bar
                CreateTitleBar();

                // Create DPS text display
                CreateDpsDisplay();

                // Create buttons
                CreateButtons();

                // Make draggable
                _draggable = gameObject.AddComponent<DraggablePanel>();
                if (_draggable == null)
                {
                    Plugin.Instance.Log.LogWarning("Failed to add DraggablePanel component, panel will not be draggable");
                }
                else
                {
                    Plugin.Instance.Log.LogInfo("DraggablePanel added successfully.");
                }

                Plugin.Instance.Log.LogInfo("DPS Panel initialized successfully!");
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"Failed to initialize DPS panel: {ex}");
            }
        }

        private void CreateTitleBar()
        {
            try
            {
                var titleObj = new GameObject("Title");
                if (titleObj == null)
                {
                    Plugin.Instance.Log.LogError("Failed to create title GameObject!");
                    return;
                }
                titleObj.layer = 5;
                titleObj.transform.SetParent(transform, false);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 30);

            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "DPS Meter";
            titleText.fontSize = 18;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;
            titleText.raycastTarget = false;
            titleText.font = FontManager.DefaultFont;
            if (FontManager.DefaultFontMaterial != null)
            {
                titleText.fontMaterial = FontManager.DefaultFontMaterial;
            }

            var titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.minHeight = 30;
            titleLayout.preferredHeight = 30;

                Plugin.Instance.Log.LogInfo("Title bar created.");
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"Failed to create title bar: {ex}");
            }
        }

        private void CreateDpsDisplay()
        {
            try
            {
                var displayObj = new GameObject("DpsDisplay");
                if (displayObj == null)
                {
                    Plugin.Instance.Log.LogError("Failed to create DpsDisplay GameObject!");
                    return;
                }
                displayObj.layer = 5;
                displayObj.transform.SetParent(transform, false);

            var displayRect = displayObj.AddComponent<RectTransform>();
            displayRect.sizeDelta = new Vector2(0, 100);

            _dpsText = displayObj.AddComponent<TextMeshProUGUI>();
            _dpsText.text = "Ready...\nDamage: 0\nDPS: 0.0";
            _dpsText.fontSize = 14;
            _dpsText.alignment = TextAlignmentOptions.TopLeft;
            _dpsText.color = Color.white;
            _dpsText.raycastTarget = false;
            _dpsText.font = FontManager.DefaultFont;
            if (FontManager.DefaultFontMaterial != null)
            {
                _dpsText.fontMaterial = FontManager.DefaultFontMaterial;
            }

            var displayLayout = displayObj.AddComponent<LayoutElement>();
            displayLayout.flexibleHeight = 1;
            displayLayout.minHeight = 100;

                Plugin.Instance.Log.LogInfo("DPS display created.");
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"Failed to create DPS display: {ex}");
            }
        }

        private void CreateButtons()
        {
            try
            {
                var buttonContainer = new GameObject("ButtonContainer");
                if (buttonContainer == null)
                {
                    Plugin.Instance.Log.LogError("Failed to create ButtonContainer GameObject!");
                    return;
                }
                buttonContainer.layer = 5;
                buttonContainer.transform.SetParent(transform, false);

            var buttonRect = buttonContainer.AddComponent<RectTransform>();

            var buttonLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 5;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;

            var containerLayoutElement = buttonContainer.AddComponent<LayoutElement>();
            containerLayoutElement.minHeight = 30;
            containerLayoutElement.preferredHeight = 30;

            // Reset button
            CreateResetButton(buttonContainer);

            // Close button
            CreateCloseButton(buttonContainer);

                Plugin.Instance.Log.LogInfo("Buttons created.");
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"Failed to create buttons: {ex}");
            }
        }

        private void CreateResetButton(GameObject parent)
        {
            var buttonObj = new GameObject("Button_Reset");
            buttonObj.layer = 5;
            buttonObj.transform.SetParent(parent.transform, false);

            var buttonRect = buttonObj.AddComponent<RectTransform>();

            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            buttonImage.raycastTarget = true;

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(new Action(OnResetClicked));

            var buttonText = new GameObject("Text");
            buttonText.layer = 5;
            buttonText.transform.SetParent(buttonObj.transform, false);

            var textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComp = buttonText.AddComponent<TextMeshProUGUI>();
            textComp.text = "Reset";
            textComp.fontSize = 14;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.color = Color.white;
            textComp.raycastTarget = false;
            textComp.font = FontManager.DefaultFont;
            if (FontManager.DefaultFontMaterial != null)
            {
                textComp.fontMaterial = FontManager.DefaultFontMaterial;
            }

            var buttonLayoutElement = buttonObj.AddComponent<LayoutElement>();
            buttonLayoutElement.minHeight = 25;
            buttonLayoutElement.preferredHeight = 25;
            buttonLayoutElement.flexibleWidth = 1;
        }

        private void CreateCloseButton(GameObject parent)
        {
            var buttonObj = new GameObject("Button_Close");
            buttonObj.layer = 5;
            buttonObj.transform.SetParent(parent.transform, false);

            var buttonRect = buttonObj.AddComponent<RectTransform>();

            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            buttonImage.raycastTarget = true;

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(new Action(OnCloseClicked));

            var buttonText = new GameObject("Text");
            buttonText.layer = 5;
            buttonText.transform.SetParent(buttonObj.transform, false);

            var textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComp = buttonText.AddComponent<TextMeshProUGUI>();
            textComp.text = "Close";
            textComp.fontSize = 14;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.color = Color.white;
            textComp.raycastTarget = false;
            textComp.font = FontManager.DefaultFont;
            if (FontManager.DefaultFontMaterial != null)
            {
                textComp.fontMaterial = FontManager.DefaultFontMaterial;
            }

            var buttonLayoutElement = buttonObj.AddComponent<LayoutElement>();
            buttonLayoutElement.minHeight = 25;
            buttonLayoutElement.preferredHeight = 25;
            buttonLayoutElement.flexibleWidth = 1;
        }

        private void OnResetClicked()
        {
            UpdateDpsText("Reset\nDamage: 0\nDPS: 0.0");
            Plugin.Instance.Log.LogInfo("DPS Reset.");
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
            Plugin.Instance.Log.LogInfo("DPS Panel closed.");
        }

        public void UpdateDpsText(string text)
        {
            if (_dpsText != null)
            {
                _dpsText.text = text;
            }
        }
    }
}
