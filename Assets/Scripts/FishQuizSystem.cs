using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// FishQuizSystem — Runtime UI quiz panel attached to the AR Aquarium project.
/// Generates a 10-question (randomly drawn from 15+) multiple-choice quiz
/// for each fish species. Called from ARAquariumController when the user
/// presses "Mini Test Başlat" on the fish info panel.
/// </summary>
public class FishQuizSystem : MonoBehaviour
{
    // ─── Singleton ──────────────────────────────────────────────────────────
    public static FishQuizSystem Instance { get; private set; }

    // ─── Data ────────────────────────────────────────────────────────────────
    private struct QuizQuestion
    {
        public string question;
        public string[] options;   // always 4 options
        public int correctIndex;   // 0-3
    }

    // ─── UI references (built at runtime) ───────────────────────────────────
    private Canvas        quizCanvas;
    private GameObject    quizPanel;
    private Text          quizHeaderText;
    private Text          progressText;
    private Text          questionText;
    private Button[]      answerButtons   = new Button[4];
    private Text[]        answerTexts     = new Text[4];
    private Image[]       answerImages    = new Image[4];
    private GameObject    resultPanel;
    private Text          resultTitleText;
    private Text          resultScoreText;
    private Text          resultStarsText;
    private Button        resultRestartBtn;
    private Button        resultCloseBtn;
    private Sprite        circleSprite;
    private Font          uiFont;

    // ─── State ───────────────────────────────────────────────────────────────
    private bool                    uiBuilt;
    private string                  currentFishName;
    private List<QuizQuestion>      currentQuestions;
    private int                     currentIndex;
    private int                     correctCount;
    private int                     wrongCount;
    private bool                    waitingForNext;

    // ─── Colors ──────────────────────────────────────────────────────────────
    private static readonly Color ColCorrect  = new Color(0.12f, 0.75f, 0.35f, 1f);
    private static readonly Color ColWrong    = new Color(0.87f, 0.18f, 0.18f, 1f);
    private static readonly Color ColDefault  = new Color(0.14f, 0.36f, 0.62f, 1f);
    private static readonly Color ColDisabled = new Color(0.45f, 0.50f, 0.55f, 1f);

