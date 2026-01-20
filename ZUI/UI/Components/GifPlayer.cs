using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Il2CppInterop.Runtime.Attributes;
using ZUI.Utils;

namespace ZUI.UI.Components
{
    public class GifPlayer : MonoBehaviour
    {
        public GifPlayer(IntPtr ptr) : base(ptr) { }

        private List<Sprite> _sprites = new List<Sprite>();
        private List<float> _delays = new List<float>();
        private Image _targetImage;
        private int _currentIndex = 0;
        private float _timer = 0f;
        private bool _isPlaying = false;

        private void Awake()
        {
            _targetImage = GetComponent<Image>();
            if (_targetImage != null)
            {
                _targetImage.type = Image.Type.Simple;
                _targetImage.preserveAspect = true;
            }
        }

        [HideFromIl2Cpp]
        public void SetGifData(List<GifFrame> frames)
        {
            _sprites.Clear();
            _delays.Clear();

            if (frames == null || frames.Count == 0) return;

            foreach (var frame in frames)
            {
                if (frame.Texture != null)
                {
                    var sprite = Sprite.Create(frame.Texture, new Rect(0, 0, frame.Texture.width, frame.Texture.height), new Vector2(0.5f, 0.5f));
                    _sprites.Add(sprite);
                    _delays.Add(frame.Delay);
                }
            }

            if (_sprites.Count > 0)
            {
                _currentIndex = 0;
                _timer = 0f;
                _isPlaying = true;
                UpdateVisual();
            }
        }

        private void Update()
        {
            if (!_isPlaying || _sprites.Count <= 1 || _targetImage == null) return;

            _timer += Time.unscaledDeltaTime;
            float currentDelay = _delays[_currentIndex];
            if (currentDelay <= 0.01f) currentDelay = 0.1f;

            if (_timer >= currentDelay)
            {
                _timer -= currentDelay;
                _currentIndex = (_currentIndex + 1) % _sprites.Count;
                UpdateVisual();
            }
        }

        private void UpdateVisual()
        {
            if (_targetImage != null && _currentIndex < _sprites.Count)
            {
                var s = _sprites[_currentIndex];
                // Update BOTH to prevent any fallback to the original texture
                _targetImage.sprite = s;
                _targetImage.overrideSprite = s;
            }
        }

        private void OnEnable() { if (_sprites.Count > 0) _isPlaying = true; }
        private void OnDisable() { _isPlaying = false; }
    }
}