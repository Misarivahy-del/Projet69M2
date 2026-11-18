using UnityEngine;
using UnityEngine.UI;

public class Ouverture_porte : MonoBehaviour
{
    public Button monButton;
    public GameObject goPorte;
    public GameObject goCanvas;
    public float tempsDepart_simu;
    public float moveSpeed = 2;
    private bool boutoncliquer = false;
    private float tempsEcoule = 0;
    public float dureeOuverture = 0.001f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        monButton.onClick.AddListener(TaskOnClic);
    }

    // Update is called once per frame
    void Update()
    {
        if (boutoncliquer && (tempsEcoule < dureeOuverture)) {
            tempsEcoule += Time.deltaTime;
            goPorte.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
    }

    public void TaskOnClic()
    {
        tempsDepart_simu = Time.realtimeSinceStartup;
        Debug.Log("temps de debut de simu : " + tempsDepart_simu);
        boutoncliquer = true;
        Debug.Log(boutoncliquer);
        goCanvas.SetActive(false);
    } 
}