using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class AuthHandler : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;

    public Button loginButton;
    public Button registerButton;

    public TextMeshProUGUI feedbackText;

    private void Start()
    {
        // koppelen van buttons aan handlers
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
    }
    private void OnLoginClicked()
    {
        feedbackText.text = "Inloggen... ";
        StartCoroutine(APIManager.Login(
            usernameField.text,
            passwordField.text,
            OnAuthResult));
    }

    private void OnRegisterClicked()
    {
        feedbackText.text = "Account wordt gemaakt...";
        StartCoroutine(APIManager.Register(
            usernameField.text,
            passwordField.text,
            registerResult =>
            {
                //COntroleert hier op fouten
                if (!registerResult.Success)
                {
                    feedbackText.text = "Oeps: " + registerResult.Message;
                    return;
                }

                //Automatisch inloggen na registratie, ik wil niet een apart scherm maken voor inlog en registratie haha
                StartCoroutine(APIManager.Login(
                    usernameField.text,
                    passwordField.text,
                    OnAuthResult));
            }));
    }

    //Callback voor de login/registratie
    private void OnAuthResult(APIResponse result)
    {
        if (result.Success)
        {
            feedbackText.text = "Success!";
            SceneManager.LoadScene("WorldMenu");
        }
        else
        {
            feedbackText.text = "Oops: " + result.Message;
        }
    }
}