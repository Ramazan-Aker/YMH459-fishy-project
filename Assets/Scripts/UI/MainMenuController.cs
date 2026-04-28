using FishMuseum.Data;
using FishMuseum.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FishMuseum.UI
{
    /// <summary>
    /// MenuScene'deki balık seçim arayüzünü yönetir.
    ///
    /// Kurulum (Inspector):
    ///   fishDataList  — Palyaço ve Cerrah FishData asset'leri buraya sürüklenir.
    ///   buttonContainer — Butonların runtime'da oluşturulacağı transform.
    ///   fishButtonPrefab — İkon + isim içeren UI Button prefabı.
    ///   arSceneName  — Yükleneceği AR sahnesinin Build Settings'teki adı ("ARScene").
    ///
    /// Her butona tıklandığında:
    ///   1. SceneFlow.SelectedFish = ilgili FishData
    ///   2. SceneManager.LoadScene(arSceneName)
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Balık Listesi")]
        [Tooltip("Menüde gösterilecek balıkların FishData asset'leri")]
        [SerializeField] private FishData[] fishDataList;

        [Header("UI Referansları")]
        [Tooltip("Butonların oluşturulacağı Content transform (ör: ScrollView > Viewport > Content)")]
        [SerializeField] private Transform buttonContainer;

        [Tooltip("İkon + isim içeren Button prefabı")]
        [SerializeField] private GameObject fishButtonPrefab;

        [Header("Sahne Geçişi")]
        [Tooltip("Build Settings'teki AR sahnesinin adı")]
        [SerializeField] private string arSceneName = "ARScene";

        private void Start()
        {
            SceneFlow.Reset();
            BuildFishButtons();
        }

        private void BuildFishButtons()
        {
            if (fishDataList == null || buttonContainer == null || fishButtonPrefab == null)
            {
                Debug.LogError("[MainMenuController] Inspector bağlantıları eksik!");
                return;
            }

            foreach (FishData data in fishDataList)
            {
                if (data == null) continue;

                GameObject btnGO = Instantiate(fishButtonPrefab, buttonContainer);
                SetupButton(btnGO, data);
            }
        }

        private void SetupButton(GameObject btnGO, FishData data)
        {
            // Buton onClick bağla
            Button btn = btnGO.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => SelectFish(data));
            }

            // İkon (Image bileşeni "Icon" adlı child'ta aranır)
            Image icon = btnGO.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && data.menuIcon != null)
                icon.sprite = data.menuIcon;

            // İsim (TextMeshProUGUI bileşeni "Label" adlı child'ta aranır)
            TextMeshProUGUI label = btnGO.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.text = data.displayName;
        }

        private void SelectFish(FishData data)
        {
            SceneFlow.SelectedFish = data;
            SceneManager.LoadScene(arSceneName);
        }
    }
}
