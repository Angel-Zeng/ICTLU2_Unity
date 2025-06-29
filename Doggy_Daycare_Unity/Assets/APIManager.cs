using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class APIManager
{
    private const string BASE_URL = "https://avansict2237024.azurewebsites.net/api";

    private static string jwtToken = "";
    private static int id;          // (still unused, kept exactly as in your file)

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
        jwtToken = "";
        PlayerPrefs.DeleteKey("authToken");
        PlayerPrefs.Save();
    }

    /* ─────────── REGISTER ─────────── */
    public static IEnumerator Register(string username,
                                       string password,
                                       Action<APIResponse> resultCallback)
    {
        string jsonBody = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";

        using UnityWebRequest request =
            new UnityWebRequest(BASE_URL + "/Auth/register", "POST");

        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? "OK" : request.downloadHandler.text;

        APIResponse response = new APIResponse(
            success,
            message,
            request.downloadHandler.text,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }

    /* ─────────── LOGIN ─────────── */
    public static IEnumerator Login(string username,
                                    string password,
                                    Action<APIResponse> resultCallback)
    {
        string jsonBody = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";

        using UnityWebRequest request =
            new UnityWebRequest(BASE_URL + "/Auth/login", "POST");

        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string body = request.downloadHandler.text;
        string message = success ? "OK" : body;

        if (success)
        {
            AuthResponse auth = JsonUtility.FromJson<AuthResponse>(body);
            jwtToken = auth.token;
            PlayerPrefs.SetString("authToken", jwtToken);
            PlayerPrefs.Save();
        }

        APIResponse response = new APIResponse(
            success,
            message,
            body,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }

    /* ─────────── GET WORLDS ─────────── */
    public static IEnumerator GetWorlds(Action<APIResponse> resultCallback)
    {
        using UnityWebRequest request =
            new UnityWebRequest(BASE_URL + "/Worlds", "GET");

        request.downloadHandler = new DownloadHandlerBuffer();
        AddAuthHeader(request);

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? "OK" : request.downloadHandler.text;

        APIResponse response = new APIResponse(
            success,
            message,
            request.downloadHandler.text,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }

    /* ─────────── CREATE WORLD ─────────── */
    public static IEnumerator CreateWorld(string worldName,
                                          int width,
                                          int height,
                                          Action<APIResponse> resultCallback)
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

        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? "OK" : request.downloadHandler.text;

        APIResponse response = new APIResponse(
            success,
            message,
            request.downloadHandler.text,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }

    /* ─────────── DELETE WORLD ─────────── */
    public static IEnumerator DeleteWorld(int worldId,
                                          Action<APIResponse> resultCallback)
    {
        using UnityWebRequest request =
            new UnityWebRequest($"{BASE_URL}/Worlds/{id}", "DELETE"); // URL unchanged

        request.downloadHandler = new DownloadHandlerBuffer();
        AddAuthHeader(request);

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? "OK" : request.downloadHandler.text;

        APIResponse response = new APIResponse(
            success,
            message,
            request.downloadHandler.text,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }

    /* ─────────── ADD OBJECT ─────────── */
    public static IEnumerator AddObject(int worldId,
                                        string type,
                                        float x,
                                        float y,
                                        Action<APIResponse> resultCallback)
    {
        string jsonBody = $"{{\"type\":\"{type}\",\"x\":{x},\"y\":{y}}}";

        using UnityWebRequest request =
            new UnityWebRequest($"{BASE_URL}/Worlds/{id}/objects", "POST"); // URL unchanged

        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AddAuthHeader(request);

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? "OK" : request.downloadHandler.text;

        APIResponse response = new APIResponse(
            success,
            message,
            request.downloadHandler.text,
            (int)request.responseCode);

        resultCallback?.Invoke(response);
    }

    /* ─────────── helper ─────────── */
    private static void AddAuthHeader(UnityWebRequest request)
    {
        if (!string.IsNullOrEmpty(jwtToken))
            request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
    }
}

/* ─────────── DTOs ─────────── */
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
