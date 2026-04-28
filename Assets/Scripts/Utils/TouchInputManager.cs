using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace FishMuseum.Utils
{
    /// <summary>
    /// Yeni Input System'ı (Enhanced Touch) kullanarak tek noktadan
    /// tap ve drag event'lerini yayınlar. Diğer sistemler buraya abone olur;
    /// doğrudan Input.touches kullanmaz.
    /// </summary>
    public class TouchInputManager : MonoBehaviour
    {
        // --- Tap: parmak basıp kısa sürede bırakmak ---
        public static event Action<Vector2> OnTap;

        // --- Drag: parmak sürükleme delta'sı (önceki pos, şimdiki pos) ---
        public static event Action<Vector2, Vector2> OnDrag;

        // --- Drag sona erdi ---
        public static event Action OnDragEnded;

        [Tooltip("Bu süreden (saniye) kısa basışlar 'tap' sayılır")]
        [SerializeField] private float tapMaxDuration = 0.25f;

        [Tooltip("Bu mesafeden (piksel) az hareket eden basışlar 'tap' sayılır")]
        [SerializeField] private float tapMaxMovePx = 20f;

        private float _touchStartTime;
        private Vector2 _touchStartPos;
        private bool _isDragging;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += HandleFingerDown;
            Touch.onFingerMove += HandleFingerMove;
            Touch.onFingerUp += HandleFingerUp;
        }

        private void OnDisable()
        {
            Touch.onFingerDown -= HandleFingerDown;
            Touch.onFingerMove -= HandleFingerMove;
            Touch.onFingerUp -= HandleFingerUp;
            EnhancedTouchSupport.Disable();
        }

        private void HandleFingerDown(Finger finger)
        {
            if (finger.index != 0) return;

            _touchStartTime = Time.realtimeSinceStartup;
            _touchStartPos = finger.currentTouch.screenPosition;
            _isDragging = false;
        }

        private void HandleFingerMove(Finger finger)
        {
            if (finger.index != 0) return;

            Vector2 delta = finger.currentTouch.delta;
            if (delta.sqrMagnitude < 0.01f) return;

            float movedPx = (finger.currentTouch.screenPosition - _touchStartPos).magnitude;
            if (movedPx > tapMaxMovePx)
            {
                _isDragging = true;
                Vector2 prev = finger.currentTouch.screenPosition - delta;
                OnDrag?.Invoke(prev, finger.currentTouch.screenPosition);
            }
        }

        private void HandleFingerUp(Finger finger)
        {
            if (finger.index != 0) return;

            if (_isDragging)
            {
                OnDragEnded?.Invoke();
                _isDragging = false;
                return;
            }

            float duration = Time.realtimeSinceStartup - _touchStartTime;
            float moved = (finger.currentTouch.screenPosition - _touchStartPos).magnitude;

            if (duration <= tapMaxDuration && moved <= tapMaxMovePx)
            {
                OnTap?.Invoke(finger.currentTouch.screenPosition);
            }

            _isDragging = false;
        }
    }
}
