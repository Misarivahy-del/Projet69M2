using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SnapZone : MonoBehaviour
{
    public string snapType;
    [HideInInspector] public bool isOccupied = false;

    public void Snap(Snappable piece, SnapPoint point)
    {
        if (isOccupied) return;

        StartCoroutine(SmoothSnap(piece, point));
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

    private IEnumerator SmoothSnap(Snappable piece, SnapPoint point)
    {
        Transform pieceTransform = piece.transform;
        Transform pointTransform = point.transform;
        Transform zoneTransform = transform;

        Rigidbody rb = piece.GetComponent<Rigidbody>();
        var grab = piece.GetComponent<XRGrabInteractable>();

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

        pieceTransform.SetParent(zoneTransform);
        isOccupied = true;

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        var grabComponent = piece.GetComponent<XRGrabInteractable>();
        

        Collider childCollider = piece.GetComponent<Collider>();
        Collider[] parentColliders = zoneTransform.GetComponentsInParent<Collider>();
        foreach (var parentCol in parentColliders)
            Physics.IgnoreCollision(childCollider, parentCol, true);


        var parentGrabComponent = GetRootGrabInteractable(zoneTransform);

        if (grabComponent != null)
            grabComponent.enabled = false;

        if (parentGrabComponent != null)
        {
            Collider childCollider2 = piece.GetComponent<Collider>();

            if (childCollider2 != null && !parentGrabComponent.colliders.Contains(childCollider2))
            {
                parentGrabComponent.colliders.Add(childCollider2);

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
}