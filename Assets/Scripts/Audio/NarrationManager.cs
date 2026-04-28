using UnityEngine;

namespace FishMuseum.Audio
{
    /// <summary>
    /// Balık sesli anlatımlarını oynatır, duraklatır ve durdurur.
    /// InfoPanelController tarafından kullanılır.
    /// Tek bir AudioSource üzerinde çalışır; aynı anda tek anlatım oynar.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class NarrationManager : MonoBehaviour
    {
        private AudioSource _audioSource;
        private AudioClip _currentClip;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
        }

        /// <summary>
        /// Verilen klibi baştan oynatır. Zaten aynı klip oynuyorsa yeniden başlatır.
        /// </summary>
        public void Play(AudioClip clip)
        {
            if (clip == null) return;

            _currentClip = clip;
            _audioSource.clip = clip;
            _audioSource.Play();
        }

        /// <summary>
        /// Mevcut anlatımı duraklatır. Resume() ile devam ettirilebilir.
        /// </summary>
        public void Pause()
        {
            if (_audioSource.isPlaying)
                _audioSource.Pause();
        }

        /// <summary>
        /// Duraklatılmış anlatımı devam ettirir.
        /// </summary>
        public void Resume()
        {
            if (!_audioSource.isPlaying && _currentClip != null)
                _audioSource.UnPause();
        }

        /// <summary>
        /// Anlatımı tamamen durdurur ve sıfırlar.
        /// Panel kapanırken çağrılır.
        /// </summary>
        public void Stop()
        {
            _audioSource.Stop();
            _currentClip = null;
        }

        public bool IsPlaying => _audioSource.isPlaying;

        /// <summary>
        /// Toggle: oynuyorsa durdur, duruyorsa başlat/devam ettir.
        /// </summary>
        public void TogglePlayPause(AudioClip clip)
        {
            if (_audioSource.isPlaying)
                Pause();
            else if (_currentClip == clip)
                Resume();
            else
                Play(clip);
        }
    }
}
