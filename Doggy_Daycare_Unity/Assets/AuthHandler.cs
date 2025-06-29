using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
 * AuthHandler
 * -----------
 * • Handles the two buttons on the login screen.
 * • Shows feedback in a TMP label.
 * • After Register, automatically logs in with the same credentials.
 * • When Login succeeds, loads WorldsScene.
 */
public class AuthHandler : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;

    [Header("Buttons")]
    public Button loginButton;
    public Button registerButton;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;

    // Unity calls Start() once this GameObject is active.
    private void Start()
    {
        // Bind button clicks to our methods.
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
    }

    /* ---------------- BUTTON HANDLERS ---------------- */

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
            registerResult =>
            {
                if (!registerResult.Success)
                {
                    feedbackText.text = "Error: " + registerResult.Message;
                    return;
                }

                // Auto-login right after a successful register.
                StartCoroutine(APIManager.Login(
                    usernameField.text,
                    passwordField.text,
                    OnAuthResult));
            }));
    }

    /* ---------------- LOGIN / REGISTER CALLBACK ---------------- */

    private void OnAuthResult(APIResponse result)
    {
        if (result.Success)
        {
            feedbackText.text = "Success!";
            Debug.Log("Succesful login or registration");
            SceneManager.LoadScene("WorldMenu");   // go to next scene

        }
        else
        {
            feedbackText.text = "Error: " + result.Message;
        }
    }

}
