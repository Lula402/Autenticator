using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú "SID2 > Construir escena Firebase": crea toda la interfaz (login, registro,
// recuperar contraseña, menú, leaderboard, juego y game over) en la escena abierta
// y conecta las referencias de los scripts. Se puede ajustar el diseño a mano después.
public static class FirebaseSceneBuilder
{
    private const string CanvasName = "FirebaseUI";
    private const string ServicesName = "FirebaseServices";
    private const string GameName = "Game";

    private static readonly Color BackgroundColor = new Color32(18, 14, 28, 255);
    private static readonly Color CardColor = new Color32(34, 27, 52, 255);
    private static readonly Color InputColor = new Color32(50, 41, 74, 255);
    private static readonly Color PrimaryColor = new Color32(34, 170, 90, 255);
    private static readonly Color SecondaryColor = new Color32(72, 58, 104, 255);
    private static readonly Color DangerColor = new Color32(185, 60, 60, 255);
    private static readonly Color TextColor = new Color32(235, 240, 250, 255);
    private static readonly Color MutedTextColor = new Color32(150, 160, 180, 255);

    [MenuItem("SID2/Construir escena Firebase")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();

        GameObject existing = FindRoot(scene, CanvasName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Construir escena Firebase",
                "Ya existe una interfaz de Firebase en la escena. ¿Quieres reemplazarla?", "Reemplazar", "Cancelar"))
            {
                return;
            }
            Object.DestroyImmediate(existing);
            DestroyRoot(scene, ServicesName);
            DestroyRoot(scene, GameName);
        }

        DisableOldUI(scene);
        EnsureEventSystem(scene);

        // ---------- Servicios y juego ----------
        var services = new GameObject(ServicesName);
        services.AddComponent<FirebaseService>();
        services.AddComponent<AuthStateHandler>();
        Undo.RegisterCreatedObjectUndo(services, "Construir escena Firebase");

        var game = new GameObject(GameName);
        SnakeGameManager gameManager = game.AddComponent<SnakeGameManager>();
        Undo.RegisterCreatedObjectUndo(game, "Construir escena Firebase");

        // ---------- Canvas ----------
        var canvasObject = new GameObject(CanvasName, typeof(RectTransform));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasObject, "Construir escena Firebase");
        Transform root = canvasObject.transform;

        UIManager uiManager = canvasObject.AddComponent<UIManager>();

        GameObject loginPanel = BuildLoginPanel(root);
        GameObject registerPanel = BuildRegisterPanel(root);
        GameObject resetPanel = BuildResetPasswordPanel(root);
        GameObject homePanel = BuildHomePanel(root);
        GameObject gamePanel = BuildGamePanel(root, gameManager);
        GameObject gameOverPanel = BuildGameOverPanel(root, gameManager);

        Set(uiManager, "_loginPanel", loginPanel);
        Set(uiManager, "_registerPanel", registerPanel);
        Set(uiManager, "_resetPasswordPanel", resetPanel);
        Set(uiManager, "_homePanel", homePanel);
        Set(uiManager, "_gamePanel", gamePanel);
        Set(uiManager, "_gameOverPanel", gameOverPanel);

        // Mensajes de estado (abajo, encima del pie de página)
        TMP_Text status = CreateText(root, "StatusMessage", "", 26, TextAlignmentOptions.Center, TextColor);
        Anchor(status.rectTransform, new Vector2(0.1f, 0f), new Vector2(0.9f, 0f), new Vector2(0, 70), new Vector2(0, 130));
        var statusMessage = status.gameObject.AddComponent<StatusMessage>();
        Set(statusMessage, "_label", status);

        // Pie de página con el nombre completo
        var footerBar = CreateImage(root, "Footer", new Color(0f, 0f, 0f, 0.55f));
        Anchor(footerBar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0, 0), new Vector2(0, 56));
        footerBar.raycastTarget = false;
        TMP_Text footerText = CreateText(footerBar.transform, "AuthorFooter", ProjectInfo.AuthorFullName, 24, TextAlignmentOptions.Center, TextColor);
        Stretch(footerText.rectTransform);
        var footer = footerText.gameObject.AddComponent<AuthorFooter>();
        Set(footer, "_label", footerText);

        // Deja solo el login visible en el editor
        registerPanel.SetActive(false);
        resetPanel.SetActive(false);
        homePanel.SetActive(false);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = canvasObject;
        Debug.Log("Escena Firebase construida. Guarda la escena (Ctrl+S) y dale Play.");
    }

    // ================= Paneles =================

    private static GameObject BuildLoginPanel(Transform root)
    {
        GameObject panel = CreatePanel(root, "LoginPanel");
        Transform card = CreateCard(panel.transform, "Card", 720);

        CreateTitle(card, ProjectInfo.GameName);
        CreateSubtitle(card, "Inicia sesión para jugar Snake y guardar tus puntajes");
        TMP_InputField email = CreateInput(card, "EmailField", "Correo electrónico", TMP_InputField.ContentType.EmailAddress);
        TMP_InputField password = CreateInput(card, "PasswordField", "Contraseña", TMP_InputField.ContentType.Password);

        Button login = CreateButton(card, "LoginButton", "Iniciar sesión", PrimaryColor);
        var buttonLogin = login.gameObject.AddComponent<ButtonLogin>();
        Set(buttonLogin, "_loginButton", login);
        Set(buttonLogin, "_emailInputField", email);
        Set(buttonLogin, "_passwordInputField", password);

        CreateNavigationButton(card, "ForgotPasswordButton", "¿Olvidaste tu contraseña?", SecondaryColor, AppScreen.ResetPassword);
        CreateNavigationButton(card, "GoToRegisterButton", "Crear una cuenta", SecondaryColor, AppScreen.Register);
        return panel;
    }

    private static GameObject BuildRegisterPanel(Transform root)
    {
        GameObject panel = CreatePanel(root, "RegisterPanel");
        Transform card = CreateCard(panel.transform, "Card", 820);

        CreateTitle(card, "Crear cuenta");
        TMP_InputField username = CreateInput(card, "UsernameField", "Nombre de usuario", TMP_InputField.ContentType.Alphanumeric);
        username.characterLimit = 16;
        TMP_InputField email = CreateInput(card, "EmailField", "Correo electrónico", TMP_InputField.ContentType.EmailAddress);
        TMP_InputField password = CreateInput(card, "PasswordField", "Contraseña (mínimo 6 caracteres)", TMP_InputField.ContentType.Password);
        TMP_InputField confirm = CreateInput(card, "ConfirmPasswordField", "Confirmar contraseña", TMP_InputField.ContentType.Password);

        Transform extraRow = CreateRow(card, "DatosAdicionales", 64);
        TMP_InputField city = CreateInput(extraRow, "CityField", "Ciudad", TMP_InputField.ContentType.Standard);
        TMP_InputField birthDate = CreateInput(extraRow, "BirthDateField", "Nacimiento (dd/mm/aaaa)", TMP_InputField.ContentType.Standard);
        birthDate.characterLimit = 10;

        Button register = CreateButton(card, "RegisterButton", "Registrarme", PrimaryColor);
        var buttonRegister = register.gameObject.AddComponent<ButtonRegister>();
        Set(buttonRegister, "_registerButton", register);
        Set(buttonRegister, "_usernameInputField", username);
        Set(buttonRegister, "_emailInputField", email);
        Set(buttonRegister, "_passwordInputField", password);
        Set(buttonRegister, "_confirmPasswordInputField", confirm);
        Set(buttonRegister, "_cityInputField", city);
        Set(buttonRegister, "_birthDateInputField", birthDate);

        CreateNavigationButton(card, "BackToLoginButton", "Ya tengo cuenta", SecondaryColor, AppScreen.Login);
        return panel;
    }

    private static GameObject BuildResetPasswordPanel(Transform root)
    {
        GameObject panel = CreatePanel(root, "ResetPasswordPanel");
        Transform card = CreateCard(panel.transform, "Card", 720);

        CreateTitle(card, "Recuperar contraseña");
        CreateSubtitle(card, "Escribe tu correo y te enviaremos un enlace para crear una contraseña nueva");
        TMP_InputField email = CreateInput(card, "EmailField", "Correo electrónico", TMP_InputField.ContentType.EmailAddress);

        Button send = CreateButton(card, "SendResetButton", "Enviar enlace", PrimaryColor);
        var buttonReset = send.gameObject.AddComponent<ButtonResetPassword>();
        Set(buttonReset, "_resetButton", send);
        Set(buttonReset, "_emailInputField", email);

        CreateNavigationButton(card, "BackToLoginButton", "Volver", SecondaryColor, AppScreen.Login);
        return panel;
    }

    private static GameObject BuildHomePanel(Transform root)
    {
        GameObject panel = CreatePanel(root, "HomePanel");

        var columns = CreateUIObject(panel.transform, "Columns");
        var columnsRect = columns.GetComponent<RectTransform>();
        columnsRect.anchorMin = columnsRect.anchorMax = new Vector2(0.5f, 0.5f);
        columnsRect.sizeDelta = new Vector2(1500, 760);
        columnsRect.anchoredPosition = new Vector2(0, 40);
        var columnsLayout = columns.AddComponent<HorizontalLayoutGroup>();
        columnsLayout.spacing = 40;
        columnsLayout.childControlWidth = true;
        columnsLayout.childControlHeight = true;
        columnsLayout.childForceExpandWidth = false;
        columnsLayout.childForceExpandHeight = true;

        // Columna izquierda: perfil y acciones
        Transform profile = CreateColumnCard(columns.transform, "ProfileCard", 600);
        CreateTitle(profile, ProjectInfo.GameName);
        TMP_Text greeting = CreateText(profile, "Greeting", "Hola", 40, TextAlignmentOptions.Center, TextColor, 56);
        greeting.fontStyle = FontStyles.Bold;
        TMP_Text details = CreateText(profile, "Details", "", 24, TextAlignmentOptions.Center, MutedTextColor, 70);
        TMP_Text best = CreateText(profile, "BestScore", "Mejor puntaje: ...", 32, TextAlignmentOptions.Center, new Color32(255, 210, 80, 255), 50);

        var profileLabels = profile.gameObject.AddComponent<ProfileLabels>();
        Set(profileLabels, "_greetingLabel", greeting);
        Set(profileLabels, "_bestScoreLabel", best);
        Set(profileLabels, "_detailsLabel", details);

        CreateText(profile, "HowToPlay",
            "Come la comida <color=#FF5A66>roja</color> (+10) y la <color=#FFD133>dorada</color> (+30, desaparece rápido).\n" +
            "No choques con los bordes ni contigo misma. Cada comida te hace más rápida.\n" +
            "Muévete con las flechas / WASD o deslizando el dedo.",
            22, TextAlignmentOptions.Center, MutedTextColor, 110);

        Button play = CreateButton(profile, "PlayButton", "Jugar", PrimaryColor);
        AddGameButton(play, GameButtonAction.StartGame);

        Button logout = CreateButton(profile, "LogoutButton", "Cerrar sesión", DangerColor);
        var buttonLogout = logout.gameObject.AddComponent<ButtonLogout>();
        Set(buttonLogout, "_logoutButton", logout);

        // Columna derecha: tabla de puntajes
        Transform board = CreateColumnCard(columns.transform, "LeaderboardCard", 860);
        CreateTitle(board, "Tabla de puntajes");
        CreateSubtitle(board, "Top 10 · se actualiza en tiempo real");

        GameObject header = CreateLeaderboardRow(board, "Header", "#", "Jugador", "Puntaje", 26, MutedTextColor);
        header.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        var rows = CreateUIObject(board, "Rows");
        var rowsLayout = rows.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 4;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;
        rows.AddComponent<LayoutElement>().flexibleHeight = 1;

        GameObject template = CreateLeaderboardRow(rows.transform, "RowTemplate", "#1", "Jugador", "0", 28, TextColor);
        template.SetActive(false);

        TMP_Text empty = CreateText(rows.transform, "EmptyLabel", "Cargando puntajes...", 26, TextAlignmentOptions.Center, MutedTextColor, 60);

        var leaderboard = board.gameObject.AddComponent<Leaderboard>();
        Set(leaderboard, "_rowsContainer", rows.transform);
        Set(leaderboard, "_rowTemplate", template);
        Set(leaderboard, "_emptyLabel", empty);

        return panel;
    }

    private static GameObject BuildGamePanel(Transform root, SnakeGameManager gameManager)
    {
        // Panel transparente: el juego se ve detrás (mundo 2D).
        GameObject panel = CreatePanel(root, "GamePanel");
        Image background = panel.GetComponent<Image>();
        background.color = new Color(0, 0, 0, 0);
        background.raycastTarget = false;

        var topBar = CreateImage(panel.transform, "TopBar", new Color(0f, 0f, 0f, 0.45f));
        Anchor(topBar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -90), new Vector2(0, 0));

        TMP_Text score = CreateText(topBar.transform, "ScoreLabel", "Puntaje: 0", 40, TextAlignmentOptions.Left, TextColor);
        score.fontStyle = FontStyles.Bold;
        Anchor(score.rectTransform, new Vector2(0f, 0f), new Vector2(0.4f, 1f), new Vector2(40, 0), new Vector2(0, 0));

        TMP_Text length = CreateText(topBar.transform, "LengthLabel", "Longitud: 3", 40, TextAlignmentOptions.Right, new Color32(140, 255, 140, 255));
        length.fontStyle = FontStyles.Bold;
        Anchor(length.rectTransform, new Vector2(0.6f, 0f), new Vector2(1f, 1f), new Vector2(0, 0), new Vector2(-40, 0));

        Button end = CreateButton(topBar.transform, "EndGameButton", "Terminar", SecondaryColor);
        Object.DestroyImmediate(end.GetComponent<LayoutElement>());
        var endRect = end.GetComponent<RectTransform>();
        endRect.anchorMin = endRect.anchorMax = new Vector2(0.5f, 0.5f);
        endRect.sizeDelta = new Vector2(220, 60);
        AddGameButton(end, GameButtonAction.EndGame);

        Set(gameManager, "_scoreLabel", score);
        Set(gameManager, "_lengthLabel", length);
        return panel;
    }

    private static GameObject BuildGameOverPanel(Transform root, SnakeGameManager gameManager)
    {
        GameObject panel = CreatePanel(root, "GameOverPanel");
        panel.GetComponent<Image>().color = new Color(BackgroundColor.r, BackgroundColor.g, BackgroundColor.b, 0.85f);
        Transform card = CreateCard(panel.transform, "Card", 640);

        CreateTitle(card, "¡Fin del juego!");
        CreateSubtitle(card, "Tu puntaje");
        TMP_Text finalScore = CreateText(card, "FinalScore", "0", 96, TextAlignmentOptions.Center, new Color32(255, 210, 80, 255), 120);
        finalScore.fontStyle = FontStyles.Bold;
        TMP_Text record = CreateText(card, "RecordLabel", "", 30, TextAlignmentOptions.Center, TextColor, 50);

        Button again = CreateButton(card, "PlayAgainButton", "Jugar de nuevo", PrimaryColor);
        AddGameButton(again, GameButtonAction.StartGame);
        CreateNavigationButton(card, "MenuButton", "Menú y puntajes", SecondaryColor, AppScreen.Home);

        Set(gameManager, "_finalScoreLabel", finalScore);
        Set(gameManager, "_recordLabel", record);
        return panel;
    }

    // ================= Helpers de UI =================

    private static GameObject CreateUIObject(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        Image image = CreateImage(parent, name, BackgroundColor);
        Stretch(image.rectTransform);
        return image.gameObject;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = CreateUIObject(parent, name);
        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Transform CreateCard(Transform parent, string name, float width)
    {
        Image image = CreateImage(parent, name, CardColor);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, 0);
        rect.anchoredPosition = new Vector2(0, 30);

        AddVerticalLayout(image.gameObject);
        var fitter = image.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return image.transform;
    }

    private static Transform CreateColumnCard(Transform parent, string name, float width)
    {
        Image image = CreateImage(parent, name, CardColor);
        AddVerticalLayout(image.gameObject);
        var layout = image.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        return image.transform;
    }

    private static void AddVerticalLayout(GameObject go)
    {
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 40, 40);
        layout.spacing = 18;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static Transform CreateRow(Transform parent, string name, float height)
    {
        GameObject row = CreateUIObject(parent, name);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        row.AddComponent<LayoutElement>().preferredHeight = height;
        return row.transform;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float size,
        TextAlignmentOptions alignment, Color color, float preferredHeight = -1)
    {
        GameObject go = CreateUIObject(parent, name);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        if (preferredHeight > 0) go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        return label;
    }

    private static void CreateTitle(Transform parent, string text)
    {
        TMP_Text title = CreateText(parent, "Title", text, 54, TextAlignmentOptions.Center, TextColor, 72);
        title.fontStyle = FontStyles.Bold;
    }

    private static void CreateSubtitle(Transform parent, string text)
    {
        CreateText(parent, "Subtitle", text, 26, TextAlignmentOptions.Center, MutedTextColor, 64);
    }

    private static TMP_InputField CreateInput(Transform parent, string name, string placeholder, TMP_InputField.ContentType contentType)
    {
        GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        go.name = name;
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        go.GetComponent<Image>().color = InputColor;
        var input = go.GetComponent<TMP_InputField>();
        input.contentType = contentType;
        input.pointSize = 30;
        input.textComponent.color = TextColor;
        input.caretColor = TextColor;
        input.customCaretColor = true;

        if (input.placeholder is TMP_Text placeholderText)
        {
            placeholderText.text = placeholder;
            placeholderText.color = MutedTextColor;
            placeholderText.fontStyle = FontStyles.Normal;
        }

        go.AddComponent<LayoutElement>().preferredHeight = 64;
        return input;
    }

    private static Button CreateButton(Transform parent, string name, string text, Color color)
    {
        GameObject go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
        go.name = name;
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        go.GetComponent<Image>().color = color;
        var label = go.GetComponentInChildren<TMP_Text>();
        label.text = text;
        label.fontSize = 30;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;

        go.AddComponent<LayoutElement>().preferredHeight = 68;
        return go.GetComponent<Button>();
    }

    private static void CreateNavigationButton(Transform parent, string name, string text, Color color, AppScreen target)
    {
        Button button = CreateButton(parent, name, text, color);
        var navigation = button.gameObject.AddComponent<NavigationButton>();
        Set(navigation, "_button", button);
        SetEnum(navigation, "_target", (int)target);
    }

    private static void AddGameButton(Button button, GameButtonAction action)
    {
        var gameButton = button.gameObject.AddComponent<GameButton>();
        Set(gameButton, "_button", button);
        SetEnum(gameButton, "_action", (int)action);
    }

    private static GameObject CreateLeaderboardRow(Transform parent, string name, string rank, string player, string score, float size, Color color)
    {
        Image background = CreateImage(parent, name, new Color(1f, 1f, 1f, 0.04f));
        var layout = background.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 0, 0);
        layout.spacing = 12;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        background.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

        TMP_Text rankText = CreateText(background.transform, "Rank", rank, size, TextAlignmentOptions.Left, color);
        rankText.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;

        TMP_Text nameText = CreateText(background.transform, "Name", player, size, TextAlignmentOptions.Left, color);
        nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        TMP_Text scoreText = CreateText(background.transform, "Score", score, size, TextAlignmentOptions.Right, color);
        scoreText.gameObject.AddComponent<LayoutElement>().preferredWidth = 180;

        return background.gameObject;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    // ================= Helpers de escena =================

    private static void Set(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"No se encontró el campo {propertyName} en {target.GetType().Name}");
            return;
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string propertyName, int value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"No se encontró el campo {propertyName} en {target.GetType().Name}");
            return;
        }
        property.enumValueIndex = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == name) return go;
        }
        return null;
    }

    private static void DestroyRoot(Scene scene, string name)
    {
        GameObject go = FindRoot(scene, name);
        if (go != null) Object.DestroyImmediate(go);
    }

    // Apaga (sin borrar) los Canvas anteriores y el script de la actividad REST anterior.
    private static void DisableOldUI(Scene scene)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == CanvasName) continue;

            if (go.GetComponent<Canvas>() != null && go.activeSelf)
            {
                Undo.RecordObject(go, "Apagar UI anterior");
                go.SetActive(false);
                Debug.Log("Se apagó el Canvas anterior: " + go.name + " (puedes volver a activarlo si lo necesitas).");
            }

            foreach (MonoBehaviour behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null && (behaviour.GetType().Name == "Script" || behaviour.GetType().Name == "ScriptGeneral") && behaviour.enabled)
                {
                    Undo.RecordObject(behaviour, "Apagar script REST");
                    behaviour.enabled = false;
                }
            }
        }
    }

    private static void EnsureEventSystem(Scene scene)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            EventSystem eventSystem = go.GetComponentInChildren<EventSystem>(true);
            if (eventSystem != null)
            {
                eventSystem.gameObject.SetActive(true);
                return;
            }
        }

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystemObject, "Crear EventSystem");
    }
}
