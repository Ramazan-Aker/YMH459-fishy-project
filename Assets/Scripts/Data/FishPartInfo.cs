using System;
using UnityEngine;

namespace FishMuseum.Data
{
    /// <summary>
    /// Bir balığa ait anatomik parçanın (yüzgeç, ağız, göz vb.) verilerini tutar.
    /// FishData listesi içinde [Serializable] olarak saklanır.
    /// </summary>
    [Serializable]
    public class FishPartInfo
    {
        [Tooltip("Parçanın görüntülenecek adı (ör: Dorsal Yüzgeci)")]
        public string partName;

        [Tooltip("Bu parçaya dokunulduğunda gösterilecek kısa bilgi metni")]
        [TextArea(2, 5)]
        public string infoText;

        [Tooltip("Prefab'daki ilgili child GameObject'in adı (Collider bu objeye bağlı)")]
        public string childObjectName;

        [Tooltip("Tooltip'in dünya uzayında görüneceği offset (parça merkezinden)")]
        public Vector3 tooltipOffset = Vector3.up * 0.1f;
    }
}
