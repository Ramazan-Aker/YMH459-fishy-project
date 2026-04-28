using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FishMuseum.UI
{
    /// <summary>
    /// AR sahnesi yüklendiğinde "Zemin tarayın" talimatını gösterir.
    /// ARPlaneManager ilk plane'i tespit ettiğinde overlay yumuşakça kaybolur.
    ///
    /// Inspector bağlantıları:
    ///   overlayRoot     — fade edilecek Canvas Group (veya GameObject)
    ///   planeManager    — ARPlaneManager referansı
    ///   fadeDuration    — soluma süresi (saniye)
    /// </summary>
    public class OnboardingOverlay : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup overlayGroup;

        [Header("AR")]
        [SerializeField] private ARPlaneManager planeManager;

        [Header("Ayarlar")]
        [Tooltip("Plane tespit edildikten sonra overlay'in soluma süresi")]
        [SerializeField] private float fadeDuration = 1.2f;

        [Tooltip("İlk plane tespitinden sonra kaç saniye beklenip fade başlasın")]
        [SerializeField] private float delayBeforeFade = 0.5f;

        private bool _fadingOut;

        private void Awake()
        {
            if (overlayGroup != null)
            {
                overlayGroup.alpha = 1f;
                overlayGroup.blocksRaycasts = false; // Overlay'in altındaki UI'ya tıklamayı engelleme
            }

            if (planeManager == null)
                planeManager = FindFirstObjectByType<ARPlaneManager>();
        }

        private void OnEnable()
        {
            if (planeManager != null)
                planeManager.trackablesChanged.AddListener(OnPlanesChanged);
        }

        private void OnDisable()
        {
            if (planeManager != null)
                planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
        }

        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (_fadingOut) return;

            // Yeni bir plane eklendiyse overlay'i kapat
            if (args.added.Count > 0)
            {
                _fadingOut = true;
                StartCoroutine(FadeOut());
            }
        }

        private IEnumerator FadeOut()
        {
            yield return new WaitForSeconds(delayBeforeFade);

            float elapsed = 0f;
            float startAlpha = overlayGroup != null ? overlayGroup.alpha : 1f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);

                if (overlayGroup != null)
                    overlayGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

                yield return null;
            }

            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.gameObject.SetActive(false);
            }
        }
    }
}
