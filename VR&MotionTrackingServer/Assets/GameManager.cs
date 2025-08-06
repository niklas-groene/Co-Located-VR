using System.Collections.Generic;
using HCIKonstanz.Colibri.Synchronization;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class GameManager : MonoBehaviour
{
    // # 0 - Server
    // # 1 - PlayerOne
    // # 2 - PlayerTwo
    // # ...
    // # n - PlayerN
    int i = 0;
    public GameObject PlayerOne;
    public GameObject PlayerTwo;
    public GameObject PlayerThree;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      i++;
      if (false)
      {
            PlayerOne.GetComponent<SyncTransform>().Active = true;
            PlayerTwo.GetComponent<SyncTransform>().Active = true;
            PlayerThree.GetComponent<SyncTransform>().Active = true;

      }
        
    }

    // Update is called once per frame
    /// <summary>
    /// We use the update Function to update the users 
    /// </summary> <summary>
    /// 
    /// </summary>
    void Update()
    {
    
    }


    
}
