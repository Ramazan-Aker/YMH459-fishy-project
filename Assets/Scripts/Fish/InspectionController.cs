using FishMuseum.Data;
using FishMuseum.Utils;
using UnityEngine;

namespace FishMuseum.Fish
{
    /// <summary>
    /// "Detaylı İncele" modunu yönetir.
    ///
    /// Giriş (BeginInspection):
    ///   - Balığın o anki pozisyon/rotasyonu kaydedilir.
    ///   - Balık kameraya doğru sabit mesafeye (inspectionDistance) yumuşakça taşınır.
    ///   - Animator dondurulur (SwimAnimatorBridge.PauseSwim).
    ///   - Drag event'leri artık balığı döndürür.
    ///
    /// Çıkış (EndInspection):
    ///   - Balık kayıt yerine yumuşakça geri döner.
    ///   - Animator devam ettirilir.
    /// </summary>
    public class InspectionController : MonoBehaviour
    {
        [Header("İnceleme Pozisyonu")]
        [Tooltip("Kameradan ne kadar uzakta tutulacak (metre)")]
        [SerializeField] private float inspectionDistance = 0.6f;

        [Tooltip("Kameradan dikey offset (aşağı kaydırarak ekranın alt kısmından uzaklaştır)")]
        [SerializeField] private float verticalOffset = -0.05f;

        [Header("Geçiş Hızları")]
        [Tooltip("Balığın inspection pozisyonuna gitme hızı")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("Sürükleme döndürme hassasiyeti")]
        [SerializeField] private float rotationSensitivity = 0.4f;

        [Tooltip("Geri dönüş sırasında lerp hızı")]
        [SerializeField] private float returnSpeed = 4f;

        private FishData _fishData;
        private SwimAnimatorBridge _swimBridge;
        private FishController _fishController;

        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _inspectionTargetPosition;

        private bool _isActive;
        private bool _isReturning;

        public void Initialize(FishData data, SwimAnimatorBridge swimBridge, FishController controller)
        {
            _fishData = data;
            _swimBridge = swimBridge;
            _fishController = controller;
        }

        private void OnEnable()
        {
            TouchInputManager.OnDrag += HandleDrag;
        }

        private void OnDisable()
        {
            TouchInputManager.OnDrag -= HandleDrag;
        }

        private void Update()
        {
            if (_isActive && !_isReturning)
            {
                // Balığı hedef inspection pozisyonuna yumuşakça taşı
                transform.position = Vector3.Lerp(
                    transform.position,
                    _inspectionTargetPosition,
                    Time.deltaTime * moveSpeed);
            }
            else if (_isReturning)
            {
                // Orijinal pozisyona geri dön
                transform.position = Vector3.Lerp(
                    transform.position,
                    _originalPosition,
                    Time.deltaTime * returnSpeed);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    _originalRotation,
                    Time.deltaTime * returnSpeed);

                // Hedefe yeterince yaklaşıldığında tamamlandı say
                float distSq = (transform.position - _originalPosition).sqrMagnitude;
                if (distSq < 0.0001f)
                {
                    transform.SetPositionAndRotation(_originalPosition, _originalRotation);
                    _isReturning = false;
                }
            }
        }

        /// <summary>
        /// FishController.EnterInspectionMode() tarafından çağrılır.
        /// </summary>
        public void BeginInspection()
        {
            _originalPosition = transform.position;
            _originalRotation = transform.rotation;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 camForward = cam.transform.forward;
            _inspectionTargetPosition = cam.transform.position
                                        + camForward * inspectionDistance
                                        + Vector3.up * verticalOffset;

            _isActive = true;
            _isReturning = false;
        }

        /// <summary>
        /// FishController.ExitInspectionMode() veya "Geri Dön" butonu tarafından çağrılır.
        /// </summary>
        public void EndInspection()
        {
            _isActive = false;
            _isReturning = true;
        }

        private void HandleDrag(Vector2 prevPos, Vector2 currentPos)
        {
            if (!_isActive || _isReturning) return;

            // Ekran delta → dünya rotasyonu
            Vector2 delta = currentPos - prevPos;
            float rotX = delta.y * rotationSensitivity;  // dikey sürükle → X ekseni
            float rotY = -delta.x * rotationSensitivity; // yatay sürükle → Y ekseni

            transform.Rotate(Camera.main.transform.up, rotY, Space.World);
            transform.Rotate(Camera.main.transform.right, rotX, Space.World);
        }
    }
}
