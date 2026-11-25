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

    // Synchronisé automatiquement, même pour les joueurs qui rejoignent en retard
    private NetworkVariable<float> netScaleMultiplier = new NetworkVariable<float>(1f);
    private NetworkVariable<bool> netIsFrozen = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> netIsGrabbedOnce = new NetworkVariable<bool>(false);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        originalScale = transform.localScale;
        if (rb != null)
            originalConstraints = rb.constraints;
    }

    public override void OnNetworkSpawn()
    {
        netScaleMultiplier.OnValueChanged += (_, _) => ApplyVisualAndPhysicsState();
        netIsFrozen.OnValueChanged += (_, _) => ApplyVisualAndPhysicsState();
        netIsGrabbedOnce.OnValueChanged += (_, _) => ApplyVisualAndPhysicsState();

        ApplyVisualAndPhysicsState();

        grabInteractable.selectEntered.AddListener(OnLocalGrabbed);
    }

    public override void OnNetworkDespawn()
    {
        grabInteractable.selectEntered.RemoveListener(OnLocalGrabbed);
    }

    // Appelé côté serveur par NetworkInteractableSpawner juste après netObj.Spawn()
    public void ConfigureOnServer(float scaleMultiplier, bool freeze)
    {
        if (!IsServer) return;

        netScaleMultiplier.Value = scaleMultiplier;
        netIsFrozen.Value = freeze;
        netIsGrabbedOnce.Value = false;
        ApplyVisualAndPhysicsState();
    }

    private void ApplyVisualAndPhysicsState()
    {
        if (netIsGrabbedOnce.Value)
        {
            transform.localScale = originalScale;
            if (rb != null)
            {
                rb.constraints = originalConstraints;
                rb.isKinematic = false;
            }
        }
        else
        {
            transform.localScale = originalScale * netScaleMultiplier.Value;
            if (rb != null && netIsFrozen.Value)
            {
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }
    }

    private void OnLocalGrabbed(SelectEnterEventArgs args)
    {
        if (!IsSpawned || netIsGrabbedOnce.Value) return;

        RequestGrabServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server)]
    private void RequestGrabServerRpc(ulong clientId)
    {
        if (netIsGrabbedOnce.Value) return;

        if (NetworkObject.OwnerClientId != clientId)
        {
            NetworkObject.ChangeOwnership(clientId);
        }

        netIsGrabbedOnce.Value = true;
        netIsFrozen.Value = false;
        netScaleMultiplier.Value = 1f;
    }
}
