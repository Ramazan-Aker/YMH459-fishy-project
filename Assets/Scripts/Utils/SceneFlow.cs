using FishMuseum.Data;

namespace FishMuseum.Utils
{
    /// <summary>
    /// Sahneler arası veri transferini static alan üzerinden sağlar.
    /// MenuScene'de seçilen FishData, ARScene yüklendiğinde buradan okunur.
    /// Unity'de SceneManager.LoadScene() DontDestroyOnLoad gerektirmeden
    /// basit tek-yönlü veri geçişi için yeterlidir.
    /// </summary>
    public static class SceneFlow
    {
        /// <summary>
        /// MenuScene'den ARScene'e taşınan seçili balık verisi.
        /// ARScene yüklendikten sonra null kontrolü yapılmalıdır.
        /// </summary>
        public static FishData SelectedFish { get; set; }

        /// <summary>
        /// Uygulama başında veya test sırasında state'i sıfırlamak için.
        /// </summary>
        public static void Reset()
        {
            SelectedFish = null;
        }
    }
}
