using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageSetter : MonoBehaviour
{
    // List of Prefabs to replace
    public List<GameObject> Symbols;
    [SerializeField]
    private GameObject _bonusSymbol;
    // Method to set random image, int to determine which child
    public void RandomImage(int childIndex)
    {
        // create reference
        GameObject child = transform.GetChild(childIndex).gameObject;
        // destroy current child
        Destroy(child.transform.GetChild(0).gameObject);
        int rando = Random.Range(0, Symbols.Count -1);
        //make a new child
        GameObject newChild = Instantiate(Symbols[rando]);
        // add to reel
        newChild.transform.SetParent(child.transform, false);
    }
    // Set the bonus symbol to the 2nd row for activation
    public void SetBonusSymbol()
    {        
        // Check symbols for bonus symbol
        for(int i = 2; i <= 3; i++)
        {
            if(transform.GetChild(i).GetChild(0).gameObject == _bonusSymbol)
            {
                Debug.Log("Bonus Symbol Present");
                return;
            }
        }
        // if we dont return
        // destroy the child and replace with bonus
        Destroy(transform.GetChild(2).GetChild(0).gameObject);
        GameObject newChild = Instantiate(Symbols[0]);
        newChild.transform.SetParent(transform.GetChild(2), false);
    }
}