    // ════════════════════════════════════════════════════════════════════════
    //  QUESTION BANK  (15 questions per fish — 10 randomly selected per quiz)
    // ════════════════════════════════════════════════════════════════════════
    private static readonly Dictionary<string, QuizQuestion[]> QuestionBank =
        new Dictionary<string, QuizQuestion[]>
    {
        // ── Palyaço Balığı (marine_clownfish) ─────────────────────────────
        { "marine_clownfish", new QuizQuestion[]
            {
                new QuizQuestion { question="Palyaço balığının bilimsel adı nedir?", options=new[]{"Amphiprion ocellaris","Poecilia reticulata","Pomacanthus imperator","Paracanthurus hepatus"}, correctIndex=0 },
                new QuizQuestion { question="Palyaço balığı hangi canlıyla birlikte yaşar?", options=new[]{"Köpekbalığı","Deniz anemonuyla","İstakozla","Denizyıldızıyla"}, correctIndex=1 },
                new QuizQuestion { question="Palyaço balığı hangi okyanusta yaşar?", options=new[]{"Atlantik Okyanusu","Arktik Okyanusu","Hint-Pasifik Okyanusu","Kuzey Buz Denizi"}, correctIndex=2 },
                new QuizQuestion { question="Palyaço balığı ne ile beslenir?", options=new[]{"Sadece büyük balıklarla","Alg ve planktonla","Yalnızca taşlarla","Yalnızca suyla"}, correctIndex=1 },
                new QuizQuestion { question="Palyaço balığı ne kadar büyüyebilir?", options=new[]{"30-40 cm","50-60 cm","8-11 cm","1-2 cm"}, correctIndex=2 },
                new QuizQuestion { question="Palyaço balığı neden anemon zehrinden etkilenmez?", options=new[]{"Kabuğu vardır","Özel mukus tabakası vardır","Zehire alışmıştır","Çok hızlı yüzer"}, correctIndex=1 },
                new QuizQuestion { question="Palyaço balıkları hangi cinsiyetle doğar?", options=new[]{"Dişi","Erkek","Hermafrodit","İkisi birden"}, correctIndex=1 },
                new QuizQuestion { question="Palyaço balığı hangi familyaya aittir?", options=new[]{"Pomacanthidae","Poeciliidae","Amphiprioninae","Lamnidae"}, correctIndex=2 },
                new QuizQuestion { question="Gruptaki en büyük palyaço balığı ne olur?", options=new[]{"Dişiye dönüşür","Ölür","Grubu terk eder","Küçülür"}, correctIndex=0 },
                new QuizQuestion { question="Palyaço balığının beslenme tipi nedir?", options=new[]{"Karnivor","Omnivor","Otçul","Filtre besleyici"}, correctIndex=1 },
                new QuizQuestion { question="Palyaço balığı hangi derinlikte yaşar?", options=new[]{"1000 m altında","500-800 m","Sığ tropikal sularda","Buzul sularında"}, correctIndex=2 },
                new QuizQuestion { question="Anemon ile palyaço balığı arasındaki ilişki nedir?", options=new[]{"Avcı-av","Simbiyotik","Rakip","Parazit"}, correctIndex=1 },
                new QuizQuestion { question="Palyaço balığı hangi ülkeyle özdeşleşmiştir?", options=new[]{"Nemo filmiyle","Dory filmiyle","Şark filmiyle","Findet Nemo filmiyle"}, correctIndex=0 },
                new QuizQuestion { question="Palyaço balığının dişisi ölünce ne olur?", options=new[]{"Yeni dişi gelir","Baskın erkek dişiye dönüşür","Grup dağılır","Erkekler de ölür"}, correctIndex=1 },
                new QuizQuestion { question="Palyaço balığı hangi suyu tercih eder?", options=new[]{"Soğuk kutup suyu","Tatlı su","Sıcak tropikal tuzlu su","Acı su"}, correctIndex=2 },
            }
        },

        // ── Lepistes (freshWater_guppy) ────────────────────────────────────
        { "freshWater_guppy", new QuizQuestion[]
            {
                new QuizQuestion { question="Lepistes balığının bilimsel adı nedir?", options=new[]{"Amphiprion ocellaris","Poecilia reticulata","Carassius auratus","Danio rerio"}, correctIndex=1 },
                new QuizQuestion { question="Lepistes hangi sularda yaşar?", options=new[]{"Tuzlu su","Tatlı su","Acı su","Buzul suyu"}, correctIndex=1 },
                new QuizQuestion { question="Lepistes balığı nereden gelir?", options=new[]{"Kuzey Avrupa","Güney Amerika","Doğu Afrika","Japonya"}, correctIndex=1 },
                new QuizQuestion { question="Lepistesi diğer balıklardan ayıran üreme özelliği nedir?", options=new[]{"Yumurta bırakır","Canlı doğurur","İki kez ürer","Klonlanır"}, correctIndex=1 },
                new QuizQuestion { question="Lepistes kaç cm büyüyebilir?", options=new[]{"15-20 cm","30-40 cm","3-6 cm","1 cm"}, correctIndex=2 },
                new QuizQuestion { question="Lepistes hangi familyaya aittir?", options=new[]{"Poeciliidae","Amphiprioninae","Acanthuridae","Balaenopteridae"}, correctIndex=0 },
                new QuizQuestion { question="Lepistes balıkları neden popülerdir?", options=new[]{"Çok büyürler","Renkleri ve kolaylıkları","Zehirlidirler","Yenilebilirler"}, correctIndex=1 },
                new QuizQuestion { question="Lepisteste hangi cins daha renklidir?", options=new[]{"Dişiler","Erkekler","İkisi de eşit","Hiçbiri"}, correctIndex=1 },
                new QuizQuestion { question="Lepistes akvaryumda nasıl beslenir?", options=new[]{"Yalnız büyük et","Pul yem, canlı yem, bitkiler","Yalnız su yosunu","Hiç beslenmez"}, correctIndex=1 },
                new QuizQuestion { question="Dişi lepistes çiftleşme spermi ne kadar saklayabilir?", options=new[]{"1 gün","1 hafta","Aylarca","Asla saklayamaz"}, correctIndex=2 },
                new QuizQuestion { question="Lepistes hangi organizmaları yer?", options=new[]{"Köpekbalığı","Sivrisinek larvaları","Balina","Köpek"}, correctIndex=1 },
                new QuizQuestion { question="Lepistes akvaryumda kaç yıl yaşayabilir?", options=new[]{"1 ay","50 yıl","2-5 yıl","100 yıl"}, correctIndex=2 },
                new QuizQuestion { question="Lepisteste erkek ve dişi arasındaki boyut farkı?", options=new[]{"Erkekler daha büyük","Dişiler daha büyük","Aynı boyut","Yaşa göre değişir"}, correctIndex=1 },
                new QuizQuestion { question="Lepistes ne tür beslenme tipindedir?", options=new[]{"Otçul","Etçil","Omnivor","Filtre"}, correctIndex=2 },
                new QuizQuestion { question="Lepistes özgün ortamda hangi haşereleri kontrol eder?", options=new[]{"Arıları","Sivrisinekleri","Çekirgeleri","Karıncaları"}, correctIndex=1 },
            }
        },

        // ── İmparator Melek Balığı (EmperorAngelfish_swim1) ───────────────
        { "EmperorAngelfish_swim1", new QuizQuestion[]
            {
                new QuizQuestion { question="İmparator Melek Balığının bilimsel adı nedir?", options=new[]{"Amphiprion ocellaris","Pomacanthus imperator","Paracanthurus hepatus","Carcharodon carcharias"}, correctIndex=1 },
                new QuizQuestion { question="İmparator Melek Balığı kaç cm büyür?", options=new[]{"5-10 cm","30-40 cm","100 cm","1 metre"}, correctIndex=1 },
                new QuizQuestion { question="Hangi okyanusta yaşar?", options=new[]{"Atlantik","Hint-Pasifik ve Kızıldeniz","Arktik","Antarktik"}, correctIndex=1 },
                new QuizQuestion { question="Ne ile beslenir?", options=new[]{"Büyük balıklar","Süngerler ve tunikatlar","Sadece su","Kril"}, correctIndex=1 },
                new QuizQuestion { question="Gençken nasıl görünür?", options=new[]{"Sarı çizgili","Koyu mavi-beyaz halkalı","Tamamen beyaz","Kırmızı"}, correctIndex=1 },
                new QuizQuestion { question="Yetişkinken nasıl görünür?", options=new[]{"Hâlâ koyu mavi","Sarı-mavi yatay çizgili","Tamamen siyah","Gri"}, correctIndex=1 },
                new QuizQuestion { question="Hangi familyaya aittir?", options=new[]{"Poeciliidae","Amphiprioninae","Pomacanthidae","Acanthuridae"}, correctIndex=2 },
                new QuizQuestion { question="Kaç metreye kadar inerler?", options=new[]{"1 metre","10 metre","100 metre","1000 metre"}, correctIndex=2 },
                new QuizQuestion { question="Gençleri ve yetişkinleri neden farklı tür sanılmıştır?", options=new[]{"Boyutları çok farklı","Tamamen farklı renk desenleri","Farklı sularda yaşarlar","Farklı sesler çıkarırlar"}, correctIndex=1 },
                new QuizQuestion { question="Beslenme tipi nedir?", options=new[]{"Karnivor","Omnivor","Otçul","Filtre besleyici"}, correctIndex=1 },
                new QuizQuestion { question="Lagün ve resif yamacını neden tercih eder?", options=new[]{"Su sıcaklığı","Besin zenginliği","Korunma","Hepsi"}, correctIndex=3 },
                new QuizQuestion { question="'İmparator' adını neden almıştır?", options=new[]{"En büyük olduğu için","Görkemli renkleri nedeniyle","En zehirli olduğu için","En hızlı olduğu için"}, correctIndex=1 },
                new QuizQuestion { question="İmparator Melek Balığı hangi ürünü sever?", options=new[]{"Tunikat","Alg","Kril","Canlı av"}, correctIndex=0 },
                new QuizQuestion { question="Renk değişimi ne zaman başlar?", options=new[]{"Doğumda","Yetişkinliğe geçişte","Yaşlıkta","Ölmeden önce"}, correctIndex=1 },
                new QuizQuestion { question="Kaç metreye kadar yaşar bu balık?", options=new[]{"2 yıl","5 yıl","15+ yıl","100 yıl"}, correctIndex=2 },
            }
        },

        // ── Cerrah Balığı (Surgeonfish) ────────────────────────────────────
        { "Surgeonfish", new QuizQuestion[]
            {
                new QuizQuestion { question="Cerrah Balığının bilimsel adı nedir?", options=new[]{"Pomacanthus imperator","Paracanthurus hepatus","Amphiprion ocellaris","Megaptera novaeangliae"}, correctIndex=1 },
                new QuizQuestion { question="Cerrah Balığı hangi filmde ünlü oldu?", options=new[]{"Köpekbalığı","Nemo'yu Bul / Dory'yi Bul","Su Altı Dünyası","Avatar"}, correctIndex=1 },
                new QuizQuestion { question="Cerrah Balığı ne ile beslenir?", options=new[]{"Etçil - büyük av","Omnivor","Otçul - alg ve zooplankton","Filtre besleyici"}, correctIndex=2 },
                new QuizQuestion { question="Cerrah Balığı kaç cm büyür?", options=new[]{"5-8 cm","15-31 cm","50-70 cm","1-2 cm"}, correctIndex=1 },
                new QuizQuestion { question="Cerrah Balığının özel silahı nedir?", options=new[]{"Zehirli dikenler","Kuyruk sapındaki keskin diken","Elektrik organı","Mürekkep"}, correctIndex=1 },
                new QuizQuestion { question="Ekosistemde ne işe yarar?", options=new[]{"Büyük balık avlar","Alg büyümesini kontrol eder","Su temizler","Diğer balıkları korur"}, correctIndex=1 },
                new QuizQuestion { question="Hangi familyaya aittir?", options=new[]{"Pomacanthidae","Poeciliidae","Acanthuridae","Lamnidae"}, correctIndex=2 },
                new QuizQuestion { question="Hangi suları tercih eder?", options=new[]{"Derin karanlık sular","Berrak sığ mercan resifleri","Buzul suları","Tatlı su"}, correctIndex=1 },
                new QuizQuestion { question="Neden 'Cerrah' adını almıştır?", options=new[]{"Diğer balıkları tedavi eder","Jilet gibi keskin dikeni olduğundan","En zeki balık olduğundan","Beyazlar giyer"}, correctIndex=1 },
                new QuizQuestion { question="Stres altında ne olur?", options=new[]{"Rengi solar","Büyür","Saldırır","Uyur"}, correctIndex=0 },
                new QuizQuestion { question="Hangi okyanusta yaşar?", options=new[]{"Atlantik Okyanusu","Hint-Pasifik Okyanusu","Arktik","Antarktik"}, correctIndex=1 },
                new QuizQuestion { question="'Dory' karakterinin renkleri nelerdir?", options=new[]{"Turuncu-beyaz","Mavi-sarı","Kırmızı-siyah","Yeşil-beyaz"}, correctIndex=1 },
                new QuizQuestion { question="Kaç metreye kadar iner?", options=new[]{"1000 m","200 m","40 m","5 m"}, correctIndex=2 },
                new QuizQuestion { question="Cerrah balığının beslenme biçimi neden önemlidir?", options=new[]{"Ekosistemi dengeler","En lezzetli balıktır","En hızlı yüzen balıktır","Diğer balıkları besler"}, correctIndex=0 },
                new QuizQuestion { question="Kuyruk dikeni ne için kullanılır?", options=new[]{"Yüzmek için","Saldırı ve savunma","Balık avlamak","Navigasyon"}, correctIndex=1 },
            }
        },

        // ── Palyaço Balığı-1 (Clownfish 1) ────────────────────────────────
        { "Clownfish 1", new QuizQuestion[]
            {
                new QuizQuestion { question="Ocellaris Palyaço Balığının yaşadığı yer?", options=new[]{"Kuzey Buz Denizi","Batı Pasifik ve Doğu Hint Okyanusu","Atlantik Okyanusu","Tatlı su gölleri"}, correctIndex=1 },
                new QuizQuestion { question="Kaç metreye kadar iner?", options=new[]{"500 m","100 m","1-15 m","1000 m"}, correctIndex=2 },
                new QuizQuestion { question="Ocellaris palyaço balığı ne kadar büyür?", options=new[]{"30-40 cm","7-11 cm","50 cm","1-2 cm"}, correctIndex=1 },
                new QuizQuestion { question="Dişi ve erkek boyutu karşılaştırması?", options=new[]{"Erkekler daha büyük","Dişiler daha büyük","Aynı boyut","Değişir"}, correctIndex=1 },
                new QuizQuestion { question="Mukus tabakası ne işe yarar?", options=new[]{"Hızlı yüzmeyi sağlar","Anemon zehrinden korur","Renk verir","Isıtır"}, correctIndex=1 },
                new QuizQuestion { question="Hiyerarşide en büyük birey kimdir?", options=new[]{"Erkek","Dişi","Her ikisi de eşit","Yaşlı olan"}, correctIndex=1 },
                new QuizQuestion { question="Dişi ölünce baskın erkek ne olur?", options=new[]{"O da ölür","Dişiye dönüşür","Grubu terk eder","Küçülür"}, correctIndex=1 },
                new QuizQuestion { question="Ne ile beslenir?", options=new[]{"Büyük balıklarla","Alg, plankton, isopodlarla","Sadece su","Krilyle"}, correctIndex=1 },
                new QuizQuestion { question="Hangi familyaya aittir?", options=new[]{"Amphiprioninae","Pomacentridae","Pomacanthidae","Lamnidae"}, correctIndex=1 },
                new QuizQuestion { question="Anemonuyla ilişkisi nedir?", options=new[]{"Onu yer","Karşılıklı fayda sağlar","Ondan kaçar","Yok"}, correctIndex=1 },
                new QuizQuestion { question="Anemonunu nasıl temizler?", options=new[]{"Su püskürtür","Onu yerken","Üzerinde yüzerek","Temizlemez"}, correctIndex=1 },
                new QuizQuestion { question="Ocellaris balığının karakteristik rengi?", options=new[]{"Mavi-sarı","Turuncu-beyaz-siyah","Yeşil-beyaz","Kırmızı-siyah"}, correctIndex=1 },
                new QuizQuestion { question="Palyaço balığının beslenme tipi?", options=new[]{"Omnivor","Karnivor","Otçul","Filtre"}, correctIndex=0 },
                new QuizQuestion { question="Hangi ocyanusta yaşarlar genellikle?", options=new[]{"Atlantik","Hint-Pasifik","Arktik","Antarktik"}, correctIndex=1 },
                new QuizQuestion { question="Anemon ile palyaço balığı birlikte ne yapar?", options=new[]{"Aynı yiyeceği yer","Birbirini korur, birlikte yaşar","Rekabet eder","Hiç etkileşmez"}, correctIndex=1 },
            }
        },

        // ── Balina (Whale) ─────────────────────────────────────────────────
        { "Whale", new QuizQuestion[]
            {
                new QuizQuestion { question="Kambur Balinanın bilimsel adı nedir?", options=new[]{"Orcinus orca","Megaptera novaeangliae","Balaena mysticetus","Physeter macrocephalus"}, correctIndex=1 },
                new QuizQuestion { question="Kambur balina kaç metre uzunluğa ulaşır?", options=new[]{"3-5 m","12-16 m","30-40 m","50-60 m"}, correctIndex=1 },
                new QuizQuestion { question="Kambur balina kaç tona ulaşabilir?", options=new[]{"1 ton","5 ton","30 ton","500 ton"}, correctIndex=2 },
                new QuizQuestion { question="Kambur balina nasıl beslenir?", options=new[]{"Büyük köpekbalığı avlar","Kril ve küçük balıkları filtreler","Yosun yer","Hiçbir şey yemez"}, correctIndex=1 },
                new QuizQuestion { question="Yazları nerede yaşar?", options=new[]{"Tropik sularda","Kutup sularında","Tatlı sularda","Kuzey denizlerinde"}, correctIndex=1 },
                new QuizQuestion { question="Kışları nereye göç eder?", options=new[]{"Kutup sularına","Tropikal sulara","Güney'e","Tatlı sulara"}, correctIndex=1 },
                new QuizQuestion { question="Kambur balina neden ünlüdür?", options=new[]{"En büyük olduğu için","Şarkı söylediği için","Zehirli olduğu için","Uçabildiği için"}, correctIndex=1 },
                new QuizQuestion { question="Hangi cins şarkı söyler?", options=new[]{"Dişiler","Erkekler","İkisi birden","Hiçbiri"}, correctIndex=1 },
                new QuizQuestion { question="Kuyruk yüzgecinin benzersiz özelliği nedir?", options=new[]{"Her balina için aynıdır","Her bireye özgü parmak izi gibidir","Işık saçar","Değişmez"}, correctIndex=1 },
                new QuizQuestion { question="Hangi familyaya aittir?", options=new[]{"Delphinidae","Physeteridae","Balaenopteridae","Orcidae"}, correctIndex=2 },
                new QuizQuestion { question="Kambur balina memeli midir?", options=new[]{"Hayır, balıktır","Evet, memelilerdir","Sürüngendir","Amfibidir"}, correctIndex=1 },
                new QuizQuestion { question="Kambur balinanın beslenme sistemi nedir?", options=new[]{"Diş sistemi","Filtre sistemi (balin)","Pençe","Boru ağzı"}, correctIndex=1 },
                new QuizQuestion { question="Kambur balina ne kadar yaşayabilir?", options=new[]{"5 yıl","50 yıl","45-80 yıl","200 yıl"}, correctIndex=2 },
                new QuizQuestion { question="Göç nedenlerinden biri nedir?", options=new[]{"Renk değiştirmek","Üremek ve doğurmak","Avlanmak","Uyumak"}, correctIndex=1 },
                new QuizQuestion { question="Kambur balinanın şarkıları ne kadar sürebilir?", options=new[]{"Birkaç saniye","10 dakika","Saatlerce","Günlerce"}, correctIndex=2 },
            }
        },

        // ── Köpekbalığı (Shark) ────────────────────────────────────────────
        { "Shark", new QuizQuestion[]
            {
                new QuizQuestion { question="Büyük Beyaz Köpekbalığının bilimsel adı?", options=new[]{"Rhincodon typus","Carcharodon carcharias","Isurus oxyrinchus","Sphyrna lewini"}, correctIndex=1 },
                new QuizQuestion { question="Büyük beyaz köpekbalığı kaç metreye ulaşır?", options=new[]{"1-2 m","4.5-6 m","10-12 m","20 m"}, correctIndex=1 },
                new QuizQuestion { question="Hangi cins daha büyüktür?", options=new[]{"Erkekler","Dişiler","Aynı boyut","Değişir"}, correctIndex=1 },
                new QuizQuestion { question="Lorenzini Ampulleri ne için kullanılır?", options=new[]{"Görmek için","Sudaki elektrik alanlarını algılamak için","Solunum için","Üremek için"}, correctIndex=1 },
                new QuizQuestion { question="Büyük beyaz köpekbalığı ne yer?", options=new[]{"Yalnız bitkiler","Balık, vatozlar, deniz memelileri","Yalnız kril","Su yosunu"}, correctIndex=1 },
                new QuizQuestion { question="Yaşamı boyunca kaç diş değiştirir?", options=new[]{"10","100","1.000","20.000"}, correctIndex=3 },
                new QuizQuestion { question="Büyük beyaz köpekbalığı hangi familyadadır?", options=new[]{"Carcharhinidae","Sphyrnidae","Lamnidae","Alopiidae"}, correctIndex=2 },
                new QuizQuestion { question="Hangi sularda yaşar?", options=new[]{"Buzul suları","Ilıman kıyı suları","Tropikal sıcak sular","Tatlı su"}, correctIndex=1 },
                new QuizQuestion { question="Büyük beyaz köpekbalığı nasıl avlanır?", options=new[]{"Hız ve sürpriz saldırı","Ağ kurar","Zehir kullanır","Bekler ve uyur"}, correctIndex=0 },
                new QuizQuestion { question="Büyük beyaz köpekbalığı endanjere (nesli tükenmekte) midir?", options=new[]{"Hayır, bolca var","Evet, tehlike altında","Soyu tükenmiş","Bilmiyoruz"}, correctIndex=1 },
                new QuizQuestion { question="Köpekbalıkları ne tür canlılardır?", options=new[]{"Memeli","Sürüngen","Balık (kondrihtyen)","Amfibi"}, correctIndex=2 },
                new QuizQuestion { question="Büyük beyaz köpekbalığı neden tehlikeli kabul edilir?", options=new[]{"Zehirlidir","Güçlü çeneleri ve keskin dişleri","Elektrik verir","Saldırgan davranışı"}, correctIndex=1 },
                new QuizQuestion { question="Büyük beyaz köpekbalıklarında diş nasıl yenilenir?", options=new[]{"Hiç yenilenmez","Arka sıradan yenisi gelir","Dışarıdan büyür","Beslenmeyle gelişir"}, correctIndex=1 },
                new QuizQuestion { question="Köpekbalığının en belirgin özelliği nedir?", options=new[]{"Renkli olması","Sırt yüzgeci ve güçlü vücut","Işık saçması","Küçük gözleri"}, correctIndex=1 },
                new QuizQuestion { question="Büyük beyaz köpekbalığı ne kadar yaşar?", options=new[]{"5 yıl","10 yıl","70+ yıl","1 yıl"}, correctIndex=2 },
            }
        },
    };

