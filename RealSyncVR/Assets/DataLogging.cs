using System.Collections.Generic;
using System.IO;
using HCIKonstanz.Colibri.Synchronization;
using UnityEngine;

public class DataLogging : MonoBehaviour
{
    [SerializeField] GameObject _mocapData;
    [SerializeField] GameObject _playerData;
     // Assign PlayerOne, PlayerTwo, etc., in the Inspector
    public string fileName = "PlayerData.csv"; // File name for the CSV
    private bool isRecording = false; // Whether recording is active
    private List<string> recordedData = new List<string>(); // List to store recorded data
    int i = 0;
    private bool stopped = false;

    void Start()
    {
        // Add a header for the CSV file
        recordedData.Add("Timestamp,Player,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ");
    }

    void Update()
    {
        if (!stopped){
        if (i == 0) // Start recording
        {
            StartRecording();
            
        }
        else if (i > 77 * 100) // Stop recording
        {
            StopRecording();
        }

        if (isRecording)
        {
            i++;
            RecordPlayerData();
        }
        }
    }

    public void StartRecording()
    {            //player.GetComponent<SyncTransform>().Active = true;
    
        isRecording = true;
        Debug.LogError("Recording started.");
    }

    public void StopRecording()
    {
        stopped = true;
        isRecording = false;
        Debug.LogError("Recording stopped.");
        SaveToCSV();
    }

    private void RecordPlayerData()
    {
       Vector3 mocapPosition = _mocapData.transform.position;
       Vector3 mocapRotationEuler = _mocapData.transform.rotation.eulerAngles;
       Quaternion mocapRotationQuaternion = _mocapData.transform.rotation;

       Vector3 headsetPosition = _playerData.transform.position;
       Vector3 headsetRotationEuler = _playerData.transform.rotation.eulerAngles;
       Quaternion headsetRotationQuaternion = _playerData.transform.rotation;

                string data = $"{Time.time};{mocapPosition.x};{mocapPosition.y};{mocapPosition.z};{mocapRotationEuler.x};{mocapRotationEuler.y};{mocapRotationEuler.z};{headsetPosition.x};{headsetPosition.y};{headsetPosition.z};{headsetRotationEuler.x};{headsetRotationEuler.y};{headsetRotationEuler.z}; {mocapRotationQuaternion};{headsetRotationQuaternion}";
                recordedData.Add(data);
            
        
    }

    private void SaveToCSV()
    {
        string filePath = Path.Combine(Application.dataPath, fileName);

        try
        {
            File.WriteAllLines(filePath, recordedData);
            Debug.Log($"Data saved to {filePath}");
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to save data to CSV: {e.Message}");
        }
    }
}
