using Firebase.Auth;
using UnityEngine;

// Escucha el estado de autenticación de Firebase y muestra el panel que corresponde.
public class AuthStateHandler : MonoBehaviour
{
    private bool _stateDirty;
    private bool _subscribed;
    private string _currentUserId;
    private bool _hasHandledFirstState;

    void Start()
    {
        FirebaseService.WhenReady(() =>
        {
            FirebaseService.Auth.StateChanged += AuthStateChanged;
            _subscribed = true;
            _stateDirty = true;
        });
    }

    void OnDestroy()
    {
        if (_subscribed) FirebaseService.Auth.StateChanged -= AuthStateChanged;
    }

    private void AuthStateChanged(object sender, System.EventArgs e)
    {
        // El evento puede llegar fuera del hilo principal; la UI se actualiza en Update.
        _stateDirty = true;
    }

    void Update()
    {
        if (!_stateDirty) return;
        _stateDirty = false;

        FirebaseUser user = FirebaseService.Auth.CurrentUser;
        string userId = user != null ? user.UserId : null;

        // StateChanged también se dispara al refrescar el token; solo reaccionamos si cambió el usuario.
        if (_hasHandledFirstState && userId == _currentUserId) return;
        _hasHandledFirstState = true;
        _currentUserId = userId;

        if (user != null)
        {
            Debug.Log("User is signed in: " + user.Email);
            UIManager.Instance.Show(AppScreen.Home);
        }
        else
        {
            if (SnakeGameManager.Instance != null) SnakeGameManager.Instance.StopGame();
            UIManager.Instance.Show(AppScreen.Login);
        }
    }
}
