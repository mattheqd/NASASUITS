   using UnityEngine;
   using UnityEngine.UI;
   using TMPro;

   public class GaugeUI : MonoBehaviour
   {
     public Image fillImage;
     public TMP_Text valueText;
     public TMP_Text unitText;
     public TMP_Text labelText;
     public float maxValue = 100f;

     public void Set(string label, float value, string unit)
     {
       labelText.text = label;
       float pct = Mathf.Clamp01(value / maxValue);
       fillImage.fillAmount = pct;
       valueText.text = value.ToString("F0");
       unitText.text = unit;
     }
   }