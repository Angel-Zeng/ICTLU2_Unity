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
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
    }

    private void OnLoginClicked()
    {
        feedbackText.text = "Logging in...";
        StartCoroutine(APIManager.Login(
            usernameField.text,
            passwordField.text,
            OnAuthResult));
    }

    private void OnRegisterClicked()
    {
        feedbackText.text = "Creating account...";
        StartCoroutine(APIManager.Register(
            usernameField.text,
            passwordField.text,
            OnAuthResult));
    }

    private void OnAuthResult(APIResponse result)
    {
        if (result.Success)
        {
            feedbackText.text = "Success!";
            SceneManager.LoadScene("WorldsScene");
        }
        else
        {
            feedbackText.text = "Error: " + result.Message;
        }
    }
}