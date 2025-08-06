using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;


public class CalibrationOffset
{
    public float error;
    public Vector3 t;
    public Vector3 R;
    public Quaternion Rq;

    public float bestError;
    public Vector3 bestT;
    public Vector3 bestR;
    public Quaternion bestRq;

    public CalibrationOffset()
    {
        error = 0f;
        t = Vector3.zero;
        R = Vector3.zero;
        Rq = Quaternion.identity;

        bestError = 1000.0f;
        bestT = Vector3.zero;
        bestR = Vector3.zero;
        bestRq = Quaternion.identity;
    }
}

public class CalibrationData
{

    public List<float> TimeStamp = new();
    public List<Vector3> MocapPosition = new();
    public List<Quaternion> MocapQuaternion = new();
    public List<Vector3> CamPosition = new();
    public List<Quaternion> CamQuaternion = new();
    public float calibrationError = new();

    public void ClearData()
    {
        MocapPosition.Clear();
        MocapQuaternion.Clear();    
        CamPosition.Clear();
        CamQuaternion.Clear();
        TimeStamp.Clear();
        calibrationError = 0f;
    }
}

public class LoggedData
{
    public List<float> TimeStamp = new();
    public List<Vector3> MocapPosition = new();
    public List<Quaternion> MocapQuaternion = new();
    public List<Vector3> CamPosition = new();
    public List<Quaternion> CamQuaternion = new();

    public List<Vector3> RightControllerPosition = new();
    public List<Quaternion> RightControllerQuaternion = new();

    public List<Vector3> LeftControllerPosition = new();
    public List<Quaternion> LeftControllerQuaternion = new();

    public void ClearData()
    {
        MocapPosition.Clear();
        MocapQuaternion.Clear();
        CamPosition.Clear();
        CamQuaternion.Clear();
        RightControllerPosition.Clear();
        RightControllerQuaternion.Clear();
        LeftControllerPosition.Clear();
        LeftControllerQuaternion.Clear();
    }

}

public static class CalibrationFileIO
{
    //Write to either android or editor 
    public static string GetWritePath(string filename)
    {
#if UNITY_EDITOR
        // Save to Assets in Unity editor
        return Path.Combine(Application.dataPath, filename);
#elif UNITY_ANDROID
    return Path.Combine(Application.persistentDataPath, filename);
#else
    // Fallback: write near data folder
    return Path.Combine(Application.dataPath, filename);
#endif
    }

    public static void WriteCalibrationParams(CalibrationOffset offset)
    {
        string filePath = GetWritePath("calibrationParams.json");
        string json = JsonUtility.ToJson(offset, true);
        File.WriteAllText(filePath, json);
        Debug.Log("Calibration parameters written to: " + filePath);
    }

    public static bool ReadCalibrationParams(ref CalibrationOffset calibrationParams)
    {
        string filePath = GetWritePath("calibrationParams.json");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("Calibration file not found: " + filePath);
            calibrationParams = new CalibrationOffset();
            return false;
        }

