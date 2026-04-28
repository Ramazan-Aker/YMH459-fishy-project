using FishMuseum.Data;
using FishMuseum.Utils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FishMuseum.AR
{
    /// <summary>
    /// Tap-to-place akışını yönetir:
    ///   1. SceneFlow'dan seçili FishData'yı okur.
    ///   2. Kullanıcı plane üzerine tap ettiğinde balığı instantiate eder.
    ///   3. İlk yerleştirme sonrasında indicator'ı kapatır ve kendini devre dışı bırakır.
    ///
    /// Yerleştirilen balık prefabının üzerinde FishController bileşeni olması gerekir.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager))]
    public class PlacementController : MonoBehaviour
    {
        [Header("Bağımlılıklar")]
        [SerializeField] private PlacementIndicator indicator;

        [Header("Fallback (Test)")]
        [Tooltip("SceneFlow'da balık yoksa bu prefab kullanılır (sadece editörde)")]
        [SerializeField] private GameObject fallbackPrefab;

        private ARRaycastManager _raycastManager;
        private FishData _fishData;
        private bool _fishPlaced;

        private void Awake()
        {
            _raycastManager = GetComponent<ARRaycastManager>();
        }

        private void OnEnable()
        {
            TouchInputManager.OnTap += HandleTap;
        }

        private void OnDisable()
        {
            TouchInputManager.OnTap -= HandleTap;
        }

        private void Start()
        {
            _fishData = SceneFlow.SelectedFish;

            if (_fishData == null)
            {
                Debug.LogWarning("[PlacementController] SceneFlow.SelectedFish null. " +
                                 "Fallback prefab kullanılacak.");
            }
        }

        private void HandleTap(Vector2 screenPos)
        {
            if (_fishPlaced) return;

            bool hit = ARRaycastHelper.TryRaycastPlane(_raycastManager, screenPos, out var arHit);
            if (!hit) return;

            PlaceFish(arHit.pose);
        }

        private void PlaceFish(Pose pose)
        {
            GameObject prefabToSpawn = _fishData != null ? _fishData.prefab : fallbackPrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogError("[PlacementController] Yerleştirilecek prefab bulunamadı!");
                return;
            }

            GameObject fish = Instantiate(prefabToSpawn, pose.position, pose.rotation);

            // Balığa FishData referansını enjekte et (Inspector'da sürüklemeye gerek yok)
            var fishController = fish.GetComponent<Fish.FishController>();
            if (fishController != null && _fishData != null)
            {
                fishController.Initialize(_fishData);
            }

            _fishPlaced = true;

            if (indicator != null)
                indicator.gameObject.SetActive(false);

            // Bu bileşen artık gerekli değil
            enabled = false;
        }
    }
}
