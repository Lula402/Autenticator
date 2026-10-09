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
            // Me quito de users-online antes de cerrar sesión (después ya no tendría permiso).
            if (FirebaseService.Auth.CurrentUser != null)
            {
                FirebaseService.Database.RootReference.Child("users-online").Child(FirebaseService.Auth.CurrentUser.UserId).SetValueAsync(null);
            }
            FirebaseService.Auth.SignOut();
            // AuthStateHandler detecta el cambio y vuelve al login.
        });
    }
}
