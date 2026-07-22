using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    // Otoriteyi Sunucudan alıp doğrudan karakterin sahibine (Client'a) veriyoruz!
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}