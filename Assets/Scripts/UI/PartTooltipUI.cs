using FishMuseum.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishMuseum.UI
{
    /// <summary>
    /// Balık parçasına dokunulduğunda kısa bir tooltip paneli gösterir.
    ///
    /// Çalışma prensibi:
    ///   - Tooltip bir Canvas (World Space veya Screen Space Overlay) üzerinde bulunur.
    ///   - Show() çağrıldığında paneli aktif eder, metni doldurur ve
    ///     dünya pozisyonundan ekran pozisyonuna dönüştürerek konumlandırır.
    ///   - Hide() butonuna basılınca veya başka bir parçaya dokunulunca kapanır.
    ///
    /// Inspector bağlantıları:
    ///   tooltipPanel   — açılıp kapanan panel kök GameObject'i
    ///   partNameText   — parçanın adı
    ///   infoText       — parça açıklama metni
    ///   closeButton    — tooltip'i kapatan buton
    /// </summary>
    public class PartTooltipUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject tooltipPanel;

        [Header("Metin Alanları")]
        [SerializeField] private TextMeshProUGUI partNameText;
        [SerializeField] private TextMeshProUGUI infoText;

        [Header("Buton")]
        [SerializeField] private Button closeButton;

        [Tooltip("Tooltip ekran kenarlarından taşmasın diye clamp uygulanır")]
        [SerializeField] private float edgePadding = 20f;

        private RectTransform _panelRect;
        private Canvas _parentCanvas;

        private void Awake()
        {
            _panelRect = tooltipPanel != null ? tooltipPanel.GetComponent<RectTransform>() : null;
            _parentCanvas = GetComponentInParent<Canvas>();

            closeButton?.onClick.AddListener(Hide);

            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        /// <summary>
        /// Belirtilen dünya pozisyonunun ekran koordinatına dönüştürülmüş yerinde tooltip gösterir.
        /// </summary>
        public void Show(FishPartInfo info, Vector3 worldPosition)
        {
            if (info == null) return;

            if (partNameText != null) partNameText.text = info.partName;
            if (infoText != null) infoText.text = info.infoText;

            if (tooltipPanel != null)
                tooltipPanel.SetActive(true);

            PositionTooltip(worldPosition);
        }

        public void Hide()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        private void PositionTooltip(Vector3 worldPos)
        {
            if (_panelRect == null || Camera.main == null) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // Nesne kameranın arkasındaysa gösterme
            if (screenPos.z < 0)
            {
                Hide();
                return;
            }

            // Screen Space Overlay canvas için direkt piksel koordinatı kullan
            if (_parentCanvas != null && _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                _panelRect.position = ClampToScreen(screenPos);
            }
            else
            {
                // World / Camera space canvas → ScreenToWorldPoint dönüşümü
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    _parentCanvas.transform as RectTransform,
                    screenPos,
                    _parentCanvas.worldCamera,
                    out Vector3 worldUIPos);

                _panelRect.position = worldUIPos;
            }
        }

        private Vector3 ClampToScreen(Vector3 screenPos)
        {
            float halfW = _panelRect.rect.width * 0.5f;
            float halfH = _panelRect.rect.height * 0.5f;

            screenPos.x = Mathf.Clamp(screenPos.x, halfW + edgePadding, Screen.width - halfW - edgePadding);
            screenPos.y = Mathf.Clamp(screenPos.y, halfH + edgePadding, Screen.height - halfH - edgePadding);

            return screenPos;
        }
    }
}
