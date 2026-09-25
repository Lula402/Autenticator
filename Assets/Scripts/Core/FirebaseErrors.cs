using System;
using Firebase;
using Firebase.Auth;

// Traduce los errores de Firebase Auth a mensajes en español para mostrar en pantalla.
public static class FirebaseErrors
{
    public static string DescribeAuth(AggregateException exception)
    {
        if (exception != null)
        {
            foreach (Exception inner in exception.Flatten().InnerExceptions)
            {
                if (inner is FirebaseException firebaseException)
                {
                    return DescribeAuthError((AuthError)firebaseException.ErrorCode, firebaseException.Message);
                }
            }
        }
        return "Ocurrió un error inesperado. Intenta de nuevo.";
    }

    private static string DescribeAuthError(AuthError error, string fallback)
    {
        switch (error)
        {
            case AuthError.MissingEmail: return "Escribe tu correo electrónico.";
            case AuthError.InvalidEmail: return "El correo electrónico no es válido.";
            case AuthError.MissingPassword: return "Escribe tu contraseña.";
            case AuthError.WeakPassword: return "La contraseña debe tener al menos 6 caracteres.";
            case AuthError.EmailAlreadyInUse: return "Ya existe una cuenta con ese correo.";
            case AuthError.WrongPassword:
            case AuthError.UserNotFound:
            case AuthError.InvalidCredential: return "Correo o contraseña incorrectos.";
            case AuthError.UserDisabled: return "Esta cuenta está deshabilitada.";
            case AuthError.TooManyRequests: return "Demasiados intentos. Espera un momento e intenta de nuevo.";
            case AuthError.NetworkRequestFailed: return "Sin conexión. Revisa tu internet.";
            default: return "Error: " + fallback;
        }
    }
}
