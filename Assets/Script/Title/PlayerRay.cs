using UnityEngine;

public class PlayerRay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray,out hit,10.0f))
        {
            Debug.Log(hit.collider.gameObject.transform.position);
        }
    }
}
