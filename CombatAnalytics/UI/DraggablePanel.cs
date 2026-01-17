using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Il2CppInterop.Runtime.Attributes;

namespace CombatAnalytics.UI
{
    public class DraggablePanel : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private Vector2 _dragOffset;
        private bool _isDragging;
        private RectTransform _titleBar; // Reference to title bar for drag detection

        public DraggablePanel(IntPtr ptr) : base(ptr) { }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            
            // Try to find title bar for drag area
            var titleTransform = transform.Find("Title");
            if (titleTransform != null)
            {
                _titleBar = titleTransform.GetComponent<RectTransform>();
                UnityEngine.Debug.Log("[CombatAnalytics] DraggablePanel: Found title bar for dragging");
            }
            else
            {
                UnityEngine.Debug.Log("[CombatAnalytics] DraggablePanel: No title bar found, using entire panel");
            }
        }

        private void Update()
        {
            if (_rectTransform == null || _canvas == null) return;

            // Get canvas RectTransform (parent for coordinate conversion)
            var canvasRect = _canvas.GetComponent<RectTransform>();
            if (canvasRect == null) return;

            // Determine which rect to use for drag detection (title bar if available, otherwise full panel)
            RectTransform dragRect = _titleBar != null ? _titleBar : _rectTransform;

            // Check for mouse button down on the drag area
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePosition = Input.mousePosition;
                
                // Check if mouse is over the drag area
                if (RectTransformUtility.RectangleContainsScreenPoint(dragRect, mousePosition, _canvas.worldCamera))
                {
                    // Check if we're clicking on a UI element (button, etc.) - if so, don't start dragging
                    var eventSystem = EventSystem.current;
                    if (eventSystem != null)
                    {
                        var pointerData = new PointerEventData(eventSystem)
                        {
                            position = mousePosition
                        };
                        
                        var results = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
                        EventSystem.current.RaycastAll(pointerData, results);
                        
                        // Check if we hit a button or other interactive element
                        bool hitButton = false;
                        foreach (var result in results)
                        {
                            if (result.gameObject.GetComponent<UnityEngine.UI.Button>() != null)
                            {
                                hitButton = true;
                                UnityEngine.Debug.Log("[CombatAnalytics] DraggablePanel: Clicked on button, not dragging");
                                break;
                            }
                        }
                        
                        if (hitButton)
                            return;
                    }
                    
                    _isDragging = true;
                    UnityEngine.Debug.Log("[CombatAnalytics] DraggablePanel: Started dragging");
                    
                    // Calculate offset - use the panel's parent (canvas rect) for coordinate conversion
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        mousePosition,
                        _canvas.worldCamera,
                        out var localPoint))
                    {
                        _dragOffset = _rectTransform.anchoredPosition - localPoint;
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (_isDragging)
                {
                    UnityEngine.Debug.Log("[CombatAnalytics] DraggablePanel: Stopped dragging");
                }
                _isDragging = false;
            }

            if (_isDragging)
            {
                Vector2 mousePosition = Input.mousePosition;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    mousePosition,
                    _canvas.worldCamera,
                    out var localPoint))
                {
                    _rectTransform.anchoredPosition = localPoint + _dragOffset;
                }
            }
        }
    }
}
