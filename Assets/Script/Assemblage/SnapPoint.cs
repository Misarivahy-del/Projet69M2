using UnityEngine;

public class SnapPoint : MonoBehaviour
{
    public string snapType;
    [HideInInspector] public Snappable parentPiece;

    private void Awake()
    {
        if (parentPiece == null)
            parentPiece = GetComponentInParent<Snappable>();
    }
}