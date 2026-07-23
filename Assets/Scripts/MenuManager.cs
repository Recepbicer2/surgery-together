using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Menü Panelleri")]
    public GameObject settingsPanel; // Ayarlar panelini Unity'den buraya sürükleyeceğiz

    // "Oyna" butonuna basınca çalışacak fonksiyon
    public void PlayGame()
    {
        // Arkadaşının yaptığı lobi/oyun sahnesine (SampleScene) geçiş yapar.
        // Build Index'ini birazdan 1 olarak ayarlayacağız.
        SceneManager.LoadScene(1);
    }

    // "Çıkış" butonuna basınca çalışacak fonksiyon
    public void QuitGame()
    {
        Debug.Log("Oyundan Çıkış Yapıldı!");
        Application.Quit(); // Bu gerçek oyunda çalışır.

        // Bu kod ise sadece Unity Editöründeyken "Play" modunu durdurur.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Ayarlar menüsünü açar
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    // Ayarlar menüsünü kapatır
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
}