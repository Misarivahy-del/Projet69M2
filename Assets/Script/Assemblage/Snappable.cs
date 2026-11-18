using System.Collections;
using System.Drawing;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


public class Snappable : MonoBehaviour
{
    [Header("Snap Settings")]
    public float snapDistance = 0.35f;
    public float snapAlignment = 0.65f;

    [Header("Smooth Snap")]
    public float snapDuration = 0.25f;
    public AnimationCurve snapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private SnapPoint[] snapPoints;
    private SnapZone currentSnapZone;
    private XRGrabInteractable grabInteractable;
    private Renderer rend;
    private UnityEngine.Color originalColor;
    private Rigidbody rb;

    private bool isSnapping = false;


    #region Unity Lifecycle

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();

        originalColor = rend.material.color;
        snapPoints = GetComponentsInChildren<SnapPoint>();

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void Update()
    {
        if (!grabInteractable.isSelected || isSnapping)
            return;

        UpdateVisualFeedback();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out SnapZone zone))
        {
            currentSnapZone = zone;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out SnapZone zone) && currentSnapZone == zone)
        {
            currentSnapZone = null;
            ResetVisual();
        }
    }

    #endregion

    #region Core Logic

    private bool TryGetValidSnapPoint(out SnapPoint validPoint)
    {
        validPoint = null;

        if (currentSnapZone == null)
            return false;

        foreach (var point in snapPoints)
        {
            if (point.snapType != currentSnapZone.snapType)
                continue;

            float distance = Vector3.Distance(point.transform.position, currentSnapZone.transform.position);
            float alignment = Vector3.Dot(point.transform.forward, -currentSnapZone.transform.forward);

            bool isValid = distance < snapDistance && alignment > snapAlignment;

            if (isValid)
            {
                validPoint = point;

                return true;
            }
        }

        return false;
    }

    private void UpdateVisualFeedback()
    {
        if (TryGetValidSnapPoint(out _))
            rend.material.color = UnityEngine.Color.green;
        else
            ResetVisual();
    }

    private void ResetVisual()
    {
        rend.material.color = originalColor;
    }

    #endregion

    #region Interaction

    private void OnGrab(SelectEnterEventArgs args)
    {
       // rb.isKinematic = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        ResetVisual();

        if (!TryGetValidSnapPoint(out SnapPoint validPoint))
            return;

        currentSnapZone.Snap(this, validPoint);
    }

    #endregion

   



}
