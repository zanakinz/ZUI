using System;
using UnityEngine;
using UnityEngine.UI;
using ZUI.UI.UniverseLib.UI;
using ZUI.UI.UniverseLib.UI.Models;

namespace ZUI.UI.CustomLib.Controls
{
    /// <summary>
    /// Custom color picker with HSV wheel, similar to Google's color picker.
    /// </summary>
    public class ColorPickerControl
    {
        public GameObject RootObject { get; private set; }
        public Color SelectedColor { get; private set; } = Color.white;
        public string HexColor => ColorUtility.ToHtmlStringRGB(SelectedColor);
        
        public event Action<Color> OnColorChanged;
        
        private RawImage _colorWheel;
        private RawImage _brightnessSlider;
        private Image _selectedColorPreview;
        private InputFieldRef _hexInput;
        private Texture2D _wheelTexture;
        private Texture2D _brightnessTexture;
        
        private float _currentHue = 0f;
        private float _currentSaturation = 1f;
        private float _currentValue = 1f;
        
        private bool _updatingFromCode = false;
        
        public ColorPickerControl(GameObject parent)
        {
            ConstructUI(parent);
            UpdateColor();
        }
        
        private void ConstructUI(GameObject parent)
        {
            // Main container
            RootObject = UIFactory.CreateVerticalGroup(parent, "ColorPicker", true, false, true, true, 5, 
                new Vector4(10, 10, 10, 10), new Color(0.1f, 0.1f, 0.1f, 0.95f));
            UIFactory.SetLayoutElement(RootObject, minWidth: 300, minHeight: 400, flexibleWidth: 0, flexibleHeight: 0);
            
            // Color wheel container
            var wheelContainer = UIFactory.CreateUIObject("WheelContainer", RootObject);
            UIFactory.SetLayoutElement(wheelContainer, minWidth: 280, minHeight: 280, flexibleWidth: 0, flexibleHeight: 0);
            
            // Create color wheel
            _colorWheel = wheelContainer.AddComponent<RawImage>();
            _wheelTexture = CreateColorWheelTexture(256);
            _colorWheel.texture = _wheelTexture;
            
            // Add click handler for wheel
            var wheelButton = wheelContainer.AddComponent<Button>();
            wheelButton.onClick.AddListener(() => OnWheelClicked(Input.mousePosition));
            
            // Brightness slider container
            var sliderContainer = UIFactory.CreateHorizontalGroup(RootObject, "SliderContainer", false, false, true, true, 5);
            UIFactory.SetLayoutElement(sliderContainer, minHeight: 30, flexibleWidth: 9999);
            
            var sliderLabel = UIFactory.CreateLabel(sliderContainer, "Label", "Brightness:");
            UIFactory.SetLayoutElement(sliderLabel.GameObject, minWidth: 80, minHeight: 30);
            
            var sliderObj = UIFactory.CreateUIObject("BrightnessSlider", sliderContainer);
            UIFactory.SetLayoutElement(sliderObj, minHeight: 30, flexibleWidth: 9999);
            
            _brightnessSlider = sliderObj.AddComponent<RawImage>();
            _brightnessTexture = CreateBrightnessTexture(256);
            _brightnessSlider.texture = _brightnessTexture;
            
            var brightnessButton = sliderObj.AddComponent<Button>();
            brightnessButton.onClick.AddListener(() => OnBrightnessClicked(Input.mousePosition));
            
            // Selected color preview
            var previewContainer = UIFactory.CreateHorizontalGroup(RootObject, "PreviewContainer", false, false, true, true, 5);
            UIFactory.SetLayoutElement(previewContainer, minHeight: 50, flexibleWidth: 9999);
            
            var previewLabel = UIFactory.CreateLabel(previewContainer, "Label", "Selected:");
            UIFactory.SetLayoutElement(previewLabel.GameObject, minWidth: 80, minHeight: 50);
            
            var previewObj = UIFactory.CreateUIObject("ColorPreview", previewContainer);
            UIFactory.SetLayoutElement(previewObj, minHeight: 50, flexibleWidth: 9999);
            _selectedColorPreview = previewObj.AddComponent<Image>();
            _selectedColorPreview.color = SelectedColor;
            
            // Hex input
            var hexContainer = UIFactory.CreateHorizontalGroup(RootObject, "HexContainer", false, false, true, true, 5);
            UIFactory.SetLayoutElement(hexContainer, minHeight: 30, flexibleWidth: 9999);
            
            var hexLabel = UIFactory.CreateLabel(hexContainer, "Label", "Hex (#):");
            UIFactory.SetLayoutElement(hexLabel.GameObject, minWidth: 80, minHeight: 30);
            
            _hexInput = UIFactory.CreateInputField(hexContainer, "HexInput", "FFFFFF");
            UIFactory.SetLayoutElement(_hexInput.GameObject, minHeight: 30, flexibleWidth: 9999);
            _hexInput.OnValueChanged += OnHexInputChanged;
        }
        
