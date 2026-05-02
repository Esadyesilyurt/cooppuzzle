using UnityEngine;
using UnityEngine.UI; // UI kütüphanesinin ekli olduðundan emin ol

public class AudioUIController : MonoBehaviour
{
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    public Image targetImage;

    // Sesin baþlangýçta açýk olduðunu varsayýyoruz
    private bool isAudioOn = true;

    public void ToggleAudio()
    {
        // Durumu tersine çevir (Açýksa kapat, kapalýysa aç)
        isAudioOn = !isAudioOn;

        if (isAudioOn)
        {
            AudioListener.volume = 1f; // Oyunun sesini aç
            targetImage.sprite = soundOnSprite; // Görseli açýk ses görseli yap
        }
        else
        {
            AudioListener.volume = 0f; // Oyunun sesini kapat
            targetImage.sprite = soundOffSprite; // Görseli çarpýlý kapalý görsel yap
        }
    }
}