        string json = File.ReadAllText(filePath);
        calibrationParams = JsonUtility.FromJson<CalibrationOffset>(json);
        return true;
    }

    public static bool WriteFile(string filename, string content)
    {
        try
        {
            string filePath = GetWritePath(filename);
            System.IO.File.WriteAllText(filePath, content);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.Message);
            return false;
        }
    }

    public static bool ReadFile(string filename, ref string content)
    {
        try
        {
            string filePath = GetWritePath(filename);
            content = File.ReadAllText(filePath);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Read failed: " + e.Message);
            return false;
        }
    }

    public static bool WriteCalibrationDataToFile(string filename, CalibrationData data)
    {
        string filePath = GetWritePath(filename);
        string header = "Time;" +
                        "MocapX;MocapY;MocapZ;MocapQx;MocapQy;MocapQz;MocapQw;" +
                        "CamX;CamY;CamZ;CamQx;CamQy;CamQz;CamQw\n";

        try
        {
            using (StreamWriter sw = new StreamWriter(filePath, false)) // overwrite
            {
                sw.Write(header);

                int count = data.MocapPosition.Count;

                for (int i = 0; i < count; i++)
                {
                    Vector3 mPos = data.MocapPosition[i];
                    Quaternion mRot = data.MocapQuaternion[i];
                    Vector3 cPos = data.CamPosition[i];
                    Quaternion cRot = data.CamQuaternion[i];
                    float time = (i < data.TimeStamp.Count) ? data.TimeStamp[i] : 0f;

                    string line = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0:F6};{1:F6};{2:F6};{3:F6};{4:F6};{5:F6};{6:F6};{7:F6};" +
                        "{8:F6};{9:F6};{10:F6};{11:F6};{12:F6};{13:F6};{14:F6}\n",
                        time,
                        mPos.x, mPos.y, mPos.z,
                        mRot.x, mRot.y, mRot.z, mRot.w,
                        cPos.x, cPos.y, cPos.z,
                        cRot.x, cRot.y, cRot.z, cRot.w
                    );

                    sw.Write(line);
                }
            }
            Debug.Log("Writing" + filename);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to write trajectoryData: " + e.Message);
            return false;
        }
    }

    public static bool WriteLogToFile(string filename, LoggedData data)
    {
        string filePath = GetWritePath(filename);
        string header = "Time;" +
                        "MocapX;MocapY;MocapZ;MocapQx;MocapQy;MocapQz;MocapQw;" +
                        "CamX;CamY;CamZ;CamQx;CamQy;CamQz;CamQw;" +
                        "RControllerX;RControllerY;RControllerZ;RControllerQx;RControllerQy;RControllerQz;RControllerQw;" +
                        "LControllerX;LControllerY;LControllerZ;LControllerQx;LControllerQy;LControllerQz;LControllerQw\n";

        try
        {
            using (StreamWriter sw = new StreamWriter(filePath, false))
            {
                sw.Write(header);
                int count = data.TimeStamp.Count;
                for (int i = 0; i < count; i++)
                {
                    Vector3 mPos = data.MocapPosition[i];
                    Quaternion mRot = data.MocapQuaternion[i];
                    Vector3 cPos = data.CamPosition[i];
                    Quaternion cRot = data.CamQuaternion[i];
                    Vector3 rcPos = data.RightControllerPosition[i];
                    Quaternion rcRot = data.RightControllerQuaternion[i];
                    Vector3 lcPos = data.LeftControllerPosition[i];
                    Quaternion lcRot = data.LeftControllerQuaternion[i];
                    float time = data.TimeStamp[i];

                    string line = string.Format(CultureInfo.InvariantCulture,
                        "{0:F6};" +
                        "{1:F6};{2:F6};{3:F6};{4:F6};{5:F6};{6:F6};{7:F6};" +
                        "{8:F6};{9:F6};{10:F6};{11:F6};{12:F6};{13:F6};{14:F6};" +
                        "{15:F6};{16:F6};{17:F6};{18:F6};{19:F6};{20:F6};{21:F6};" +
                        "{22:F6};{23:F6};{24:F6};{25:F6};{26:F6};{27:F6};{28:F6}\n",
                        time,
                        mPos.x, mPos.y, mPos.z, mRot.x, mRot.y, mRot.z, mRot.w,
                        cPos.x, cPos.y, cPos.z, cRot.x, cRot.y, cRot.z, cRot.w,
                        rcPos.x, rcPos.y, rcPos.z, rcRot.x, rcRot.y, rcRot.z, rcRot.w,
                        lcPos.x, lcPos.y, lcPos.z, lcRot.x, lcRot.y, lcRot.z, lcRot.w
                    );

                    sw.Write(line);
                }
            }
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to write LogAllData: " + e.Message);
            return false;
        }
    }

    private static Quaternion ReadQuaternion(string s)
    {
        // Assumes format: "(x, y, z, w)"
        s = s.Trim().Trim('(', ')');  // removes whitespace, then outer brackets
        string[] parts = s.Split(',');

        if (parts.Length != 4)
            throw new System.FormatException("Invalid quaternion format");

        return new Quaternion(
            float.Parse(parts[0].Trim()),
            float.Parse(parts[1].Trim()),
            float.Parse(parts[2].Trim()),
            float.Parse(parts[3].Trim())
        );
    }

    public static bool ReadCalibrationFile(string filename, ref CalibrationData trajectoryData)
    {
        string fileContent = "";
        if (!ReadFile(filename, ref fileContent))
            return false;

        List<string> lines = fileContent.Split('\n').ToList();

        if (lines.Count == 0)
        {
            Debug.LogError("Calibration File is empty.");
            return false;
        }

        //skip first line, it's a header
        for(int i = 1; i< lines.Count; i++)
        {
            string[] elements = lines[i].Split(';');
            if (elements.Length < 16)
                break;

            float test = float.Parse(elements[0]);
            
            trajectoryData.TimeStamp.Add(float.Parse(elements[0]));

            trajectoryData.MocapPosition.Add(new Vector3(float.Parse(elements[1]), float.Parse(elements[2]), float.Parse(elements[3])));
            trajectoryData.CamPosition.Add(new Vector3(float.Parse(elements[7]), float.Parse(elements[8]), float.Parse(elements[9])));

            trajectoryData.MocapQuaternion.Add(ReadQuaternion(elements[13]));
            trajectoryData.CamQuaternion.Add(ReadQuaternion(elements[14]));
        }
        return true;
    }
}


