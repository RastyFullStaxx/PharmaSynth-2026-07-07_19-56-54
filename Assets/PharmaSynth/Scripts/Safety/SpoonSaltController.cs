using UnityEngine;
using System.Collections.Generic;

public class SpoonSaltController : MonoBehaviour
{
    [Header("Salt Visuals on Spoon")]
    public GameObject[] saltVisuals;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("bacl2"))
        {
            SetActiveSalt(0);
        }
        else if (other.CompareTag("cacl2"))
        {
            SetActiveSalt(1);
        }
        else if (other.CompareTag("srcl2"))
        {
            SetActiveSalt(2);
        }
    }

    private void SetActiveSalt(int activeIndex)
    {
        for (int i = 0; i < saltVisuals.Length; i++)
        {
            saltVisuals[i].SetActive(i == activeIndex);
        }
    }

}