using UnityEngine;
using UnityEngine.UI;

public class activation_canvas : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 1. On cherche le composant GraphicRaycaster sur cet objet
        GraphicRaycaster monRaycaster = GetComponent<GraphicRaycaster>();

        // 2. Si on l'a trouvé, on l'active
        if (monRaycaster != null)
        {
            monRaycaster.enabled = true;
            Debug.Log("GraphicRaycaster a été activé !");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
