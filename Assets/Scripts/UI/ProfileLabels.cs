using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine;

// Muestra el nombre de usuario y el mejor puntaje, escuchando users/{uid} en tiempo real.
public class ProfileLabels : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _greetingLabel;
    [SerializeField]
    private TMP_Text _bestScoreLabel;
    [SerializeField]
    private TMP_Text _detailsLabel;

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

        _greetingLabel.text = "Hola, " + (string.IsNullOrEmpty(user.DisplayName) ? user.Email : user.DisplayName);
        _bestScoreLabel.text = "Mejor puntaje: ...";
        if (_detailsLabel != null) _detailsLabel.text = user.Email;

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
        if (!string.IsNullOrEmpty(username)) _greetingLabel.text = "Hola, " + username;

        _bestScoreLabel.text = "Mejor puntaje: " + FirebaseService.ToLong(snapshot.Child("score").Value);

        if (_detailsLabel != null)
        {
            string city = snapshot.Child("ciudad").Value as string;
            string birthDate = snapshot.Child("fechaNacimiento").Value as string;
            FirebaseUser user = FirebaseService.Auth.CurrentUser;
            string email = user != null ? user.Email : "";
            _detailsLabel.text = $"{email}\n{city}  ·  Nació el {birthDate}";
        }
    }
}
