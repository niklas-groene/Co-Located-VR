using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class FileUploader : MonoBehaviour
{
    private string shareToken = "x_redacted_x";

    private string baseWebDAVUrl = "x_redacted_x";

    /// <summary>
    /// Start the upload coroutine for the given local file path.
    /// </summary>
    public void UploadLogFile(string localFilePath)
    {
        StartCoroutine(UploadFileCoroutine(localFilePath));
    }

    private IEnumerator UploadFileCoroutine(string localFilePath)
    {
        // Extract filename and escape it for a safe URL segment
        string fileName = Path.GetFileName(localFilePath);
        string escapedFileName = UnityWebRequest.EscapeURL(fileName);
        string uploadUrl = baseWebDAVUrl + escapedFileName;
        string credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(shareToken + ":"));

        byte[] fileData = File.ReadAllBytes(localFilePath);

        // Attempt initial PUT
        using (var putReq = new UnityWebRequest(uploadUrl, "PUT"))
        {
            putReq.uploadHandler = new UploadHandlerRaw(fileData);
            putReq.downloadHandler = new DownloadHandlerBuffer();
            putReq.SetRequestHeader("Authorization", "Basic " + credentials);

            yield return putReq.SendWebRequest();

            if (putReq.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Upload succeeded: " + uploadUrl);
                yield break;
            }

            // Handle 409 Conflict by deleting existing then retrying
            if (putReq.result == UnityWebRequest.Result.ProtocolError && putReq.responseCode == 409)
            {
                Debug.LogWarning("409 Conflict detected. Deleting existing file and retrying upload.");

                // Delete existing file (ignore 404 Not Found)
                using (var delReq = UnityWebRequest.Delete(uploadUrl))
                {
                    delReq.SetRequestHeader("Authorization", "Basic " + credentials);
                    yield return delReq.SendWebRequest();

                    if (!(delReq.result == UnityWebRequest.Result.Success || delReq.responseCode == 404))
                    {
                        Debug.LogError($"Failed to delete existing file ({delReq.responseCode}): {delReq.error}");
                        yield break;
                    }
                }

                // Retry PUT after deletion
                using (var retryPut = new UnityWebRequest(uploadUrl, "PUT"))
                {
                    retryPut.uploadHandler = new UploadHandlerRaw(fileData);
                    retryPut.downloadHandler = new DownloadHandlerBuffer();
                    retryPut.SetRequestHeader("Authorization", "Basic " + credentials);

                    yield return retryPut.SendWebRequest();

                    if (retryPut.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log("Upload succeeded after deleting old file: " + uploadUrl);
                    }
                    else
                    {
                        Debug.LogError($"Retry upload failed ({retryPut.responseCode}): {retryPut.error}\n{retryPut.downloadHandler.text}");
                    }
                    yield break;
                }
            }

            // Other errors
            Debug.LogError($"Upload failed ({putReq.responseCode}): {putReq.error}\n{putReq.downloadHandler.text}");
        }
    }
}