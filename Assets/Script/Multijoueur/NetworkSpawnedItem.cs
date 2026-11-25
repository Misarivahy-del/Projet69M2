using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(NetworkObject), typeof(XRGrabInteractable))]
public class NetworkSpawnedItem : NetworkBehaviour
{
    private Vector3 originalScale;
    private RigidbodyConstraints originalConstraints;
    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private bool isGrabbedOnce = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Sauvegarde l'échelle et les contraintes d'origine du prefab
        originalScale = transform.localScale;
        if (rb != null)
        {
            originalConstraints = rb.constraints;
        }
    }

    public override void OnNetworkSpawn()
    {
        grabInteractable.selectEntered.AddListener(OnLocalGrabbed);
    }

    public override void OnNetworkDespawn()
    {
        grabInteractable.selectEntered.RemoveListener(OnLocalGrabbed);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void InitializeSpawnStateRpc(float scaleMultiplier, bool freeze)
    {
        transform.localScale = originalScale * scaleMultiplier;

        if (freeze && rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void OnLocalGrabbed(SelectEnterEventArgs args)
    {
        if (!IsSpawned || isGrabbedOnce) return;

        RequestGrabServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server)]
    private void RequestGrabServerRpc(ulong clientId)
    {
        if (isGrabbedOnce) return;

        // Transfère la propriété au joueur qui saisit l'objet
        if (NetworkObject.OwnerClientId != clientId)
        {
            NetworkObject.ChangeOwnership(clientId);
        }

        RestoreObjectStateRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RestoreObjectStateRpc()
    {
        isGrabbedOnce = true;
        transform.localScale = originalScale;

        if (rb != null)
        {
            rb.constraints = originalConstraints;
        }
    }
}
