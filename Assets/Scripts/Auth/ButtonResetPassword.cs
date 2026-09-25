using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Envía el correo de recuperación de contraseña de Firebase.
public class ButtonResetPassword : MonoBehaviour
{
    [SerializeField]
    private Button _resetButton;

    [SerializeField]
    private TMP_InputField _emailInputField;

    private void Reset()
    {
        _resetButton = GetComponent<Button>();
    }

    void Start()
    {
        _resetButton.onClick.AddListener(HandleResetButtonClicked);
    }

    private void HandleResetButtonClicked()
    {
        string email = _emailInputField.text.Trim();
        if (string.IsNullOrEmpty(email))
        {
            StatusMessage.Show("Escribe el correo de tu cuenta.", true);
            return;
        }

        _resetButton.interactable = false;
        StatusMessage.Show("Enviando correo...");

        FirebaseService.Auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            _resetButton.interactable = true;

            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("SendPasswordResetEmailAsync encountered an error: " + task.Exception);
                StatusMessage.Show(FirebaseErrors.DescribeAuth(task.Exception), true);
                return;
            }

            Debug.Log("Password reset email sent to " + email);
            StatusMessage.Show("Si el correo está registrado, te llegará un enlace para cambiar la contraseña. Revisa también spam.");
        });
    }
}
