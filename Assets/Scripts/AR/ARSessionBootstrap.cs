using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FishMuseum.AR
{
    /// <summary>
    /// AR oturumunun durumunu izler ve kullanıcıya durum bildirimleri gönderir.
    /// XR Origin'in altında veya ayrı bir Managers GameObject'inde yer alır.
    /// </summary>
    [RequireComponent(typeof(ARSession))]
    public class ARSessionBootstrap : MonoBehaviour
    {
        public static event System.Action<ARSessionState> OnSessionStateChanged;

        private ARSession _arSession;

        private void Awake()
        {
            _arSession = GetComponent<ARSession>();
        }

        private void OnEnable()
        {
            ARSession.stateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ARSessionStateChangedEventArgs args)
        {
            OnSessionStateChanged?.Invoke(args.state);

            switch (args.state)
            {
                case ARSessionState.Unsupported:
                    Debug.LogError("[ARSessionBootstrap] Bu cihaz AR desteklemiyor.");
                    break;
                case ARSessionState.CheckingAvailability:
                    Debug.Log("[ARSessionBootstrap] AR kullanılabilirliği kontrol ediliyor...");
                    break;
                case ARSessionState.Ready:
                    Debug.Log("[ARSessionBootstrap] AR oturumu hazır.");
                    break;
                case ARSessionState.SessionTracking:
                    Debug.Log("[ARSessionBootstrap] AR takibi başladı.");
                    break;
            }
        }
    }
}
