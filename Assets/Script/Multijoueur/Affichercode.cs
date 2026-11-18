using UnityEngine;
using System.Collections;
using TMPro;
using XRMultiplayer;

public class Affichercode : MonoBehaviour
{
    public TMP_Text ZoneDeTexteCode;

    void Start()
    {
        // Au démarrage, on lance la tâche en arrière-plan
        StartCoroutine(AttendreLeCodeEtAfficher());
    }

    private IEnumerator AttendreLeCodeEtAfficher()
    {
        // TANT QUE le code est vide, on met la fonction en pause jusqu'à la frame suivante
        while (string.IsNullOrEmpty(XRINetworkGameManager.ConnectedRoomCode))
        {
            yield return null;
        }

        // Dès que la boucle s'arrête (le code n'est plus vide), on arrive ici.
        // On affiche le code UNE SEULE FOIS, et la Coroutine s'arrête définitivement.
        if (ZoneDeTexteCode != null)
        {
            ZoneDeTexteCode.text = XRINetworkGameManager.ConnectedRoomCode;
            Debug.Log("Le code a été récupéré et affiché avec succès !");
        }
    }

}
