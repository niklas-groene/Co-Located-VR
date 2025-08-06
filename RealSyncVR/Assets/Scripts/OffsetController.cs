using Cysharp.Threading.Tasks;
using HCIKonstanz.Colibri.Networking;
using HCIKonstanz.Colibri.Setup;
using HCIKonstanz.Colibri.Synchronization;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
//using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

public enum ProgramStates
{
    START,
    CALIBRATE,
    CALIBRATEDONE,
    TRACK,
    CORRECT,
    ERROR
}


public class AlphaBetaGammaFilter
{
    // Position state
    private Vector3 position;
    private Vector3 velocity;
    private Vector3 acceleration;

    // Rotation state
    private Quaternion rotation;

    private readonly float alpha;
    private readonly float beta;
    private readonly float gamma;
    private bool initialized = false;

    public AlphaBetaGammaFilter(float alpha, float beta, float gamma)
    {
        this.alpha = Mathf.Clamp01(alpha);
        this.beta = Mathf.Clamp01(beta);
        this.gamma = Mathf.Clamp01(gamma);
    }

    public void UpdateFilter(Vector3 measuredPosition, Quaternion measuredRotation, float dt, out Vector3 filteredPos, out Quaternion filteredRot)
    {
        if (!initialized)
        {
            position = measuredPosition;
            velocity = Vector3.zero;
            acceleration = Vector3.zero;
            rotation = measuredRotation;
            initialized = true;
        }

        dt = Mathf.Clamp(dt, 0.001f, 0.1f);

        // --- POSITION ---
        Vector3 predictedPos = position + velocity * dt + 0.5f * acceleration * dt * dt;
        Vector3 predictedVel = velocity + acceleration * dt;
        Vector3 posResidual = measuredPosition - predictedPos;

        position = predictedPos + alpha * posResidual;
        velocity = predictedVel + beta * posResidual / dt;
        acceleration = acceleration + gamma * posResidual / (0.5f * dt * dt);

        // --- ROTATION ---
        rotation = Quaternion.Slerp(rotation, measuredRotation, alpha);

        // Output
        filteredPos = position;
        filteredRot = rotation;
    }
}

public class OffsetController : MonoBehaviour
{
    AlphaBetaGammaFilter smoothFilter;
    private Vector3 verification = new Vector3(0.12345f, 0.54321f, 0.12345f);
    private GameObject mocapData;
    public GameObject m_gizmoMocap;
    public GameObject m_gizmoMainCamera;


    [SerializeField] float delta = 0.1f;

    [SerializeField] private GameObject MainCamera;
    [SerializeField] private GameObject XROriginObject;
    //[SerializeField] private GameObject MouseGizmo;
    //[SerializeField] private GameObject MouseGizmoEyeCenter;

    [SerializeField] private WebServerConnection webServerConnection;

    [SerializeField] private AudioSource uiAudio;

    private bool m_mocapValid = false;
    private bool m_vrValid = false;

    public bool useMocap = false;
    public int _PlayerNumber;

    public Vector3 eyeCenterOffsetPos = Vector3.zero;
    public Vector3 eyeCenterOffsetRot = Vector3.zero;

    private ProgramStates programState = ProgramStates.START;
    private ProgramStates prevState = ProgramStates.CORRECT;

    private int m_calibrationCounter = 0;
    public int calibrationMaxCount = 100;

    public Canvas menuCanvas;

    private LoggedData m_loggedData; //this is logged during tracking, logs everything
    private bool IslogDataOn = false; //this turns logging on/off during tracking

    private Vector3 initMocapPos;
    private Vector3 initCamPos;

    //public bool useMouseGizmo = true;
    private bool setButtonTriggerOff = false;

    private float prevResidual = 0.0f;

    private void Awake()
    {
        //mocapData.transform.position = verification;
        prevState = ProgramStates.TRACK;
    }
    void Start()
    {
        if (_PlayerNumber < 1 || _PlayerNumber > 6)
        {
            Debug.LogError("The Player has not been Initialized!");
        }
        else
        {
            InitPlayer();
            StartCoroutine(WaitForMocap());
            StartCoroutine(WaitForHMD());
        }

        smoothFilter = new AlphaBetaGammaFilter(alpha: 0.125f, beta: 0.005f, gamma: 0.0001f);

        m_loggedData = new LoggedData();
    }
	
	//Mains states. The program transitions to states depending on function
	//This binds also the Menu, where user can choose actions and see data
	
