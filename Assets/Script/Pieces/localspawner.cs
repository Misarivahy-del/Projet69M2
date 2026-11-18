using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;



public class LocalInteractableSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnTransform;
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField]private TMPro.TMP_Text textUI;

    public bool freezeOnSpawn = true;
    public float distanceToSpawnNew = 0.15f;
    public float spawnCooldown = 0.5f;
    public float multiplierscale =0.5f;
    public float objectlimit=3;
    public string objectName;
    private float spawnCooldownTimer = 0f;
    private GameObject currentObject;
    private Vector3 originalScale;
    private RigidbodyConstraints originalConstraints;
    private float objectcount = 0;

    Transform SpawnPoint => spawnTransform != null ? spawnTransform : transform;
    void Start()
    {
        if (textUI != null)
    {
        textUI.text = objectName;
    }
    }
    void Awake()
    {
        if (spawnPrefab == null)
        {
            Debug.LogError("No prefab assigned to spawner", this);
            enabled = false;
        }
    }


    void Update()
    {
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

    void CheckSpawn()
    {
        if (spawnCooldownTimer > 0)
    {
        spawnCooldownTimer -= Time.deltaTime;
        return;
    }

    // Bloquer quand la limite est atteinte
    if (objectcount >= objectlimit)
        return;

    SpawnObject();
    }


    bool CheckObjectDistance()
    {
        if (currentObject == null)
            return true;

        return Vector3.Distance(
            currentObject.transform.position,
            SpawnPoint.position
        ) > distanceToSpawnNew;
    }

    void SpawnObject()
    {
    spawnCooldownTimer = spawnCooldown;

    currentObject = Instantiate(
        spawnPrefab,
        SpawnPoint.position,
        SpawnPoint.rotation
    );

    originalScale = currentObject.transform.localScale;

    // Preview scale
    currentObject.transform.localScale = originalScale * multiplierscale;

    // --- INTANGIBLE: Freeze complet ---
    Rigidbody rb = currentObject.GetComponent<Rigidbody>();
    if (rb != null)
    {
        originalConstraints = rb.constraints;      // sauver contraintes normales
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    var grab = currentObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    if (grab != null)
        grab.selectEntered.AddListener(OnObjectGrabbed);
    }


void OnObjectGrabbed(SelectEnterEventArgs args)
    {
    Transform t = args.interactableObject.transform;

    Rigidbody rb = t.GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.constraints = originalConstraints;   // restaurer les contraintes du prefab
    }

    // Restaure la taille normale
    t.localScale = originalScale;

    var grab = t.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    if (grab != null)
        grab.selectEntered.RemoveListener(OnObjectGrabbed);
    }


}