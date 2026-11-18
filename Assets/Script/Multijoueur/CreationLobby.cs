using UnityEngine;
using XRMultiplayer;
using TMPro;


public class CreationLobby : MonoBehaviour
{
    public TMP_InputField ifCodeRoom;
    public GameObject goCamera;

    // Fonction à lier au bouton "Créer une session"
    public async void CreerPartie()
    {
        await XRINetworkGameManager.Instance.sessionManager.LeaveSession();
        Debug.Log("Création du serveur en cours...");

        // On demande au SessionManager de créer une partie
        await XRINetworkGameManager.Instance.sessionManager.CreateSession();


        if (XRINetworkGameManager.Instance.sessionManager.currentSession != null)
            goCamera.SetActive(false);
        else
        {
            
            Debug.LogWarning("Échec : Code invalide ou aucun salon disponible.");

            
            await XRINetworkGameManager .Instance.sessionManager.LeaveSession();
            goCamera.SetActive(true);
        }



    }

    // Fonction à lier au bouton "Rejoindre"
    public async void RejoindrePartie()
    {
        await XRINetworkGameManager.Instance.sessionManager.LeaveSession();
        string codeSaisi = ifCodeRoom.text.Trim();
        if (codeSaisi == "")
            codeSaisi = null;
        Debug.Log(codeSaisi);
        Debug.Log(null);

        if (codeSaisi != null && codeSaisi.Length != 6)
        {
            Debug.LogWarning("Erreur : Le code doit faire exactement 6 caractères.");
            return;
        }

        await XRINetworkGameManager.Instance.sessionManager.JoinLobby(null, codeSaisi);

        if (XRINetworkGameManager.Instance.sessionManager.currentSession != null)
            goCamera.SetActive(false);
        else
        {
            //ca fonctionne pas quand je me reconnecte je suis deja dans un serveur
            Debug.LogWarning("Échec : Code invalide ou aucun salon disponible.");

            
            await XRINetworkGameManager.Instance.sessionManager.LeaveSession();
            goCamera.SetActive(true);
        }


    }
}