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
    private GameObject aboutPanel;
    private static bool splashPlayed = false;

    private void Start()
    {
        EnsureMenuReferences();
        ConfigureResponsiveLayout();
        BindButtonListeners();

        if (splashLogo != null)
        {
            Image splashLogoImage = splashLogo.GetComponent<Image>();
            if (splashLogoImage != null)
            {
                splashLogoImage.sprite = CreateProfessionalLogoSprite(512);
            }
        }

        if (!splashPlayed)
        {
            splashPlayed = true;

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
                if (btn == null) continue;
                btn.localScale = Vector3.one * 0.86f;
                CanvasGroup buttonGroup = GetOrAddCanvasGroup(btn.gameObject);
                buttonGroup.alpha = 0f;
            }

            // Animasyon Sekansını Başlat
            StartCoroutine(SplashSequence());
        }
        else
        {
            // Skip splash, show main menu immediately
            splashPanel.gameObject.SetActive(false);
            loadingPanel.gameObject.SetActive(false);

            mainMenuPanel.alpha = 1f;
            mainMenuPanel.gameObject.SetActive(true);

            logoTargetPosition = logoText.anchoredPosition;
            logoText.anchoredPosition = logoTargetPosition;

            foreach (var btn in menuButtons)
            {
                if (btn == null) continue;
                btn.localScale = Vector3.one;
                CanvasGroup buttonGroup = GetOrAddCanvasGroup(btn.gameObject);
                buttonGroup.alpha = 1f;
            }
        }
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
        // Reset progress bar values
        if (progressBar != null)
            progressBar.value = 0f;
        if (progressText != null)
            progressText.text = "%0";

        // Menüyü karart
        StartCoroutine(FadeOutPanel(mainMenuPanel, 0.3f));
        yield return new WaitForSeconds(0.3f);
        mainMenuPanel.gameObject.SetActive(false);

        // Loading panelini göster
        loadingPanel.gameObject.SetActive(true);
        StartCoroutine(FadeInPanel(loadingPanel, 0.3f));
        yield return new WaitForSeconds(0.3f);

        // Asenkron sahne yükleme işlemi
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float fakeProgress = 0f;

        while (!operation.isDone)
        {
            // Advance fake progress to make the bar move smoothly
            fakeProgress = Mathf.MoveTowards(fakeProgress, 1f, Time.deltaTime * 1.5f);
            
            // Real progress is capped at 0.9f until activation
            float realProgress = operation.progress / 0.9f;
            
            // Displayed progress is the minimum of fake progress and real progress
            float displayProgress = Mathf.Min(fakeProgress, realProgress);

            if (progressBar != null)
                progressBar.value = displayProgress;
            
            if (progressText != null)
                progressText.text = "%" + (displayProgress * 100f).ToString("F0");

            if (realProgress >= 1f && fakeProgress >= 0.95f)
            {
                progressBar.value = 1f;
                if (progressText != null) progressText.text = "%100";
                
                yield return new WaitForSeconds(0.2f); // %100'ü kullanıcı görsün
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

    private void BindButtonListeners()
    {
        if (menuButtons == null || menuButtons.Length == 0) return;

        List<RectTransform> activeButtons = new List<RectTransform>();

        foreach (var rect in menuButtons)
        {
            if (rect == null) continue;

            Button button = rect.GetComponent<Button>();
            if (button == null) continue;

            Text btnText = rect.GetComponentInChildren<Text>(true);
            string label = (btnText != null) ? btnText.text.ToLower() : "";
            string objName = rect.gameObject.name.ToLower();

            // Check if it's the Settings button (remove/hide it)
            if (label.Contains("ayar") || label.Contains("setting") || objName.Contains("ayar") || objName.Contains("settings"))
            {
                rect.gameObject.SetActive(false);
                continue; // Skip settings button
            }

            activeButtons.Add(rect);

            button.onClick.RemoveAllListeners();

            if (label.Contains("hakkında") || label.Contains("about") || objName.Contains("about") || objName.Contains("hakkinda"))
            {
                button.onClick.AddListener(ShowAboutPanel);
            }
            else if (label.Contains("çıkış") || label.Contains("cikis") || label.Contains("exit") || objName.Contains("exit") || objName.Contains("cikis"))
            {
                button.onClick.AddListener(ExitApplication);
            }
            else
            {
                button.onClick.AddListener(StartARScene);
            }
        }

        menuButtons = activeButtons.ToArray();
    }

    private void ShowAboutPanel()
    {
        if (aboutPanel != null)
        {
            aboutPanel.SetActive(true);
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Overlay background (semi-transparent dark blue glassmorphism)
        aboutPanel = new GameObject("AboutPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        aboutPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRT = aboutPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        
        Image panelImg = aboutPanel.GetComponent<Image>();
        panelImg.color = new Color(0.01f, 0.04f, 0.08f, 0.95f);

        // Center card
        GameObject card = new GameObject("AboutCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(aboutPanel.transform, false);
        
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.1f, 0.25f);
        cardRT.anchorMax = new Vector2(0.9f, 0.75f);
        cardRT.offsetMin = Vector2.zero;
        cardRT.offsetMax = Vector2.zero;
        
        Image cardImg = card.GetComponent<Image>();
        cardImg.color = new Color(0.03f, 0.10f, 0.22f, 1f);
        
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.05f, 0.77f, 0.90f, 0.3f);
        cardOutline.effectDistance = new Vector2(3f, -3f);

        Font uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Title
        GameObject titleObj = new GameObject("AboutTitle", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(card.transform, false);
        
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.05f, 0.8f);
        titleRT.anchorMax = new Vector2(0.95f, 0.95f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        
        Text titleText = titleObj.GetComponent<Text>();
        titleText.font = uiFont;
        titleText.text = "PROJE HAKKINDA";
        titleText.fontSize = 44;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.05f, 0.77f, 0.90f, 1f);

        // Description Text
        GameObject descObj = new GameObject("AboutDesc", typeof(RectTransform), typeof(Text));
        descObj.transform.SetParent(card.transform, false);
        
        RectTransform descRT = descObj.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0.08f, 0.28f);
        descRT.anchorMax = new Vector2(0.92f, 0.76f);
        descRT.offsetMin = Vector2.zero;
        descRT.offsetMax = Vector2.zero;
        
        Text descText = descObj.GetComponent<Text>();
        descText.font = uiFont;
        descText.text = "Bu uygulama, Vuforia AR (Artırılmış Gerçeklik) teknolojisi kullanılarak hazırlanmış interaktif bir sanal akvaryum projesidir.\n\n" +
                        "Görsel hedef kartlarını (Image Target) kameraya okutarak balıkları canlandırabilir, üzerlerine dokunarak sesli bilgilendirme panelini açabilir, her balığa özel 10 soruluk testler çözebilir ve akvaryum oyununda balığınızı büyütebilirsiniz.";
        descText.fontSize = 28;
        descText.alignment = TextAnchor.MiddleLeft;
        descText.color = Color.white;
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow = VerticalWrapMode.Overflow;

        // Close Button
        GameObject closeBtnObj = new GameObject("BtnCloseAbout", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(card.transform, false);
        
        RectTransform closeBtnRT = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(0.25f, 0.08f);
        closeBtnRT.anchorMax = new Vector2(0.75f, 0.22f);
        closeBtnRT.offsetMin = Vector2.zero;
        closeBtnRT.offsetMax = Vector2.zero;
        
        Image closeBtnImg = closeBtnObj.GetComponent<Image>();
        closeBtnImg.color = new Color(0.78f, 0.18f, 0.14f, 1f);
        
        Button closeBtn = closeBtnObj.GetComponent<Button>();
        ColorBlock colors = closeBtn.colors;
        colors.normalColor = new Color(0.78f, 0.18f, 0.14f, 1f);
        colors.highlightedColor = new Color(0.88f, 0.24f, 0.18f, 1f);
        colors.pressedColor = new Color(0.60f, 0.12f, 0.10f, 1f);
        colors.selectedColor = colors.highlightedColor;
        closeBtn.colors = colors;

        GameObject closeBtnTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        closeBtnTextObj.transform.SetParent(closeBtnObj.transform, false);
        
        RectTransform closeTextRT = closeBtnTextObj.GetComponent<RectTransform>();
        closeTextRT.anchorMin = Vector2.zero;
        closeTextRT.anchorMax = Vector2.one;
        closeTextRT.offsetMin = Vector2.zero;
        closeTextRT.offsetMax = Vector2.zero;
        
        Text closeBtnText = closeBtnTextObj.GetComponent<Text>();
        closeBtnText.font = uiFont;
        closeBtnText.text = "Kapat";
        closeBtnText.fontSize = 32;
        closeBtnText.fontStyle = FontStyle.Bold;
        closeBtnText.alignment = TextAnchor.MiddleCenter;
        closeBtnText.color = Color.white;

        closeBtn.onClick.AddListener(() => aboutPanel.SetActive(false));
    }

    private void ExitApplication()
    {
        Debug.Log("[MainMenuController] Exiting application...");
        Application.Quit();
    }

    private Sprite CreateProfessionalLogoSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float halfSize = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = ((x - halfSize) / halfSize) * 1.2f;
                float py = ((y - halfSize) / halfSize) * 1.2f;
                
                float r = Mathf.Sqrt(px * px + py * py);
                Color pixelColor = Color.clear;
                
                if (r <= 1.0f)
                {
                    float tBg = (py + 1.0f) / 2.0f;
                    Color bgColor = Color.Lerp(
                        new Color(0.02f, 0.05f, 0.12f, 1f),
                        new Color(0.04f, 0.22f, 0.38f, 1f),
                        tBg
                    );
                    pixelColor = bgColor;

                    if (r >= 0.92f && r <= 0.98f)
                    {
                        float borderAlpha = 1.0f;
                        if (r < 0.94f) borderAlpha = (r - 0.92f) / 0.02f;
                        else if (r > 0.96f) borderAlpha = (0.98f - r) / 0.02f;
                        
                        Color borderColor = Color.Lerp(
                            new Color(0.95f, 0.75f, 0.15f, 1f),
                            new Color(0.19f, 0.84f, 0.93f, 1f),
                            (px + 1f) / 2f
                        );
                        pixelColor = Color.Lerp(pixelColor, borderColor, borderAlpha);
                    }
                    
                    float fx = px + 0.1f;
                    float fy = py;
                    
                    bool insideFish = false;
                    
                    if (fx >= -0.7f && fx <= 0.7f)
                    {
                        float widthFactor = (1.0f - (fx * fx) / 0.49f);
                        float thickness = 0.28f * Mathf.Sqrt(Mathf.Max(0f, widthFactor));
                        if (Mathf.Abs(fy) <= thickness)
                        {
                            insideFish = true;
                        }
                    }
                    
                    if (fx < -0.6f && fx >= -0.95f)
                    {
                        float distBack = -0.6f - fx;
                        float tailHeight = distBack * 0.8f;
                        if (Mathf.Abs(fy) <= tailHeight && Mathf.Abs(fy) >= tailHeight * 0.4f)
                        {
                            insideFish = true;
                        }
                    }

                    if (fx >= -0.3f && fx <= 0.2f && fy > 0f)
                    {
                        float finTop = 0.15f * (0.2f - fx) * (fx + 0.3f);
                        if (fy <= finTop + 0.1f && fy >= 0f)
                        {
                            insideFish = true;
                        }
                    }

                    if (insideFish)
                    {
                        float eyeDx = fx - 0.4f;
                        float eyeDy = fy - 0.06f;
                        float eyeR = Mathf.Sqrt(eyeDx * eyeDx + eyeDy * eyeDy);
                        
                        if (eyeR <= 0.04f)
                        {
                            pixelColor = new Color(0.02f, 0.05f, 0.12f, 1f);
                        }
                        else
                        {
                            float tFish = (fx + 0.95f) / 1.7f;
                            Color fishColor = Color.Lerp(
                                new Color(0.19f, 0.84f, 0.93f, 1f),
                                Color.white,
                                tFish
                            );
                            pixelColor = fishColor;
                        }
                    }
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }
}