    void FixedUpdate()
    {

        switch (programState)
        {
            case ProgramStates.START:
                {

                    if (prevState != programState)
                    {

                        Debug.Log("START State!");
                        prevState = programState;

                        initCamPos = new Vector3(0f, 0f, 0f);
                        initMocapPos = new Vector3(0f, 0f, 0f);

                        XROriginObject.transform.position = Vector3.zero;
                        XROriginObject.transform.rotation = Quaternion.identity;

                        Align(true);
                    }

                    break;
                }
            //Main state, where drift/residual is constantly checked
			case ProgramStates.TRACK:
                {
					//State transition, align once! 
                    if (prevState != programState)
                    {
                        Debug.Log("TRACK State!");
                        prevState = programState;
                        initCamPos = MainCamera.transform.position;

						//when using the mouse gizmo to test calibration
                        //if (!useMouseGizmo)
                        //    initMocapPos = mocapData.transform.position;
                        //else initCamPos = MouseGizmo.transform.position;
                        
                        Align(true);
                    }

                    //update debug screen
                    UpdateTrackingResidual();


                    break;
                }
            //If residual/drift too high, jumps to this state
			case ProgramStates.CORRECT:
                {
                    if (prevState != programState)
                    {
                        Debug.Log("CORRECT State!");
                        prevState = programState;
                        Align(true);
                        programState = ProgramStates.TRACK;
                    }
                    break;
                }
         
        }


		//constantly update these (gizmos are visible objects used for debugging)
        if (m_gizmoMainCamera != null && MainCamera != null)
        {
            m_gizmoMainCamera.transform.position = MainCamera.transform.position;
            m_gizmoMainCamera.transform.rotation = MainCamera.transform.rotation;
        }
        if ((m_gizmoMocap != null) && (m_mocapValid))
        {
            m_gizmoMocap.transform.position = mocapData.transform.position; 
            m_gizmoMocap.transform.rotation = mocapData.transform.rotation; 
        }


        //log all data
        if (IslogDataOn)
        {
            float currentTime = Time.time;
            m_loggedData.TimeStamp.Add(currentTime);

            if (m_mocapValid)
            {
                m_loggedData.MocapPosition.Add(mocapData.transform.position);
                m_loggedData.MocapQuaternion.Add(mocapData.transform.rotation);
            }
            else
            {
                m_loggedData.MocapPosition.Add(Vector3.zero);
                m_loggedData.MocapQuaternion.Add(Quaternion.identity);
            }

            if (m_vrValid)
            {
                m_loggedData.CamPosition.Add(MainCamera.transform.position);
                m_loggedData.CamQuaternion.Add(MainCamera.transform.rotation);

                InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                Vector3 rcPos = Vector3.zero;
                Quaternion rcRot = Quaternion.identity;
                if (rightController.isValid)
                {
                    rightController.TryGetFeatureValue(CommonUsages.devicePosition, out rcPos);
                    rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out rcRot);
                }
                m_loggedData.RightControllerPosition.Add(rcPos);
                m_loggedData.RightControllerQuaternion.Add(rcRot);

                InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                Vector3 lcPos = Vector3.zero;
                Quaternion lcRot = Quaternion.identity;
                if (leftController.isValid)
                {
                    leftController.TryGetFeatureValue(CommonUsages.devicePosition, out lcPos);
                    leftController.TryGetFeatureValue(CommonUsages.deviceRotation, out lcRot);
                }
                m_loggedData.LeftControllerPosition.Add(lcPos);
                m_loggedData.LeftControllerQuaternion.Add(lcRot);
            }
            else
            {
                m_loggedData.CamPosition.Add(Vector3.zero);
                m_loggedData.CamQuaternion.Add(Quaternion.identity);
                m_loggedData.RightControllerPosition.Add(Vector3.zero);
                m_loggedData.RightControllerQuaternion.Add(Quaternion.identity);
                m_loggedData.LeftControllerPosition.Add(Vector3.zero);
                m_loggedData.LeftControllerQuaternion.Add(Quaternion.identity);
            }
        }


