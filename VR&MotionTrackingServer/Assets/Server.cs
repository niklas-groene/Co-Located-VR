using HCIKonstanz.Colibri.Synchronization;
using UnityEngine;

public class Server : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void TriggerEnableDisableGameObject()
    {
         Sync.Receive("trigger::enable", (string b) => {
            // We will look for wether a specific game object exists
            GameObject turnOn = GameObject.Find(b);
             if (turnOn != null)
            {
                turnOn.SetActive(true);
            }
        });

        Sync.Receive("trigger::disable", (string b) => {
            GameObject turnOff = GameObject.Find(b);
            if (turnOff != null)
            {
                turnOff.SetActive(false);
            }
        });
    }

    void TriggerEvent()
    {
         Sync.Receive("trigger::event", (string b) => {
            // We will look for wether a specific game object exists
            GameObject turnOff = GameObject.Find(b);
        });

        Sync.Receive("trigger::on", (string b) => {
            GameObject turnOff = GameObject.Find(b);
        });
    }

   
}
