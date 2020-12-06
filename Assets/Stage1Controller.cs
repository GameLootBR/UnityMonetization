using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage1Controller : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        MonetizationManager.Instance.ShowInterstitial();
    }

}
