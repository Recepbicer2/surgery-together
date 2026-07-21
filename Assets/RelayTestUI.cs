using UnityEngine;

public class RelayTestUI : MonoBehaviour
{
    private string inputJoinCode = "";
    private string currentJoinCode = "";
    private bool isConnected = false;
    private bool isConnecting = false; // İşlem sürerken çift tıklamayı önler

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 350, 300));

        if (!isConnected)
        {
            if (isConnecting)
            {
                GUILayout.Label("Bağlanıyor, lütfen bekleyin...");
            }
            else
            {
                // ODA OLUŞTURMA (HOST)
                if (GUILayout.Button("Host Ol (Oda Kur)", GUILayout.Height(40)))
                {
                    StartHostProcess();
                }

                GUILayout.Space(20);

                // ODAYA KATILMA (CLIENT)
                GUILayout.Label("Oda Kodu Girin:");
                inputJoinCode = GUILayout.TextField(inputJoinCode, GUILayout.Height(30));

                if (GUILayout.Button("Odaya Katıl (Client)", GUILayout.Height(40)))
                {
                    if (!string.IsNullOrEmpty(inputJoinCode))
                    {
                        StartJoinProcess(inputJoinCode.Trim().ToUpper());
                    }
                }
            }
        }
        else
        {
            GUILayout.Label("--- BAĞLANTI BAŞARILI ---");
            if (!string.IsNullOrEmpty(currentJoinCode))
            {
                GUILayout.Label($"Arkadaşına Göndereceğin Kod: {currentJoinCode}", GUI.skin.box);
            }
        }

        GUILayout.EndArea();
    }

    private async void StartHostProcess()
    {
        isConnecting = true;
        currentJoinCode = await RelayManager.Instance.CreateRelay(4);

        if (!string.IsNullOrEmpty(currentJoinCode))
        {
            isConnected = true;
        }
        isConnecting = false;
    }

    private async void StartJoinProcess(string code)
    {
        isConnecting = true;
        bool success = await RelayManager.Instance.JoinRelay(code);

        if (success)
        {
            isConnected = true;
        }
        isConnecting = false;
    }
}