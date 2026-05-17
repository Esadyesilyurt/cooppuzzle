using UnityEngine;

namespace CoopPuzzle.Questions
{
    /// <summary>
    /// Bilge için tek ana referans belgesi. Tüm kapı/soruların bilgisi burada;
    /// oyuncu metin içinden ilgili kısmı kendisi bulur.
    /// </summary>
    [CreateAssetMenu(menuName = "CoopPuzzle/Sage/Master Document", fileName = "SageMasterDocument")]
    public sealed class SageMasterDocument : ScriptableObject
    {
        [SerializeField] private string title = "Bilge El Kitabı";

        [TextArea(8, 24)]
        [SerializeField] private string bodyText;

        [Tooltip("Doluysa bodyText yerine bu .txt dosyası kullanılır (uzun belgeler için).")]
        [SerializeField] private TextAsset externalTextFile;

        public string Title => string.IsNullOrWhiteSpace(title) ? "Bilgi Belgesi" : title;

        public string GetBody()
        {
            if (externalTextFile != null && !string.IsNullOrWhiteSpace(externalTextFile.text))
                return externalTextFile.text.Trim();

            return string.IsNullOrWhiteSpace(bodyText)
                ? "Belge metni henüz eklenmedi.\n\nProject penceresinde SageMasterDocument asset'ini düzenle\nveya bir .txt dosyası bağla (External Text File)."
                : bodyText.Trim();
        }

        public bool HasContent =>
            (externalTextFile != null && !string.IsNullOrWhiteSpace(externalTextFile.text))
            || !string.IsNullOrWhiteSpace(bodyText);
    }
}
