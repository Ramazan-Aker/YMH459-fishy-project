# Unity Editor Kurulum Kılavuzu

Bu kılavuz, kodu Unity 6.3 LTS'te çalışır hale getirmek için
gereken tüm Editor adımlarını açıklar.

---

## 1. Proje Açma

1. Unity Hub → **Open** → `YMH459-fishy-project` klasörünü seç.
2. Unity 6.3 LTS ile açılmasını doğrula (ProjectVersion.txt).
3. İlk açılışta paketler otomatik yüklenir (`Packages/manifest.json`).

---

## 2. Paket Yöneticisi Doğrulama

**Window → Package Manager** açın. Şu paketlerin yüklü olduğunu kontrol edin:

| Paket | Versiyon |
|-------|----------|
| AR Foundation | 6.0.x |
| ARCore XR Plugin | 6.0.x |
| ARKit XR Plugin | 6.0.x |
| XR Plugin Management | 4.5.x |
| Input System | 1.8.x |
| TextMeshPro | 3.0.x |

Eksik paket varsa **+ → Add package by name** ile ekleyin.

---

## 3. XR Plug-in Management

**Edit → Project Settings → XR Plug-in Management**

- **Android** sekmesi → ✅ **ARCore** işaretle
- **iOS** sekmesi → ✅ **ARKit** işaretle

---

## 4. Player Settings (Android)

**Edit → Project Settings → Player → Android sekmesi**

| Ayar | Değer |
|------|-------|
| Minimum API Level | Android 7.0 (API 24) |
| Target API Level | Android 13+ (API 33) |
| Scripting Backend | IL2CPP |
| Target Architectures | ARM64 ✅ |

**Other Settings:**
- `ARCore Supported` → Checked
- `Auto Graphics API` → Devre dışı; **OpenGLES3** veya **Vulkan** seç

---

## 5. Player Settings (iOS)

**Edit → Project Settings → Player → iOS sekmesi**

| Ayar | Değer |
|------|-------|
| Minimum iOS Version | 13.0 |
| Bundle Identifier | com.yourorg.arfishmuseum |
| Camera Usage Description | AR deneyimi için kameraya ihtiyaç duyuyoruz |

---

## 6. Input System Geçiş Uyarısı

Unity'nin eski Input Manager yerine yeni Input System paketini
zorunlu kılmak için:

**Edit → Project Settings → Player → Other Settings → Active Input Handling**
→ **Input System Package (New)** seç → Editor yeniden başlatılacak.

---

## 7. MenuScene Kurulumu

`Assets/Scenes/MenuScene.unity` oluştur:

```
MenuScene (Scene)
├── EventSystem
└── MenuCanvas (Screen Space - Overlay)
    ├── MainMenuController (Script bileşeni)
    │   ├── fishDataList: [Clownfish_Data, Surgeonfish_Data] (drag & drop)
    │   ├── buttonContainer: ScrollContent transform
    │   ├── fishButtonPrefab: Assets/Prefabs/UI/FishMenuButton.prefab
    │   └── arSceneName: "ARScene"
    └── ScrollView
        └── Viewport
            └── Content (← buttonContainer'a ata)
```

**FishMenuButton Prefab kurulumu:**
- Button komponenti
- Child: `Image` adında GameObject (ikon için)
- Child: `Label` adında TextMeshProUGUI (isim için)

---

## 8. ARScene Kurulumu

`Assets/Scenes/ARScene.unity` oluştur:

