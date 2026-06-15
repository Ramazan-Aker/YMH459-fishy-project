# Artırılmış Gerçeklik (AR) Tabanlı İnteraktif Balık Müzesi ve Eğitim Uygulaması

Bu proje, deniz canlıları ve su altı ekosistemleri hakkındaki eğitimi, artırılmış gerçeklik (AR) teknolojilerini kullanarak zenginleştirmeyi amaçlayan yenilikçi bir mobil uygulamadır. Geleneksel müze sergilerini dinamik, etkileşimli ve eğlenceli bir öğrenme deneyimine dönüştürür.

---

## 👥 Proje Ekibi ve Katkıda Bulunanlar

Uygulama, Fırat Üniversitesi Yazılım Mühendisliği bölümü öğrencileri tarafından **YMH459** dersi kapsamında geliştirilmiştir.

| Ad Soyad | Rol | Öğrenci Numarası | Kurumsal E-Posta |
| :--- | :--- | :--- | :--- |
| **Talha Eren Bilikci** | Scrum Master, Yazılım Geliştirme | 220541071 | [220541071@firat.edu.tr](mailto:220541071@firat.edu.tr) |
| **Mustafa Çalbak** | Yazılım Geliştirme | 220542013 | [220542013@firat.edu.tr](mailto:220542013@firat.edu.tr) |
| **Yusuf Afşar** | Yazılım Geliştirme | 220541105 | [220541105@firat.edu.tr](mailto:220541105@firat.edu.tr) |
| **Ramazan Aker** | Yazılım Geliştirme | 220541049 | [220541049@firat.edu.tr](mailto:220541049@firat.edu.tr) |

---

## 📱 Uygulama Kurulum Rehberi (APK Kurulumu)

Uygulamanın çalıştırılmaya hazır en güncel Android paketi (APK), proje klasörü içerisinde yer almaktadır:
* **APK Dosya Yolu:** [Fish_App_Son_Hal.apk](file:///c:/Users/musta/Desktop/YMH459-fishy-project/AndroidApplication/Fish_App_Son_Hal.apk)

### Adım Adım Kurulum:
1. **APK Dosyasını Telefona Aktarın:** Bilgisayarınızdaki `AndroidApplication/Fish_App_Son_Hal.apk` dosyasını USB kablosu, e-posta veya bulut servisleri (Google Drive vb.) aracılığıyla Android cihazınıza gönderin.
2. **Bilinmeyen Kaynaklara İzin Verin:** Android cihazınızda dosyalardan APK'yı açtığınızda güvenlikle ilgili bir uyarı alabilirsiniz. Ayarlardan "Bu kaynaktan yüklemeye izin ver" seçeneğini aktif hale getirin.
3. **Yüklemeyi Tamamlayın:** Ekranda beliren talimatları takip ederek "Yükle" (Install) butonuna basın ve yükleme işleminin tamamlanmasını bekleyin.
4. **Kamera İzinlerini Onaylayın:** Uygulamayı ilk kez başlattığınızda AR özelliklerinin çalışabilmesi için kamera erişim iznini onaylayın.

> [!IMPORTANT]
> Uygulama artırılmış gerçeklik özellikleri kullandığı için cihazınızın **ARCore** (Google Play Services for AR) destekli bir Android cihazı olması gerekmektedir.

---

## 🔍 Uygulama Nasıl Kullanılır? (Kullanım Kılavuzu)

Uygulama, **Vuforia Image Target** (Hedef Görsel) teknolojisini kullanmaktadır. Balıkları AR formatında görüntülemek ve diğer özellikleri keşfetmek için aşağıdaki adımları izleyin:

### 1. Hedef Görselleri Hazırlayın
Proje ana dizininde bulunan **[ImageTarget/](file:///c:/Users/musta/Desktop/YMH459-fishy-project/ImageTarget)** klasöründeki görselleri başka bir bilgisayar/tablet ekranında açın veya kağıda yazdırın. 

Hedef görseller ve tetikledikleri balıklar şunlardır:
* `fishimage.png` ➔ **Palyaço Balığı (Clownfish)**
* `Fish_Water_New.png` ➔ **Lepistes (Guppy)**
* `whale.png` ➔ **Balina (Whale)**
* `suggenofish.png` ➔ **Cerrah Balığı (Surgeonfish)**
* `shark.png` ➔ **Köpekbalığı (Shark)**

### 2. Kamerayı Odaklayın
Uygulamayı açtıktan sonra mobil cihazınızın kamerasını bu görsellerden birine doğrultun.

### 3. Keşfedin ve Etkileşime Geçin
* **3D AR Canlandırma:** Görsel algılandığı anda üzerinde ilgili balığın 3 boyutlu, animasyonlu modeli belirecektir.
* **🔄 360 Derece Döndürme ve Dikey Hareket:** Algılanan 3D balık modeline ekranda dokunup parmağınızı sağa-sola kaydırarak modeli yatay eksende **360 derece pürüzsüzce döndürebilirsiniz**. Parmağınızı yukarı-aşağı sürükleyerek ise balığın akvaryum içindeki dikey yüksekliğini ayarlayabilirsiniz. Bu sayede balıkları her açıdan detaylıca incelemek mümkündür.
* **Detaylı Bilgi Kartı:** Balığın üzerine dokunduğunuzda açılan panellerden türün habitatı, beslenme şekli gibi bilgileri öğrenebilirsiniz.
* **Sesli Anlatıcı (Narrator):** Bilgi paneli açıldığında sesli anlatım otomatik olarak başlayarak bilgilendirmeyi dinlemenizi sağlar.
* **Bilgi Testi (Quiz):** Öğrendiğiniz bilgileri sınamak için arayüzde bulunan mini testi (Quiz) başlatabilir ve soruları yanıtlayarak kendinizi test edebilirsiniz.
* **🎮 2D Akvaryum Oyunu (Oyun Modu):** Ekrandaki "Oyun Modu" butonuna basarak 2D akvaryum simülasyonunu başlatabilirsiniz. Bu modda:
  - Seçtiğiniz balık türüne göre özelleştirilmiş 2D balığınızı ekrandaki **dokunmatik Joystick** ile yönlendirirsiniz.
  - Akvaryumdaki diğer küçük yem balıklarını yiyerek puan toplar ve kendi balığınızı **büyütürsünüz**.
  - **⚠️ Tehlikeler ve Oyun Mekanikleri:**
    - **Deniz Anaları (Jellyfish):** Akvaryumda yüzen deniz analarını yemek balığınızın **zehirlenmesine** (yavaşlamasına ve zayıflamasına) yol açar.
    - **Büyük Balıklar (Big Fish):** Sizden daha büyük balıkları yemeye çalışırsanız, büyük balıklar tarafından avlanırsınız ve balığınız **ölür** (Oyun Biter).
  - AR moduna dilediğiniz zaman "AR'a Dön" butonuyla geçiş yapabilirsiniz.

---

## 🛠️ Teknoloji Yığını (Technology Stack)

* **Oyun Motoru:** Unity 3D
* **AR Kütüphanesi:** Vuforia Engine (Image Target takibi için)
* **Programlama Dili:** C# (.NET Framework 4.7.1+)
* **3D Tasarım:** Blender (Optimize edilmiş Low-poly balık modelleri ve yüzme animasyonları)
* **Sürüm Kontrolü:** Git & GitHub