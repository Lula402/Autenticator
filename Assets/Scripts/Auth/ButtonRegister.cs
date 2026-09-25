using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonRegister : MonoBehaviour
{
    [SerializeField]
    private Button _registerButton;
    [SerializeField]
    private TMP_InputField _usernameInputField;
    [SerializeField]
    private TMP_InputField _emailInputField;
    [SerializeField]
    private TMP_InputField _passwordInputField;

    private void Reset()
    {
        _registerButton = GetComponent<Button>();
    }

    void Start()
    {
        _registerButton.onClick.AddListener(OnRegisterButtonClick);
    }

    private void OnRegisterButtonClick()
    {
        string username = _usernameInputField.text.Trim();
        string email = _emailInputField.text.Trim();
        string password = _passwordInputField.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            StatusMessage.Show("Completa todos los campos.", true);
            return;
        }
        if (username.Length < 3 || username.Length > 16)
        {
            StatusMessage.Show("El usuario debe tener entre 3 y 16 caracteres.", true);
            return;
        }

        RegisterUser(username, email, password);
    }

    // Se usa ContinueWithOnMainThread en vez de una corrutina: al crear la cuenta Firebase
    // inicia sesión y AuthStateHandler oculta este panel, lo que detendría la corrutina.
    private void RegisterUser(string username, string email, string password)
    {
        _registerButton.interactable = false;

        FirebaseService.Auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(registerTask =>
        {
            if (registerTask.IsCanceled || registerTask.IsFaulted)
            {
                _registerButton.interactable = true;
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + registerTask.Exception);
                StatusMessage.Show(FirebaseErrors.DescribeAuth(registerTask.Exception), true);
                return;
            }

            FirebaseUser newUser = registerTask.Result.User;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})", newUser.Email, newUser.UserId);

            newUser.UpdateUserProfileAsync(new UserProfile { DisplayName = username });
            SaveUserData(newUser.UserId, username);
        });
    }

    // users/{uid}: nombre de usuario y puntaje inicial.
    private void SaveUserData(string userId, string username)
    {
        var userData = new Dictionary<string, object>
        {
            { "username", username },
            { "score", 0 },
            { "creado", ServerValue.Timestamp }
        };

        FirebaseService.Users.Child(userId).UpdateChildrenAsync(userData).ContinueWithOnMainThread(saveTask =>
        {
            _registerButton.interactable = true;

            if (saveTask.IsCanceled || saveTask.IsFaulted)
            {
                Debug.LogError("Error guardando los datos del usuario: " + saveTask.Exception);
                StatusMessage.Show("No se pudo guardar tu usuario.", true);
                return;
            }

            _usernameInputField.text = "";
            _emailInputField.text = "";
            _passwordInputField.text = "";
        });
    }
}
