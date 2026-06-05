using System.Collections.Generic;
using FishAlive;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ARAquariumController : MonoBehaviour
{
    private const string ClownfishName = "marine_clownfish";
    private const string GuppyName = "freshWater_guppy";
    private const int NoFingerId = -999;

    private class FishRuntime
    {
        public string fishName;
        public Transform root;
        public Renderer[] renderers;
        public Animator animator;
        public FishMotion fishMotion;
        public Vector3 baseLocalPosition;
        public float baseHeight;
        public float minHeight;
        public float maxHeight;
    }

    private class AquariumPrey
    {
        public GameObject instance;
        public Vector3 velocity;
        public float bobOffset;
        public float bobSpeed;
        public float scale;
        public bool isJellyfish;
        public bool isBigFish;
    }

    private class FishInfo
    {
        public string turkishName;
        public string scientificName;
        public string family;
        public string habitat;
        public string diet;
        public string size;
        public string feature;
        public string funFact;
    }

    private readonly Dictionary<string, FishRuntime> fishLookup = new Dictionary<string, FishRuntime>();
    private readonly List<FishRuntime> fishList = new List<FishRuntime>();
    private readonly List<AquariumPrey> preyFish = new List<AquariumPrey>();
    private readonly Dictionary<string, FishInfo> fishInfoDatabase = new Dictionary<string, FishInfo>();

    private Camera arCamera;
    private Font uiFont;
    public Texture2D marineAquariumBackground;
    public Texture2D freshwaterAquariumBackground;

    private FishRuntime currentFish;
    private FishRuntime lastKnownFish;

    private Canvas uiCanvas;
    private GameObject arControlPanel;
    private Text selectedFishText;
    private Text helperText;
    private Button rotateLeftButton;
    private Button rotateRightButton;
    private Button moveUpButton;
    private Button moveDownButton;
    private Button swimButton;
    private Text controlTitleText;

    private GameObject aquariumOverlay;
    private RawImage aquariumView;
    private Text aquariumInfoText;
    private Text aquariumHintText;
    private Button exitAquariumButton;
    private Image aquariumHudTop;
    private Image aquariumHudBottom;
    private RectTransform joystickBaseRect;
    private RectTransform joystickKnobRect;
    private Image joystickBaseImage;
    private Image joystickKnobImage;
    private Vector2 joystickInput;
    private int joystickFingerId = NoFingerId;
    private float joystickRadius = 110f;
    private Sprite circleSprite;

    private GameObject infoPanel;
    private Text infoTitleText;
    private Text infoScientificText;
    private Text infoHabitatValue;
    private Text infoDietValue;
    private Text infoSizeValue;
    private Text infoFeatureValue;
    private Text infoFunFactText;
    private Button infoCloseButton;
    private bool infoVisible;

    // Aquarium game (UI-based, no extra camera needed)
    private RectTransform gameWorldPanel; // full-screen UI panel that holds fish images
    private RectTransform player2DRect;   // player fish RectTransform

    private GameObject playerFishInstance;
    private FishRuntime aquariumSourceFish;
    private Vector3 playerTargetPosition;
    private Vector3 playerBaseScale;
    private int eatenFishCount;
    private bool aquariumMode;
    private bool draggingFish;
    private Vector2 lastMousePosition;
    private Vector2 touchStartPos;
    private float touchStartTime;

    // Game Over state and UI
    private GameObject gameOverPanel;
    private Text gameOverReasonText;
    private Text gameOverScoreText;
    private Button gameOverRestartButton;
    private Button gameOverExitButton;
    private bool isGameOver;

    private void Awake()
    {
        arCamera = GetComponent<Camera>();
        if (arCamera == null) arCamera = Camera.main;
        uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        circleSprite = CreateCircleSprite(256);

        CacheFishRoots();
        CreateRuntimeUI();
        PopulateFishInfo();
        CreateInfoPanel();
        CreateAquariumOverlayUI();
        CreateAquariumWorld();
        ApplyFishDefaults();
        DisableImageTargetPreviewRenderers();

        if (fishList.Count > 0)
        {
            lastKnownFish = fishList[0];
        }
    }

    private void Update()
    {
        if (aquariumMode)
        {
            if (isGameOver) return;
            UpdateAquariumWorld();
            HandleAquariumInput();
            return;
        }

        UpdateCurrentFish();
        DisableImageTargetPreviewRenderers();
        UpdateArPanelState();
        HandleArTouchInput();
    }

    private void CacheFishRoots()
    {
        fishLookup.Clear();
        fishList.Clear();

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate.name != ClownfishName && candidate.name != GuppyName)
            {
                continue;
            }

            FishRuntime fish = new FishRuntime();
            fish.fishName = candidate.name;
            fish.root = candidate;
            fish.renderers = candidate.GetComponentsInChildren<Renderer>(true);
            fish.animator = candidate.GetComponent<Animator>();
            fish.fishMotion = candidate.GetComponent<FishMotion>();
            fish.baseLocalPosition = candidate.localPosition;
            fish.baseHeight = candidate.localPosition.y;
            fish.minHeight = fish.baseHeight - 0.15f;
            fish.maxHeight = fish.baseHeight + 0.2f;

            // Ensure the fish has a collider for raycasting
            Collider col = candidate.GetComponentInChildren<Collider>(true);
            if (col == null)
            {
                MeshFilter[] meshFilters = candidate.GetComponentsInChildren<MeshFilter>(true);
                Bounds localBounds = new Bounds();
                bool boundsInitialized = false;
                for (int m = 0; m < meshFilters.Length; m++)
                {
                    MeshFilter mf = meshFilters[m];
                    if (mf != null && mf.sharedMesh != null)
                    {
                        Bounds meshBounds = mf.sharedMesh.bounds;
                        if (mf.transform == candidate)
                        {
                            if (!boundsInitialized)
                            {
                                localBounds = meshBounds;
                                boundsInitialized = true;
                            }
                            else
                            {
                                localBounds.Encapsulate(meshBounds);
                            }
                        }
                        else
                        {
                            Vector3 localCenter = candidate.InverseTransformPoint(mf.transform.TransformPoint(meshBounds.center));
                            Vector3 localExtents = candidate.InverseTransformVector(mf.transform.TransformVector(meshBounds.extents));
                            localExtents = new Vector3(Mathf.Abs(localExtents.x), Mathf.Abs(localExtents.y), Mathf.Abs(localExtents.z));
                            Bounds transformedBounds = new Bounds(localCenter, localExtents * 2f);
                            if (!boundsInitialized)
                            {
                                localBounds = transformedBounds;
                                boundsInitialized = true;
                            }
                            else
                            {
                                localBounds.Encapsulate(transformedBounds);
                            }
                        }
                    }
                }

                if (!boundsInitialized && fish.renderers != null)
                {
                    Bounds worldBounds = new Bounds();
                    for (int r = 0; r < fish.renderers.Length; r++)
                    {
                        Renderer rendererComponent = fish.renderers[r];
                        if (rendererComponent != null)
                        {
                            if (!boundsInitialized)
                            {
                                worldBounds = rendererComponent.bounds;
                                boundsInitialized = true;
                            }
                            else
                            {
                                worldBounds.Encapsulate(rendererComponent.bounds);
                            }
                        }
                    }
                    if (boundsInitialized)
                    {
                        Vector3 localCenter = candidate.InverseTransformPoint(worldBounds.center);
                        Vector3 localSize = candidate.InverseTransformVector(worldBounds.size);
                        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
                        localBounds = new Bounds(localCenter, localSize);
                    }
                }

                if (!boundsInitialized)
                {
                    localBounds = new Bounds(Vector3.zero, Vector3.one * 0.5f);
                }

                BoxCollider box = candidate.gameObject.AddComponent<BoxCollider>();
                box.center = localBounds.center;
                box.size = localBounds.size;
                Debug.Log("[ARAquariumController] Added dynamic BoxCollider to " + candidate.name + " with center: " + box.center + ", size: " + box.size);
            }

            fishLookup[fish.fishName] = fish;
            fishList.Add(fish);
        }
    }

    private void ApplyFishDefaults()
    {
        for (int i = 0; i < fishList.Count; i++)
        {
            FishRuntime fish = fishList[i];
            if (fish.root == null)
            {
                continue;
            }

            float targetHeight = fish.fishName == ClownfishName ? 0.12f : 0.16f;
            Vector3 localPosition = fish.root.localPosition;
            localPosition.y = targetHeight;
            fish.root.localPosition = localPosition;

            fish.baseLocalPosition = fish.root.localPosition;
            fish.baseHeight = targetHeight;
            fish.minHeight = targetHeight - 0.15f;
            fish.maxHeight = targetHeight + 0.2f;

            if (fish.animator != null)
            {
                fish.animator.enabled = true;
            }

            if (fish.fishMotion != null)
            {
                fish.fishMotion.SetAutoMotion(false);
                fish.fishMotion.enabled = true;
            }
        }
    }

    private void UpdateCurrentFish()
    {
        FishRuntime visibleFish = null;
        FishRuntime enabledFish = null;

        for (int i = 0; i < fishList.Count; i++)
        {
            FishRuntime fish = fishList[i];
            if (fish.root == null || fish.renderers == null)
            {
                continue;
            }

            for (int rendererIndex = 0; rendererIndex < fish.renderers.Length; rendererIndex++)
            {
                Renderer rendererComponent = fish.renderers[rendererIndex];
                if (rendererComponent == null || !rendererComponent.enabled || !rendererComponent.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (enabledFish == null)
                {
                    enabledFish = fish;
                }

                if (rendererComponent.isVisible)
                {
                    visibleFish = fish;
                    break;
                }
            }

            if (visibleFish != null)
            {
                break;
            }
        }

        currentFish = visibleFish != null ? visibleFish : enabledFish;

        if (currentFish != null)
        {
            lastKnownFish = currentFish;
        }
        else if (lastKnownFish == null && fishList.Count > 0)
        {
            lastKnownFish = fishList[0];
        }
    }

    private void UpdateArPanelState()
    {
        if (arControlPanel != null)
        {
            arControlPanel.SetActive(!aquariumMode && !infoVisible && fishList.Count > 0);
        }

        if (selectedFishText == null)
        {
            return;
        }

        FishRuntime fish = GetControllableFish();
        if (fish == null)
        {
            selectedFishText.text = "Balik secili degil";
            helperText.text = "Baligi secmek icin modele dokun.";
            return;
        }

        selectedFishText.text = "Secili balik: " + PrettyFishName(fish.fishName);
        helperText.text = "Modele dokunup sec ve surukleyerek kontrol et.";
    }

    private void HandleArTouchInput()
    {
        bool inputBegan = false;
        bool inputMoving = false;
        bool inputEnded = false;
        Vector2 inputPosition = Vector2.zero;
        Vector2 inputDelta = Vector2.zero;
        int fingerId = -1;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPosition = touch.position;
            inputDelta = touch.deltaPosition;
            fingerId = touch.fingerId;

            if (touch.phase == TouchPhase.Began)
            {
                inputBegan = true;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                inputMoving = true;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                inputEnded = true;
            }
        }
        else if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
        {
            inputPosition = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                inputBegan = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                inputEnded = true;
            }
            else
            {
                inputMoving = true;
                inputDelta = (Vector2)Input.mousePosition - lastMousePosition;
                lastMousePosition = Input.mousePosition;
            }
        }
        else
        {
            draggingFish = false;
            return;
        }

        FishRuntime fish = GetControllableFish();
        if (fish == null)
        {
            return;
        }

        bool isOverUI = false;
        if (inputBegan && EventSystem.current != null)
        {
            if (fingerId >= 0)
            {
                isOverUI = EventSystem.current.IsPointerOverGameObject(fingerId);
            }
            else
            {
                isOverUI = EventSystem.current.IsPointerOverGameObject();
            }
        }

        if (isOverUI)
        {
            draggingFish = false;
            return;
        }

        if (inputBegan)
        {
            Debug.Log("[ARAquariumController] Click/Touch began at: " + inputPosition);
            touchStartPos = inputPosition;
            touchStartTime = Time.time;
            draggingFish = (fish != null);
            return;
        }

        if (!draggingFish)
        {
            return;
        }

        if (inputMoving)
        {
            RotateFish(fish, inputDelta.x * 0.22f);
            MoveFishVertical(fish, inputDelta.y * 0.0015f);
        }

        if (inputEnded)
        {
            float dragDistance = Vector2.Distance(inputPosition, touchStartPos);
            float dragDuration = Time.time - touchStartTime;
            Debug.Log("[ARAquariumController] Touch ended. Drag distance: " + dragDistance + ", duration: " + dragDuration);

            // A tap is defined by low distance and short duration
            if (dragDistance < 25f && dragDuration < 0.35f)
            {
                if (infoVisible)
                {
                    Debug.Log("[ARAquariumController] Tap detected while panel open, closing panel.");
                    HideFishInfo();
                }
                else
                {
                    bool hitFish = TrySelectFish(inputPosition);
                    if (hitFish)
                    {
                        Debug.Log("[ARAquariumController] Tap detected on fish! Showing panel.");
                        ShowFishInfo(currentFish);
                    }
                    else
                    {
                        Debug.Log("[ARAquariumController] Tap detected on empty space.");
                    }
                }
            }

            draggingFish = false;
        }
    }

    private void RotateCurrentFish(float direction)
    {
        FishRuntime fish = GetControllableFish();
        if (fish == null)
        {
            return;
        }

        RotateFish(fish, direction * 12f);
    }

    private void MoveCurrentFish(float amount)
    {
        FishRuntime fish = GetControllableFish();
        if (fish == null)
        {
            return;
        }

        MoveFishVertical(fish, amount);
    }

    private void RotateFish(FishRuntime fish, float angleDelta)
    {
        if (fish == null || fish.root == null)
        {
            return;
        }

        fish.root.Rotate(0f, angleDelta, 0f, Space.Self);
    }

    private void MoveFishVertical(FishRuntime fish, float deltaY)
    {
        if (fish == null || fish.root == null)
        {
            return;
        }

        Vector3 position = fish.root.localPosition;
        position.y = Mathf.Clamp(position.y + deltaY, fish.minHeight, fish.maxHeight);
        fish.root.localPosition = position;
    }

    private void OpenAquariumMode()
    {
        HideFishInfo();

        FishRuntime fish = GetControllableFish();
        if (fish == null)
        {
            Debug.LogWarning("[ARAquariumController] OpenAquariumMode: No controllable fish found!");
            return;
        }

        aquariumSourceFish = fish;
        aquariumMode = true;

        // Show the overlay canvas (ScreenSpaceOverlay: renders on top of EVERYTHING)
        if (aquariumOverlay != null) aquariumOverlay.SetActive(true);

        // Show the game world panel (covers the AR view)
        if (gameWorldPanel != null) gameWorldPanel.gameObject.SetActive(true);

        BuildAquariumLevel();
        ResetJoystick();

        Debug.Log("[ARAquariumController] Aquarium mode opened with fish: " + fish.fishName);
    }

    private void CloseAquariumMode()
    {
        aquariumMode = false;
        isGameOver = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Hide the game world panel
        if (gameWorldPanel != null) gameWorldPanel.gameObject.SetActive(false);

        // Hide the whole overlay
        if (aquariumOverlay != null) aquariumOverlay.SetActive(false);

        ClearAquariumLevel();
        ResetJoystick();

        Debug.Log("[ARAquariumController] Aquarium mode closed.");
    }

    private void TriggerGameOver(string reason)
    {
        isGameOver = true;
        ResetJoystick();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        if (gameOverReasonText != null)
        {
            gameOverReasonText.text = reason;
        }
        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = "Skor: " + eatenFishCount;
        }
    }

    private void RestartAquariumGame()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        isGameOver = false;
        BuildAquariumLevel();
    }

    private Sprite Draw2DJellyfish(int w, int h, Color bellColor)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        float cx = w * 0.5f;
        float cy = h * 0.65f;
        float rx = w * 0.45f;
        float ry = h * 0.28f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                if (dy >= 0f)
                {
                    if (dx * dx + dy * dy <= 1f)
                    {
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01((1f - dist) * 2f);
                        float glow = 0.5f + 0.5f * dist;
                        Color c = bellColor;
                        px[y * w + x] = new Color(c.r * glow, c.g * glow, c.b * glow, alpha * 0.85f);
                    }
                }
                else if (dy >= -0.2f)
                {
                    float wave = 0.15f * Mathf.Sin((float)x / w * Mathf.PI * 4f);
                    if (dx * dx + (dy - wave) * (dy - wave) <= 1.0f && y >= cy - ry * 0.2f)
                    {
                        px[y * w + x] = new Color(bellColor.r, bellColor.g, bellColor.b, 0.7f);
                    }
                }
            }
        }

        for (int y = 0; y < (int)(cy); y++)
        {
            float tFactor = (float)y / cy;
            float alpha = Mathf.Clamp01(tFactor);

            float cX = cx + w * 0.08f * Mathf.Sin((float)y / h * Mathf.PI * 4.5f);
            float lX = cx - w * 0.22f + w * 0.08f * Mathf.Sin((float)y / h * Mathf.PI * 4.5f + 1.5f);
            float rX = cx + w * 0.22f + w * 0.08f * Mathf.Sin((float)y / h * Mathf.PI * 4.5f - 1.5f);

            int[] xCoords = { (int)cX, (int)lX, (int)rX };
            foreach (int tx in xCoords)
            {
                for (int offset = -2; offset <= 2; offset++)
                {
                    int pxX = tx + offset;
                    if (pxX >= 0 && pxX < w && y < h)
                    {
                        float thickGlow = 1f - (Mathf.Abs(offset) / 2.0f);
                        Color tentacleColor = Color.Lerp(bellColor, Color.white, 0.3f);
                        int idx = y * w + pxX;
                        if (px[idx].a < 0.1f)
                        {
                            px[idx] = new Color(tentacleColor.r, tentacleColor.g, tentacleColor.b, alpha * 0.65f * thickGlow);
                        }
                    }
                }
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
    }

    private void CreateRuntimeUI()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("RuntimeARCanvas");
        uiCanvas = canvasObject.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject safePanel = CreatePanel("ARControlsPanel", uiCanvas.transform, new Color(0.02f, 0.06f, 0.13f, 0.82f));
        arControlPanel = safePanel;
        RectTransform safeRect = safePanel.GetComponent<RectTransform>();
        safeRect.anchorMin = new Vector2(0.04f, 0.02f);
        safeRect.anchorMax = new Vector2(0.96f, 0.27f);
        safeRect.offsetMin = Vector2.zero;
        safeRect.offsetMax = Vector2.zero;

        controlTitleText = CreateText("ControlTitle", safePanel.transform, "AR Balik Kontrolleri", 30, TextAnchor.UpperLeft);
        SetRect(controlTitleText.rectTransform, new Vector2(0.04f, 0.75f), new Vector2(0.52f, 0.96f), Vector2.zero, Vector2.zero);
        controlTitleText.fontStyle = FontStyle.Bold;

        selectedFishText = CreateText("SelectedFishText", safePanel.transform, "Secili balik: -", 34, TextAnchor.UpperRight);
        SetRect(selectedFishText.rectTransform, new Vector2(0.48f, 0.75f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
        selectedFishText.fontStyle = FontStyle.Bold;

        helperText = CreateText("HelperText", safePanel.transform, "Surukleyerek modeli rahatca kontrol et.", 26, TextAnchor.MiddleLeft);
        SetRect(helperText.rectTransform, new Vector2(0.04f, 0.50f), new Vector2(0.70f, 0.72f), Vector2.zero, Vector2.zero);

        /* 
        // User requested removing these AR screen buttons:
        rotateLeftButton = CreateButton("BtnRotateLeft", safePanel.transform, "Sola Don", new Color(0.04f, 0.41f, 0.71f, 1f));
        SetRect(rotateLeftButton.GetComponent<RectTransform>(), new Vector2(0.03f, 0.08f), new Vector2(0.24f, 0.38f), Vector2.zero, Vector2.zero);
        rotateLeftButton.onClick.AddListener(delegate { RotateCurrentFish(-1f); });

        rotateRightButton = CreateButton("BtnRotateRight", safePanel.transform, "Saga Don", new Color(0.04f, 0.41f, 0.71f, 1f));
        SetRect(rotateRightButton.GetComponent<RectTransform>(), new Vector2(0.26f, 0.08f), new Vector2(0.47f, 0.38f), Vector2.zero, Vector2.zero);
        rotateRightButton.onClick.AddListener(delegate { RotateCurrentFish(1f); });

        moveUpButton = CreateButton("BtnMoveUp", safePanel.transform, "Yukari Cik", new Color(0.04f, 0.58f, 0.46f, 1f));
        SetRect(moveUpButton.GetComponent<RectTransform>(), new Vector2(0.49f, 0.08f), new Vector2(0.70f, 0.38f), Vector2.zero, Vector2.zero);
        moveUpButton.onClick.AddListener(delegate { MoveCurrentFish(0.025f); });

        moveDownButton = CreateButton("BtnMoveDown", safePanel.transform, "Asagi In", new Color(0.04f, 0.58f, 0.46f, 1f));
        SetRect(moveDownButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.08f), new Vector2(0.93f, 0.38f), Vector2.zero, Vector2.zero);
        moveDownButton.onClick.AddListener(delegate { MoveCurrentFish(-0.025f); });
        */

        swimButton = CreateButton("BtnSwim", safePanel.transform, "Oyun Modu", new Color(0.90f, 0.49f, 0.10f, 1f));
        SetRect(swimButton.GetComponent<RectTransform>(), new Vector2(0.73f, 0.50f), new Vector2(0.96f, 0.74f), Vector2.zero, Vector2.zero);
        swimButton.onClick.AddListener(OpenAquariumMode);
    }

    private void CreateAquariumOverlayUI()
    {
        aquariumOverlay = CreatePanel("AquariumOverlay", uiCanvas.transform, new Color(0.01f, 0.04f, 0.08f, 0.98f));
        SetRect(aquariumOverlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        aquariumOverlay.SetActive(false);

        aquariumView = CreateRawImage("AquariumView", aquariumOverlay.transform, new Color(1f, 1f, 1f, 1f));
        SetRect(aquariumView.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        aquariumView.raycastTarget = false;
        aquariumView.enabled = false;

        aquariumHudTop = CreateImage("AquariumHudTop", aquariumOverlay.transform, new Color(0.01f, 0.08f, 0.14f, 0.82f));
        SetRect(aquariumHudTop.rectTransform, new Vector2(0.03f, 0.88f), new Vector2(0.97f, 0.98f), Vector2.zero, Vector2.zero);

        aquariumInfoText = CreateText("AquariumInfo", aquariumHudTop.transform, "Akvaryum modu", 34, TextAnchor.MiddleLeft);
        SetRect(aquariumInfoText.rectTransform, new Vector2(0.03f, 0.08f), new Vector2(0.72f, 0.92f), Vector2.zero, Vector2.zero);

        exitAquariumButton = CreateButton("BtnExitAquarium", aquariumHudTop.transform, "AR'a Don", new Color(0.82f, 0.24f, 0.18f, 1f));
        SetRect(exitAquariumButton.GetComponent<RectTransform>(), new Vector2(0.78f, 0.16f), new Vector2(0.97f, 0.84f), Vector2.zero, Vector2.zero);
        exitAquariumButton.onClick.AddListener(CloseAquariumMode);

        aquariumHudBottom = CreateImage("AquariumHudBottom", aquariumOverlay.transform, new Color(0.01f, 0.08f, 0.14f, 0.72f));
        SetRect(aquariumHudBottom.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.24f), Vector2.zero, Vector2.zero);

        aquariumHintText = CreateText("AquariumHint", aquariumHudBottom.transform, "Joystick ile hareket et. Kucuk baliklari yiyip buyu.", 28, TextAnchor.UpperLeft);
        SetRect(aquariumHintText.rectTransform, new Vector2(0.03f, 0.60f), new Vector2(0.72f, 0.95f), Vector2.zero, Vector2.zero);

        joystickBaseImage = CreateImage("JoystickBase", aquariumHudBottom.transform, new Color(0.08f, 0.23f, 0.34f, 0.85f));
        joystickBaseRect = joystickBaseImage.rectTransform;
        joystickBaseImage.sprite = circleSprite;
        joystickBaseImage.type = Image.Type.Simple;
        SetRect(joystickBaseRect, new Vector2(0.03f, 0.05f), new Vector2(0.25f, 0.60f), Vector2.zero, Vector2.zero);

        joystickKnobImage = CreateImage("JoystickKnob", joystickBaseImage.transform, new Color(0.19f, 0.84f, 0.93f, 0.95f));
        joystickKnobRect = joystickKnobImage.rectTransform;
        joystickKnobImage.sprite = circleSprite;
        joystickKnobImage.type = Image.Type.Simple;
        joystickKnobRect.anchorMin = new Vector2(0.5f, 0.5f);
        joystickKnobRect.anchorMax = new Vector2(0.5f, 0.5f);
        joystickKnobRect.sizeDelta = new Vector2(96f, 96f);
        joystickKnobRect.anchoredPosition = Vector2.zero;

        // ---- Game Over Panel ----
        gameOverPanel = CreatePanel("GameOverPanel", aquariumOverlay.transform, new Color(0.01f, 0.05f, 0.1f, 0.92f));
        SetRect(gameOverPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        
        GameObject card = CreatePanel("GameOverCard", gameOverPanel.transform, new Color(0.05f, 0.12f, 0.22f, 0.95f));
        RectTransform cardRT = card.GetComponent<RectTransform>();
        SetRect(cardRT, new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.75f), Vector2.zero, Vector2.zero);
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.19f, 0.84f, 0.93f, 0.3f);
        cardOutline.effectDistance = new Vector2(4f, -4f);

        Text gameOverTitle = CreateText("GameOverTitle", card.transform, "OYUN BİTTİ", 52, TextAnchor.UpperCenter);
        SetRect(gameOverTitle.rectTransform, new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
        gameOverTitle.fontStyle = FontStyle.Bold;
        gameOverTitle.color = new Color(0.95f, 0.25f, 0.25f, 1f);

        gameOverReasonText = CreateText("GameOverReason", card.transform, "", 36, TextAnchor.MiddleCenter);
        SetRect(gameOverReasonText.rectTransform, new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.72f), Vector2.zero, Vector2.zero);
        gameOverReasonText.color = new Color(0.9f, 0.9f, 0.95f, 1f);

        gameOverScoreText = CreateText("GameOverScoreText", card.transform, "Skor: 0", 42, TextAnchor.MiddleCenter);
        SetRect(gameOverScoreText.rectTransform, new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.48f), Vector2.zero, Vector2.zero);
        gameOverScoreText.fontStyle = FontStyle.Bold;
        gameOverScoreText.color = new Color(0.19f, 0.84f, 0.93f, 1f);

        gameOverRestartButton = CreateButton("BtnRestart", card.transform, "Yeniden Başla", new Color(0.05f, 0.58f, 0.46f, 1f));
        SetRect(gameOverRestartButton.GetComponent<RectTransform>(), new Vector2(0.12f, 0.10f), new Vector2(0.46f, 0.26f), Vector2.zero, Vector2.zero);
        gameOverRestartButton.onClick.AddListener(RestartAquariumGame);

        gameOverExitButton = CreateButton("BtnExit", card.transform, "AR'a Dön", new Color(0.82f, 0.24f, 0.18f, 1f));
        SetRect(gameOverExitButton.GetComponent<RectTransform>(), new Vector2(0.54f, 0.10f), new Vector2(0.88f, 0.26f), Vector2.zero, Vector2.zero);
        gameOverExitButton.onClick.AddListener(CloseAquariumMode);

        gameOverPanel.SetActive(false);
    }

    // Creates the UI panel used as the 2D game world (called once from Awake)
    private void CreateAquariumWorld()
    {
        if (aquariumOverlay == null)
        {
            Debug.LogError("[ARAquariumController] CreateAquariumWorld: aquariumOverlay is null!");
            return;
        }

        // --- Full-screen background panel (behind everything else) ---
        GameObject panelObj = new GameObject("AquariumGameWorld",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObj.transform.SetParent(aquariumOverlay.transform, false);
        panelObj.transform.SetSiblingIndex(0); // render BEHIND HUD children

        Image bgImage = panelObj.GetComponent<Image>();
        bgImage.color = Color.white;
        bgImage.raycastTarget = false;

        // Full-screen anchors
        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        // Apply procedural ocean gradient as the background
        Texture2D bgTex = GenerateUnderwaterGradient(256, 512);
        bgImage.sprite = Sprite.Create(bgTex,
            new Rect(0, 0, 256, 512), new Vector2(0.5f, 0.5f), 100f);
        bgImage.type = Image.Type.Simple;
        bgImage.preserveAspect = false;

        panelObj.SetActive(false); // hidden until game mode opens
        gameWorldPanel = panelRT;

        Debug.Log("[ARAquariumController] UI-based aquarium game world created.");
    }


    private Texture2D GenerateUnderwaterGradient(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            // Deep dark blue at bottom, teal-cyan at top
            Color c = Color.Lerp(
                new Color(0.00f, 0.05f, 0.14f, 1f),  // deep ocean bottom
                new Color(0.04f, 0.32f, 0.45f, 1f),  // lighter teal top
                t);
            for (int x = 0; x < width; x++)
            {
                // Add subtle horizontal shimmer
                float shimmer = 1f + 0.04f * Mathf.Sin((float)x / width * Mathf.PI * 8f + t * 5f);
                tex.SetPixel(x, y, new Color(c.r * shimmer, c.g * shimmer, c.b * shimmer, 1f));
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    // (unused - kept for reference)
    private void CreateOrResizeAquariumTexture() { }

    private void BuildAquariumLevel()
    {
        ClearAquariumLevel();
        eatenFishCount = 0;
        player2DRect = null;
        isGameOver = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (gameWorldPanel == null)
        {
            Debug.LogError("[ARAquariumController] BuildAquariumLevel: gameWorldPanel is null!");
            return;
        }

        bool isMarine = (aquariumSourceFish != null && aquariumSourceFish.fishName == ClownfishName);

        // ---- Player fish as UI Image ----
        Color playerBody   = isMarine ? new Color(1f, 0.42f, 0.04f) : new Color(0.18f, 0.65f, 1f);
        Color playerAccent = isMarine ? Color.white : new Color(0.85f, 0.18f, 0.85f);
        Sprite playerSprite = Draw2DFish(200, 124, playerBody, playerAccent, isMarine);

        playerFishInstance = CreateFishUIObject("Player2DFish", playerSprite);
        RectTransform pRT = playerFishInstance.GetComponent<RectTransform>();
        pRT.sizeDelta = new Vector2(200f, 124f);
        pRT.anchoredPosition = new Vector2(-150f, 0f);
        player2DRect = pRT;

        // ---- Prey fish ----
        for (int i = 0; i < 8; i++) SpawnPreyFish(i);

        RefreshAquariumHud();
        Debug.Log("[ARAquariumController] 2D UI level built.");
    }

    // Creates a UI GameObject with Image component inside gameWorldPanel
    private GameObject CreateFishUIObject(string objName, Sprite sprite)
    {
        GameObject obj = new GameObject(objName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(gameWorldPanel, false);

        Image img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.preserveAspect = false;
        img.raycastTarget = false;
        img.type = Image.Type.Simple;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        return obj;
    }

    // Body colors for the 8 different prey fish varieties
    private static readonly Color[] PreyBodyColors =
    {
        new Color(1.00f, 0.30f, 0.10f), // orange-red
        new Color(0.15f, 0.55f, 1.00f), // cobalt blue
        new Color(0.10f, 0.75f, 0.35f), // emerald green
        new Color(0.75f, 0.18f, 0.80f), // violet
        new Color(1.00f, 0.82f, 0.08f), // golden yellow
        new Color(0.08f, 0.80f, 0.82f), // teal cyan
        new Color(0.90f, 0.30f, 0.55f), // rose
        new Color(0.55f, 0.85f, 0.20f), // lime
    };
    private static readonly Color[] PreyAccentColors =
    {
        Color.white,
        new Color(0.85f, 1f, 0.20f),
        Color.white,
        new Color(1f, 0.90f, 0.10f),
        new Color(1f, 0.50f, 0.05f),
        Color.white,
        new Color(1f, 0.90f, 0.20f),
        new Color(0.20f, 0.45f, 1f),
    };

    private void SpawnPreyFish(int index)
    {
        if (gameWorldPanel == null) return;

        AquariumPrey prey = new AquariumPrey();
        
        bool isJelly = (index % 4 == 0);
        bool isBig   = (index % 4 == 1);
        
        prey.isJellyfish = isJelly;
        prey.isBigFish = isBig;

        float w, h;
        Sprite preySprite;
        string objName;

        if (isJelly)
        {
            w = 110f;
            h = 150f;
            Color jellyColor = (index % 8 == 0) ? new Color(0.95f, 0.35f, 0.95f) : new Color(0.25f, 0.85f, 0.95f);
            preySprite = Draw2DJellyfish((int)w, (int)h, jellyColor);
            objName = "Jellyfish_" + index;
        }
        else if (isBig)
        {
            w = 260f;
            h = 162f;
            Color bigBody = new Color(0.12f, 0.15f, 0.22f);
            Color bigAccent = new Color(0.95f, 0.15f, 0.15f);
            preySprite = Draw2DFish((int)w, (int)h, bigBody, bigAccent, true);
            objName = "BigFish_" + index;
        }
        else
        {
            int colorIdx = index % PreyBodyColors.Length;
            bool hasStripes = (index % 3 == 0);
            w = Random.Range(90f, 150f);
            h = w * 0.625f;
            preySprite = Draw2DFish((int)w, (int)h, PreyBodyColors[colorIdx], PreyAccentColors[colorIdx], hasStripes);
            objName = "Prey_" + index;
        }

        GameObject preyObj = CreateFishUIObject(objName, preySprite);
        RectTransform preyRT = preyObj.GetComponent<RectTransform>();
        preyRT.sizeDelta = new Vector2(w, h);

        prey.instance  = preyObj;
        prey.scale     = w;
        prey.bobOffset = Random.Range(0f, Mathf.PI * 2f);
        prey.bobSpeed  = isJelly ? Random.Range(0.4f, 0.9f) : Random.Range(0.6f, 1.8f);
        
        float minSpeed = isJelly ? -150f : (isBig ? -380f : -350f);
        float maxSpeed = isJelly ? -80f  : (isBig ? -240f : -180f);
        prey.velocity  = new Vector3(Random.Range(minSpeed, maxSpeed), 0f, 0f);

        ResetPrey(prey, true);
        preyFish.Add(prey);
    }

    private void ClearAquariumLevel()
    {
        for (int i = 0; i < preyFish.Count; i++)
        {
            if (preyFish[i].instance != null) Destroy(preyFish[i].instance);
        }
        preyFish.Clear();

        if (playerFishInstance != null)
        {
            Destroy(playerFishInstance);
            playerFishInstance = null;
        }
        player2DRect = null;
    }



    private void HandleAquariumInput()
    {
        if (playerFishInstance == null)
        {
            ResetJoystick();
            return;
        }

        bool joystickUpdated = false;

        // --- Touch input ---
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                bool isCurrentFinger   = joystickFingerId == touch.fingerId;
                bool inJoystickArea    = IsTouchInsideJoystickArea(touch.position);
                bool eligibleForJoystick = isCurrentFinger || inJoystickArea;

                // Skip touches that are NOT joystick touches AND are on other UI elements
                if (!eligibleForJoystick)
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                        continue;
                    continue; // not in joystick area at all
                }

                if (touch.phase == TouchPhase.Began) joystickFingerId = touch.fingerId;

                if (touch.phase == TouchPhase.Moved ||
                    touch.phase == TouchPhase.Stationary ||
                    touch.phase == TouchPhase.Began)
                {
                    UpdateJoystickFromTouch(touch.position);
                    joystickUpdated = true;
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    ResetJoystick();

                if (joystickUpdated) break;
            }
        }
        // --- Mouse input (editor / PC fallback) ---
        else if (Input.GetMouseButton(0))
        {
            bool inJoystickArea = IsTouchInsideJoystickArea(Input.mousePosition);
            bool isTracked      = joystickFingerId != NoFingerId;
            if (inJoystickArea || isTracked)
            {
                joystickFingerId = 1;
                UpdateJoystickFromTouch(Input.mousePosition);
                joystickUpdated = true;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            ResetJoystick();
        }

        if (!joystickUpdated && joystickFingerId == NoFingerId)
        {
            joystickInput = Vector2.zero;
            if (joystickKnobRect != null) joystickKnobRect.anchoredPosition = Vector2.zero;
        }
    }

    private void UpdateAquariumWorld()
    {
        if (playerFishInstance == null || player2DRect == null) return;

        // --- Player movement (canvas units per second) ---
        Vector2 swimDir = joystickInput;
        const float MoveSpeed = 800f; // canvas units/s  (ref: 1080x1920)
        float growth = 1f + eatenFishCount * 0.04f;

        if (swimDir.sqrMagnitude > 0.001f)
        {
            Vector2 pos = player2DRect.anchoredPosition;
            pos += swimDir * MoveSpeed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, -510f, 510f);
            pos.y = Mathf.Clamp(pos.y, -900f, 900f);
            player2DRect.anchoredPosition = pos;

            // Flip to face swim direction
            float sx = swimDir.x >= 0f ? 1f : -1f;
            player2DRect.localScale = new Vector3(sx, 1f, 1f);
        }
        else
        {
            // Gentle idle bob
            Vector2 pos = player2DRect.anchoredPosition;
            pos.y += Mathf.Sin(Time.time * 1.2f) * Time.deltaTime * 28f;
            player2DRect.anchoredPosition = pos;
        }

        // Player size based on base (200x124) × growth factor — update only when eaten count changes
        float playerW = 200f * growth;
        float playerH = 124f * growth;
        player2DRect.sizeDelta = new Vector2(playerW, playerH);

        // --- Prey movement & eating ---
        for (int i = preyFish.Count - 1; i >= 0; i--)
        {
            AquariumPrey prey = preyFish[i];
            if (prey.instance == null) { preyFish.RemoveAt(i); continue; }

            RectTransform preyRT = prey.instance.GetComponent<RectTransform>();
            if (preyRT == null) continue;

            Vector2 pPos = preyRT.anchoredPosition;
            pPos.x += prey.velocity.x * Time.deltaTime;
            pPos.y += Mathf.Sin(Time.time * prey.bobSpeed + prey.bobOffset) * Time.deltaTime * 55f;
            preyRT.anchoredPosition = pPos;

            // Eating: simple rectangle overlap
            float halfPW = playerW * 0.35f;
            float halfPH = playerH * 0.35f;
            float halfEW = preyRT.sizeDelta.x * 0.35f;
            float halfEH = preyRT.sizeDelta.y * 0.35f;
            Vector2 playerPos = player2DRect.anchoredPosition;

            bool overlapX = Mathf.Abs(playerPos.x - pPos.x) < halfPW + halfEW;
            bool overlapY = Mathf.Abs(playerPos.y - pPos.y) < halfPH + halfEH;

            if (overlapX && overlapY)
            {
                if (prey.isJellyfish)
                {
                    TriggerGameOver("Zehirlendiniz! Kaybettiniz.");
                    break;
                }
                else if (prey.isBigFish)
                {
                    TriggerGameOver("Büyük balığa yem oldunuz! Kaybettiniz.");
                    break;
                }
                else
                {
                    eatenFishCount++;
                    RefreshAquariumHud();
                    ResetPrey(prey, false);
                    continue;
                }
            }

            // Respawn from right when fish exits screen left
            if (pPos.x < -700f) ResetPrey(prey, true);
        }
    }

    private void ResetPrey(AquariumPrey prey, bool randomizeHeight)
    {
        if (prey == null || prey.instance == null) return;

        RectTransform rt = prey.instance.GetComponent<RectTransform>();
        if (rt == null) return;

        float height = randomizeHeight
            ? Random.Range(-900f, 900f)
            : Mathf.Clamp(rt.anchoredPosition.y + Random.Range(-200f, 200f), -900f, 900f);

        float playerW = 200f;
        if (player2DRect != null) playerW = player2DRect.sizeDelta.x;

        if (prey.isJellyfish)
        {
            float newW = 110f;
            float newH = 150f;
            prey.scale = newW;
            prey.velocity = new Vector3(Random.Range(-150f, -80f), 0f, 0f);
            rt.sizeDelta = new Vector2(newW, newH);
            rt.anchoredPosition = new Vector2(Random.Range(680f, 950f), height);
            rt.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (prey.isBigFish)
        {
            float newW = playerW * Random.Range(1.3f, 1.6f);
            float newH = newW * 0.625f;
            prey.scale = newW;
            prey.velocity = new Vector3(Random.Range(-380f, -240f), 0f, 0f);
            rt.sizeDelta = new Vector2(newW, newH);
            rt.anchoredPosition = new Vector2(Random.Range(680f, 950f), height);
            rt.localScale = new Vector3(-1f, 1f, 1f);
        }
        else
        {
            float newW = Random.Range(Mathf.Max(60f, playerW * 0.4f), playerW * 0.9f);
            float newH = newW * 0.625f;
            prey.scale = newW;
            prey.velocity = new Vector3(Random.Range(-350f, -180f), 0f, 0f);
            rt.sizeDelta = new Vector2(newW, newH);
            rt.anchoredPosition = new Vector2(Random.Range(680f, 950f), height);
            rt.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

    private void RefreshAquariumHud()
    {
        if (aquariumInfoText == null || aquariumSourceFish == null) return;
        aquariumInfoText.text = PrettyFishName(aquariumSourceFish.fishName) +
            "  |  Yenen: " + eatenFishCount +
            "  |  Boyut: x" + (1f + eatenFishCount * 0.04f).ToString("F2");
    }

    // ========================================================
    //  PROCEDURAL 2D FISH SPRITE GENERATOR
    // ========================================================
    private Sprite Draw2DFish(int w, int h, Color bodyColor, Color accentColor, bool addStripes)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        // Body ellipse — center slightly right of middle, facing right
        float cx = w * 0.54f;
        float cy = h * 0.5f;
        float rx = w * 0.36f;
        float ry = h * 0.44f;

        // --- Body ---
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1f)
                {
                    // Subtle shading: lighter toward head-top
                    float shade = 1f + 0.22f * (-dx * 0.3f - dy * 0.4f);
                    shade = Mathf.Clamp(shade, 0.72f, 1.28f);
                    px[y * w + x] = new Color(
                        Mathf.Clamp01(bodyColor.r * shade),
                        Mathf.Clamp01(bodyColor.g * shade),
                        Mathf.Clamp01(bodyColor.b * shade), 1f);
                }
            }
        }

        // --- Tail fin (fan shape) ---
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < (int)(w * 0.30f); x++)
            {
                float t = (float)x / (w * 0.30f);
                float halfSpan = (1f - t * t) * ry * 1.05f;
                float distFromCenter = Mathf.Abs(y - cy);
                if (distFromCenter < halfSpan)
                {
                    float bodyDx = (x - cx) / rx;
                    float bodyDy = (y - cy) / ry;
                    if (bodyDx * bodyDx + bodyDy * bodyDy > 0.85f)
                    {
                        // Fork: two lobes
                        float lobe1 = Mathf.Abs(y - (cy + ry * 0.45f));
                        float lobe2 = Mathf.Abs(y - (cy - ry * 0.45f));
                        float lobeDist = Mathf.Min(lobe1, lobe2);
                        float alpha = Mathf.Clamp01(1f - lobeDist / (halfSpan * 0.55f)) * (1f - t);
                        if (alpha > 0.1f && px[y * w + x].a < 0.1f)
                        {
                            px[y * w + x] = new Color(
                                bodyColor.r * 0.78f, bodyColor.g * 0.78f, bodyColor.b * 0.78f, alpha);
                        }
                    }
                }
            }
        }

        // --- Stripes (clownfish) or gradient fin (guppy) ---
        if (addStripes)
        {
            // 3 white vertical bands
            int[] sxArr = { (int)(cx - rx * 0.52f), (int)(cx - rx * 0.10f), (int)(cx + rx * 0.32f) };
            int sw = Mathf.Max(3, (int)(rx * 0.13f));
            foreach (int sxi in sxArr)
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = sxi - sw; x <= sxi + sw; x++)
                    {
                        if (x < 0 || x >= w) continue;
                        int idx = y * w + x;
                        if (px[idx].a > 0.5f)
                        {
                            float blend = 1f - (float)Mathf.Abs(x - sxi) / sw;
                            px[idx] = Color.Lerp(px[idx], accentColor, blend * 0.88f);
                        }
                    }
                }
            }
        }
        else
        {
            // Guppy: colourful tail region
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < (int)(w * 0.30f); x++)
                {
                    int idx = y * w + x;
                    if (px[idx].a > 0.2f)
                    {
                        float t = (float)x / (w * 0.30f);
                        Color tc = Color.Lerp(accentColor, bodyColor, t);
                        px[idx] = new Color(tc.r, tc.g, tc.b, px[idx].a);
                    }
                }
            }
        }

        // --- Pectoral fin ---
        float finCx = cx - rx * 0.1f;
        float finCy = cy + ry * 0.25f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float fdx = (x - finCx) / (rx * 0.42f);
                float fdy = (y - finCy) / (ry * 0.38f);
                float finDist = fdx * fdx + fdy * fdy;
                if (finDist < 1f)
                {
                    float bodyDx2 = (x - cx) / rx;
                    float bodyDy2 = (y - cy) / ry;
                    // Only draw where NOT inside the main body
                    if (bodyDx2 * bodyDx2 + bodyDy2 * bodyDy2 > 0.92f && px[y * w + x].a < 0.1f)
                    {
                        float a = Mathf.Clamp01(1f - finDist) * 0.75f;
                        px[y * w + x] = new Color(bodyColor.r * 0.7f, bodyColor.g * 0.7f, bodyColor.b * 0.7f, a);
                    }
                }
            }
        }

        // --- Eye ---
        int eyeX = (int)(cx + rx * 0.55f);
        int eyeY = (int)(cy - ry * 0.12f);
        int eyeR = Mathf.Max(3, h / 10);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float d = Mathf.Sqrt((x - eyeX) * (x - eyeX) + (y - eyeY) * (y - eyeY));
                if (d < eyeR * 0.55f)
                    px[y * w + x] = Color.black;
                else if (d < eyeR * 0.82f && px[y * w + x].a > 0.5f)
                    px[y * w + x] = new Color(0.95f, 0.95f, 1f, 1f);
                else if (d < eyeR && px[y * w + x].a > 0.5f)
                    px[y * w + x] = new Color(0.25f, 0.25f, 0.3f, 1f);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
    }

    private void PopulateFishInfo()
    {
        FishInfo clownfish = new FishInfo();
        clownfish.turkishName = "Palyaco Baligi (Clownfish)";
        clownfish.scientificName = "Amphiprion ocellaris";
        clownfish.family = "Amphiprioninae";
        clownfish.habitat = "Hint-Pasifik Okyanusu'ndaki tropikal mercan resiflerinde, sig ve ilik sularda yasar. Deniz anemonlarinin arasinda barinir.";
        clownfish.diet = "Omnivor (hem etcil hem otcul). Algler, plankton, kucuk kabuklular ve anemon artiklari ile beslenir.";
        clownfish.size = "8 - 11 cm (yetiskin)";
        clownfish.feature = "Deniz anemonlari ile simbiyotik iliski kurar. Anemonun zehirli dokungaclarina karsi bagisikligi vardir.";
        clownfish.funFact = "Tum palyaco baliklari erkek dogar. Gruptaki en buyuk birey disiye donusur!";
        fishInfoDatabase[ClownfishName] = clownfish;

        FishInfo guppy = new FishInfo();
        guppy.turkishName = "Lepistes (Guppy)";
        guppy.scientificName = "Poecilia reticulata";
        guppy.family = "Poeciliidae";
        guppy.habitat = "Guney Amerika'nin tatli su kaynaklari, nehirler ve goletler. Dunya genelinde tropikal ve subtropikal akvaryumlarda yetistirilir.";
        guppy.diet = "Omnivor. Algler, sivrisinek larvalari, kucuk bocekler, pul yem ve canli yem ile beslenir.";
        guppy.size = "3 - 6 cm (erkekler daha kucuk ve renkli)";
        guppy.feature = "Canli dogurucu bir turdur, yumurta birakmaz. Erkekleri son derece renkli ve gosterisli kuyruk desenlerine sahiptir.";
        guppy.funFact = "Bir disi lepistes, tek ciftlesmeden elde ettigi spermi aylarca saklayarak birden fazla nesil uretebilir!";
        fishInfoDatabase[GuppyName] = guppy;
    }

    private void CreateInfoPanel()
    {
        // White background with slight transparency for a clean glass/card look
        infoPanel = CreatePanel("FishInfoPanel", uiCanvas.transform, new Color(0.98f, 0.98f, 0.98f, 0.98f));
        RectTransform panelRect = infoPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.04f, 0.08f);
        panelRect.anchorMax = new Vector2(0.96f, 0.92f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Add a premium card border/shadow
        Outline panelOutline = infoPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.08f, 0.12f, 0.18f, 0.15f);
        panelOutline.effectDistance = new Vector2(4f, -4f);

        // Header Background in soft blue
        Image headerBg = CreateImage("InfoHeaderBg", infoPanel.transform, new Color(0.12f, 0.45f, 0.74f, 1f));
        SetRect(headerBg.rectTransform, new Vector2(0f, 0.90f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        // Bold white title, size 40 (was 36)
        infoTitleText = CreateText("InfoTitle", headerBg.transform, "", 40, TextAnchor.MiddleLeft);
        SetRect(infoTitleText.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.82f, 0.95f), Vector2.zero, Vector2.zero);
        infoTitleText.fontStyle = FontStyle.Bold;
        infoTitleText.color = Color.white;

        infoCloseButton = CreateButton("BtnCloseInfo", headerBg.transform, "X", new Color(0.78f, 0.18f, 0.14f, 1f));
        SetRect(infoCloseButton.GetComponent<RectTransform>(), new Vector2(0.86f, 0.12f), new Vector2(0.97f, 0.88f), Vector2.zero, Vector2.zero);
        infoCloseButton.onClick.AddListener(HideFishInfo);

        // Scientific name, size 28 (was 24) in dark slate grey for contrast
        infoScientificText = CreateText("InfoScientific", infoPanel.transform, "", 28, TextAnchor.MiddleLeft);
        SetRect(infoScientificText.rectTransform, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.89f), Vector2.zero, Vector2.zero);
        infoScientificText.fontStyle = FontStyle.Italic;
        infoScientificText.color = new Color(0.25f, 0.35f, 0.45f, 1f);

        // Clean light grey divider
        Image divider1 = CreateImage("Divider1", infoPanel.transform, new Color(0.85f, 0.88f, 0.92f, 1f));
        SetRect(divider1.rectTransform, new Vector2(0.05f, 0.835f), new Vector2(0.95f, 0.838f), Vector2.zero, Vector2.zero);

        CreateInfoRow("Habitat", "YASAM ALANI", 0.82f, 0.68f);
        CreateInfoRow("Diet", "BESLENME", 0.66f, 0.52f);
        CreateInfoRow("Size", "BOYUT", 0.50f, 0.40f);
        CreateInfoRow("Feature", "OZELLIK", 0.38f, 0.22f);

        // Warm light yellow/cream container for Fun Fact
        Image funFactBg = CreateImage("FunFactBg", infoPanel.transform, new Color(0.96f, 0.94f, 0.88f, 1f));
        SetRect(funFactBg.rectTransform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.16f), Vector2.zero, Vector2.zero);

        Outline funFactOutline = funFactBg.gameObject.AddComponent<Outline>();
        funFactOutline.effectColor = new Color(0.85f, 0.82f, 0.75f, 0.5f);
        funFactOutline.effectDistance = new Vector2(1f, -1f);

        // Fun Fact Title, size 24 (was 20) in dark bronze/orange
        Text funFactLabel = CreateText("FunFactLabel", funFactBg.transform, "BILIYOR MUYDUNUZ?", 24, TextAnchor.UpperCenter);
        SetRect(funFactLabel.rectTransform, new Vector2(0.03f, 0.62f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero);
        funFactLabel.fontStyle = FontStyle.Bold;
        funFactLabel.color = new Color(0.75f, 0.35f, 0.05f, 1f);

        // Fun Fact Text, size 24 (was 20) in dark charcoal
        infoFunFactText = CreateText("InfoFunFact", funFactBg.transform, "", 24, TextAnchor.UpperCenter);
        SetRect(infoFunFactText.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.62f), Vector2.zero, Vector2.zero);
        infoFunFactText.color = new Color(0.25f, 0.22f, 0.18f, 1f);
        infoFunFactText.fontStyle = FontStyle.Italic;

        infoPanel.SetActive(false);
        infoVisible = false;
    }

    private void CreateInfoRow(string name, string label, float top, float bottom)
    {
        // Category Label, size 26 (was 22) in soft blue
        Text labelText = CreateText(name + "Label", infoPanel.transform, label, 26, TextAnchor.UpperLeft);
        SetRect(labelText.rectTransform, new Vector2(0.05f, top - 0.06f), new Vector2(0.95f, top), Vector2.zero, Vector2.zero);
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = new Color(0.12f, 0.45f, 0.74f, 1f);

        // Content Text, size 26 (was 22) in dark charcoal for maximum readability
        Text valueText = CreateText(name + "Value", infoPanel.transform, "", 26, TextAnchor.UpperLeft);
        SetRect(valueText.rectTransform, new Vector2(0.05f, bottom), new Vector2(0.95f, top - 0.05f), Vector2.zero, Vector2.zero);
        valueText.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        if (name == "Habitat") infoHabitatValue = valueText;
        else if (name == "Diet") infoDietValue = valueText;
        else if (name == "Size") infoSizeValue = valueText;
        else if (name == "Feature") infoFeatureValue = valueText;

        // Soft grey-blue divider line
        Image divider = CreateImage(name + "Divider", infoPanel.transform, new Color(0.88f, 0.90f, 0.94f, 0.8f));
        SetRect(divider.rectTransform, new Vector2(0.05f, bottom - 0.003f), new Vector2(0.95f, bottom), Vector2.zero, Vector2.zero);
    }

    private void ShowFishInfo(FishRuntime fish)
    {
        if (fish == null || infoPanel == null)
        {
            return;
        }

        FishInfo info;
        if (!fishInfoDatabase.TryGetValue(fish.fishName, out info))
        {
            return;
        }

        infoTitleText.text = info.turkishName;
        infoScientificText.text = info.scientificName + "  |  " + info.family;
        infoHabitatValue.text = info.habitat;
        infoDietValue.text = info.diet;
        infoSizeValue.text = info.size;
        infoFeatureValue.text = info.feature;
        infoFunFactText.text = info.funFact;

        infoPanel.SetActive(true);
        infoVisible = true;
    }

    private void HideFishInfo()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        infoVisible = false;
    }

    private string PrettyFishName(string fishName)
    {
        if (fishName == ClownfishName)
        {
            return "Marine Clownfish";
        }

        if (fishName == GuppyName)
        {
            return "Freshwater Guppy";
        }

        return fishName;
    }

    private FishRuntime GetControllableFish()
    {
        if (currentFish != null)
        {
            return currentFish;
        }

        if (lastKnownFish != null)
        {
            return lastKnownFish;
        }

        return fishList.Count > 0 ? fishList[0] : null;
    }

    private bool TrySelectFish(Vector2 screenPosition)
    {
        if (arCamera == null)
        {
            arCamera = GetComponent<Camera>();
        }
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }
        if (arCamera == null)
        {
            arCamera = FindObjectOfType<Camera>();
        }

        if (arCamera == null)
        {
            Debug.LogError("[ARAquariumController] TrySelectFish: No AR Camera found!");
            return false;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        Debug.Log("[ARAquariumController] Raycasting from camera: " + arCamera.name + " using screenPosition: " + screenPosition);

        if (!Physics.Raycast(ray, out hit, 100f))
        {
            Debug.Log("[ARAquariumController] Raycast did not hit any collider.");
            return false;
        }

        Transform hitTransform = hit.transform;
        Debug.Log("[ARAquariumController] Raycast hit object: " + hitTransform.name + " on layer: " + hitTransform.gameObject.layer);

        for (int i = 0; i < fishList.Count; i++)
        {
            FishRuntime fish = fishList[i];
            if (fish.root == null)
            {
                continue;
            }

            if (hitTransform == fish.root || hitTransform.IsChildOf(fish.root))
            {
                currentFish = fish;
                lastKnownFish = fish;
                return true;
            }
        }

        Debug.Log("[ARAquariumController] Hit object is not part of any cached fish.");
        return false;
    }

    private void DisableImageTargetPreviewRenderers()
    {
        MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer meshRenderer = meshRenderers[i];
            if (meshRenderer == null)
            {
                continue;
            }

            string objectName = meshRenderer.gameObject.name;
            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
            bool isImageTargetPreview = objectName.StartsWith("ImageTarget", System.StringComparison.OrdinalIgnoreCase);
            bool usesImageTargetMesh = meshFilter != null &&
                meshFilter.sharedMesh != null &&
                meshFilter.sharedMesh.name.StartsWith("ImageTargetMesh", System.StringComparison.OrdinalIgnoreCase);

            if (isImageTargetPreview || usesImageTargetMesh)
            {
                meshRenderer.enabled = false;
            }
        }
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private GameObject CreatePanel(string objectName, Transform parent, Color color)
    {
        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>();
        image.color = color;
        return panel;
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
        text.font = uiFont;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private Button CreateButton(string objectName, Transform parent, string label, Color color)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = color;
        buttonImage.sprite = circleSprite;
        buttonImage.type = Image.Type.Simple;

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.10f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.1f;
        colors.pressedColor = color * 0.85f;
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text buttonText = CreateText("Label", buttonObject.transform, label, 26, TextAnchor.MiddleCenter);
        SetRect(buttonText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        buttonText.fontStyle = FontStyle.Bold;
        return button;
    }

    private RawImage CreateRawImage(string objectName, Transform parent, Color color)
    {
        GameObject rawImageObject = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
        rawImageObject.transform.SetParent(parent, false);
        RawImage rawImage = rawImageObject.GetComponent<RawImage>();
        rawImage.color = color;
        return rawImage;
    }

    private void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    // Background is now set procedurally in CreateAquariumWorld()

    private bool IsTouchInsideJoystickArea(Vector2 screenPosition)
    {
        if (joystickBaseRect == null || uiCanvas == null)
        {
            return false;
        }

        // For ScreenSpaceOverlay canvas, worldCamera is null - pass null to use screen coords
        Camera canvasCamera = (uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : uiCanvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(joystickBaseRect, screenPosition, canvasCamera);
    }

    private void UpdateJoystickFromTouch(Vector2 screenPosition)
    {
        if (joystickBaseRect == null || joystickKnobRect == null)
        {
            return;
        }

        Vector2 localPoint;
        Camera canvasCamera = (uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : uiCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBaseRect, screenPosition, canvasCamera, out localPoint);
        Vector2 normalized = Vector2.ClampMagnitude(localPoint / joystickRadius, 1f);
        joystickInput = normalized;
        joystickKnobRect.anchoredPosition = normalized * joystickRadius;
    }

    private void ResetJoystick()
    {
        joystickInput = Vector2.zero;
        joystickFingerId = NoFingerId;
        if (joystickKnobRect != null)
        {
            joystickKnobRect.anchoredPosition = Vector2.zero;
        }
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
}
