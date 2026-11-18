using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class LancementSimulation : MonoBehaviour
{

    public GameObject goJoueurVR;
    public GameObject goMenuInstructeur;


        // En utilisant IEnumerator Start, on crée une Coroutine automatique
        private IEnumerator Start()
        {
            // 1. On attend 0.5 seconde pour laisser le temps à l'XR de s'initialiser
            yield return new WaitForSeconds(0.5f);

            // 2. On fait la vérification APRÈS l'attente
            List<InputDevice> appareilsVR = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, appareilsVR);

            // 3. On applique la logique
            if (appareilsVR.Count == 0)
            {
                goJoueurVR.SetActive(false);
                // On s'assure que le menu est bien actif
                goMenuInstructeur.SetActive(true);
                Debug.Log("Je suis l'instructeur (Aucun casque trouvé)");
            }
            else
            {
                goMenuInstructeur.SetActive(false);
                // On s'assure que le joueur VR est bien actif
                goJoueurVR.SetActive(true);
                Debug.Log("Je suis en VR (Casque détecté : " + appareilsVR[0].name + ")");
            }
        }
    }