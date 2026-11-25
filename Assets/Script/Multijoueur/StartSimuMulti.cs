using Unity.Netcode;
using UnityEngine;

public class SoloSessionStarter : MonoBehaviour
{
    [SerializeField] private bool isSoloMode = true;

    private void Start()
    {
        // Lance automatiquement le mode Host en solo pour activer les NetworkBehaviour
        if (isSoloMode && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
        }
    }
}
