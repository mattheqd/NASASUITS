using System.Collections.Generic;
using UnityEngine;

public class TabManager : MonoBehaviour
{
    public List<GameObject> panels;

    void Start()
    {
        ShowPanel(0);
    }

    public void ShowPanel(int index)
    {
        for (int i = 0; i < panels.Count; i++)
            panels[i].SetActive(i == index);
    }
}