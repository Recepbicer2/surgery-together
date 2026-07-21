using UnityEngine;

public class RelayTestUI : MonoBehaviour
{
    private string inputJoinCode = "";
    private string currentJoinCode = "";
    private bool isConnected = false;

    private async void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 350, 300));

        if (!isConnected)
        {
            // ODA OLUŞTURMA (HOST)
            if (GUILayout.Button("Host Ol (Oda Kur)", GUILayout.Height(40)))
            {
                currentJoinCode = await RelayManager.Instance.CreateRelay(4);
                if (!string.IsNullOrEmpty(currentJoinCode))
                {
                    isConnected = true;
                }
            }

            GUILayout.Space(20);

            // ODAYA KATILMA (CLIENT)
            GUILayout.Label("Oda Kodu Girin:");
            inputJoinCode = GUILayout.TextField(inputJoinCode, GUILayout.Height(30));

            if (GUILayout.Button("Odaya Katıl (Client)", GUILayout.Height(40)))
            {
                if (!string.IsNullOrEmpty(inputJoinCode))
                {
                    bool success = await RelayManager.Instance.JoinRelay(inputJoinCode.Trim());
                    if (success)
                    {
                        isConnected = true;
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
}