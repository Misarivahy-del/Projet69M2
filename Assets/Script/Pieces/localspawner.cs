using Unity.Netcode;
using UnityEngine;
using TMPro;

public class NetworkInteractableSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnTransform;
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private TMP_Text textUI;

    public bool freezeOnSpawn = true;
    public float distanceToSpawnNew = 0.15f;
    public float spawnCooldown = 0.5f;
    public float multiplierscale = 0.5f;
    public float objectlimit = 3;
    public string objectName;

    private float spawnCooldownTimer = 0f;
    private GameObject currentObject;
    private float objectcount = 0;

    Transform SpawnPoint => spawnTransform != null ? spawnTransform : transform;

    private void Awake()
    {
        if (spawnPrefab == null)
        {
            Debug.LogError("No prefab assigned to spawner", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (textUI != null)
        {
            textUI.text = objectName;
        }
    }

    private void Update()
    {
        // Seul le serveur gère le spawn et le compteur
        if (!IsServer) return;

        if (currentObject != null)
        {
            if (CheckObjectDistance() && objectcount < objectlimit)
            {
                currentObject = null;
                spawnCooldownTimer = spawnCooldown;
                objectcount++;
            }
        }
        else
        {
            CheckSpawn();
        }
    }

    private void CheckSpawn()
    {
        if (spawnCooldownTimer > 0)
        {
            spawnCooldownTimer -= Time.deltaTime;
            return;
        }

        if (objectcount >= objectlimit)
            return;

        SpawnObject();
    }

    private bool CheckObjectDistance()
    {
        if (currentObject == null)
            return true;

        return Vector3.Distance(
            currentObject.transform.position,
            SpawnPoint.position
        ) > distanceToSpawnNew;
    }

    private void SpawnObject()
    {
        spawnCooldownTimer = spawnCooldown;

        // 1. Instanciation locale sur le serveur
        currentObject = Instantiate(
            spawnPrefab,
            SpawnPoint.position,
            SpawnPoint.rotation
        );

        // 2. Réplication de l'objet sur tout le réseau
        NetworkObject netObj = currentObject.GetComponent<NetworkObject>();
        netObj.Spawn();

        // 3. Applique la taille réduite et le gel physique chez tout le monde
        if (currentObject.TryGetComponent(out NetworkSpawnedItem item))
        {
            item.InitializeSpawnStateRpc(multiplierscale, freezeOnSpawn);
        }
    }
}
