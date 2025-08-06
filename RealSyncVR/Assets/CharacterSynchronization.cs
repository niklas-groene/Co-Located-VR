using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

public class CharacterSynchronization : MonoBehaviour
{

    protected GameObject head;
    protected GameObject leftHand;
    protected GameObject rightHand;

    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;

    [SerializeField] bool _useController;

    [SerializeField]
    protected GameObject[] Players;

    [SerializeField]
    protected Camera camera;

    public GameObject hand_l, hand_r;




    private int iD;

    private void Awake()
    {
        iD = this.GetComponent<OffsetController>()._PlayerNumber;
        if (iD > 0 && iD < Players.Length + 1)
        {
            Players[iD - 1].SetActive(true);
        }
        else
        {
            Debug.LogError("Invalid player role!");
        }
        InitializeData();
    }


    void Start()
    {

        // Initialize the devices
        InitializeDevices();
    }

    void Update()
    {
        if (_useController)
        {
            // Check if the devices are still valid
            //if (!leftHandDevice.isValid || !rightHandDevice.isValid)
            if (!rightHandDevice.isValid)
            {
                InitializeDevices();
            }

            if(leftHandDevice.isValid)
                leftHand.GetComponent<MeshRenderer>().enabled = true;

            if(rightHandDevice.isValid)
                rightHand.GetComponent<MeshRenderer>().enabled = true;
            
            
            // Get position and rotation for the left controller
            if (leftHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPosition))
            {
                hand_l.transform.localPosition = leftPosition;
                leftHand.transform.localPosition = hand_l.transform.GetWorldPose().position;
                
            }

            if (leftHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion leftRotation))
            {
                hand_l.transform.localRotation = leftRotation;
                leftHand.transform.localRotation = hand_l.transform.GetWorldPose().rotation;
            }

            // Get position and rotation for the right controller
            if (rightHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPosition))
            {
                hand_r.transform.localPosition = rightPosition;
                rightHand.transform.localPosition = hand_r.transform.GetWorldPose().position;
            }

            if (rightHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightRotation))
            {
                hand_r.transform.localRotation = rightRotation;
                rightHand.transform.rotation = hand_r.transform.GetWorldPose().rotation;
            }
        }

        head.transform.localPosition = camera.transform.GetWorldPose().position;
        head.transform.localRotation = camera.transform.GetWorldPose().rotation;
    }

    private void InitializeDevices()
    {

        // Controller:
        if (_useController)
        {
            // Find the left-hand and right-hand devices
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevices(inputDevices);

            foreach (var device in inputDevices)
            {
                if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeldInHand) &&
                    device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
                {
                    if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
                    {
                        leftHandDevice = device;
                        //Debug.Log("Left Hand Device Found");
                    }
                    else if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
                    {
                        rightHandDevice = device;
                        //Debug.Log("Right Hand Device Found");
                    }
                }
            }
        }

    }

    private void InitializeData()
    {

        head = GameObject.Find($"PlayerData/Player{iD}/Head");
        if (head == null)
        {
            Debug.LogError("Head not found!");
        }
        leftHand = GameObject.Find($"PlayerData/Player{iD}/LeftHand");
        if (leftHand == null)
        {
            Debug.LogError("Left-Hand not found!");
        }
        rightHand = GameObject.Find($"PlayerData/Player{iD}/RightHand");
        if (rightHand == null)
        {
            Debug.LogError("Right-Hand not found!");
        }

    }


}
