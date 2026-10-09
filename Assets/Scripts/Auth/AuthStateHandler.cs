using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
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
            SetOnline(user);
        }
        else
        {
            if (SnakeGameManager.Instance != null) SnakeGameManager.Instance.StopGame();
            UIManager.Instance.Show(AppScreen.Login);
        }
    }

    // users-online/{uid} = username. Si la app se cae, Firebase borra el nodo solo (OnDisconnect).
    private void SetOnline(FirebaseUser user)
    {
        FirebaseService.Users.Child(user.UserId).Child("username").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            string username = null;
            if (!task.IsFaulted && !task.IsCanceled && task.Result.Value != null) username = task.Result.Value.ToString();
            if (string.IsNullOrEmpty(username)) username = user.DisplayName;
            if (string.IsNullOrEmpty(username)) username = user.Email.Split('@')[0];

            DatabaseReference onlineRef = FirebaseService.Database.RootReference.Child("users-online").Child(user.UserId);
            onlineRef.OnDisconnect().RemoveValue();
            onlineRef.SetValueAsync(username);
        });
    }
}
