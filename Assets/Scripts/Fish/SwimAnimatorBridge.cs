using UnityEngine;

namespace FishMuseum.Fish
{
    /// <summary>
    /// Animator bileşenini FishMuseum sistemine köprüler.
    /// Hash önbelleğe alınır; string araması her frame çalışmaz.
    /// Animator Controller'da "Swim" ve "IsInspecting" parametreleri tanımlı olmalıdır.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class SwimAnimatorBridge : MonoBehaviour
    {
        private Animator _animator;

        private static readonly int _isInspectingHash = Animator.StringToHash("IsInspecting");
        private static readonly int _swimSpeedHash = Animator.StringToHash("SwimSpeed");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        /// <summary>
        /// Yüzme animasyonunu durdurur (Inspection modu başlar).
        /// </summary>
        public void PauseSwim()
        {
            _animator.speed = 0f;
            _animator.SetBool(_isInspectingHash, true);
        }

        /// <summary>
        /// Yüzme animasyonunu devam ettirir (Inspection modu biter).
        /// </summary>
        public void ResumeSwim()
        {
            _animator.speed = 1f;
            _animator.SetBool(_isInspectingHash, false);
        }

        /// <summary>
        /// Yüzme hızını ayarlar (0 = dondurulmuş, 1 = normal, >1 = hızlı).
        /// </summary>
        public void SetSwimSpeed(float speed)
        {
            _animator.SetFloat(_swimSpeedHash, speed);
        }

        public bool IsPaused => _animator.speed == 0f;
    }
}