        //To manually align, use right controller
        //InputDevice handControllerSImple = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        //if (handControllerSImple.isValid &&
        //    handControllerSImple.TryGetFeatureValue(CommonUsages.trigger, out float simpleTrigger) &&
        //    simpleTrigger > 0.1f)
        //{
        //    Align(true);
        //}

    }
	
	//This is the main function used for alignment
    private void Align(bool useCalibrationOffset)
    {
        if (mocapData == null || MainCamera == null || XROriginObject == null)
            return;

        if (!useCalibrationOffset)
        {
            eyeCenterOffsetPos = Vector3.zero;
            eyeCenterOffsetRot = Vector3.zero;
        }

        XROriginObject.transform.rotation = Quaternion.identity;


        Vector3 hmdLocalPos = MainCamera.transform.localPosition;
        Quaternion hmdLocalRot = MainCamera.transform.localRotation;

        Vector3 mocapPos = mocapData.transform.position;
        Quaternion mocapRot = mocapData.transform.rotation;

        //if (useMouseGizmo)
        //{
        //    mocapPos = MouseGizmo.transform.position;
        //    mocapRot = MouseGizmo.transform.rotation;
        //}

        Vector3 eyeCenterPos = mocapPos + mocapRot * eyeCenterOffsetPos;
        Quaternion eyeCenterRot = mocapRot * Quaternion.Euler(eyeCenterOffsetRot);

        Vector3 hmdFwd = MainCamera.transform.forward;
        hmdFwd.y = 0;
        hmdFwd.Normalize();

        Vector3 mocapFwd = eyeCenterRot * Vector3.forward;
        mocapFwd.y = 0;
        mocapFwd.Normalize();

        //measure yaw angle difference between cam and eyecenter coordinate frames 
        float angle = Vector3.SignedAngle(hmdFwd, mocapFwd, Vector3.up);
        Quaternion deltaYaw = Quaternion.Euler(0, angle, 0);

        //rotate rig offset using yaw angle
        XROriginObject.transform.localRotation = deltaYaw;

        //shift whole tracking rig such that cam matches eye center
        Vector3 positionShift = deltaYaw * hmdLocalPos;
        XROriginObject.transform.position = eyeCenterPos - positionShift;
    }

	//Util functions...
    public void SetLogDataOn(bool value)
    {
        //in case it was being logged
        if (IslogDataOn && !value)
        {
            WriteLog();
        }
        IslogDataOn = value;
    }
    public void SetProgramState(ProgramStates newState)
    {
        programState = newState;
    }
    private void InitPlayer()
    {
        mocapData = GameObject.Find("MotionTrackingData/Player" + _PlayerNumber);
    }
    private bool IsMocapValid()
    {
        if (!(mocapData == null || mocapData.transform.position == verification))
        {
            m_mocapValid = true;
        }
        else
        {
            m_mocapValid = false;
        }
        menuCanvas.gameObject.GetComponent<VRPopupMenu>().SetIsMocapValid(m_mocapValid);
        return m_mocapValid;
    }
    private bool IsVRValid()
    {
        InputDevice headset = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (headset.isValid && headset.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked)
        {
            m_vrValid = true;
            Debug.Log("VR iS VALID:" + m_vrValid);

        }
        else
        {
            m_vrValid = false;
        }
        menuCanvas.gameObject.GetComponent<VRPopupMenu>().SetIsHMDValid(m_vrValid);
        return m_vrValid;
    }
    private IEnumerator WaitForMocap()
    {
        // Wait until mocap data isvalid
        while (!IsMocapValid())
        {
            yield return null; // wait for next frame
        }
    }
    private IEnumerator WaitForHMD()
    {
        // Wait until both VR tracking is valid
        while (!IsVRValid())
        {
            yield return null; // wait for next frame
        }

    }
  
    public void WriteLog()
    {
        string timeInfo = System.DateTime.Now.ToString("HH_mm_ss");
        string name = timeInfo + "_BigLog.txt";
        if (m_loggedData == null)
            m_loggedData = new LoggedData();
        if (CalibrationFileIO.WriteLogToFile(name, m_loggedData))
        {
            Debug.Log("All Log saved");
        }
        else
        {
            Debug.Log("Error saving log");
        }

        m_loggedData.ClearData();

    }
    private void UpdateTrackingResidual()
    {
        float mocapCamXdt;
        float mocapCamYdt;
        float mocapCamZdt;

        float camXdt;
        float camYdt;
        float camZdt;

        float mocapXdt;
        float mocapYdt;
        float mocapZdt;

        //if (!useMouseGizmo)
        //{
            mocapCamXdt = MainCamera.transform.position.x - mocapData.transform.position.x;
            mocapCamYdt = MainCamera.transform.position.y - mocapData.transform.position.y;
            mocapCamZdt = MainCamera.transform.position.z - mocapData.transform.position.z;

            camXdt = initCamPos.x - MainCamera.transform.position.x;
            camYdt = initCamPos.y - MainCamera.transform.position.y;
            camZdt = initCamPos.z - MainCamera.transform.position.y;

            mocapXdt = initMocapPos.x - mocapData.transform.position.x;
            mocapYdt = initMocapPos.y - mocapData.transform.position.y;
            mocapZdt = initMocapPos.z - mocapData.transform.position.z;


        //verify residual
        float residual = (float)Math.Sqrt((mocapCamXdt * mocapCamXdt) + (mocapCamYdt * mocapCamYdt) + (mocapCamZdt * mocapCamZdt));

        float smoothesidual = SmoothFilter(residual, prevResidual, 0.5f);

        bool exceededDelta = false;



        if (residual > delta)
        {
            exceededDelta = true;
            Align(true);
        }


        //Update to UI text
        string residualString = "Residual: " + residual + "\n";
        menuCanvas.gameObject.GetComponent<VRPopupMenu>().UpdateTrackingDebugText(residualString, exceededDelta);

        prevResidual = residual;
    }

    private float SmoothFilter(float currentVlaue, float prevValue, float alpha)
    {
        return alpha *  currentVlaue + (1-alpha) * prevValue;
    }
}