        private Texture2D CreateColorWheelTexture(int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            var center = new Vector2(resolution / 2f, resolution / 2f);
            var radius = resolution / 2f;
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var dx = x - center.x;
                    var dy = y - center.y;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                    else
                    {
                        var angle = Mathf.Atan2(dy, dx);
                        var hue = (angle + Mathf.PI) / (2f * Mathf.PI);
                        var saturation = distance / radius;
                        
                        var color = Color.HSVToRGB(hue, saturation, 1f);
                        texture.SetPixel(x, y, color);
                    }
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        private Texture2D CreateBrightnessTexture(int width)
        {
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false);
            
            for (int x = 0; x < width; x++)
            {
                var brightness = x / (float)width;
                var color = Color.HSVToRGB(_currentHue, _currentSaturation, brightness);
                texture.SetPixel(x, 0, color);
            }
            
            texture.Apply();
            return texture;
        }
        
        private void UpdateBrightnessTexture()
        {
            if (_brightnessTexture == null) return;
            
            var width = _brightnessTexture.width;
            for (int x = 0; x < width; x++)
            {
                var brightness = x / (float)width;
                var color = Color.HSVToRGB(_currentHue, _currentSaturation, brightness);
                _brightnessTexture.SetPixel(x, 0, color);
            }
            
            _brightnessTexture.Apply();
        }
        
        private void OnWheelClicked(Vector2 mousePos)
        {
            if (_colorWheel == null) return;
            
            var rect = _colorWheel.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, mousePos, null, out localPoint);
            
            var size = rect.rect.size;
            var center = Vector2.zero;
            var normalized = new Vector2(
                (localPoint.x - center.x) / (size.x / 2f),
                (localPoint.y - center.y) / (size.y / 2f)
            );
            
            var distance = normalized.magnitude;
            if (distance > 1f) return; // Outside wheel
            
            var angle = Mathf.Atan2(normalized.y, normalized.x);
            _currentHue = (angle + Mathf.PI) / (2f * Mathf.PI);
            _currentSaturation = Mathf.Clamp01(distance);
            
            UpdateColor();
            UpdateBrightnessTexture();
        }
        
        private void OnBrightnessClicked(Vector2 mousePos)
        {
            if (_brightnessSlider == null) return;
            
            var rect = _brightnessSlider.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, mousePos, null, out localPoint);
            
            var width = rect.rect.width;
            var normalizedX = Mathf.Clamp01((localPoint.x + width / 2f) / width);
            _currentValue = normalizedX;
            
            UpdateColor();
        }
        
        private void UpdateColor()
        {
            _updatingFromCode = true;
            
            SelectedColor = Color.HSVToRGB(_currentHue, _currentSaturation, _currentValue);
            
            if (_selectedColorPreview != null)
                _selectedColorPreview.color = SelectedColor;
            
            if (_hexInput != null)
                _hexInput.Text = ColorUtility.ToHtmlStringRGB(SelectedColor);
            
            OnColorChanged?.Invoke(SelectedColor);
            
            _updatingFromCode = false;
        }
        
        private void OnHexInputChanged(string hex)
        {
            if (_updatingFromCode) return;
            
            // Remove # if present
            hex = hex.TrimStart('#');
            
            if (hex.Length != 6) return;
            
            Color color;
            if (ColorUtility.TryParseHtmlString("#" + hex, out color))
            {
                Color.RGBToHSV(color, out _currentHue, out _currentSaturation, out _currentValue);
                SelectedColor = color;
                
                if (_selectedColorPreview != null)
                    _selectedColorPreview.color = SelectedColor;
                
                UpdateBrightnessTexture();
                OnColorChanged?.Invoke(SelectedColor);
            }
        }
        
        public void SetColor(Color color)
        {
            Color.RGBToHSV(color, out _currentHue, out _currentSaturation, out _currentValue);
            UpdateColor();
            UpdateBrightnessTexture();
        }
        
        public void Destroy()
        {
            if (_wheelTexture != null)
                UnityEngine.Object.Destroy(_wheelTexture);
            if (_brightnessTexture != null)
                UnityEngine.Object.Destroy(_brightnessTexture);
            if (RootObject != null)
                UnityEngine.Object.Destroy(RootObject);
        }
    }
}
