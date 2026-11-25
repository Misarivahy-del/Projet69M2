using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(NetworkObject))]
public class Snappable : NetworkBehaviour
{
    [Header("Snap Settings")]
    public float snapDistance = 0.35f;
    public float snapAlignment = 0.65f;

    [Header("Smooth Snap")]
    public float snapDuration = 0.25f;
    public AnimationCurve snapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [HideInInspector] public SnapPoint[] snapPoints;
    private SnapZone currentSnapZone;
    private XRGrabInteractable grabInteractable;
    private Renderer rend;
    private Color originalColor;

    private bool isSnapping = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rend = GetComponent<Renderer>();

        if (rend != null) originalColor = rend.material.color;
        snapPoints = GetComponentsInChildren<SnapPoint>();

        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void Update()
    {
        // La détection visuelle reste 100 % locale pour le joueur qui tient l'objet
        if (!grabInteractable.isSelected || isSnapping)
            return;

        UpdateVisualFeedback();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out SnapZone zone))
            currentSnapZone = zone;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out SnapZone zone) && currentSnapZone == zone)
        {
            currentSnapZone = null;
            ResetVisual();
        }
    }

    private bool TryGetValidSnapPoint(out int validPointIndex)
    {
        validPointIndex = -1;

        if (currentSnapZone == null || currentSnapZone.isOccupied)
            return false;

        for (int i = 0; i < snapPoints.Length; i++)
        {
            SnapPoint point = snapPoints[i];
            if (point.snapType != currentSnapZone.snapType)
                continue;

            float distance = Vector3.Distance(point.transform.position, currentSnapZone.transform.position);
            float alignment = Vector3.Dot(point.transform.forward, -currentSnapZone.transform.forward);

            if (distance < snapDistance && alignment > snapAlignment)
            {
                validPointIndex = i;
                return true;
            }
        }

        return false;
    }

    private void UpdateVisualFeedback()
    {
        if (rend == null) return;
        rend.material.color = TryGetValidSnapPoint(out _) ? Color.green : originalColor;
    }

    private void ResetVisual()
    {
        if (rend != null) rend.material.color = originalColor;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        ResetVisual();

        // Seul le propriétaire actuel de la pièce déclenche la demande d'assemblage
        if (!IsSpawned || !IsOwner) return;

        if (TryGetValidSnapPoint(out int validPointIndex))
        {
            isSnapping = true;
            currentSnapZone.RequestSnapServerRpc(NetworkObjectId, validPointIndex);
        }
    }
}
