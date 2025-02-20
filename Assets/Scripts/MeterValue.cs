using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class MeterValue : MonoBehaviour
{
    // coin shower prefab
    [SerializeField]
    private GameObject _coinShower;
    // jackpot value
    int value = 100000;



    // method to add to value
    public void AddToValue(int amount)
    {
        value += amount;
        SetText();
    }
    // method to apply to textmesh
    private void SetText()
    {
        GetComponentInChildren<TextMeshPro>().text = $"${value:N0}";
    }
    // method to set off coin shower
    private void CoinShower()
    {
        _coinShower.SetActive(true);
    }
    // method to count up jackpot
    public void CountUp()
    {
        StartCoroutine(CountUpRoutine());
    }
    // routine for incremental count up
    private IEnumerator CountUpRoutine()
    {
        int newValue = value * 2;
        while (value < newValue)
        {
            CoinShower();
            value += 100;
            SetText();
            yield return new WaitForSeconds(0.01f);
        }
        _coinShower.SetActive(false);
    }
}
