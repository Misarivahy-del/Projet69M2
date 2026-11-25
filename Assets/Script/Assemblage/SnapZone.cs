using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRMultiplayer;

public class SnapZone : NetworkBehaviour
{
    public string snapType;
    [HideInInspector] public bool isOccupied = false;

    [Rpc(SendTo.Server)]
    public void RequestSnapServerRpc(ulong pieceNetworkObjectId, int pointIndex)
    {
        if (isOccupied) return;

        isOccupied = true;
        ExecuteSnapClientsRpc(pieceNetworkObjectId, pointIndex);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteSnapClientsRpc(ulong pieceNetworkObjectId, int pointIndex)
    {
        isOccupied = true;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetworkObjectId, out NetworkObject pieceNetObj))
            return;

        Snappable piece = pieceNetObj.GetComponent<Snappable>();
        if (piece == null || pointIndex < 0 || pointIndex >= piece.snapPoints.Length)
            return;

        StartCoroutine(SmoothSnap(piece, piece.snapPoints[pointIndex]));
    }

    private IEnumerator SmoothSnap(Snappable piece, SnapPoint point)
    {
        Transform pieceTransform = piece.transform;
        Transform pointTransform = point.transform;
        Transform zoneTransform = transform;

        // 1. Désactive les composants réseau d'interaction et de transform de la pièce
        if (piece.TryGetComponent(out NetworkBaseInteractable netInteractable))
            netInteractable.enabled = false;

        if (piece.TryGetComponent(out NetworkTransform netTransform))
            netTransform.enabled = false;

        var grabComponent = piece.GetComponent<XRGrabInteractable>();
        if (grabComponent != null)
            grabComponent.enabled = false;

        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Vector3 startPos = pieceTransform.position;
        Quaternion startRot = pieceTransform.rotation;

        float duration = 0.25f;
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            Quaternion targetRotation = Quaternion.LookRotation(-zoneTransform.forward, zoneTransform.up)
                                        * Quaternion.Inverse(pointTransform.rotation)
                                        * startRot;

            Vector3 pointOffsetLocal = pointTransform.position - pieceTransform.position;
            Vector3 rotatedOffset = targetRotation * Quaternion.Inverse(pieceTransform.rotation) * pointOffsetLocal;
            Vector3 currentTargetPos = zoneTransform.position - rotatedOffset;

            pieceTransform.position = Vector3.Lerp(startPos, currentTargetPos, eased);
            pieceTransform.rotation = Quaternion.Slerp(startRot, targetRotation, eased);

            time += Time.deltaTime;
            yield return null;
        }

        Quaternion finalRotation = Quaternion.LookRotation(-zoneTransform.forward, zoneTransform.up)
                                    * Quaternion.Inverse(pointTransform.rotation)
                                    * pieceTransform.rotation;

        Vector3 finalPointOffset = pointTransform.position - pieceTransform.position;
        Vector3 finalRotatedOffset = finalRotation * Quaternion.Inverse(pieceTransform.rotation) * finalPointOffset;
        pieceTransform.position = zoneTransform.position - finalRotatedOffset;
        pieceTransform.rotation = finalRotation;

        // 2. Verrouille la physique
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // 3. Parentage réseau vers le NetworkObject racine spawné du support
        if (IsServer)
        {
            NetworkObject targetParentNetObj = GetValidParentNetworkObject(zoneTransform);
            if (targetParentNetObj != null)
            {
                piece.NetworkObject.AutoObjectParentSync = true;
                bool success = piece.NetworkObject.TrySetParent(targetParentNetObj, worldPositionStays: true);

                if (!success)
                {
                    Debug.LogError($"[SnapZone] Échec de TrySetParent de '{piece.name}' vers '{targetParentNetObj.name}' (PieceSpawned={piece.NetworkObject.IsSpawned}, ParentSpawned={targetParentNetObj.IsSpawned}).");
                }
            }
            else
            {
                Debug.LogError($"[SnapZone] Aucun NetworkObject spawné trouvé sur '{zoneTransform.name}' ou ses parents : ajoute un NetworkObject actif à la racine du support !");
            }
        }

        Collider childCollider = piece.GetComponent<Collider>();
        Collider[] parentColliders = zoneTransform.GetComponentsInParent<Collider>();
        foreach (var parentCol in parentColliders)
        {
            if (childCollider != null)
                Physics.IgnoreCollision(childCollider, parentCol, true);
        }

        var parentGrabComponent = GetRootGrabInteractable(zoneTransform);
        if (parentGrabComponent != null && childCollider != null)
        {
            if (!parentGrabComponent.colliders.Contains(childCollider))
            {
                parentGrabComponent.colliders.Add(childCollider);

                if (!parentGrabComponent.isSelected)
                {
                    parentGrabComponent.enabled = false;
                    parentGrabComponent.enabled = true;
                    RecalculateParentCollider(parentGrabComponent);
                }
                else
                {
                    parentGrabComponent.selectExited.AddListener(RefreshOnRelease);
                }
            }
        }

        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null) zoneCollider.enabled = false;
    }

    private NetworkObject GetValidParentNetworkObject(Transform startTransform)
    {
        Transform current = startTransform;
        while (current != null)
        {
            if (current.TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
            {
                return netObj;
            }
            current = current.parent;
        }
        return null;
    }

    private void RecalculateParentCollider(XRGrabInteractable parentGrab)
    {
        BoxCollider parentBox = parentGrab.GetComponent<BoxCollider>();
        if (parentBox == null) return;

        Collider[] childColliders = parentGrab.GetComponentsInChildren<Collider>();
        if (childColliders.Length == 0) return;

        Bounds combinedBounds = new Bounds(parentBox.transform.InverseTransformPoint(childColliders[0].bounds.center), Vector3.zero);

        foreach (var col in childColliders)
        {
            if (col.gameObject == parentGrab.gameObject) continue;

            Vector3 localCenter = parentBox.transform.InverseTransformPoint(col.bounds.center);
            Vector3 localSize = col.bounds.size;
            combinedBounds.Encapsulate(new Bounds(localCenter, localSize));
        }

        parentBox.center = combinedBounds.center;
        parentBox.size = combinedBounds.size;
    }

    private void RefreshOnRelease(SelectExitEventArgs args)
    {
        var grab = args.interactableObject as XRGrabInteractable;
        if (grab == null) return;

        grab.enabled = false;
        grab.enabled = true;
        grab.selectExited.RemoveListener(RefreshOnRelease);
    }

    private XRGrabInteractable GetRootGrabInteractable(Transform t)
    {
        XRGrabInteractable root = null;
        Transform current = t;

        while (current != null)
        {
            var grab = current.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.enabled)
                root = grab;
            current = current.parent;
        }

        return root;
    }
}
