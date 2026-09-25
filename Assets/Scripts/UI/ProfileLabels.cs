using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine;

// Muestra el nombre de usuario y el mejor puntaje, escuchando users/{uid} en tiempo real.
public class ProfileLabels : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _usernameLabel;
    [SerializeField]
    private TMP_Text _bestScoreLabel;

    private DatabaseReference _userReference;

    void OnEnable()
    {
        FirebaseService.WhenReady(Subscribe);
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (!isActiveAndEnabled || _userReference != null) return;

        FirebaseUser user = FirebaseService.Auth.CurrentUser;
        if (user == null) return;

        _usernameLabel.text = string.IsNullOrEmpty(user.DisplayName) ? user.Email : user.DisplayName;
        _bestScoreLabel.text = "-";

        _userReference = FirebaseService.Users.Child(user.UserId);
        _userReference.ValueChanged += HandleValueChanged;
    }

    private void Unsubscribe()
    {
        if (_userReference == null) return;
        _userReference.ValueChanged -= HandleValueChanged;
        _userReference = null;
    }

    private void HandleValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError("Error leyendo el perfil: " + args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;
        if (snapshot == null || !snapshot.Exists) return;

        string username = snapshot.Child("username").Value as string;
        if (!string.IsNullOrEmpty(username)) _usernameLabel.text = username;
        _bestScoreLabel.text = FirebaseService.ToLong(snapshot.Child("score").Value).ToString();
    }
}
