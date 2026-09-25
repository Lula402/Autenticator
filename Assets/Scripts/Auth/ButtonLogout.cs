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
            FirebaseService.Auth.SignOut();
            // AuthStateHandler detecta el cambio y vuelve al login.
        });
    }
}
