namespace FishMuseum.Fish
{
    /// <summary>
    /// Dokunulabilir herhangi bir sahne nesnesinin uygulaması gereken arayüz.
    /// FishController (balık gövdesi) ve FishPartController (parçalar) bu arayüzü uygular.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Nesneye tap geldiğinde çağrılır.
        /// </summary>
        void OnTapped();

        /// <summary>
        /// Bu nesnenin şu an etkileşime açık olup olmadığını döndürür.
        /// Örneğin FishPartController sadece inspection modunda aktiftir.
        /// </summary>
        bool IsInteractable { get; }
    }
}
