using FishMuseum.Data;
using FishMuseum.UI;
using FishMuseum.Utils;
using UnityEngine;

namespace FishMuseum.Fish
{
    /// <summary>
    /// Balığın dokunulabilir anatomik parçalarına eklenir (yüzgeç, ağız, göz, kuyruk).
    /// Sadece inspection modunda aktif kabul eder; normal yüzme sırasında tap'e tepki vermez.
    ///
    /// Kurulum:
    ///   - Her parça için ayrı bir child GameObject oluştur.
    ///   - Üzerine bu script + Collider (MeshCollider veya BoxCollider) ekle.
    ///   - Inspector'da FishController referansını ve FishPartInfo'yu doldur.
    ///   - PartTooltipUI referansını da Inspector'dan ya da FindFirstObjectByType ile bağla.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FishPartController : MonoBehaviour, IInteractable
    {
        [Tooltip("Bu parçanın ait olduğu balık kontrolcüsü")]
        [SerializeField] private FishController fishController;

        [Tooltip("Bu parça için gösterilecek bilgi (FishData.parts listesindeki öğeyle eşleşmeli)")]
        [SerializeField] private FishPartInfo partInfo;

        [Tooltip("Tooltip UI bileşeni; sahne başlangıcında otomatik aranır")]
        [SerializeField] private PartTooltipUI tooltip;

        // IInteractable: sadece inspection modunda etkileşime açık
        public bool IsInteractable => fishController != null && fishController.IsInspecting;

        private void Awake()
        {
            if (fishController == null)
                fishController = GetComponentInParent<FishController>();

            if (tooltip == null)
                tooltip = FindFirstObjectByType<PartTooltipUI>();
        }

        private void OnEnable()
        {
            TouchInputManager.OnTap += HandleTap;
        }

        private void OnDisable()
        {
            TouchInputManager.OnTap -= HandleTap;
        }

        private void HandleTap(Vector2 screenPos)
        {
            if (!IsInteractable) return;

            if (ARRaycastHelper.TryRaycastCollider(screenPos, out RaycastHit hit))
            {
                if (hit.collider.gameObject == gameObject)
                    OnTapped();
            }
        }

        public void OnTapped()
        {
            if (partInfo == null || tooltip == null) return;

            Vector3 worldPos = transform.position + partInfo.tooltipOffset;
            tooltip.Show(partInfo, worldPos);
        }

        /// <summary>
        /// Dışarıdan part bilgisini runtime'da set etmek için (FishController/Initialize akışı).
        /// </summary>
        public void SetPartInfo(FishPartInfo info) => partInfo = info;
    }
}
