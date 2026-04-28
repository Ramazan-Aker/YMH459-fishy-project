using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FishMuseum.Utils
{
    /// <summary>
    /// ARRaycastManager üzerinden plane/mesh raycast işlemlerini sarmalayan
    /// yardımcı sınıf. Her bileşenin kendi liste ve cast kodunu tekrarlamaması için.
    /// </summary>
    public static class ARRaycastHelper
    {
        private static readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();

        /// <summary>
        /// Ekran pozisyonundan AR plane'e raycast atar.
        /// Başarılıysa 'hit' doldurulup true döner.
        /// </summary>
        public static bool TryRaycastPlane(
            ARRaycastManager raycastManager,
            Vector2 screenPos,
            out ARRaycastHit hit,
            TrackableType trackableTypes = TrackableType.PlaneWithinPolygon)
        {
            _hits.Clear();
            bool didHit = raycastManager.Raycast(screenPos, _hits, trackableTypes);
            hit = didHit ? _hits[0] : default;
            return didHit;
        }

        /// <summary>
        /// Ekran pozisyonundan sahne içindeki collider'lara fizik raycasti atar.
        /// AR kamera referansı verilmezse Camera.main kullanılır.
        /// </summary>
        public static bool TryRaycastCollider(
            Vector2 screenPos,
            out RaycastHit hit,
            float maxDistance = 100f,
            int layerMask = ~0,
            Camera cam = null)
        {
            Camera camera = cam != null ? cam : Camera.main;
            Ray ray = camera.ScreenPointToRay(screenPos);
            return Physics.Raycast(ray, out hit, maxDistance, layerMask);
        }
    }
}
