using ZUI.Config;
using ZUI.Services;
using ZUI.UI.CustomLib.Controls;
using ZUI.UI.CustomLib.Panel;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModContent.Data;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;
using ZUI.UI.UniverseLib.UI.Panels;
using ZUI.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZUI.UI.ModContent
{
    internal class SignsPanel : ResizeablePanelBase
    {
        public override string PanelId => "SignsPanel";
        public override int MinWidth => 320;
        public override int MinHeight => 550;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);
        public override Vector2 DefaultPivot => new Vector2(0.5f, 0.5f);
        public override bool CanDrag => true;
        public override PanelDragger.ResizeTypes CanResize => PanelDragger.ResizeTypes.All;
        public override PanelType PanelType => PanelType.Signs;
        public override float Opacity => Settings.UITransparency;

        private GameObject _contentLayout;
        private ColorPickerControl _colorPicker;
        private InputFieldRef _textInput;
        private InputFieldRef _sizeInput;
        private string _selectedHex = "FFFFFF";

        // Sprite Cache
        private Sprite _btnNormalSprite;
        private Sprite _btnSelectedSprite;

        public SignsPanel(UIBase owner) : base(owner)
        {
        }

        protected override void ConstructPanelContent()
        {
            // --- LOAD SPRITES ---
            var panelSprite = SpriteLoader.LoadSprite("panel.png", 100f, new Vector4(30, 30, 30, 30));
            _btnNormalSprite = SpriteLoader.LoadSprite("button.png", 100f, new Vector4(10, 10, 10, 10));
            _btnSelectedSprite = SpriteLoader.LoadSprite("button_selected.png", 100f, new Vector4(10, 10, 10, 10));

            // Apply Panel Background
            if (panelSprite != null)
            {
                var bgImage = ContentRoot.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.sprite = panelSprite;
                    bgImage.type = Image.Type.Sliced;
                    bgImage.color = Color.white;
                }
            }

            SetTitle("Sign Creator");

            // --- SCROLLVIEW SETUP ---
            var scrollView = UIFactory.CreateScrollView(ContentRoot, "SignsScrollView", out _contentLayout, out var autoScroll,
                new Color(0.05f, 0.05f, 0.05f, 0f));

            UIFactory.SetLayoutElement(scrollView, flexibleWidth: 9999, flexibleHeight: 9999);

            // Layout Group settings
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(_contentLayout, false, false, true, true, 8, 10, 10, 10, 10);

            // Check for ScarletSigns dependency
            if (!DependencyService.HasScarletSigns)
            {
                CreateMissingDependencyMessage("ScarletSigns");
                return;
            }

            // Instructions
            var instructionLabel = UIFactory.CreateLabel(_contentLayout, "Instructions",
                "Create custom colored signs in the game world", TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(instructionLabel.GameObject, minHeight: 25, flexibleWidth: 9999);
            instructionLabel.TextMesh.fontSize = 11;
            instructionLabel.TextMesh.color = new Color(0.8f, 0.8f, 0.8f, 1f);

            // Color picker
            _colorPicker = new ColorPickerControl(_contentLayout);
            _colorPicker.OnColorChanged += (color) =>
            {
                _selectedHex = _colorPicker.HexColor;
            };

            // Text input section
            var textLabel = UIFactory.CreateLabel(_contentLayout, "TextLabel", "Sign Text:", TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(textLabel.GameObject, minHeight: 20, flexibleWidth: 9999);
            textLabel.TextMesh.fontStyle = FontStyles.Bold;

            _textInput = UIFactory.CreateInputField(_contentLayout, "TextInput", "Enter sign text...");
            UIFactory.SetLayoutElement(_textInput.GameObject, minHeight: 30, flexibleWidth: 9999);
            _textInput.Component.characterLimit = 50;

            // Size input section
            var sizeLabel = UIFactory.CreateLabel(_contentLayout, "SizeLabel", "Sign Size (1-10):", TextAlignmentOptions.Left);
            UIFactory.SetLayoutElement(sizeLabel.GameObject, minHeight: 20, flexibleWidth: 9999);
            sizeLabel.TextMesh.fontStyle = FontStyles.Bold;

            _sizeInput = UIFactory.CreateInputField(_contentLayout, "SizeInput", "5");
            UIFactory.SetLayoutElement(_sizeInput.GameObject, minHeight: 30, flexibleWidth: 9999);
            _sizeInput.Component.characterLimit = 2;
            _sizeInput.Component.contentType = TMPro.TMP_InputField.ContentType.IntegerNumber;

            // Action buttons
            var buttonContainer = UIFactory.CreateHorizontalGroup(_contentLayout, "ButtonContainer", false, false, true, true, 5);
            UIFactory.SetLayoutElement(buttonContainer, minHeight: 40, flexibleWidth: 9999);

            var createBtn = UIFactory.CreateButton(buttonContainer, "CreateBtn", "Create Sign");
            UIFactory.SetLayoutElement(createBtn.GameObject, minHeight: 40, flexibleWidth: 9999);
            StyleButton(createBtn); // Apply Style
            createBtn.OnClick = OnCreateSign;

            var removeBtn = UIFactory.CreateButton(buttonContainer, "RemoveBtn", "Remove Sign");
            UIFactory.SetLayoutElement(removeBtn.GameObject, minHeight: 40, flexibleWidth: 9999);
            StyleButton(removeBtn); // Apply Style
            removeBtn.OnClick = OnRemoveSign;

            // Help text
            var helpLabel = UIFactory.CreateLabel(_contentLayout, "Help",
                "Signs will be created at your current position. Use Remove to delete the nearest sign.",
                TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(helpLabel.GameObject, minHeight: 40, flexibleWidth: 9999);
            helpLabel.TextMesh.fontSize = 10;
            helpLabel.TextMesh.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        private void CreateMissingDependencyMessage(string dependencyName)
        {
            var warningLabel = UIFactory.CreateLabel(_contentLayout, "MissingDependency",
                $"<color=#FF6B6B>? {dependencyName} Not Detected</color>\n\n" +
                $"This feature requires the {dependencyName} mod to be installed on the server.\n\n" +
                "Please contact your server administrator or install the required mod.",
                TextAlignmentOptions.Center);
            UIFactory.SetLayoutElement(warningLabel.GameObject, minHeight: 200, flexibleWidth: 9999);
            warningLabel.TextMesh.fontSize = 14;
            warningLabel.TextMesh.fontStyle = FontStyles.Bold;
        }

        private void OnCreateSign()
        {
            var text = _textInput.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                Plugin.LogInstance.LogWarning("Sign text cannot be empty");
                return;
            }

            var sizeText = _sizeInput.Text;
            if (!int.TryParse(sizeText, out int size))
            {
                Plugin.LogInstance.LogWarning("Invalid sign size");
                return;
            }

            size = Mathf.Clamp(size, 1, 10);

            // Format: .sign create {text} {hexcolor} {size}
            // IMPORTANT: No quotes around text!
            var command = $".sign create {text} {_selectedHex} {size}";
            MessageService.EnqueueMessage(command);

            Plugin.LogInstance.LogInfo($"Creating sign: '{text}' Color: #{_selectedHex} Size: {size}");
        }

        private void OnRemoveSign()
        {
            MessageService.EnqueueMessage(MessageService.BCCOM_SIGN_REMOVE);
            Plugin.LogInstance.LogInfo("Removing nearest sign");
        }

        private void StyleButton(ButtonRef btn)
        {
            if (_btnNormalSprite == null) return;

            var img = btn.GameObject.GetComponent<Image>();
            if (img)
            {
                img.sprite = _btnNormalSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }

            if (_btnSelectedSprite != null)
            {
                var comp = btn.Component;
                comp.transition = Selectable.Transition.SpriteSwap;
                var state = comp.spriteState;
                state.highlightedSprite = _btnSelectedSprite;
                state.pressedSprite = _btnSelectedSprite;
                state.selectedSprite = _btnSelectedSprite;
                comp.spriteState = state;
            }
        }

        protected override void OnClosePanelClicked()
        {
            SetActive(false);
        }

        internal override void Reset()
        {
        }

        public override void Destroy()
        {
            _colorPicker?.Destroy();
            base.Destroy();
        }
    }
}