    // ════════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════════════════════════════════
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Called by ARAquariumController to initialise the quiz canvas once.</summary>
    public void Initialise(Canvas parentCanvas, Font font, Sprite circle)
    {
        quizCanvas   = parentCanvas;
        uiFont       = font;
        circleSprite = circle;

        if (!uiBuilt)
        {
            BuildQuizUI();
            uiBuilt = true;
        }
    }

    /// <summary>Start the quiz for the given fish species.</summary>
    public void StartQuiz(string fishName)
    {
        if (!QuestionBank.ContainsKey(fishName))
        {
            Debug.LogWarning($"[FishQuizSystem] No questions for fish: {fishName}");
            return;
        }

        currentFishName = fishName;
        PrepareQuestions(fishName);
        currentIndex  = 0;
        correctCount  = 0;
        wrongCount    = 0;
        waitingForNext = false;

        quizPanel.SetActive(true);
        resultPanel.SetActive(false);

        string prettyName = GetPrettyName(fishName);
        quizHeaderText.text = "🎯  " + prettyName + "  Bilgi Testi";

        ShowQuestion(currentIndex);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  QUESTION LOGIC
    // ════════════════════════════════════════════════════════════════════════
    private void PrepareQuestions(string fishName)
    {
        QuizQuestion[] pool = QuestionBank[fishName];
        List<QuizQuestion> shuffled = new List<QuizQuestion>(pool);
        // Fisher-Yates shuffle
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            QuizQuestion tmp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = tmp;
        }
        currentQuestions = shuffled.GetRange(0, Mathf.Min(10, shuffled.Count));
    }

