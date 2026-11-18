using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode; // Indispensable pour parler au réseau

// Attention : On utilise NetworkBehaviour et plus MonoBehaviour
public class JoueurCameleon : NetworkBehaviour
{
    [Header("Glisse ici tes deux sous-objets")]
    public GameObject modeVR;
    public GameObject pcObs;

    // Cette fonction s'exécute automatiquement quand le joueur apparaît sur le réseau
    public override void OnNetworkSpawn()
    {
        // 1. RÈGLE D'OR : Est-ce que ce personnage est le mien ? (IsOwner)
        if (!IsOwner)
        {
            // Si c'est le personnage d'un AUTRE joueur :
            // On cache son écran PC (pour ne pas voir son interface flotter dans le vide)
            if (pcObs != null) pcObs.SetActive(false);

            // On laisse son corps VR visible pour le voir bouger
            if (modeVR != null) modeVR.SetActive(true);

            return; // On arrête le script ici pour ne pas aller plus loin
        }

        // 2. Si c'est NOTRE personnage, on vérifie NOTRE matériel
        List<InputDevice> appareilsVR = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, appareilsVR);

        // 3. La transformation s'opère !
        if (appareilsVR.Count == 0)
        {
            // AUCUN CASQUE = On active l'Instructeur PC
            if (modeVR != null) modeVR.SetActive(false);
            if (pcObs != null) pcObs.SetActive(true);
            Debug.Log("Mode Instructeur PC activé !");
        }
        else
        {
            // CASQUE DÉTECTÉ = On active le Joueur VR
            if (modeVR != null) modeVR.SetActive(true);
            if (pcObs != null) pcObs.SetActive(false);
            Debug.Log("Mode Joueur VR activé (" + appareilsVR[0].name + ") !");
        }
    }
}