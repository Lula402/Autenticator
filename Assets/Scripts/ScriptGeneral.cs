using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class ScriptGeneral : MonoBehaviour
{
    private string Url = "http://127.0.0.1:1234/";
    private string token = "";
    private string username = "";

    [Header("Paneles UI")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject mainDashboardPanel;

    [Header("Inputs - Login")]
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;

    [Header("Inputs - Registro")]
    [SerializeField] private TMP_InputField registerUsernameInput;
    [SerializeField] private TMP_InputField registerPasswordInput;

    [Header("Inputs - Actualizar Data")]
    [SerializeField] private TMP_InputField updateScoreInput;
    [SerializeField] private TMP_InputField updateLevelInput;

    [Header("Textos UI - Perfil")]
    [SerializeField] private TMP_Text profileUsernameText;
    [SerializeField] private TMP_Text profileScoreText;
    [SerializeField] private TMP_Text profileLevelText;

    [Header("Textos UI - Leaderboard y Estado")]
    [SerializeField] private TMP_Text leaderboardText;
    [SerializeField] private TMP_Text statusText;

    void Start()
    {
        token = PlayerPrefs.GetString("token", "");
        username = PlayerPrefs.GetString("username", "");

        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username))
        {
            StartCoroutine(GetProfile());
        }
        else
        {
            ShowLoginPanel();
        }
    }

    #region Navegación entre Pantallas UI

    public void ShowLoginPanel()
    {
        SetStatus("");
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (mainDashboardPanel != null) mainDashboardPanel.SetActive(false);
    }

    public void ShowRegisterPanel()
    {
        SetStatus("");
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        if (mainDashboardPanel != null) mainDashboardPanel.SetActive(false);
    }

    public void ShowMainDashboard(UserData user)
    {
        SetStatus("");
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (mainDashboardPanel != null) mainDashboardPanel.SetActive(true);

        if (profileUsernameText != null) profileUsernameText.text = "Usuario: " + user.username;
        if (profileScoreText != null) profileScoreText.text = "Score: " + (user.data != null ? user.data.score.ToString() : "0");
        if (profileLevelText != null) profileLevelText.text = "Nivel: " + (user.data != null ? user.data.level.ToString() : "0");
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    #endregion

    #region Eventos de Botones

    public void OnLoginButtonClick()
    {
        StartCoroutine(Login(loginUsernameInput.text, loginPasswordInput.text));
    }

    public void OnRegisterButtonClick()
    {
        StartCoroutine(RegisterUser(registerUsernameInput.text, registerPasswordInput.text));
    }

    public void OnLogoutButtonClick()
    {
        token = "";
        username = "";
        PlayerPrefs.DeleteKey("token");
        PlayerPrefs.DeleteKey("username");
        ShowLoginPanel();
    }

    public void OnUpdateDataButtonClick()
    {
        int score = 0;
        int level = 0;

        int.TryParse(updateScoreInput.text, out score);
        int.TryParse(updateLevelInput.text, out level);

        StartCoroutine(UpdateData(score, level));
    }

    public void OnRefreshLeaderboardButtonClick()
    {
        StartCoroutine(GetUsersList());
    }

    #endregion

    #region Corrutinas de Red (API REST)

    // 1. Proceso de Registro (POST /api/usuarios)
    IEnumerator RegisterUser(string user, string pass)
    {
        AuthData authData = new AuthData { username = user, password = pass };
        string jsonData = JsonUtility.ToJson(authData);

        UnityWebRequest www = UnityWebRequest.Post(Url + "/api/usuarios", jsonData, "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error registro: " + www.downloadHandler.text);
            SetStatus("Error al registrar: usuario en uso o datos inválidos.");
        }
        else
        {
            SetStatus("Registro exitoso. Iniciando sesión...");
            StartCoroutine(Login(user, pass));
        }
    }

    // 2. Proceso de Autenticación (POST /api/auth/login)
    IEnumerator Login(string user, string pass)
    {
        AuthData authData = new AuthData { username = user, password = pass };
        string jsonData = JsonUtility.ToJson(authData);

        UnityWebRequest www = UnityWebRequest.Post(Url + "/api/auth/login", jsonData, "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error login: " + www.downloadHandler.text);
            SetStatus("Usuario o contraseña incorrectos.");
        }
        else
        {
            UserResponse userResponse = JsonUtility.FromJson<UserResponse>(www.downloadHandler.text);

            token = userResponse.token;
            username = userResponse.usuario.username;

            PlayerPrefs.SetString("token", token);
            PlayerPrefs.SetString("username", username);

            ShowMainDashboard(userResponse.usuario);
            StartCoroutine(GetUsersList()); // Carga el leaderboard automáticamente
        }
    }

    // 3. Proceso Obtener Perfil (GET /api/usuarios/{username})
    IEnumerator GetProfile()
    {
        UnityWebRequest www = UnityWebRequest.Get(Url + "/api/usuarios/" + username);
        www.SetRequestHeader("x-token", token);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Token expirado o error: " + www.downloadHandler.text);
            OnLogoutButtonClick();
        }
        else
        {
            UserResponse userResponse = JsonUtility.FromJson<UserResponse>(www.downloadHandler.text);
            ShowMainDashboard(userResponse.usuario);
            StartCoroutine(GetUsersList());
        }
    }

    // 4. Proceso Actualizar Data (PATCH /api/usuarios)
    IEnumerator UpdateData(int score, int level)
    {
        UpdateUserData updateData = new UpdateUserData
        {
            username = this.username,
            data = new GameData { score = score, level = level }
        };

        string jsonData = JsonUtility.ToJson(updateData);

        UnityWebRequest www = new UnityWebRequest(Url + "/api/usuarios", "PATCH");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("x-token", token);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error al actualizar: " + www.downloadHandler.text);
            SetStatus("No se pudo actualizar la data.");
        }
        else
        {
            UserResponse userResponse = JsonUtility.FromJson<UserResponse>(www.downloadHandler.text);
            ShowMainDashboard(userResponse.usuario);
            StartCoroutine(GetUsersList()); // Refresca la lista global
        }
    }

    // 5. Proceso Listar Todos los Usuarios (GET /api/usuarios)
   IEnumerator GetUsersList()
{
    UnityWebRequest www = UnityWebRequest.Get(Url + "/api/usuarios");
    www.SetRequestHeader("x-token", token);

    yield return www.SendWebRequest();

    if (www.result != UnityWebRequest.Result.Success)
    {
        Debug.Log("Error al listar: " + www.downloadHandler.text);
        if (leaderboardText != null) leaderboardText.text = "Error al cargar ranking.";
    }
    else
    {
        string rawJson = www.downloadHandler.text;
        Debug.Log("JSON recibido: " + rawJson);

        // Deserializa usando directamente la clase contenedora de la API
        UsersListResponse response = JsonUtility.FromJson<UsersListResponse>(rawJson);

        if (response != null && response.usuarios != null && leaderboardText != null)
        {
            StringBuilder sb = new StringBuilder();

            foreach (UserData user in response.usuarios)
            {
                if (user == null) continue;

                int s = (user.data != null) ? user.data.score : 0;
                int l = (user.data != null) ? user.data.level : 0;
                string uName = string.IsNullOrEmpty(user.username) ? "Anónimo" : user.username;

                sb.AppendLine($"• {uName} | Nvl: {l} | Pts: {s}");
            }

            leaderboardText.text = sb.ToString();
        }
    }
}

    #endregion
}

#region Modelos de Datos y Wrappers

[System.Serializable]
public class AuthData
{
    public string username;
    public string password;
}

[System.Serializable]
public class GameData
{
    public int score;
    public int level;
}

[System.Serializable]
public class UserData
{
    public string _id;
    public string username;
    public string password;
    public bool estado;
    public GameData data;
}

[System.Serializable]
public class UserResponse
{
    public UserData usuario;
    public string token;
}

[System.Serializable]
public class UpdateUserData
{
    public string username;
    public GameData data;
}

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{\"usuarios\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.usuarios;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] usuarios;
    }
}

[System.Serializable]
public class UsersListResponse
{
    public UserData[] usuarios;
}

#endregion