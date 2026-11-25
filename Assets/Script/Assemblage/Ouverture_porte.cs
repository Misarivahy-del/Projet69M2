using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Ouverture_porte : NetworkBehaviour
{
    public Button monButton;
    public GameObject goPorte;
    public GameObject goCanvas;
    public float tempsDepart_simu;
    public float moveSpeed = 2f;
    public float dureeOuverture = 0.001f;

    private float tempsEcoule = 0f;

    // État synchronisé automatiquement entre le serveur et tous les clients
    private NetworkVariable<bool> porteOuverte = new NetworkVariable<bool>(false);

    private void Start()
    {
        monButton.onClick.AddListener(TaskOnClic);
    }

    public override void OnNetworkSpawn()
    {
        // Réagit dès que la valeur passe à true (ou immédiatement si déjà ouverte)
        porteOuverte.OnValueChanged += OnEtatPorteChange;
        if (porteOuverte.Value)
        {
            AppliquerOuvertureVisuelle();
        }
    }

    public override void OnNetworkDespawn()
    {
        porteOuverte.OnValueChanged -= OnEtatPorteChange;
    }

    private void Update()
    {
        if (porteOuverte.Value && (tempsEcoule < dureeOuverture))
        {
            tempsEcoule += Time.deltaTime;
            goPorte.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
    }

    public void TaskOnClic()
    {
        if (!IsSpawned || porteOuverte.Value) return;
        DemanderOuverturePorteRpc();
    }

    [Rpc(SendTo.Server)]
    private void DemanderOuverturePorteRpc()
    {
        porteOuverte.Value = true;
    }

    private void OnEtatPorteChange(bool ancienneValeur, bool nouvelleValeur)
    {
        if (nouvelleValeur)
        {
            AppliquerOuvertureVisuelle();
        }
    }

    private void AppliquerOuvertureVisuelle()
    {
        tempsDepart_simu = Time.realtimeSinceStartup;
        if (goCanvas != null) goCanvas.SetActive(false);
    }
}
