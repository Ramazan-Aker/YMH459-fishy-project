using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// FishNarrator — Android Text-to-Speech manager for the AR Aquarium project.
/// Each fish species gets a unique voice character (pitch + speech rate).
/// Call Narrate(fishName, text) to start speaking and StopNarration() to stop.
/// Uses Android's built-in TTS engine; no internet or audio files required.
/// </summary>
public class FishNarrator : MonoBehaviour
{
    // ─── Singleton ──────────────────────────────────────────────────────────────
    public static FishNarrator Instance { get; private set; }

    // ─── Android TTS objects ────────────────────────────────────────────────────
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject ttsObject;
    private AndroidJavaObject unityActivity;
    private bool ttsReady = false;
#endif

    // ─── Voice profiles: (pitch, speechRate) — tweak for personality ───────────
    // Pitch:       1.0 = normal, >1.0 = higher, <1.0 = lower (deep)
    // SpeechRate:  1.0 = normal, >1.0 = faster,  <1.0 = slower
    private static readonly Dictionary<string, (float pitch, float rate)> VoiceProfiles =
        new Dictionary<string, (float, float)>
    {
        { "marine_clownfish",       (1.35f, 1.15f) },  // cheerful, energetic, a bit fast
        { "freshWater_guppy",       (1.45f, 1.20f) },  // tiny, high-pitched, quick
        { "EmperorAngelfish_swim1", (0.80f, 0.88f) },  // majestic, slow, deep
        { "Surgeonfish",            (1.10f, 1.05f) },  // confident, medium
        { "Clownfish 1",            (1.30f, 1.10f) },  // playful, slightly high
        { "Whale",                  (0.55f, 0.72f) },  // very deep, slow, massive
        { "Shark",                  (0.70f, 0.90f) },  // menacing, low, measured
    };

    // ─── Narration scripts (1st-person, Turkish) ────────────────────────────────
    private static readonly Dictionary<string, string> NarrationScripts =
        new Dictionary<string, string>
    {
        {
            "marine_clownfish",
            "Merhaba! Ben palyaço balığıyım. Amphiprion ocellaris! " +
            "Hint ve Pasifik okyanuslarının sıcak mercan resiflerinde, " +
            "deniz anemonu ev sahibimle birlikte yaşıyorum. " +
            "Onun zehirli dokunmaçlarından hiç korkmuyorum çünkü " +
            "vücudumda özel bir mukus tabakası var! " +
            "Küçük olabilirim ama çok özel bir sırrım var: " +
            "hepimiz erkek doğarız. İçimizden en büyüğü dişiye dönüşür!"
        },
        {
            "freshWater_guppy",
            "Selam! Ben Lepistes balığıyım, ama herkes bana Guppy der! " +
            "Güney Amerika'nın tatlı su nehirlerinde ve göletlerinde yaşarım. " +
            "Dünya genelinde akvaryumlarda yetiştirilen en popüler balıklardan biriyim. " +
            "Beni farklı kılan şey şu: yumurta bırakmam, yavruları canlı doğururum! " +
            "Ve çok ilginç bir yeteneğim var: tek bir çiftleşmeden elde ettiğim spermi " +
            "aylarca saklayarak birden fazla nesil üretebilirim!"
        },
        {
            "EmperorAngelfish_swim1",
            "Ben... İmparator Melek Balığıyım. Pomacanthus imperator. " +
            "Hint Okyanusu'nun ve Kızıldeniz'in en derin ve en güzel mercan resiflerinde hüküm sürerim. " +
            "Otuz ile kırk santimetre boyuma ulaşabilirim. " +
            "Benim en büyük sırrım şudur: Gençliğimde tamamen farklı görünürdüm. " +
            "Koyu mavi gövde... beyaz halkalar... " +
            "Yetişkinliğe geçişte altın sarısı çizgiler belirdi. " +
            "Bu değişim o kadar dramatikti ki, bilim insanları bizi yüzyıllar boyunca " +
            "iki ayrı tür sandı."
        },
        {
            "Surgeonfish",
            "Merhaba! Ben Cerrah Balığıyım, bilim dünyası beni Paracanthurus hepatus tanır. " +
            "Ama belki 'Dory' adıyla daha çok tanıyorsunuz beni! " +
            "Hint ve Pasifik Okyanuslarının berrak sularında yüzerim. " +
            "Bitkilerle ve alglerle besleniyorum. " +
            "Aslında resifler için çok önemliyim, çünkü " +
            "aşırı büyüyen algleri yiyerek ekosistemin dengesini koruyorum. " +
            "Bir de dikkatli olun! Kuyruk sapımda jilet gibi keskin bir dikim var!"
        },
        {
            "Clownfish 1",
            "Selam! Ben de bir palyaço balığıyım, Amphiprion ocellaris! " +
            "Batı Pasifik ve Doğu Hint Okyanusunun tropik resiflerinde yaşıyorum. " +
            "Deniz anemonu olmadan bir yere gidemem desem yeridir. " +
            "Ama anemonum zehirli olduğu için diğer balıklar yaklaşamıyor! " +
            "Ben ise özel mukus tabakam sayesinde hiç etkilenmiyorum. " +
            "Hiyerarşik bir sistemimiz var: " +
            "En büyük birey dişi, ikinci büyük ise erkek. " +
            "Dişi ölünce erkek, dişiye dönüşür!"
        },
        {
            "Whale",
            "Ben... Kambur Balina'yım. Megaptera novaeangliae... " +
            "On iki ile on altı metre uzunluğum var. " +
            "Otuz tona kadar ulaşabilen ağırlığımla okyanusların dev yolcusuyum. " +
            "Yazları buzul sularında beslenirim, kışları tropikal sulara göç ederim. " +
            "Filtreleme yöntemiyle kril, plankton ve küçük balık sürüleri yemek için " +
            "devasa ağzımı açarım. " +
            "Ve benim en güzel özelliğim... şarkı söylemek. " +
            "Saatlerce süren, melodiler içeren benzersiz şarkılar söylerim. " +
            "Kuyruk yüzgecimin altındaki desenler tıpkı senin parmak izin gibi, " +
            "sadece bana aittir."
        },
        {
            "Shark",
            "Ben... Büyük Beyaz Köpekbalığı'yım. Carcharodon carcharias. " +
            "Dört buçuk ile altı metre boyumla okyanusların en korkulan avcısıyım. " +
            "Ilıman kıyı sularında yaşarım. " +
            "Foklar, deniz aslanları, balıklar ve vatozlar avlarım. " +
            "Beni diğerlerinden ayıran özelliğim, " +
            "sudaki elektrik alanlarını algılayan Lorenzini Ampullerim. " +
            "Hiçbir av benden kaçamaz. " +
            "Ve yaşamım boyunca yaklaşık yirmi bin diş değiştirebilirim. " +
            "Eskiyen dişimin hemen arkasından yenisi gelir."
        }
    };

