using UnityEngine;
using UnityEngine.UI;

public class Ouverture_porte : MonoBehaviour
{
    public Button Button;
    public GameObject goPorte;
    public CanvasGroup Canvas;
    public float tempsDepart;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Button.onClick.AddListener(TaskOnClic);
    }

    // Update is called once per frame
    void Update()
    {
            
    }

    public void TaskOnClic()
    {
        tempsDepart = Time.realtimeSinceStartup;
        Debug.Log("temps de debut de simu : " + tempsDepart);
        goPorte.SetActive(false);
        Canvas.alpha = 0;
        Canvas.blocksRaycasts = false;
        Canvas.interactable = false;
    } 
}