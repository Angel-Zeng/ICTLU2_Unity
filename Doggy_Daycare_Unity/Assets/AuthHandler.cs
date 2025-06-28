using TMPro;
using UnityEngine;

public class AuthPanel : MonoBehaviour
{
    [SerializeField] TMP_InputField user, pass;
    [SerializeField] TextMeshProUGUI feedback;

    void Start() { ApiManager.LoadTokenFromPrefs(); /* auto-login if you want */ }

    public void OnLoginClick() =>
        StartCoroutine(ApiManager.Login(user.text, pass.text,
            () => feedback.text = "Logging in!",
            err => feedback.text = $"Login failed: {err}"));

    public void OnRegisterClick() =>
        StartCoroutine(ApiManager.Register(user.text, pass.text,
            () => feedback.text = "Account created!",
            err => feedback.text = $"Register failed: {err}"));
}