    private void ShowQuestion(int index)
    {
        if (index >= currentQuestions.Count) { ShowResult(); return; }

        waitingForNext = false;
        QuizQuestion q = currentQuestions[index];

        progressText.text  = $"Soru  {index + 1}  /  {currentQuestions.Count}";
        questionText.text  = q.question;

        for (int i = 0; i < 4; i++)
        {
            answerTexts[i].text    = q.options[i];
            answerImages[i].color  = ColDefault;
            answerButtons[i].interactable = true;
        }
    }

    private void OnAnswerSelected(int chosenIndex)
    {
        if (waitingForNext) return;
        waitingForNext = true;

        QuizQuestion q = currentQuestions[currentIndex];
        bool correct   = (chosenIndex == q.correctIndex);

        // Disable all buttons while showing feedback
        for (int i = 0; i < 4; i++) answerButtons[i].interactable = false;

        // Colour feedback
        answerImages[chosenIndex].color       = correct ? ColCorrect : ColWrong;
        answerImages[q.correctIndex].color    = ColCorrect;   // always highlight correct

        if (correct) correctCount++; else wrongCount++;

        StartCoroutine(NextQuestionDelay());
    }

    private IEnumerator NextQuestionDelay()
    {
        yield return new WaitForSeconds(1.5f);
        currentIndex++;
        ShowQuestion(currentIndex);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  RESULT SCREEN
    // ════════════════════════════════════════════════════════════════════════
    private void ShowResult()
    {
        quizPanel.SetActive(false);
        resultPanel.SetActive(true);

        int total = currentQuestions.Count;
        float pct = (float)correctCount / total;

        string stars;
        string title;
        if      (pct == 1f)      { stars = "⭐⭐⭐";  title = "Mükemmel! Tebrikler!"; }
        else if (pct >= 0.70f)   { stars = "⭐⭐";    title = "Çok İyi!"; }
        else if (pct >= 0.50f)   { stars = "⭐";      title = "Fena Değil!"; }
        else                     { stars = "😢";       title = "Biraz Daha Çalış!"; }

        resultTitleText.text = title;
        resultScoreText.text =
            $"✅  Doğru:   {correctCount}\n" +
            $"❌  Yanlış:  {wrongCount}\n" +
            $"📊  Toplam: {total}";
        resultStarsText.text = stars;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  UI CONSTRUCTION
    // ════════════════════════════════════════════════════════════════════════
    private void BuildQuizUI()
    {
        BuildMainQuizPanel();
        BuildResultPanel();
        quizPanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    private void BuildMainQuizPanel()
    {
        // Full-screen overlay
        quizPanel = CreatePanel("QuizPanel", quizCanvas.transform, new Color(0.05f, 0.10f, 0.18f, 0.97f));
        SetRect(quizPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        // ── Header bar ──────────────────────────────────────────────────
        Image headerBg = CreateImage("QuizHeader", quizPanel.transform, new Color(0.08f, 0.25f, 0.55f, 1f));
        SetRect(headerBg.rectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 1f));

        quizHeaderText = CreateText("QuizHeaderText", headerBg.transform, "🎯 Bilgi Testi", 36, TextAnchor.MiddleCenter);
        SetRect(quizHeaderText.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
        quizHeaderText.fontStyle = FontStyle.Bold;
        quizHeaderText.color = Color.white;

        // ── Progress ────────────────────────────────────────────────────
        progressText = CreateText("ProgressText", quizPanel.transform, "Soru 1 / 10", 28, TextAnchor.MiddleCenter);
        SetRect(progressText.rectTransform, new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.88f));
        progressText.color = new Color(0.65f, 0.80f, 1f, 1f);

        // ── Question card ────────────────────────────────────────────────
        Image qCard = CreateImage("QuestionCard", quizPanel.transform, new Color(0.10f, 0.18f, 0.32f, 1f));
        SetRect(qCard.rectTransform, new Vector2(0.04f, 0.60f), new Vector2(0.96f, 0.82f));
        Outline qOutline = qCard.gameObject.AddComponent<Outline>();
        qOutline.effectColor    = new Color(0.30f, 0.55f, 1.00f, 0.40f);
        qOutline.effectDistance = new Vector2(3f, -3f);

        questionText = CreateText("QuestionText", qCard.transform, "", 30, TextAnchor.MiddleCenter);
        SetRect(questionText.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
        questionText.color = Color.white;

        // ── Answer buttons (2×2 grid) ────────────────────────────────────
        // Row 1
        BuildAnswerButton(0, new Vector2(0.04f, 0.41f), new Vector2(0.48f, 0.57f));
        BuildAnswerButton(1, new Vector2(0.52f, 0.41f), new Vector2(0.96f, 0.57f));
        // Row 2
        BuildAnswerButton(2, new Vector2(0.04f, 0.22f), new Vector2(0.48f, 0.38f));
        BuildAnswerButton(3, new Vector2(0.52f, 0.22f), new Vector2(0.96f, 0.38f));

        // ── Bottom close button ──────────────────────────────────────────
        Button closeBtn = CreateButton("QuizCloseBtn", quizPanel.transform, "✕  Testi Kapat", new Color(0.55f, 0.15f, 0.15f, 1f));
        SetRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0.20f, 0.07f), new Vector2(0.80f, 0.17f));
        closeBtn.onClick.AddListener(() => quizPanel.SetActive(false));
    }

    private void BuildAnswerButton(int idx, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject bg = CreatePanel($"AnswerBg_{idx}", quizPanel.transform, ColDefault);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin  = anchorMin;
        bgRT.anchorMax  = anchorMax;
        bgRT.offsetMin  = Vector2.zero;
        bgRT.offsetMax  = Vector2.zero;

        Outline outline = bg.AddComponent<Outline>();
        outline.effectColor    = new Color(1f, 1f, 1f, 0.15f);
        outline.effectDistance = new Vector2(2f, -2f);

        answerImages[idx] = bg.GetComponent<Image>();

        Button btn = bg.AddComponent<Button>();
        ColorBlock cb  = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        cb.pressedColor     = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors          = cb;

        int captured = idx;
        btn.onClick.AddListener(() => OnAnswerSelected(captured));
        answerButtons[idx] = btn;

        Text txt = CreateText($"AnswerText_{idx}", bg.transform, "", 26, TextAnchor.MiddleCenter);
        SetRect(txt.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
        txt.color     = Color.white;
        txt.fontStyle = FontStyle.Bold;
        answerTexts[idx] = txt;
    }

    private void BuildResultPanel()
    {
        resultPanel = CreatePanel("QuizResultPanel", quizCanvas.transform, new Color(0.04f, 0.08f, 0.15f, 0.98f));
        SetRect(resultPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        // Card
        Image card = CreateImage("ResultCard", resultPanel.transform, new Color(0.08f, 0.18f, 0.32f, 1f));
        SetRect(card.rectTransform, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.82f));
        Outline cardOut = card.gameObject.AddComponent<Outline>();
        cardOut.effectColor    = new Color(0.25f, 0.65f, 1f, 0.5f);
        cardOut.effectDistance = new Vector2(4f, -4f);

        // Title
        resultTitleText = CreateText("ResultTitle", card.transform, "Tebrikler!", 44, TextAnchor.UpperCenter);
        SetRect(resultTitleText.rectTransform, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.95f));
        resultTitleText.fontStyle = FontStyle.Bold;
        resultTitleText.color     = new Color(0.20f, 0.90f, 0.55f, 1f);

        // Stars
        resultStarsText = CreateText("ResultStars", card.transform, "⭐⭐⭐", 52, TextAnchor.MiddleCenter);
        SetRect(resultStarsText.rectTransform, new Vector2(0.05f, 0.57f), new Vector2(0.95f, 0.74f));

        // Score breakdown
        resultScoreText = CreateText("ResultScore", card.transform, "", 34, TextAnchor.MiddleLeft);
        SetRect(resultScoreText.rectTransform, new Vector2(0.10f, 0.25f), new Vector2(0.90f, 0.57f));
        resultScoreText.color = new Color(0.88f, 0.92f, 1f, 1f);
        resultScoreText.lineSpacing = 1.3f;

        // Restart button
        resultRestartBtn = CreateButton("ResultRestart", card.transform, "🔄  Tekrar Oyna", new Color(0.10f, 0.50f, 0.28f, 1f));
        SetRect(resultRestartBtn.GetComponent<RectTransform>(), new Vector2(0.05f, 0.05f), new Vector2(0.50f, 0.20f));
        resultRestartBtn.onClick.AddListener(() => StartQuiz(currentFishName));

        // Close button
        resultCloseBtn = CreateButton("ResultClose", card.transform, "✕  Kapat", new Color(0.55f, 0.15f, 0.15f, 1f));
        SetRect(resultCloseBtn.GetComponent<RectTransform>(), new Vector2(0.52f, 0.05f), new Vector2(0.95f, 0.20f));
        resultCloseBtn.onClick.AddListener(() => resultPanel.SetActive(false));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════════
    private string GetPrettyName(string fishName)
    {
        switch (fishName)
        {
            case "marine_clownfish":        return "Palyaço Balığı";
            case "freshWater_guppy":        return "Lepistes";
            case "EmperorAngelfish_swim1":  return "İmparator Melek Balığı";
            case "Surgeonfish":             return "Cerrah Balığı";
            case "Clownfish 1":             return "Palyaço Balığı (Ocellaris)";
            case "Whale":                   return "Kambur Balina";
            case "Shark":                   return "Büyük Beyaz Köpekbalığı";
            default:                        return fishName;
        }
    }

    private GameObject CreatePanel(string objName, Transform parent, Color color)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private Image CreateImage(string objName, Transform parent, Color color)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private Text CreateText(string objName, Transform parent, string value, int fontSize, TextAnchor anchor)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font               = uiFont;
        t.text               = value;
        t.fontSize           = fontSize;
        t.alignment          = anchor;
        t.color              = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        return t;
    }

    private Button CreateButton(string objName, Transform parent, string label, Color color)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.color  = color;
        if (circleSprite != null) { img.sprite = circleSprite; img.type = Image.Type.Simple; }

        Button btn = go.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = color;
        cb.highlightedColor = color * 1.15f;
        cb.pressedColor     = color * 0.80f;
        btn.colors          = cb;

        Text txt = CreateText("Label", go.transform, label, 26, TextAnchor.MiddleCenter);
        SetRect(txt.rectTransform, Vector2.zero, Vector2.one);
        txt.fontStyle = FontStyle.Bold;
        return btn;
    }

    private void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }
}
