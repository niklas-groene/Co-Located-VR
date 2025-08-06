using System.IO;
using UnityEngine;

public class DataLoggingDirect : MonoBehaviour
{
    [Tooltip("How many seconds to record for before stopping automatically")]
    [SerializeField] private float durationInSeconds = 10f;
    public string fileName = "PlayerData.csv";
    private GameObject _mocapData;
    [SerializeField] GameObject[] _mocapDataAll;
    [SerializeField] GameObject[] _leftControllerAll;
    [SerializeField] GameObject[] _rightControllerAll;
    private GameObject _leftController;
    private GameObject _rightController;
    [SerializeField] GameObject _playerData;
    [SerializeField] FileUploader fileUploader;
    [SerializeField] OffsetController offsetController;
    [SerializeField] GameObject _lController;
    [SerializeField] GameObject _rController;

        [Tooltip("Drag your disabled UI/Image/Text GameObject here")]
    public GameObject indicatorStop;
    public GameObject indicatorStart;
    public bool stopNow;

    [Tooltip("How long (seconds) to show the indicator before hiding it again")]
    public float displayDuration = 2f;

    private float _recordingStartTime;
    private bool isRecording = false;
    private StreamWriter writer;
    private bool stopped = false;
    private string filePath;
    private bool _isRecording = false;
    private bool _hasStopped = false;


    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Awake()
    {
        int iD = offsetController._PlayerNumber;
        if (iD > 0 && iD < _mocapDataAll.Length + 1){
            _mocapData = _mocapDataAll[iD-1];
            _leftController = _leftControllerAll[iD-1];
            _rightController = _rightControllerAll[iD-1];
        }
             
        else
            Debug.LogError("Invalid player role!");
    }


    void Update()
    {
        if (_hasStopped) return;

        if (!_isRecording)
        {
            StartRecording();
        }
        else
        {
            if (Time.time - _recordingStartTime >= durationInSeconds || stopNow)
            {
                StopRecording();
                return;
            }

            RecordPlayerData();
        }
    }

    public void StartRecording()
    {
        isRecording = true;
            _isRecording = true;
        //ShowIndicatorStart();
        _recordingStartTime = Time.time;
        filePath = Path.Combine(Application.dataPath, fileName);
        Debug.Log(filePath);

        //filePath = Path.GetFileNameWithoutExtension(filePath) + "_" + System.Guid.NewGuid().ToString() + ".txt";

        try
        {
            writer = new StreamWriter(filePath, false); // Overwrite existing file
            writer.WriteLine("Timestamp;MocapPosX;MocapPosY;MocapPosZ;MocapEulerX;MocapEulerY;MocapEulerZ;HeadsetPosX;HeadsetPosY;HeadsetPosZ;HeadsetEulerX;HeadsetEulerY;HeadsetEulerZ;Quat1;Quat2;MocapControllerX;MocapControllerY;MocapControllerZ;ControllerX,ControllerY,ControllerZ");
            
            Debug.Log("Recording started.");
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to open file for writing: {e.Message}");
            _hasStopped = true;
        }
    }

    public void StopRecording()

    {
        stopped = true;
        isRecording = false;
        _hasStopped = true;
        _isRecording = false;
        if (writer != null)
        {
            writer.Close();
            writer = null;
            Debug.LogError("Recording stopped and file saved.");
        }
        ShowIndicatorStop();
        fileUploader.UploadLogFile(filePath);
    }

    private void RecordPlayerData()
    {
        if (writer == null) return;

        float timestamp = Time.time - _recordingStartTime;

        Vector3 mocapPosition = _mocapData.transform.position;
        Vector3 mocapRotationEuler = _mocapData.transform.rotation.eulerAngles;
        Quaternion mocapRotationQuaternion = _mocapData.transform.rotation;

        Vector3 headsetPosition = _playerData.transform.position;
        Vector3 headsetRotationEuler = _playerData.transform.rotation.eulerAngles;
        Quaternion headsetRotationQuaternion = _playerData.transform.rotation;

        string data = $"{Time.time};{mocapPosition.x};{mocapPosition.y};{mocapPosition.z};{mocapRotationEuler.x};{mocapRotationEuler.y};{mocapRotationEuler.z};{headsetPosition.x};{headsetPosition.y};{headsetPosition.z};{headsetRotationEuler.x};{headsetRotationEuler.y};{headsetRotationEuler.z}; {mocapRotationQuaternion};{headsetRotationQuaternion};{_leftController.transform.position.x},{_leftController.transform.position.y},{_leftController.transform.position.z},{_lController.transform.position.x},{_lController.transform.position.y},{_lController.transform.position.z}";

        writer.WriteLine(data);
    }

        /// <summary>
    /// Call this method when your recording stops.
    /// </summary>
    public void ShowIndicatorStop()
    {
        if (indicatorStop == null) return;

        indicatorStop.SetActive(true);

        StartCoroutine(HideAfterDelayStop());
    }

    private System.Collections.IEnumerator HideAfterDelayStop()
    {
        yield return new WaitForSeconds(displayDuration);
        indicatorStop.SetActive(false);
    }
        /// <summary>
    /// Call this method when your recording stops.
    /// </summary>
    public void ShowIndicatorStart()
    {
        if (indicatorStart == null) return;

        indicatorStart.SetActive(true);

        StartCoroutine(HideAfterDelayStart());
    }

    private System.Collections.IEnumerator HideAfterDelayStart()
    {
        yield return new WaitForSeconds(displayDuration);
        indicatorStart.SetActive(false);
    }
}
