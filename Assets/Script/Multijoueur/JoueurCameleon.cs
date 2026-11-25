using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;

public class JoueurCameleon : NetworkBehaviour
{
    [Header("Glisse ici tes deux sous-objets")]
    public GameObject modeVR;
    public GameObject pcObs;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (pcObs != null) pcObs.SetActive(false);
            if (modeVR != null) modeVR.SetActive(true);
            return;
        }

        InputDevices.deviceConnected += OnPeripheriqueConnecte;
        StartCoroutine(VerifierDetectionVR());
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            InputDevices.deviceConnected -= OnPeripheriqueConnecte;
        }
    }

    private IEnumerator VerifierDetectionVR()
    {
        float tempsEcoule = 0f;

        while (tempsEcoule < 2f)
        {
            if (EstCasqueVRDetecte())
            {
                AppliquerModeLocal(true);
                yield break;
            }

            tempsEcoule += Time.unscaledDeltaTime;
            yield return null;
        }

        AppliquerModeLocal(false);
    }

    private void OnPeripheriqueConnecte(InputDevice device)
    {
        if (IsOwner && EstCasqueVRDetecte())
        {
            AppliquerModeLocal(true);
        }
    }

    private bool EstCasqueVRDetecte()
    {
        if (XRSettings.isDeviceActive)
            return true;

        List<InputDevice> appareilsVR = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, appareilsVR);
        return appareilsVR.Count > 0;
    }

    private void AppliquerModeLocal(bool enVR)
    {
        if (modeVR != null) modeVR.SetActive(enVR);
        if (pcObs != null) pcObs.SetActive(!enVR);
        Debug.Log(enVR ? "Mode Joueur VR activé !" : "Mode Instructeur PC activé !");
    }
}