using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonLogin : MonoBehaviour
{
    [SerializeField]
    private Button _loginButton;

    [SerializeField]
    private TMP_InputField _emailInputField;

    [SerializeField]
    private TMP_InputField _passwordInputField;

    private void Reset()
    {
        _loginButton = GetComponent<Button>();
    }

    void Start()
    {
        _loginButton.onClick.AddListener(HandleLoginButtonClicked);
    }

    private void HandleLoginButtonClicked()
    {
        string email = _emailInputField.text.Trim();
        string password = _passwordInputField.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            StatusMessage.Show("Escribe tu correo y tu contraseña.", true);
            return;
        }

        LoginUser(email, password);
    }

    public void LoginUser(string email, string password)
    {
        _loginButton.interactable = false;
        StatusMessage.Show("Iniciando sesión...");

        // ContinueWithOnMainThread evita errores al tocar la UI desde otro hilo.
        FirebaseService.Auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            _loginButton.interactable = true;

            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                StatusMessage.Show(FirebaseErrors.DescribeAuth(task.Exception), true);
                return;
            }

            AuthResult result = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})", result.User.DisplayName, result.User.UserId);
            _passwordInputField.text = "";
            // AuthStateHandler se encarga de mostrar el panel principal.
        });
    }
}
