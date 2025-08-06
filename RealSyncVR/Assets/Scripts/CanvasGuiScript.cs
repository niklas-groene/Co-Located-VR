using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public enum MenuStates
{
    START,
    VR,
    CORRECT
}


public class VRPopupMenu : MonoBehaviour
{

    //Gui Elements
    public Button buttonRunVR;
    public Button buttonStartLog;
    public Button buttonStopLog;
    public Button buttonExit;

    public Toggle isMocapValid;
    public Toggle isHMDValid;
    public TextMeshProUGUI TextTrackingDebug;

    private MenuStates menuStates = MenuStates.START;

    //calibration data
    private CalibrationOffset m_calibrationOffset;
    public OffsetController offsetControler;


    
    void Start()
    {
        buttonRunVR.onClick.AddListener(() => OnButtonClicked("Run VR"));
        buttonExit.onClick.AddListener(() => OnButtonClicked("Exit"));
        buttonStartLog.onClick.AddListener(() => OnButtonClicked("StartLog"));
        buttonStopLog.onClick.AddListener(() => OnButtonClicked("StopLog"));

        TextTrackingDebug.gameObject.SetActive(false);
        buttonExit.gameObject.SetActive(false);
        buttonStartLog.gameObject.SetActive(false);
        buttonStopLog.gameObject.SetActive(false);
    }


    void OnButtonClicked(string buttonName)
    {

        if (buttonName == "Run VR")
        {
            menuStates = MenuStates.VR;
            offsetControler.SetProgramState(ProgramStates.TRACK);
            Debug.Log(buttonName + " clicked");
            ShowVRScreen();
        }
        else if (buttonName == "Exit")
        {
            menuStates = MenuStates.START;
            offsetControler.SetProgramState(ProgramStates.START);
            Debug.Log(buttonName + " clicked");
            ShowMainScreen();
            offsetControler.SetLogDataOn(false);
        }
        else if (buttonName == "StartLog")
        {
            offsetControler.SetLogDataOn(true);
            ShowVRScreen();
            buttonStartLog.gameObject.SetActive(false);
            buttonStopLog.gameObject.SetActive(true);
            Debug.Log(buttonName + " Start Log");
        }
        else if (buttonName == "StopLog")
        {
            offsetControler.SetLogDataOn(false);
            ShowVRScreen();
            buttonStartLog.gameObject.SetActive(true);
            buttonStopLog.gameObject.SetActive(false);
            Debug.Log(buttonName + " Stop Log");
        }

    }

    private void ShowVRScreen()
    {
        //Hide all
        HideAll();

        //show
        buttonExit.gameObject.SetActive(true);
        buttonStartLog.gameObject.SetActive(true);
        TextTrackingDebug.gameObject.SetActive(true);
    }

    private void ShowMainScreen()
    {
        //hide all
        HideAll();

        //show
        buttonRunVR.gameObject.SetActive(true);
        isMocapValid.gameObject.SetActive(true);
        isHMDValid.gameObject.SetActive(true);
        buttonExit.gameObject.SetActive(true);

    }


    private void HideAll()
    {
        buttonRunVR.gameObject.SetActive(false);
        buttonStartLog.gameObject.SetActive(false);
        buttonStopLog.gameObject.SetActive(false);
        buttonExit.gameObject.SetActive(false);

        isMocapValid.gameObject.SetActive(false);
        isHMDValid.gameObject.SetActive(false);
        TextTrackingDebug.gameObject.SetActive(false);
}

    public void UpdateTrackingDebugText(string s, bool exceededDelta)
    {

        TextTrackingDebug.text = s;
        TextTrackingDebug.color = Color.white;
        if( exceededDelta )
            TextTrackingDebug.color = Color.red;

    }
    public void SetIsMocapValid(bool value)
    {
        isMocapValid.isOn = value;
    }
    public void SetIsHMDValid(bool value)
    {
        isHMDValid.isOn = value;
    }

}
