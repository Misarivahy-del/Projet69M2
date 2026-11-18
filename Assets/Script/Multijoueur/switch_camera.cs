using UnityEngine;
using Unity.Netcode;

public class SpectatorSwitcher : NetworkBehaviour
{
    [SerializeField] private Transform[] cameraPoints; // À remplir dans l'inspecteur
    private int currentPointIndex = 0;

    public override void OnNetworkSpawn()
    {
        // On désactive la caméra si ce n'est pas nous (pour ne pas interférer avec la VR)
        if (!IsOwner)
        {
            GetComponentInChildren<Camera>().enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // Appuie sur Espace ou Flèches pour changer de vue
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentPointIndex = (currentPointIndex + 1) % cameraPoints.Length;
            UpdatePosition();
        }
    }

    void UpdatePosition()
    {
        transform.position = cameraPoints[currentPointIndex].position;
        transform.rotation = cameraPoints[currentPointIndex].rotation;
    }
}