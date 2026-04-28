using FishMuseum.Data;
using FishMuseum.UI;
using FishMuseum.Utils;
using UnityEngine;

namespace FishMuseum.Fish
{
    /// <summary>
    /// Yerleştirilen balık nesnesinin ana kontrolcüsü.
    ///
    /// Sorumluluklar:
    ///   - FishData'yı alır ve InspectionController + NarrationManager'a dağıtır.
    ///   - Gövdeye gelen tap'i InfoPanelController'a iletir.
    ///   - Inspection/Normal mod geçişlerini koordine eder.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(SwimAnimatorBridge))]
    public class FishController : MonoBehaviour, IInteractable
    {
        [Header("UI Referansları (Sahne içinden atanır)")]
        [SerializeField] private InfoPanelController infoPanel;

        private FishData _fishData;
        private SwimAnimatorBridge _swimBridge;
        private InspectionController _inspectionController;

        public FishData FishData => _fishData;
        public bool IsInspecting { get; private set; }

        // IInteractable
        public bool IsInteractable => !IsInspecting;

        private void Awake()
        {
            _swimBridge = GetComponent<SwimAnimatorBridge>();
            _inspectionController = GetComponent<InspectionController>();
        }

        private void OnEnable()
        {
            TouchInputManager.OnTap += HandleTap;
        }

        private void OnDisable()
        {
            TouchInputManager.OnTap -= HandleTap;
        }

        /// <summary>
        /// PlacementController tarafından instantiate sonrasında çağrılır.
        /// FishData enjeksiyonunu Inspector yerine runtime'da yapar.
        /// </summary>
        public void Initialize(FishData data)
        {
            _fishData = data;

            if (_inspectionController != null)
                _inspectionController.Initialize(data, _swimBridge, this);

            // InfoPanel sahne içinde bulunuyorsa otomatik bağla
            if (infoPanel == null)
                infoPanel = FindFirstObjectByType<InfoPanelController>();
        }

        private void HandleTap(Vector2 screenPos)
        {
            if (!IsInteractable) return;

            if (ARRaycastHelper.TryRaycastCollider(screenPos, out RaycastHit hit))
            {
                if (hit.collider.gameObject == gameObject ||
                    hit.collider.transform.IsChildOf(transform))
                {
                    OnTapped();
                }
            }
        }

        public void OnTapped()
        {
            if (_fishData == null) return;

            if (infoPanel != null)
                infoPanel.Open(_fishData, this);
        }

        public void EnterInspectionMode()
        {
            IsInspecting = true;
            _swimBridge.PauseSwim();
            _inspectionController?.BeginInspection();
        }

        public void ExitInspectionMode()
        {
            IsInspecting = false;
            _swimBridge.ResumeSwim();
            _inspectionController?.EndInspection();
        }
    }
}
