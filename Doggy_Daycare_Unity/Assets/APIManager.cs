using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


public static class ApiManager
{
    private const string Base = "https://avansict2237024.azurewebsites.net/api/";
    private static string _jwt;

    private struct TokenDto { public string token; }


// For registering a new user :)
    public static IEnumerator Register(string username, string password, Action onOk, Action<string> onError)
    {
        var body = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";

        using var request = new UnityWebRequest(Base + "Auth/register", "POST");
        var bytes = Encoding.UTF8.GetBytes(body);
        request.uploadHandler = new UploadHandlerRaw(bytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            onOk?.Invoke();
        else
            onError?.Invoke(request.error);
    }

// For logging in an existing user with JWT and cache :)
    public static IEnumerator Login(string username, string password, Action onOk, Action<string> onError)
    {
        var body = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";

        using var request = new UnityWebRequest(Base + "Auth/login", "POST");
        var bytes = Encoding.UTF8.GetBytes(body);
        request.uploadHandler = new UploadHandlerRaw(bytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            var dto = JsonUtility.FromJson<TokenDto>(request.downloadHandler.text);
            _jwt = dto.token;   
            PlayerPrefs.SetString("Jwt", _jwt);  
            PlayerPrefs.Save();
            onOk?.Invoke();
        }
        else
            onError?.Invoke(request.error);
    }

//Wrapper 
    private static UnityWebRequest AuthRequest(string path, string method = "GET")
    {
        var r = new UnityWebRequest(Base + path, method);
        r.SetRequestHeader("Authorization", $"Bearer {_jwt}");
        r.downloadHandler = new DownloadHandlerBuffer();
        return r;
    }

    // convenience for app start-up
    public static void LoadTokenFromPrefs() =>
        _jwt = PlayerPrefs.GetString("Jwt", "");
}
