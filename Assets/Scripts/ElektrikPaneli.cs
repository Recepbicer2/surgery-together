using UnityEngine;

public class ElektrikPaneli : MonoBehaviour
{
    [Header("Işık Ayarları")]
    [Tooltip("9 adet Spot Light'ı buraya sürükleyin")]
    public Light[] tavanIsiklari;
    public float kapaliSiddet = 0.5f;
    public float acikSiddet = 12f;

    [Header("Animasyon Ayarları")]
    [Tooltip("Şalterin üzerindeki Animator bileşenini buraya sürükleyin")]
    public Animator salterAnimator;
    [Tooltip("Animator penceresindeki Animation State'in tam adı (Örn: 'Salter_Animasyonu')")]
    public string animasyonAdi = "Salter_Animasyonu";

    private bool elektrikVarMi = false;

    void Start()
    {
        // Başlangıçta ışıklar loş ve şalter kapalı/başta duruyor
        IsiklariAyarla(kapaliSiddet);

        if (salterAnimator != null)
        {
            salterAnimator.Play(animasyonAdi, 0, 0f);
            salterAnimator.speed = 0f; // Başlangıçta animasyon durdurulmuş olsun
        }
    }

    void Update()
    {
        // E tuşuna basıldığında tetiklenir
        if (Input.GetKeyDown(KeyCode.E))
        {
            elektrikVarMi = !elektrikVarMi; // Durumu değiştir
            SistemiTetikle();
        }
    }

    void SistemiTetikle()
    {
        if (salterAnimator == null) return;

        if (elektrikVarMi)
        {
            // Elektrik geldi -> Işıkları aç, animasyonu normal yönde (ileri) oynat
            IsiklariAyarla(acikSiddet);
            salterAnimator.speed = 1f;
        }
        else
        {
            // Elektrik gitti -> Işıkları kıs, animasyonu tersten oynat
            IsiklariAyarla(kapaliSiddet);
            salterAnimator.speed = -1f;
        }
    }

    void IsiklariAyarla(float hedefSiddet)
    {
        foreach (Light isik in tavanIsiklari)
        {
            if (isik != null)
            {
                isik.intensity = hedefSiddet;
            }
        }
    }
}