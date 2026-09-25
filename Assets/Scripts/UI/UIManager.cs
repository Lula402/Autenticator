using UnityEngine;

public enum AppScreen
{
    Login,
    Register,
    ResetPassword,
    Home,
    Game,
    GameOver
}

// Muestra un solo panel a la vez.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField]
    private GameObject _loginPanel;
    [SerializeField]
    private GameObject _registerPanel;
    [SerializeField]
    private GameObject _resetPasswordPanel;
    [SerializeField]
    private GameObject _homePanel;
    [SerializeField]
    private GameObject _gamePanel;
    [SerializeField]
    private GameObject _gameOverPanel;

    [Header("Se oculta mientras se juega")]
    [SerializeField]
    private GameObject _footer;

    public AppScreen Current { get; private set; }

    void Awake()
    {
        Instance = this;
        Show(AppScreen.Login);
    }

    public void Show(AppScreen screen)
    {
        Current = screen;
        StatusMessage.Clear();

        SetActive(_loginPanel, screen == AppScreen.Login);
        SetActive(_registerPanel, screen == AppScreen.Register);
        SetActive(_resetPasswordPanel, screen == AppScreen.ResetPassword);
        SetActive(_homePanel, screen == AppScreen.Home);
        SetActive(_gamePanel, screen == AppScreen.Game);
        SetActive(_gameOverPanel, screen == AppScreen.GameOver);
        SetActive(_footer, screen != AppScreen.Game);
    }

    private static void SetActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}