```
ARScene (Scene)
├── XR Origin (Mobile AR)          ← AR Foundation menüsünden ekle
│   ├── Camera Offset
│   │   └── Main Camera (AR Camera)
│   ├── ARPlaneManager              ← Inspector'da prefab bağlantısı gerekebilir
│   └── ARRaycastManager
│
├── AR Session                      ← AR Foundation menüsünden ekle
│   └── ARSessionBootstrap (Script)
│
├── Managers (boş GameObject)
│   ├── TouchInputManager (Script)
│   ├── PlacementController (Script)
│   │   └── indicator: PlacementIndicator prefabı
│   └── NarrationManager (Script)
│       └── AudioSource
│
├── PlacementIndicator (Prefab)
│   └── PlacementIndicator (Script)
│       └── raycastManager: XR Origin'deki ARRaycastManager
│
├── UI Canvas (Screen Space - Overlay)
│   ├── InfoPanel (Prefab)          ← başlangıçta kapalı
│   │   └── InfoPanelController (Script)
│   ├── PartTooltip (Prefab)        ← başlangıçta kapalı
│   │   └── PartTooltipUI (Script)
│   └── OnboardingOverlay
│       └── OnboardingOverlay (Script)
│           └── planeManager: XR Origin'deki ARPlaneManager
│
└── EventSystem
```

### AR Foundation Nesnelerini Ekleme
**GameObject → XR → AR Session** ve **GameObject → XR → XR Origin (Mobile AR)**

---

## 9. Balık Prefab Kurulumu

Her FBX model için (`Clownfish.fbx`, `Surgeonfish.fbx`):

1. `Assets/Models/Fish/` klasörüne FBX sürükle.
2. FBX'i Hierarchy'e sürükle, pozisyonu (0,0,0) yap.
3. Kök GameObject'e şu bileşenleri ekle:
   - `FishController`
   - `InspectionController`
   - `SwimAnimatorBridge`
   - `Animator` (Animator Controller: `Assets/Animations/Fish_AC.controller`)
   - `Collider` (Capsule veya Mesh, tüm gövdeyi kapsayacak şekilde)
   - `AudioSource`

4. Her parça için child GameObject oluştur (örn: `Part_DorsalFin`):
   - `FishPartController` ekle
   - `BoxCollider` / `MeshCollider` ekle (isTrigger = false)

5. Hierarchy'den prefab oluştur: `Assets/Prefabs/Fish/Clownfish.prefab`

6. FishData asset'ine (`Clownfish_Data.asset`) prefabı sürükle.

---

## 10. Animator Controller

`Assets/Animations/Fish_AC.controller` oluştur:

```
Animator Controller
├── Parameters: IsInspecting (Bool), SwimSpeed (Float)
└── States:
    ├── Swim (default) ← "swim_cycle" animasyon klibi; Loop Time = ON
    └── Idle ← Any State → Idle (IsInspecting == true ile trigger)
```

---

## 11. Build Settings

**File → Build Settings**

1. **MenuScene** ve **ARScene**'i sırayla ekle (index 0 ve 1).
2. Platform: **Android** → **Switch Platform**
3. **Build** veya **Build and Run**

iOS için: Platform: **iOS** → **Switch Platform** → **Build** → Xcode'da aç.

---

## 12. Test Kontrol Listesi

- [ ] Uygulama açılıyor, MenuScene yükleniyor
- [ ] Balık seçim butonları görünüyor
- [ ] Butona basınca ARScene'e geçiş yapıyor
- [ ] Onboarding overlay görünüyor
- [ ] Zemin taraması → plane dedektörü çalışıyor
- [ ] Overlay soluklaşarak kayboluyor
- [ ] Plane üzerine tap → balık yerleşiyor, yüzme animasyonu başlıyor
- [ ] Balığa tap → bilgi paneli açılıyor (isim, habitat, beslenme, özet)
- [ ] "Sesli Anlatım" butonu → ses çalıyor, tekrar basınca duruyor
- [ ] "Detaylı İncele" butonu → balık duruyor, kameraya yaklaşıyor
- [ ] Sürükle → balık döndürüyor (X ve Y ekseni)
- [ ] Yüzgeç/ağız/göz'e tap → tooltip açılıyor
- [ ] Tooltip kapatma butonu çalışıyor
- [ ] "Geri Dön" → balık yerine dönüyor, yüzme başlıyor
