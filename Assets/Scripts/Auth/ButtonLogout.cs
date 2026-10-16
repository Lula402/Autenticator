using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;

public class ButtonLogout : MonoBehaviour
{
    [SerializeField]
    private Button _logoutButton;

    private void Reset()
    {
        _logoutButton = GetComponent<Button>();
    }

    void Start()
    {
        _logoutButton.onClick.AddListener(() =>
        {
            if (SnakeGameManager.Instance != null) SnakeGameManager.Instance.StopGame();

            FirebaseUser user = FirebaseService.Auth.CurrentUser;
            if (user == null)
            {
                FirebaseService.Auth.SignOut();
                return;
            }

            // Me quito de users-online y SOLO cuando termina cierro sesión (después ya no tendría permiso).
            FirebaseService.Database.RootReference.Child("users-online").Child(user.UserId).SetValueAsync(null)
                .ContinueWithOnMainThread(_ => FirebaseService.Auth.SignOut());
            // AuthStateHandler detecta el cambio y vuelve al login.
        });
    }
}
