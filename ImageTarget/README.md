# Image Target Görselleri

Bu klasör, **YMH459 Fishy Project** kapsamında Unity sahnesindeki Image Target objelerine atanmış **5 adet görsel** dosyasını içermektedir.

## Amaç

Bu görseller, Vuforia AR motorunda **Image Target** olarak tanımlanmış hedef görsellerdir. Uygulama çalışırken mobil cihazın kamerası bu görsellerden birini algıladığında, ilgili 3D balık modeli ekranda AR (Artırılmış Gerçeklik) olarak görüntülenir.

Her bir görsel, Unity sahnesinde bir `ImageTargetBehaviour` bileşenine `mRuntimeTexture` olarak atanmıştır.

## Görseller ve Karşılık Gelen Balık Modelleri

| Dosya Adı | Vuforia Trackable Adı | Hedef Balık Modeli | Sahne Objesi |
|---|---|---|---|
| `fishimage.png` | fishimage | Clownfish (Palyaço Balığı) | ImageTarget |
| `Fish_Water_New.png` | Fish_Water_New | Guppy (Lepistes) | ImageTarget_Freshwater / ImageTarget (1) |
| `whale.png` | whale | Whale (Balina) | ImageTarget_Whale |
| `suggenofish.png` | suggenofish | Surgeonfish (Cerrah Balığı) | ImageTarget_Surgeonfish |
| `shark.png` | shark | Shark (Köpekbalığı) | ImageTarget_Shark |

## Kullanım

1. Bu görselleri yazdırın veya bir ekranda görüntüleyin.
2. Mobil cihazda uygulamayı açın.
3. Kamerayı görsele doğrultun.
4. İlgili 3D balık modeli AR olarak ekranda belirecektir.

## Not

- Bu görseller Unity sahnesindeki Image Target objelerine doğrudan atanmış `mRuntimeTexture` referans görselleridir.
- Görsellerin iyi aydınlatılmış, düz bir yüzeyde olması AR tanıma başarısını artırır.
- Vuforia veritabanında bu görseller kayıtlıdır ve uygulama içinde otomatik olarak eşleştirilir.
