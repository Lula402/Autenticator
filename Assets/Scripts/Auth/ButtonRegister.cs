using System;
using System.Collections.Generic;
using System.Globalization;
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
    [SerializeField]
    private TMP_InputField _confirmPasswordInputField;

    [Header("Datos adicionales")]
    [SerializeField]
    private TMP_InputField _cityInputField;
    [SerializeField]
    private TMP_InputField _birthDateInputField;

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
        string confirmPassword = _confirmPasswordInputField.text;
        string city = _cityInputField.text.Trim();
        string birthDateText = _birthDateInputField.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            StatusMessage.Show("Completa usuario, correo y contraseña.", true);
            return;
        }
        if (username.Length < 3 || username.Length > 16)
        {
            StatusMessage.Show("El nombre de usuario debe tener entre 3 y 16 caracteres.", true);
            return;
        }
        if (password != confirmPassword)
        {
            StatusMessage.Show("Las contraseñas no coinciden.", true);
            return;
        }
        if (string.IsNullOrEmpty(city))
        {
            StatusMessage.Show("Escribe tu ciudad.", true);
            return;
        }
        if (!DateTime.TryParseExact(birthDateText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime birthDate)
            || birthDate > DateTime.Today || birthDate.Year < 1900)
        {
            StatusMessage.Show("Escribe tu fecha de nacimiento como dd/mm/aaaa.", true);
            return;
        }

        RegisterUser(username, email, password, city, birthDate.ToString("dd/MM/yyyy"));
    }

    // Se usa ContinueWithOnMainThread en vez de una corrutina: al crear la cuenta Firebase
    // inicia sesión y AuthStateHandler oculta este panel, lo que detendría la corrutina.
    private void RegisterUser(string username, string email, string password, string city, string birthDate)
    {
        _registerButton.interactable = false;
        StatusMessage.Show("Creando cuenta...");

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
            SaveUserData(newUser.UserId, username, city, birthDate);
        });
    }

    // users/{uid}: nombre de usuario y datos adicionales del registro.
    private void SaveUserData(string userId, string username, string city, string birthDate)
    {
        var userData = new Dictionary<string, object>
        {
            { "username", username },
            { "ciudad", city },
            { "fechaNacimiento", birthDate },
            { "score", 0 },
            { "creado", ServerValue.Timestamp }
        };

        FirebaseService.Users.Child(userId).UpdateChildrenAsync(userData).ContinueWithOnMainThread(saveTask =>
        {
            _registerButton.interactable = true;

            if (saveTask.IsCanceled || saveTask.IsFaulted)
            {
                Debug.LogError("Error guardando los datos del usuario: " + saveTask.Exception);
                StatusMessage.Show("La cuenta se creó, pero no se pudieron guardar tus datos.", true);
                return;
            }

            StatusMessage.Show("¡Cuenta creada! Bienvenido, " + username + ".");
            ClearFields();
        });
    }

    private void ClearFields()
    {
        _usernameInputField.text = "";
        _emailInputField.text = "";
        _passwordInputField.text = "";
        _confirmPasswordInputField.text = "";
        _cityInputField.text = "";
        _birthDateInputField.text = "";
    }
}
