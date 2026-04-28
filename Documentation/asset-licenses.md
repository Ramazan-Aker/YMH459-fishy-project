# Asset Lisansları

Bu dosya, projede kullanılan tüm üçüncü taraf asset'lerin lisans bilgilerini takip eder.
Her asset eklendiğinde bu tabloyu güncelleyiniz.

## 3D Modeller

| Asset Adı | Kaynak | Lisans | Link | Kullanım Yeri | Tarih |
|-----------|--------|--------|------|---------------|-------|
| Clownfish FBX | Sketchfab | CC-BY 4.0 | — | Assets/Models/Fish/Clownfish/ | — |
| Surgeonfish (Blue Tang) FBX | Sketchfab | CC-BY 4.0 | — | Assets/Models/Fish/Surgeonfish/ | — |

## Sesli Anlatım

| Dosya | Üretim Yöntemi | Telif | Tarih |
|-------|---------------|-------|-------|
| clownfish_tr.wav | Google TTS / gTTS Python script | Telif yok (oluşturuldu) | — |
| surgeonfish_tr.wav | Google TTS / gTTS Python script | Telif yok (oluşturuldu) | — |

## Ses Efektleri

| Dosya | Kaynak | Lisans | Tarih |
|-------|--------|--------|-------|
| — | — | — | — |

## UI / İkonlar

| Asset | Kaynak | Lisans | Tarih |
|-------|--------|--------|-------|
| — | — | — | — |

---

## TTS Sesi Üretme Kılavuzu (gTTS)

```bash
pip install gTTS
python - <<'EOF'
from gtts import gTTS

clownfish_text = """
Palyaço balığı, Amphiprion ocellaris. Hint ve Pasifik okyanuslarının sığ mercan
resiflerinde yaşar. Deniz şakayıkları ile simbiyotik bir ilişki sürdürür; zehirli
tentaküller onu etkilemez. Turuncu rengi ve üç beyaz çizgisi sayesinde kolayca tanınır.
Gruplarda baskın dişi ölürse, baskın erkek cinsiyetini dişiye dönüştürebilir.
"""
tts = gTTS(clownfish_text, lang="tr")
tts.save("clownfish_tr.mp3")

surgeonfish_text = """
Cerrah balığı, Paracanthurus hepatus. Mavi Tang olarak da bilinir. Mercan resiflerinde
yaşar ve alg tüketimiyle resin sağlığını korur. Kuyruk sapındaki keskin dikensi
uzantısı, savunma amaçlı kullanılır. Parlak mavi rengi ve sarı kuyruk yüzgeci
ile kolayca ayırt edilir.
"""
tts = gTTS(surgeonfish_text, lang="tr")
tts.save("surgeonfish_tr.mp3")
EOF
```

Üretilen `.mp3` dosyalarını `Assets/Audio/Narration/` klasörüne kopyalayın.
Unity'de AudioClip olarak içe aktardıktan sonra ilgili FishData asset'indeki
`narrationClip` alanına sürükleyin.
