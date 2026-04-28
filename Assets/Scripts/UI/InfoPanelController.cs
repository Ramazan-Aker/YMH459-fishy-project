using FishMuseum.Audio;
using FishMuseum.Data;
using FishMuseum.Fish;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishMuseum.UI
{
    /// <summary>
    /// Balığa tap edildiğinde açılan bilgi panelini yönetir.
    ///
    /// Inspector bağlantıları:
    ///   panelRoot        — açılıp kapanan ana panel GameObject'i
    ///   nameText         — balığın Türkçe adı
    ///   scientificText   — bilimsel adı
    ///   habitatText      — yaşam alanı
    ///   dietText         — beslenme şekli
    ///   summaryText      — özet metin
    ///   narrationButton  — sesli anlatım toggle butonu
    ///   inspectButton    — detaylı inceleme modu butonu
    ///   closeButton      — paneli kapat butonu
    /// </summary>
    public class InfoPanelController : MonoBehaviour
    {
        [Header("Panel Kök")]
        [SerializeField] private GameObject panelRoot;

        [Header("Metin Alanları")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scientificText;
        [SerializeField] private TextMeshProUGUI habitatText;
        [SerializeField] private TextMeshProUGUI dietText;
        [SerializeField] private TextMeshProUGUI summaryText;

        [Header("Butonlar")]
        [SerializeField] private Button narrationButton;
        [SerializeField] private Button inspectButton;
        [SerializeField] private Button closeButton;

        [Header("Ses")]
        [SerializeField] private NarrationManager narrationManager;

        private FishData _currentFishData;
        private FishController _currentFish;

        private void Awake()
        {
            if (narrationManager == null)
                narrationManager = FindFirstObjectByType<NarrationManager>();

            if (panelRoot != null)
                panelRoot.SetActive(false);

            narrationButton?.onClick.AddListener(OnNarrationButtonClicked);
            inspectButton?.onClick.AddListener(OnInspectButtonClicked);
            closeButton?.onClick.AddListener(OnCloseButtonClicked);
        }

        /// <summary>
        /// Balık tap edildiğinde FishController tarafından çağrılır.
        /// </summary>
        public void Open(FishData data, FishController fish)
        {
            if (data == null) return;

            _currentFishData = data;
            _currentFish = fish;

            PopulateTexts(data);

            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        private void PopulateTexts(FishData data)
        {
            if (nameText != null) nameText.text = data.displayName;
            if (scientificText != null) scientificText.text = $"<i>{data.scientificName}</i>";
            if (habitatText != null) habitatText.text = $"Yaşam Alanı: {data.habitat}";
            if (dietText != null) dietText.text = $"Beslenme: {data.diet}";
            if (summaryText != null) summaryText.text = data.summary;
        }

        private void OnNarrationButtonClicked()
        {
            if (_currentFishData == null || narrationManager == null) return;
            narrationManager.TogglePlayPause(_currentFishData.narrationClip);
        }

        private void OnInspectButtonClicked()
        {
            if (_currentFish == null) return;

            Close(stopNarration: false);
            _currentFish.EnterInspectionMode();
        }

        private void OnCloseButtonClicked()
        {
            Close(stopNarration: true);
        }

        private void Close(bool stopNarration)
        {
            if (stopNarration)
                narrationManager?.Stop();

            if (panelRoot != null)
                panelRoot.SetActive(false);

            _currentFishData = null;
            _currentFish = null;
        }
    }
}
