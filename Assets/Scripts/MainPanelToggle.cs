using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> panels;
    [SerializeField] private List<Button> tabButtons;
    [SerializeField] private Color tabNormalColor = new Color(0.1686f, 0.1765f, 0.1843f, 1f);
    [SerializeField] private Color tabSelectedColor = new Color(0.0314f, 0.8667f, 1f, 1f);

    void Start()
    {
        for (int i = 0; i < tabButtons.Count; i++)
        {
            int idx = i;
            tabButtons[i].onClick.AddListener(() => ShowPanel(idx));
        }
        ShowPanel(0);
    }

    public void ShowPanel(int index)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            bool active = (i == index);
            panels[i].SetActive(active);
            tabButtons[i].image.color = active ? tabSelectedColor : tabNormalColor;
        }
    }
}