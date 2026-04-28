using System.Collections.Generic;
using UnityEngine;

namespace FishMuseum.Data
{
    /// <summary>
    /// Her balık türü için bir ScriptableObject varlığı oluşturulur.
    /// Tüm eğitim verisi, prefab referansı ve ses klibi bu nesnede toplanır.
    /// Yeni bir tür eklemek için sadece yeni bir asset oluşturup doldurmak yeterlidir.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFishData", menuName = "FishMuseum/Fish Data", order = 0)]
    public class FishData : ScriptableObject
    {
        [Header("Temel Bilgiler")]
        [Tooltip("Türkçe görünen ad (ör: Palyaço Balığı)")]
        public string displayName;

        [Tooltip("Bilimsel ad (ör: Amphiprion ocellaris)")]
        public string scientificName;

        [Header("Eğitim İçeriği")]
        [Tooltip("Yaşam alanı (ör: Hint ve Pasifik Okyanusları, mercan resifleri)")]
        public string habitat;

        [Tooltip("Beslenme şekli (ör: Omnivor – zooplankton, yosun ve küçük omurgasızlar)")]
        public string diet;

        [Tooltip("Bilgi panosunda gösterilecek özet metin (80–120 kelime)")]
        [TextArea(4, 8)]
        public string summary;

        [Header("Görsel ve Ses")]
        [Tooltip("Alt menüde görünecek 2D ikon")]
        public Sprite menuIcon;

        [Tooltip("AR sahnesine yerleştirilecek animasyonlu balık prefabı")]
        public GameObject prefab;

        [Tooltip("Bilgi paneli açıldığında çalacak Türkçe sesli anlatım klibi")]
        public AudioClip narrationClip;

        [Header("Parça Bilgileri (Detaylı İnceleme Modu)")]
        [Tooltip("Balığın dokunulabilir anatomik parçaları ve açıklamaları")]
        public List<FishPartInfo> parts = new List<FishPartInfo>();
    }
}
