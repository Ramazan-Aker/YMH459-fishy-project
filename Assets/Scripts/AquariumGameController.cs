using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AquariumGameController : MonoBehaviour
{
    public const string SelectedFishKey = "SelectedFishForAquarium";
    public const string ReturnSceneKey = "AquariumReturnScene";
    private const string MarineFishName = "marine_clownfish";
    private const string FreshwaterFishName = "freshWater_guppy";
    private const int NoFingerId = -999;

    private class PreyFish
    {
        public GameObject instance;
        public Vector2 velocity;
        public float bobSpeed;
        public float bobOffset;
        public float baseScale;
    }

    public Texture2D marineBackground;
    public Texture2D freshwaterBackground;

    public GameObject marineFishPrefab;
    public GameObject freshwaterFishPrefab;

    public Texture2D marineFishTexture;
    public Texture2D freshwaterFishTexture;

    private Sprite marineFishSprite;
    private Sprite freshwaterFishSprite;

    private readonly List<PreyFish> preyFish = new List<PreyFish>();

    private Canvas canvas;
    private Camera mainCam;
    private Font uiFont;
    private Sprite circleSprite;
    private Sprite marineBackgroundSprite;
    private Sprite freshwaterBackgroundSprite;

    private string selectedFishName;
    private int eatenFishCount;

    private GameObject playerFish;
    private float playerBaseScale = 1f; 
    private Vector2 joystickInput;
    private int joystickFingerId = NoFingerId;
    private float joystickRadius = 120f;

    private Text hudTitleText;
    private Text hudStatsText;
    private RectTransform joystickBaseRect;
    private RectTransform joystickKnobRect;
    
    private float worldWidth;
    private float worldHeight;

    private void Awake()
    {
        selectedFishName = PlayerPrefs.GetString(SelectedFishKey, MarineFishName);
        uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        circleSprite = CreateCircleSprite(256);
        marineBackgroundSprite = CreateSpriteFromTexture(marineBackground, new Color(0.05f, 0.24f, 0.34f, 1f));
        freshwaterBackgroundSprite = CreateSpriteFromTexture(freshwaterBackground, new Color(0.08f, 0.26f, 0.31f, 1f));
        marineFishSprite = CreateSpriteFromTexture(marineFishTexture, new Color(1f, 0.5f, 0f));
        freshwaterFishSprite = CreateSpriteFromTexture(freshwaterFishTexture, Color.cyan);

        CleanupCopiedScene();
        SetupWorldCameraAndBackground();
        EnsureCanvas();
        EnsureEventSystem();
        BuildUiGame();
        SpawnPlayer();
        SpawnPreySchool();
        UpdateHud();
    }

    private void Update()
    {
        UpdateJoystickInput();
        UpdatePlayerMovement();
        UpdatePreyMovement();
    }

    private void CleanupCopiedScene()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            GameObject root = rootObjects[i];
            if (root == null || root == gameObject) continue;
            root.SetActive(false);
        }
    }

    private void SetupWorldCameraAndBackground()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera", typeof(Camera));
            camObj.tag = "MainCamera";
            mainCam = camObj.GetComponent<Camera>();
        }

        mainCam.gameObject.SetActive(true);
        mainCam.orthographic = true;
        mainCam.orthographicSize = 960f; 
        mainCam.nearClipPlane = -2000f;
        mainCam.farClipPlane = 5000f;
        mainCam.cullingMask = ~0;
        
        float aspect = (float)Screen.width / Screen.height;
        worldHeight = 1920f;
        worldWidth = worldHeight * aspect;

        mainCam.transform.position = new Vector3(worldWidth * 0.5f, worldHeight * 0.5f, -1000f);
        mainCam.transform.rotation = Quaternion.identity;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = Color.black;

        GameObject bgObj = new GameObject("WorldBackground");
        bgObj.transform.position = new Vector3(worldWidth * 0.5f, worldHeight * 0.5f, 1000f);
        SpriteRenderer bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = selectedFishName == FreshwaterFishName ? freshwaterBackgroundSprite : marineBackgroundSprite;

        if (bgRenderer.sprite != null)
        {
            float boundsX = bgRenderer.sprite.bounds.size.x;
            float boundsY = bgRenderer.sprite.bounds.size.y;
            if (boundsX > 0 && boundsY > 0)
            {
                bgObj.transform.localScale = new Vector3((worldWidth / boundsX) * 1.5f, (worldHeight / boundsY) * 1.5f, 1f);
            }
        }
        
        GameObject lightObj1 = new GameObject("DirectionalLight1");
        Light dLight1 = lightObj1.AddComponent<Light>();
        dLight1.type = LightType.Directional;
        lightObj1.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        dLight1.intensity = 1.2f;

        GameObject lightObj2 = new GameObject("DirectionalLight2");
        Light dLight2 = lightObj2.AddComponent<Light>();
        dLight2.type = LightType.Directional;
        lightObj2.transform.rotation = Quaternion.Euler(-50f, 150f, 0f);
        dLight2.intensity = 0.8f;
    }

    private void EnsureCanvas()
    {
        Canvas[] existingCanvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas c in existingCanvases)
        {
            Destroy(c.gameObject);
        }

        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();

        canvas.gameObject.SetActive(true);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCam;
        canvas.planeDistance = 500f;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void BuildUiGame()
    {
        Image topPanel = CreateImage("TopPanel", canvas.transform, new Color(0.01f, 0.08f, 0.14f, 0.84f));
        SetRect(topPanel.rectTransform, new Vector2(0.03f, 0.89f), new Vector2(0.97f, 0.98f), Vector2.zero, Vector2.zero);

        hudTitleText = CreateText("HudTitle", topPanel.transform, "Aquarium Mode", 40, TextAnchor.MiddleLeft);
        SetRect(hudTitleText.rectTransform, new Vector2(0.03f, 0.10f), new Vector2(0.42f, 0.90f), Vector2.zero, Vector2.zero);
        hudTitleText.fontStyle = FontStyle.Bold;

        hudStatsText = CreateText("HudStats", topPanel.transform, "Yenilen: 0 | Boyut: x1.00", 30, TextAnchor.MiddleCenter);
        SetRect(hudStatsText.rectTransform, new Vector2(0.34f, 0.14f), new Vector2(0.79f, 0.86f), Vector2.zero, Vector2.zero);

        Button backButton = CreateStyledButton("BackButton", topPanel.transform, "AR'a Don", new Color(0.90f, 0.34f, 0.21f, 1f));
        SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.80f, 0.14f), new Vector2(0.97f, 0.86f), Vector2.zero, Vector2.zero);
        backButton.onClick.AddListener(BackToArScene);

        Image bottomPanel = CreateImage("BottomPanel", canvas.transform, new Color(0.01f, 0.08f, 0.14f, 0.78f));
        SetRect(bottomPanel.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.23f), Vector2.zero, Vector2.zero);

        Text hintText = CreateText("HintText", bottomPanel.transform, "Joystick ile hareket et, kucuk baliklari ye.", 28, TextAnchor.UpperLeft);
        SetRect(hintText.rectTransform, new Vector2(0.03f, 0.62f), new Vector2(0.75f, 0.95f), Vector2.zero, Vector2.zero);

        Image joystickBase = CreateImage("JoystickBase", bottomPanel.transform, new Color(0.08f, 0.24f, 0.34f, 0.88f));
        joystickBase.sprite = circleSprite;
        joystickBaseRect = joystickBase.rectTransform;
        SetRect(joystickBaseRect, new Vector2(0.03f, 0.06f), new Vector2(0.25f, 0.58f), Vector2.zero, Vector2.zero);

        Image joystickKnob = CreateImage("JoystickKnob", joystickBase.transform, new Color(0.16f, 0.85f, 0.95f, 0.96f));
        joystickKnob.sprite = circleSprite;
        joystickKnobRect = joystickKnob.rectTransform;
        joystickKnobRect.anchorMin = new Vector2(0.5f, 0.5f);
        joystickKnobRect.anchorMax = new Vector2(0.5f, 0.5f);
        joystickKnobRect.sizeDelta = new Vector2(96f, 96f);
        joystickKnobRect.anchoredPosition = Vector2.zero;
    }

    private GameObject Spawn3DFish(bool isFreshwater)
    {
        GameObject prefab = isFreshwater ? freshwaterFishPrefab : marineFishPrefab;
        if (prefab == null) return new GameObject("EmptyFish");

        GameObject fishObj = Instantiate(prefab);
        MonoBehaviour[] comp = fishObj.GetComponentsInChildren<MonoBehaviour>();
        if (comp != null)
        {
            foreach (var mono in comp)
            {
                if (mono != null && mono.GetType().Name != "Animator") {
                    Destroy(mono);
                }
            }
        }
        
        SetLayerRecursively(fishObj, LayerMask.NameToLayer("Default"));

        Animator anim = fishObj.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
            anim.speed = 1f;
        }

        return fishObj;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private float GetNormalizedScaleMultiplier(GameObject obj, float targetSize)
    {
        obj.transform.localScale = Vector3.one;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.position = Vector3.zero;

        Bounds localBounds = new Bounds();
        bool boundsInitialized = false;

        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter mf = meshFilters[i];
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds meshBounds = mf.sharedMesh.bounds;
                if (mf.transform == obj.transform)
                {
                    if (!boundsInitialized) { localBounds = meshBounds; boundsInitialized = true; }
                    else localBounds.Encapsulate(meshBounds);
                }
                else
                {
                    Vector3 localCenter = obj.transform.InverseTransformPoint(mf.transform.TransformPoint(meshBounds.center));
                    Vector3 localExtents = obj.transform.InverseTransformVector(mf.transform.TransformVector(meshBounds.extents));
                    localExtents = new Vector3(Mathf.Abs(localExtents.x), Mathf.Abs(localExtents.y), Mathf.Abs(localExtents.z));
                    Bounds transformedBounds = new Bounds(localCenter, localExtents * 2f);
                    if (!boundsInitialized) { localBounds = transformedBounds; boundsInitialized = true; }
                    else localBounds.Encapsulate(transformedBounds);
                }
            }
        }

        SkinnedMeshRenderer[] skinnedRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            SkinnedMeshRenderer smr = skinnedRenderers[i];
            if (smr != null && smr.sharedMesh != null)
            {
                Bounds meshBounds = smr.sharedMesh.bounds;
                if (smr.transform == obj.transform)
                {
                    if (!boundsInitialized) { localBounds = meshBounds; boundsInitialized = true; }
                    else localBounds.Encapsulate(meshBounds);
                }
                else
                {
                    Vector3 localCenter = obj.transform.InverseTransformPoint(smr.transform.TransformPoint(meshBounds.center));
                    Vector3 localExtents = obj.transform.InverseTransformVector(smr.transform.TransformVector(meshBounds.extents));
                    localExtents = new Vector3(Mathf.Abs(localExtents.x), Mathf.Abs(localExtents.y), Mathf.Abs(localExtents.z));
                    Bounds transformedBounds = new Bounds(localCenter, localExtents * 2f);
                    if (!boundsInitialized) { localBounds = transformedBounds; boundsInitialized = true; }
                    else localBounds.Encapsulate(transformedBounds);
                }
            }
        }

        if (!boundsInitialized)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds worldBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    worldBounds.Encapsulate(renderers[i].bounds);
                }
                float worldMaxDim = Mathf.Max(worldBounds.size.x, worldBounds.size.y, worldBounds.size.z);
                if (worldMaxDim > 0.0001f)
                {
                    return targetSize / worldMaxDim;
                }
            }
            return 1f;
        }

        float maxDim = Mathf.Max(localBounds.size.x, localBounds.size.y, localBounds.size.z);
        if (maxDim > 0.0001f)
        {
            return targetSize / maxDim;
        }
        return 1f;
    }

    private void SpawnPlayer()
    {
        playerFish = Spawn3DFish(selectedFishName == FreshwaterFishName);
        playerFish.name = "PlayerFish";
        
        float scaleMultiplier = GetNormalizedScaleMultiplier(playerFish, 350f);
        playerBaseScale = scaleMultiplier;
        
        playerFish.transform.localScale = Vector3.one * playerBaseScale;
        playerFish.transform.position = new Vector3(worldWidth * 0.2f, worldHeight * 0.5f, 0f);
        playerFish.transform.rotation = Quaternion.Euler(0f, 90f, 0f); 
    }

    private void SpawnPreySchool()
    {
        for (int i = 0; i < 7; i++) SpawnPrey(i);
    }

    private void SpawnPrey(int index)
    {
        GameObject preyObj = Spawn3DFish(index % 2 != 0);
        preyObj.name = "Prey_" + index;

        PreyFish prey = new PreyFish();
        prey.instance = preyObj;
        
        float targetPreySize = Random.Range(100f, 200f);
        prey.baseScale = GetNormalizedScaleMultiplier(preyObj, targetPreySize);
        prey.instance.transform.localScale = Vector3.one * prey.baseScale;
        
        prey.bobSpeed = Random.Range(1.2f, 2.1f);
        prey.bobOffset = Random.Range(0f, 5f);
        
        preyObj.transform.rotation = Quaternion.Euler(0f, -90f, 0f); 
        preyFish.Add(prey);
        
        ResetPrey(prey, true);
    }

    private void ResetPrey(PreyFish prey, bool randomHeight)
    {
        float height = randomHeight
            ? Random.Range(400f, worldHeight - 400f)
            : Mathf.Clamp(prey.instance.transform.position.y + Random.Range(-300f, 300f), 400f, worldHeight - 400f);

        prey.instance.transform.position = new Vector3(worldWidth + Random.Range(100f, 500f), height, 0f);
        prey.velocity = new Vector2(Random.Range(-400f, -200f), 0f);
        prey.instance.transform.rotation = Quaternion.Euler(0f, -90f, 0f); 
    }

    private void UpdateJoystickInput()
    {
        if (joystickBaseRect == null) return;

        if (Input.touchCount > 0)
        {
            bool activeTouch = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                bool tracked = joystickFingerId == touch.fingerId;
                bool inside = RectTransformUtility.RectangleContainsScreenPoint(joystickBaseRect, touch.position, null);
                if (!inside && !tracked) continue;

                if (touch.phase == TouchPhase.Began) joystickFingerId = touch.fingerId;

                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    UpdateJoystickPosition(touch.position);
                    activeTouch = true;
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) ResetJoystick();
            }

            if (!activeTouch && joystickFingerId == NoFingerId) ResetJoystick();
            return;
        }

        if (Input.GetMouseButton(0))
        {
            if (joystickFingerId != NoFingerId || RectTransformUtility.RectangleContainsScreenPoint(joystickBaseRect, Input.mousePosition, null))
            {
                joystickFingerId = 1;
                UpdateJoystickPosition(Input.mousePosition);
                return;
            }
        }

        ResetJoystick();
    }

    private void UpdateJoystickPosition(Vector2 screenPosition)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBaseRect, screenPosition, canvas.worldCamera, out localPoint);
        Vector2 normalized = Vector2.ClampMagnitude(localPoint / joystickRadius, 1f);
        joystickInput = normalized;
        joystickKnobRect.anchoredPosition = normalized * joystickRadius;
    }

    private void ResetJoystick()
    {
        joystickFingerId = NoFingerId;
        joystickInput = Vector2.zero;
        if (joystickKnobRect != null) joystickKnobRect.anchoredPosition = Vector2.zero;
    }

    private void UpdatePlayerMovement()
    {
        if (playerFish == null) return;

        if (joystickInput.sqrMagnitude > 0.001f)
        {
            Vector3 pos = playerFish.transform.position;
            pos.x += joystickInput.x * 550f * Time.deltaTime;
            pos.y += joystickInput.y * 550f * Time.deltaTime;
            
            pos.x = Mathf.Clamp(pos.x, 150f, worldWidth - 150f);
            pos.y = Mathf.Clamp(pos.y, 450f, worldHeight - 250f); 
            playerFish.transform.position = pos;

            float yTilt = -joystickInput.y * 20f;
            float faceDir = joystickInput.x >= 0f ? 90f : -90f;
            playerFish.transform.rotation = Quaternion.Euler(0f, faceDir, yTilt * (faceDir > 0 ? 1 : -1));
        }
    }

    private void UpdatePreyMovement()
    {
        if (playerFish == null) return;

        for (int i = preyFish.Count - 1; i >= 0; i--)
        {
            PreyFish prey = preyFish[i];
            Vector3 pos = prey.instance.transform.position;
            
            pos.x += prey.velocity.x * Time.deltaTime;
            pos.y += Mathf.Sin(Time.time * prey.bobSpeed + prey.bobOffset) * Time.deltaTime * 60f;
            prey.instance.transform.position = pos;

            if (Vector3.Distance(playerFish.transform.position, prey.instance.transform.position) < 180f)
            {
                eatenFishCount++;
                float growth = 1f + eatenFishCount * 0.04f;
                playerFish.transform.localScale = Vector3.one * playerBaseScale * growth;
                UpdateHud();
                ResetPrey(prey, false);
                continue;
            }

            if (pos.x < -250f) ResetPrey(prey, true);
        }
    }

    private void UpdateHud()
    {
        hudTitleText.text = selectedFishName == FreshwaterFishName ? "Freshwater Guppy" : "Marine Clownfish";
        hudStatsText.text = "Yenilen: " + eatenFishCount + " | Boyut: x" + (1f + eatenFishCount * 0.04f).ToString("F2");
    }

    private void BackToArScene()
    {
        SceneManager.LoadScene(PlayerPrefs.GetString(ReturnSceneKey, "MainScene"));
    }

    private Button CreateStyledButton(string objectName, Transform parent, string label, Color backgroundColor)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = backgroundColor;
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.12f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = backgroundColor * 1.08f;
        colors.pressedColor = backgroundColor * 0.86f;
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text buttonText = CreateText("Label", buttonObject.transform, label, 35, TextAnchor.MiddleCenter);
        SetRect(buttonText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        buttonText.fontStyle = FontStyle.Bold;
        return button;
    }

    private Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(string objectName, Transform parent, string value, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        if (uiFont != null) text.font = uiFont;
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size * 0.48f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = distance <= radius ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateSpriteFromTexture(Texture2D texture, Color fallbackColor)
    {
        if (texture == null)
        {
            Texture2D fallbackTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            for(int i=0; i<4; i++) fallbackTexture.SetPixel(i%2, i/2, fallbackColor);
            fallbackTexture.Apply();
            texture = fallbackTexture;
        }
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}
