using UnityEngine;

public class UIfuncs : MonoBehaviour
{
    public GameObject[] views;
    public void objectToggler(GameObject gameObject)
    {
        foreach (GameObject view in views)
        {
            view.SetActive(false);
        }
        gameObject.SetActive(true);
    }
}
