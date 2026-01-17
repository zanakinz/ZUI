using System;
using UnityEngine;

namespace CombatAnalytics.UI
{
    /// <summary>
    /// Makes a UI panel draggable by clicking and dragging this component's GameObject.
    /// Attach this to a title bar or drag handle.
    /// </summary>
    public class PanelDragger : MonoBehaviour
    {
        public RectTransform PanelTransform;
        private Vector2 _dragOffset;
        private bool _isDragging;
        private RectTransform _myRectTransform;
        private Canvas _canvas;

        public PanelDragger(IntPtr ptr) : base(ptr) { }

        private void Awake()
        {
            _myRectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            if (PanelTransform == null || _myRectTransform == null || _canvas == null) return;

            // Check if mouse is over this rect and button is pressed
            if (Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(_myRectTransform, Input.mousePosition, _canvas.worldCamera))
            {
                _isDragging = true;
                
                // Calculate offset between mouse position and panel position
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    PanelTransform.parent as RectTransform,
                    Input.mousePosition,
                    _canvas.worldCamera,
                    out Vector2 localPoint
                );
                
                _dragOffset = PanelTransform.anchoredPosition - localPoint;
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }

            if (_isDragging)
            {
                // Convert screen point to local point in parent space
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    PanelTransform.parent as RectTransform,
                    Input.mousePosition,
                    _canvas.worldCamera,
                    out Vector2 localPoint
                ))
                {
                    PanelTransform.anchoredPosition = localPoint + _dragOffset;
                }
            }
        }
    }
}
