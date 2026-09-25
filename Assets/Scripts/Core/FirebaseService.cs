using System;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

// Inicializa Firebase una sola vez y da acceso a Auth y a la Realtime Database.
public class FirebaseService : MonoBehaviour
{
    // google-services.json se descargó antes de crear la base de datos, por eso no trae la URL.
    // Se copia de la consola de Firebase: Realtime Database > Datos (la URL de arriba).
    public const string DatabaseUrl = "https://sfd4-firebase-default-rtdb.firebaseio.com/";

    public static bool IsReady { get; private set; }

    private static event Action _onReady;

    public static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;

    public static FirebaseDatabase Database => DatabaseUrl.StartsWith("https://")
        ? FirebaseDatabase.GetInstance(DatabaseUrl)
        : FirebaseDatabase.DefaultInstance;

    // users/{uid}: perfil y mejor puntaje de cada jugador
    public static DatabaseReference Users => Database.RootReference.Child("users");

    // scores/{uid}/{pushId}: historial de partidas de cada jugador
    public static DatabaseReference Scores => Database.RootReference.Child("scores");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsReady = false;
        _onReady = null;
    }

    // Ejecuta la acción cuando Firebase esté listo (o de inmediato si ya lo está).
    public static void WhenReady(Action action)
    {
        if (IsReady)
        {
            action();
        }
        else
        {
            _onReady += action;
        }
    }

    void Awake()
    {
        if (!DatabaseUrl.StartsWith("https://"))
        {
            Debug.LogWarning("Falta la URL de la Realtime Database en FirebaseService.DatabaseUrl; se usará la de google-services.json.");
        }

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled || task.Result != DependencyStatus.Available)
            {
                string reason = task.IsFaulted ? task.Exception.ToString() : task.Result.ToString();
                Debug.LogError("No se pudo inicializar Firebase: " + reason);
                StatusMessage.Show("No se pudo conectar con Firebase.", true);
                return;
            }

            IsReady = true;
            Debug.Log("Firebase listo.");

            Action callbacks = _onReady;
            _onReady = null;
            callbacks?.Invoke();
        });
    }

    public static long ToLong(object value)
    {
        if (value == null) return 0;
        try
        {
            return Convert.ToInt64(value);
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
