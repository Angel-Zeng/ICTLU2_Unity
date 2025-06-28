using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


public static class APIManager
{

    private const string BASE_URL = "https://avansict2237024.azurewebsites.net/api";

    private static string jwtToken = "";

    public static void LoadSavedToken()
    {
        jwtToken = PlayerPrefs.GetString("authToken", "");
    }

    public struct WorldDto
    {
        public int id;
        public string name;
        public int width;
        public int height;
    }
    public static void Logout()
    {
        jwtToken = "";                    // forget in memory
        PlayerPrefs.DeleteKey("authToken"); // forget on disk
        PlayerPrefs.Save();
    }


    // For registering a new user! 
    public static IEnumerator Register(string username, string password, Action<APIResponse> resultCallback)
    {
        string jsonBody = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";

        // POST reuquest
        using UnityWebRequest request = new UnityWebRequest(BASE_URL + "/Auth/register", "POST");

        byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Send request
        yield return request.SendWebRequest();

        APIResponse response = new APIResponse(
            request.result == UnityWebRequest.Result.Success,
            request.error,
            request.downloadHandler.text,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }


    public static IEnumerator Login(string username, string password, Action<APIResponse> resultCallback)
    {
        string jsonBody =
            $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";

        using UnityWebRequest request =
            new UnityWebRequest(BASE_URL + "/Auth/login", "POST");

        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string body = request.downloadHandler.text;

        // If Login was successful, gets a JWT token in the response yippiee
        if (success)
        {
            AuthResponse auth = JsonUtility.FromJson<AuthResponse>(body);
            jwtToken = auth.token;
            PlayerPrefs.SetString("authToken", jwtToken);
            PlayerPrefs.Save();
        }

        APIResponse response = new APIResponse(
            success,
            request.error,
            body,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }


    public static IEnumerator GetWorlds(Action<APIResponse> resultCallback)
    {
        using UnityWebRequest request =
            new UnityWebRequest(BASE_URL + "/worlds", "GET");

        request.downloadHandler = new DownloadHandlerBuffer();
        AddAuthHeader(request);

        yield return request.SendWebRequest();

        APIResponse response = new APIResponse(
            request.result == UnityWebRequest.Result.Success,
            request.error,
            request.downloadHandler.text,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }


    public static IEnumerator CreateWorld(string worldName,
                                          int width,
                                          int height,
                                          Action<APIResponse> resultCallback)
    {
        string jsonBody =
            $"{{\"name\":\"{worldName}\",\"width\":{width},\"height\":{height}}}";

        using UnityWebRequest request =
            new UnityWebRequest(BASE_URL + "/worlds", "POST");

        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AddAuthHeader(request);

        yield return request.SendWebRequest();

        APIResponse response = new APIResponse(
            request.result == UnityWebRequest.Result.Success,
            request.error,
            request.downloadHandler.text,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }


    public static IEnumerator DeleteWorld(int worldId,
                                          Action<APIResponse> resultCallback)
    {
        using UnityWebRequest request =
            new UnityWebRequest($"{BASE_URL}/worlds/{worldId}", "DELETE");

        request.downloadHandler = new DownloadHandlerBuffer();
        AddAuthHeader(request);

        yield return request.SendWebRequest();

        APIResponse response = new APIResponse(
            request.result == UnityWebRequest.Result.Success,
            request.error,
            request.downloadHandler.text,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }

    public static IEnumerator AddObject(int worldId,
                                        string type,
                                        float x,
                                        float y,
                                        Action<APIResponse> resultCallback)
    {
        string jsonBody =
            $"{{\"type\":\"{type}\",\"x\":{x},\"y\":{y}}}";

        using UnityWebRequest request =
            new UnityWebRequest($"{BASE_URL}/worlds/{worldId}/objects", "POST");

        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AddAuthHeader(request);

        yield return request.SendWebRequest();

        APIResponse response = new APIResponse(
            request.result == UnityWebRequest.Result.Success,
            request.error,
            request.downloadHandler.text,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }

    private static void AddAuthHeader(UnityWebRequest request)
    {
        if (!string.IsNullOrEmpty(jwtToken))
            request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
    }
}

//Stolen from friend
[System.Serializable]
public class APIResponse
{
    public bool Success;
    public string Message;
    public string Data;
    public int StatusCode;

    public APIResponse(bool success, string message,
                       string data = null, int statusCode = 0)
    {
        Success = success;
        Message = message;
        Data = data;
        StatusCode = statusCode;
    }
}

[System.Serializable]
public class AuthResponse
{
    public string token;
}