using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class APIManager
{
    //mijn url website
    private const string BASE_URL = "https://avansict2237024.azurewebsites.net/api";

    //voor jwt 
    private static string jwtToken = "";

    //laden van de opgeslagen token 
    public static void LoadSavedToken() =>
        jwtToken = PlayerPrefs.GetString("authToken", "");

    //Had deze beter in een class kunnen zetten, maar voor nu is dit prima anders breek ik alles!!!
    [System.Serializable]
    public struct WorldDto
    {
        public int id;
        public string name;
        public int width;
        public int height;
    }

    //Verwijdert de authtoken en verwijdert playerprefs
    public static void Logout()
    {
        jwtToken = "";
        PlayerPrefs.DeleteKey("authToken");
        PlayerPrefs.Save();
    }

    // Bouwt een gestandaardiseerd API antwoord
    private static APIResponse BuildResponse(UnityWebRequest request)
    {
        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? "OK" : request.downloadHandler.text;

        return new APIResponse(
            success,
            message,
            request.downloadHandler.text,
            (int)request.responseCode);
    }

    //Registreren van nieuwe accounts
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

    //authenticeert login
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

        //slaat die lange token op als het goed is
        if (response.Success)
        {
            AuthResponse auth = JsonUtility.FromJson<AuthResponse>(response.Data);
            jwtToken = auth.token;
            PlayerPrefs.SetString("authToken", jwtToken);
            PlayerPrefs.Save();
        }

        callback?.Invoke(response);
    }

    //het laden van alle werelden die op een user staat
    public static IEnumerator GetWorlds(Action<APIResponse> callback)
    {
        using UnityWebRequest request =
            new UnityWebRequest(BASE_URL + "/Worlds", "GET");

        request.downloadHandler = new DownloadHandlerBuffer();
        AddAuthHeader(request);

        yield return request.SendWebRequest();
        callback?.Invoke(BuildResponse(request));
    }

    //aanmaken van een nieuwe wereld
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

    //verwijderen van een wereld die op de user staat
    public static IEnumerator DeleteWorld(int worldId,
                                          Action<APIResponse> callback)
    {
        using UnityWebRequest request =
            new UnityWebRequest($"{BASE_URL}/Worlds/{worldId}", "DELETE");

        request.downloadHandler = new DownloadHandlerBuffer();
        AddAuthHeader(request);

        yield return request.SendWebRequest();
        callback?.Invoke(BuildResponse(request));
    }

    //toevoegen van een object, maar ik denk dat ik iets verkeerds doe met het binden op unity of misschien serializen?
    public static IEnumerator AddObject(int worldId,
                                        string type,
                                        float x,
                                        float y,
                                        Action<APIResponse> callback)
    {
        string jsonBody = $"{{\"type\":\"{type}\",\"x\":{x},\"y\":{y}}}";

        using UnityWebRequest request =
            new UnityWebRequest($"{BASE_URL}/Worlds/{worldId}/objects", "POST");

        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AddAuthHeader(request);

        yield return request.SendWebRequest();
        callback?.Invoke(BuildResponse(request));
    }

    //Nog meer DTO's voor wereld met objecten
    //Had ik alsnog beter kunnen zetten in aparte DTO files tja 
    [System.Serializable]
    public class WorldWithObjects
    {
        public WorldDto world;
        public ObjectDto[] objects;
    }

    // Data Transfer Object voor wereldobjecten
    [System.Serializable]
    public class ObjectDto
    {
        public int id;
        public string type;
        public float x;
        public float y;
    }

    //Ophalen van alle objecten in een wereld
    public static IEnumerator GetWorldObjects(int worldId, Action<WorldWithObjects> callback)
    {
        using UnityWebRequest request = UnityWebRequest.Get($"{BASE_URL}/Worlds/{worldId}");
        request.downloadHandler = new DownloadHandlerBuffer();
        AddAuthHeader(request);

        yield return request.SendWebRequest();

        //checken of de request succesvol was
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to get world objects: {request.error}");
            callback?.Invoke(null);
            yield break;
        }

        // Probeer JSON response te parsen
        try
        {
            WorldWithObjects data = JsonUtility.FromJson<WorldWithObjects>(request.downloadHandler.text);
            callback?.Invoke(data);
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON parse error: {e.Message}");
            callback?.Invoke(null);
        }
    }

    //toevoegen van jwt token aan de request header
    private static void AddAuthHeader(UnityWebRequest request)
    {
        if (!string.IsNullOrEmpty(jwtToken))
            request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
    }
}

//mn repsonse class voor api calls
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

// Authenticatie response specifiek voor tokens
[System.Serializable]
public class AuthResponse
{
    public string token;
}