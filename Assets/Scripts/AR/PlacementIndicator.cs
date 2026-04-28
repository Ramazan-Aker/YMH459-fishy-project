using FishMuseum.Utils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FishMuseum.AR
{
    /// <summary>
    /// Zemin üzerinde tespit edilen plane'i takip eden reticle (halka) göstergesi.
    /// Plane bulunamadığında gizlenir; bulununca gösterilir ve pozisyonu güncellenir.
    /// Balık yerleştirildikten sonra PlacementController tarafından devre dışı bırakılır.
    /// </summary>
    public class PlacementIndicator : MonoBehaviour
    {
        [Tooltip("AR Raycast Manager referansı (XR Origin üzerinde)")]
        [SerializeField] private ARRaycastManager raycastManager;

        [Tooltip("Ekranın tam ortasından raycast atılır")]
        private Vector2 _screenCenter;

        private bool _isVisible;

        private void Start()
        {
            _screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            SetVisible(false);
        }

        private void Update()
        {
            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            bool hit = ARRaycastHelper.TryRaycastPlane(
                raycastManager,
                _screenCenter,
                out var arHit);

            if (hit)
            {
                SetVisible(true);
                transform.SetPositionAndRotation(
                    arHit.pose.position,
                    arHit.pose.rotation);
            }
            else
            {
                SetVisible(false);
            }
        }

        private void SetVisible(bool visible)
        {
            if (_isVisible == visible) return;
            _isVisible = visible;
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// Yerleştirme tamamlandığında animatör kontrolü için mevcut pozisyonu döndürür.
        /// </summary>
        public bool TryGetPlacementPose(out Pose pose)
        {
            if (!_isVisible)
            {
                pose = default;
                return false;
            }

            pose = new Pose(transform.position, transform.rotation);
            return true;
        }
    }
}
