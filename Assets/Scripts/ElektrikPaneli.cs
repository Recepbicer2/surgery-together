using UnityEngine;
using Unity.Netcode;

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

    [Header("Etkileşim Ayarları")]
    [Tooltip("Işıkları açıp kapatmak için dibine kadar girilmesi gereken mesafe")]
    public float etkilesimMesafesi = 0.2f; // Mesafeyi 1.2 metreye düşürdük

    private bool elektrikVarMi = false;

    void Start()
    {
        IsiklariAyarla(kapaliSiddet);

        if (salterAnimator != null)
        {
            salterAnimator.SetFloat("Hiz", 1f);
            salterAnimator.Play(animasyonAdi, 0, 0f);
            salterAnimator.speed = 0f;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.LocalClient.PlayerObject == null) return;

            Vector3 playerPos = NetworkManager.Singleton.LocalClient.PlayerObject.transform.position;

            // Eğer Animator atanmışsa onun pozisyonunu, yoksa scriptin olduğu pozisyonu baz al
            Vector3 salterPos = salterAnimator != null ? salterAnimator.transform.position : transform.position;

            // Yüksekliği (Y eksenini) yok sayarak sadece yatay mesafe ölçümü yapalım (daha hassas etkileşim için)
            playerPos.y = 0;
            salterPos.y = 0;

            float mesafe = Vector3.Distance(salterPos, playerPos);

            if (mesafe <= etkilesimMesafesi)
            {
                elektrikVarMi = !elektrikVarMi;
                SistemiTetikle();
            }
        }
    }

    void SistemiTetikle()
    {
        if (salterAnimator == null) return;

        salterAnimator.speed = 1f;

        if (elektrikVarMi)
        {
            IsiklariAyarla(acikSiddet);
            salterAnimator.SetFloat("Hiz", 1f);
            salterAnimator.Play(animasyonAdi, 0, 0f);
        }
        else
        {
            IsiklariAyarla(kapaliSiddet);
            salterAnimator.SetFloat("Hiz", -1f);
            salterAnimator.Play(animasyonAdi, 0, 1f);
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

    // Editörde etkileşim alanını kırmızı bir küre olarak görmek için
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 merkez = salterAnimator != null ? salterAnimator.transform.position : transform.position;
        Gizmos.DrawWireSphere(merkez, etkilesimMesafesi);
    }
}