    // ─── Lifecycle ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitialiseTTS();
    }

    private void OnDestroy()
    {
        ShutdownTTS();
    }

    // ─── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Speak the narration for the given fish species.</summary>
    public void Narrate(string fishName)
    {
        string script;
        if (!NarrationScripts.TryGetValue(fishName, out script))
        {
            Debug.LogWarning($"[FishNarrator] No narration script for fish: {fishName}");
            return;
        }

        float pitch = 1.0f;
        float rate  = 1.0f;
        if (VoiceProfiles.TryGetValue(fishName, out var profile))
        {
            pitch = profile.pitch;
            rate  = profile.rate;
        }

        StopNarration();
        StartCoroutine(SpeakCoroutine(script, pitch, rate));
    }

    /// <summary>Immediately stop any ongoing speech.</summary>
    public void StopNarration()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (ttsObject != null)
            {
                ttsObject.Call<int>("stop");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FishNarrator] StopNarration error: {e.Message}");
        }
#endif
        StopAllCoroutines();
    }

    // ─── Internals ──────────────────────────────────────────────────────────────

    private void InitialiseTTS()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }

            // Android TextToSpeech.OnInitListener via a simple proxy
            AndroidJavaClass ttsClass = new AndroidJavaClass("android.speech.tts.TextToSpeech");
            ttsObject = new AndroidJavaObject(
                "android.speech.tts.TextToSpeech",
                unityActivity,
                new TTSInitListener(this)
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FishNarrator] TTS Initialisation error: {e.Message}");
        }
#else
        Debug.Log("[FishNarrator] TTS is Android-only. Running in Editor: narration skipped.");
#endif
    }

    private void ShutdownTTS()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (ttsObject != null)
            {
                ttsObject.Call<int>("stop");
                ttsObject.Call("shutdown");
                ttsObject.Dispose();
                ttsObject = null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FishNarrator] TTS Shutdown error: {e.Message}");
        }
#endif
    }

    private IEnumerator SpeakCoroutine(string text, float pitch, float rate)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Wait up to 3 seconds for TTS to be ready
        float waited = 0f;
        while (!ttsReady && waited < 3f)
        {
            yield return null;
            waited += Time.deltaTime;
        }

        if (!ttsReady)
        {
            Debug.LogWarning("[FishNarrator] TTS not ready in time, skipping narration.");
            yield break;
        }

        try
        {
            // Set language to Turkish
            using (AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale"))
            {
                AndroidJavaObject turkishLocale = new AndroidJavaObject("java.util.Locale", "tr", "TR");
                int langResult = ttsObject.Call<int>("setLanguage", turkishLocale);

                if (langResult == -2 || langResult == -1)
                {
                    Debug.LogWarning($"[FishNarrator] Turkish TTS not supported (code {langResult}). Falling back to default locale.");
                }
            }

            // Set pitch and speech rate
            ttsObject.Call<int>("setPitch", pitch);
            ttsObject.Call<int>("setSpeechRate", rate);

            // Speak — QUEUE_FLUSH replaces any current speech immediately
            using (AndroidJavaClass ttsConst = new AndroidJavaClass("android.speech.tts.TextToSpeech"))
            {
                int QUEUE_FLUSH = ttsConst.GetStatic<int>("QUEUE_FLUSH");
                ttsObject.Call<int>("speak", text, QUEUE_FLUSH, null, "fish_narration");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FishNarrator] Speak error: {e.Message}");
        }
#else
        // Editor fallback: just log the narration text
        Debug.Log($"[FishNarrator] (Editor) Pitch={pitch} Rate={rate}\n{text}");
        yield return null;
#endif
    }

    // ─── OnInitListener proxy ────────────────────────────────────────────────────
#if UNITY_ANDROID && !UNITY_EDITOR
    private class TTSInitListener : AndroidJavaProxy
    {
        private FishNarrator narrator;

        public TTSInitListener(FishNarrator narrator)
            : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            this.narrator = narrator;
        }

        // Called by Android when TTS engine is initialised
        public void onInit(int status)
        {
            // android.speech.tts.TextToSpeech.SUCCESS == 0
            if (status == 0)
            {
                narrator.ttsReady = true;
                Debug.Log("[FishNarrator] TTS initialised successfully.");
            }
            else
            {
                Debug.LogError($"[FishNarrator] TTS init failed with status: {status}");
            }
        }
    }
#endif
}
