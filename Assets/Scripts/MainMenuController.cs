using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public CanvasGroup splashPanel;
    public CanvasGroup mainMenuPanel;
    public CanvasGroup loadingPanel;

    [Header("Splash Elements")]
    public RectTransform splashLogo;

    [Header("Main Menu Elements")]
    public RectTransform logoText;
    public RectTransform[] menuButtons;
    public RectTransform settingsButton;

    [Header("Loading Elements")]
    public Slider progressBar;
    public Text progressText;

    [Header("Settings")]
    public string arSceneName = "MainScene";
    public float splashDuration = 2f;

    private Vector2 logoTargetPosition;

    private void Start()
    {
        EnsureMenuReferences();
        ConfigureResponsiveLayout();

        // Başlangıç durumu ayarları
        splashPanel.alpha = 1;
        splashPanel.gameObject.SetActive(true);
        splashLogo.localScale = Vector3.one * 0.8f; // Logo biraz küçük başlar

        mainMenuPanel.alpha = 0;
        mainMenuPanel.gameObject.SetActive(false);

        loadingPanel.alpha = 0;
        loadingPanel.gameObject.SetActive(false);

        // Logo yukarıdan gelsin
        logoTargetPosition = logoText.anchoredPosition;
        logoText.anchoredPosition = logoTargetPosition + new Vector2(0, 80f);

        // Menü elemanlarını gizli hale al
        foreach (var btn in menuButtons)
        {
            if (btn == null)
            {
                continue;
            }

            btn.localScale = Vector3.one * 0.86f;
            CanvasGroup buttonGroup = GetOrAddCanvasGroup(btn.gameObject);
            buttonGroup.alpha = 0f;
        }

        // Animasyon Sekansını Başlat
        StartCoroutine(SplashSequence());
    }

    private IEnumerator SplashSequence()
    {
        // Logo yavaşça büyüsün (Pürüzsüz Lerp)
        float elapsed = 0f;
        while (elapsed < splashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / splashDuration;
            // EaseOut efekti (yavaşlayarak bitme)
            t = Mathf.Sin(t * Mathf.PI * 0.5f); 
            splashLogo.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one * 1.2f, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // Splash ekranını karart (Fade Out)
        elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            splashPanel.alpha = Mathf.Lerp(1f, 0f, elapsed / 1f);
            yield return null;
        }

        splashPanel.gameObject.SetActive(false);
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.gameObject.SetActive(true);
        StartCoroutine(FadeInPanel(mainMenuPanel, 0.5f));
        StartCoroutine(AnimateMainMenuElements());
    }

    private IEnumerator AnimateMainMenuElements()
    {
        // Logo yukarıdan pürüzsüz gelsin
        StartCoroutine(MoveAnchorPos(logoText, logoTargetPosition + new Vector2(0, 80f), logoTargetPosition, 0.8f));

        // Butonlar sırayla gelsin (Stagger)
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
            {
                continue;
            }

            yield return new WaitForSeconds(0.15f); // Butonlar arası gecikme
            StartCoroutine(BounceScale(menuButtons[i], 0.45f));
            StartCoroutine(FadeCanvasGroup(GetOrAddCanvasGroup(menuButtons[i].gameObject), 0f, 1f, 0.3f));
        }
    }

    // AR Kamerayı Başlat Butonuna Tıklanınca Çalışacak
    public void StartARScene()
    {
        StartCoroutine(LoadSceneAsync(arSceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Menüyü karart
        StartCoroutine(FadeOutPanel(mainMenuPanel, 0.5f));
        yield return new WaitForSeconds(0.5f);
        mainMenuPanel.gameObject.SetActive(false);

        // Loading panelini göster
        loadingPanel.gameObject.SetActive(true);
        StartCoroutine(FadeInPanel(loadingPanel, 0.5f));

        yield return new WaitForSeconds(0.5f);

        // Asenkron sahne yükleme işlemi
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // Progress barı pürüzsüz doldur
            if (progressBar != null)
                progressBar.value = Mathf.Lerp(progressBar.value, progress, Time.deltaTime * 5f);
            
            if (progressText != null)
                progressText.text = "%" + (progress * 100f).ToString("F0");

            if (operation.progress >= 0.9f && progressBar.value >= 0.95f)
            {
                progressBar.value = 1f;
                if (progressText != null) progressText.text = "%100";
                
                yield return new WaitForSeconds(0.5f); // %100'ü kullanıcı görsün
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    #region Custom Animation Helpers (No DOTween Required)

    private IEnumerator FadeInPanel(CanvasGroup panel, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            panel.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        panel.alpha = 1f;
    }

    private IEnumerator FadeOutPanel(CanvasGroup panel, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            panel.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        panel.alpha = 0f;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        group.alpha = end;
    }

    private IEnumerator MoveAnchorPos(RectTransform rect, Vector2 start, Vector2 end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease Out
            rect.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
        rect.anchoredPosition = end;
    }

    private IEnumerator BounceScale(RectTransform rect, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Özel Bounce Matematiği (Sıfırdan büyür, sınırı hafif aşar, geri döner)
            float scale = Mathf.LerpUnclamped(0f, 1f, BounceEaseOut(t));
            rect.localScale = new Vector3(scale, scale, scale);
            
            yield return null;
        }
        rect.localScale = Vector3.one;
    }

    private IEnumerator RotateOverTime(RectTransform rect, float angle, float duration)
    {
        float elapsed = 0f;
        Vector3 startRot = rect.eulerAngles;
        Vector3 endRot = startRot + new Vector3(0, 0, angle);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease Out
            rect.eulerAngles = Vector3.Lerp(startRot, endRot, t);
            yield return null;
        }
        rect.eulerAngles = endRot;
    }

    // Basit bir yaylanma (bounce) fonksiyonu
    private float BounceEaseOut(float t)
    {
        if (t < 1f / 2.75f)
        {
            return 7.5625f * t * t;
        }
        else if (t < 2f / 2.75f)
        {
            t -= 1.5f / 2.75f;
            return 7.5625f * t * t + 0.75f;
        }
        else if (t < 2.5f / 2.75f)
        {
            t -= 2.25f / 2.75f;
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }
    }

    #endregion

    private void EnsureMenuReferences()
    {
        if (mainMenuPanel == null)
        {
            return;
        }

        if (menuButtons == null || menuButtons.Length == 0)
        {
            VerticalLayoutGroup layoutGroup = mainMenuPanel.GetComponentInChildren<VerticalLayoutGroup>(true);
            if (layoutGroup != null)
            {
                List<RectTransform> foundButtons = new List<RectTransform>();
                for (int i = 0; i < layoutGroup.transform.childCount; i++)
                {
                    Button button = layoutGroup.transform.GetChild(i).GetComponent<Button>();
                    if (button != null)
                    {
                        foundButtons.Add(button.GetComponent<RectTransform>());
                    }
                }

                menuButtons = foundButtons.ToArray();
            }
        }

        if (settingsButton == null && menuButtons != null && menuButtons.Length > 0)
        {
            settingsButton = menuButtons[menuButtons.Length - 1];
        }
    }

    private void ConfigureResponsiveLayout()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.localScale = Vector3.one;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            Transform background = canvas.transform.Find("Background");
            if (background != null)
            {
                RectTransform backgroundRect = background.GetComponent<RectTransform>();
                if (backgroundRect != null)
                {
                    backgroundRect.anchorMin = Vector2.zero;
                    backgroundRect.anchorMax = Vector2.one;
                    backgroundRect.offsetMin = Vector2.zero;
                    backgroundRect.offsetMax = Vector2.zero;
                }
            }
        }

        if (logoText != null)
        {
            logoText.anchorMin = new Vector2(0.5f, 1f);
            logoText.anchorMax = new Vector2(0.5f, 1f);
            logoText.pivot = new Vector2(0.5f, 0.5f);
            logoText.sizeDelta = new Vector2(760f, 120f);
            logoText.anchoredPosition = new Vector2(0f, -220f);

            Text titleText = logoText.GetComponent<Text>();
            if (titleText != null)
            {
                titleText.fontSize = 72;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = Color.white;
                titleText.fontStyle = FontStyle.Bold;
            }
        }

        VerticalLayoutGroup buttonLayout = mainMenuPanel != null
            ? mainMenuPanel.GetComponentInChildren<VerticalLayoutGroup>(true)
            : null;

        if (buttonLayout != null)
        {
            RectTransform containerRect = buttonLayout.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                containerRect.anchorMin = new Vector2(0.5f, 0.5f);
                containerRect.anchorMax = new Vector2(0.5f, 0.5f);
                containerRect.pivot = new Vector2(0.5f, 0.5f);
                containerRect.sizeDelta = new Vector2(760f, 760f);
                containerRect.anchoredPosition = new Vector2(0f, 80f);
                containerRect.localScale = Vector3.one;

                Image containerImage = containerRect.GetComponent<Image>();
                if (containerImage != null)
                {
                    containerImage.color = new Color(0.03f, 0.08f, 0.16f, 0.72f);
                }
            }

            buttonLayout.padding.left = 36;
            buttonLayout.padding.right = 36;
            buttonLayout.padding.top = 36;
            buttonLayout.padding.bottom = 36;
            buttonLayout.spacing = 24f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            RectTransform buttonRect = menuButtons[i];
            if (buttonRect == null)
            {
                continue;
            }

            buttonRect.localScale = Vector3.one;

            LayoutElement layoutElement = buttonRect.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.minHeight = 130f;
                layoutElement.preferredHeight = 130f;
                layoutElement.flexibleWidth = 1f;
            }

            Image buttonImage = buttonRect.GetComponent<Image>();
            if (buttonImage != null)
            {
                bool isPrimary = i == 0;
                buttonImage.color = isPrimary
                    ? new Color(0.05f, 0.77f, 0.90f, 1f)
                    : new Color(0.03f, 0.15f, 0.38f, 1f);
            }

            Button button = buttonRect.GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                bool isPrimary = i == 0;
                colors.normalColor = isPrimary
                    ? new Color(0.05f, 0.77f, 0.90f, 1f)
                    : new Color(0.03f, 0.15f, 0.38f, 1f);
                colors.highlightedColor = isPrimary
                    ? new Color(0.15f, 0.87f, 1f, 1f)
                    : new Color(0.08f, 0.24f, 0.48f, 1f);
                colors.pressedColor = isPrimary
                    ? new Color(0.04f, 0.60f, 0.72f, 1f)
                    : new Color(0.02f, 0.11f, 0.29f, 1f);
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
            }

            Text buttonText = buttonRect.GetComponentInChildren<Text>(true);
            if (buttonText != null)
            {
                buttonText.fontSize = 46;
                buttonText.fontStyle = FontStyle.Bold;
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.color = Color.white;
                buttonText.resizeTextForBestFit = false;
            }
        }

        if (splashLogo != null)
        {
            splashLogo.sizeDelta = new Vector2(320f, 320f);
            splashLogo.localScale = Vector3.one;

            Image splashLogoImage = splashLogo.GetComponent<Image>();
            if (splashLogoImage != null)
            {
                splashLogoImage.preserveAspect = true;
            }
        }

        if (progressBar != null)
        {
            RectTransform progressRect = progressBar.GetComponent<RectTransform>();
            if (progressRect != null)
            {
                progressRect.sizeDelta = new Vector2(520f, 36f);
            }
        }

        if (progressText != null)
        {
            progressText.fontSize = 36;
            progressText.alignment = TextAnchor.MiddleCenter;
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }

        return group;
    }
}
