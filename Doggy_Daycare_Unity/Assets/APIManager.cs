using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class APIManager
{
    private const string BASE_URL = "https://avansict2237024.azurewebsites.net/api";

    private static string jwtToken = "";
    private static int id;        // ← unchanged, still unused in your file

    public static void LoadSavedToken() =>
        jwtToken = PlayerPrefs.GetString("authToken", "");

//THIS NEEDS TO BE SERIALIZABLE IT DOESNT WORK WITHOUT IT. 
    [System.Serializable]
    public struct WorldDto
    {
        public int id;
        public string name;
        public int width;
        public int height;
    }

    public static void Logout()
    {
        jwtToken = "";
        PlayerPrefs.DeleteKey("authToken");
        PlayerPrefs.Save();
    }

    /* ============================================================
     *  HELPER: build a consistent APIResponse for every call
     * ========================================================== */
    private static APIResponse BuildResponse(UnityWebRequest request)
    {
        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? "OK" : request.downloadHandler.text; // REAL text on failure

        return new APIResponse(
            success,
            message,
            request.downloadHandler.text,
            (int)request.responseCode);
    }

    /* ─────────── REGISTER ─────────── */
    public static IEnumerator Register(string username,
                                       string password,
                                       Action<APIResponse> callback)
    {
        string jsonBody = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";

        using UnityWebRequest request =
            new UnityWebRequest(BASE_URL + "/Auth/register", "POST");

        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        callback?.Invoke(BuildResponse(request));
    }

    /* ─────────── LOGIN ─────────── */
    public static IEnumerator Login(string username,
                                    string password,
                                    Action<APIResponse> callback)
    {
        string jsonBody = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";

        using UnityWebRequest request =
            new UnityWebRequest(BASE_URL + "/Auth/login", "POST");

        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        APIResponse response = BuildResponse(request);

        /* store token if login succeeded */
        if (response.Success)
        {
            AuthResponse auth = JsonUtility.FromJson<AuthResponse>(response.Data);
            jwtToken = auth.token;
            PlayerPrefs.SetString("authToken", jwtToken);
            PlayerPrefs.Save();
        }

        callback?.Invoke(response);
    }

    /* ─────────── LIST WORLDS ─────────── */
    public static IEnumerator GetWorlds(Action<APIResponse> callback)
    {
        using UnityWebRequest request =
            new UnityWebRequest(BASE_URL + "/Worlds", "GET");

        request.downloadHandler = new DownloadHandlerBuffer();
        AddAuthHeader(request);

        yield return request.SendWebRequest();
        callback?.Invoke(BuildResponse(request));
    }

    /* ─────────── CREATE WORLD ─────────── */
    public static IEnumerator CreateWorld(string worldName,
                                          int width,
                                          int height,
                                          Action<APIResponse> callback)
    {
        string jsonBody =
            $"{{\"name\":\"{worldName}\",\"width\":{width},\"height\":{height}}}";

        using UnityWebRequest request =
            new UnityWebRequest(BASE_URL + "/Worlds", "POST");

        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AddAuthHeader(request);

        yield return request.SendWebRequest();
        callback?.Invoke(BuildResponse(request));
    }

    /* ─────────── DELETE WORLD ─────────── */
    public static IEnumerator DeleteWorld(int worldId,
                                          Action<APIResponse> callback)
    {
        using UnityWebRequest request =
            new UnityWebRequest($"{BASE_URL}/Worlds/{id}", "DELETE"); // URL untouched

        request.downloadHandler = new DownloadHandlerBuffer();
        AddAuthHeader(request);

        yield return request.SendWebRequest();
        callback?.Invoke(BuildResponse(request));
    }

    /* ─────────── ADD OBJECT ─────────── */
    public static IEnumerator AddObject(int worldId,
                                        string type,
                                        float x,
                                        float y,
                                        Action<APIResponse> callback)
    {
        string jsonBody = $"{{\"type\":\"{type}\",\"x\":{x},\"y\":{y}}}";

        using UnityWebRequest request =
            new UnityWebRequest($"{BASE_URL}/Worlds/{id}/objects", "POST"); // URL untouched

        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AddAuthHeader(request);

        yield return request.SendWebRequest();
        callback?.Invoke(BuildResponse(request));
    }

    /* ─────────── helper to add JWT ─────────── */
    private static void AddAuthHeader(UnityWebRequest request)
    {
        if (!string.IsNullOrEmpty(jwtToken))
            request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
    }
}

/* ===== Response & DTOs ===== */
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

