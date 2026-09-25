using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú "SID2 > Construir escena Firebase" (estilo retro 70s/90s): crea toda la interfaz (login, registro,
// recuperar contraseña, menú, leaderboard, juego y game over) en la escena abierta
// y conecta las referencias de los scripts. Se puede ajustar el diseño a mano después.
public static class FirebaseSceneBuilder
{
    private const string CanvasName = "FirebaseUI";
    private const string ServicesName = "FirebaseServices";
    private const string GameName = "Game";

    // ================= Paleta retro =================

    private static readonly Color Cream = new Color32(242, 238, 220, 255);
    private static readonly Color Paper = new Color32(251, 249, 240, 255);
    private static readonly Color Ink = new Color32(27, 27, 27, 255);
    private static readonly Color Muted = new Color32(128, 118, 100, 255);

    private static readonly Color Teal = new Color32(0, 164, 153, 255);
    private static readonly Color Sky = new Color32(16, 167, 224, 255);
    private static readonly Color Navy = new Color32(41, 58, 122, 255);
    private static readonly Color Purple = new Color32(107, 42, 138, 255);
    private static readonly Color Magenta = new Color32(229, 40, 126, 255);
    private static readonly Color Red = new Color32(230, 50, 39, 255);
    private static readonly Color Orange = new Color32(240, 138, 36, 255);
    private static readonly Color Amber = new Color32(247, 167, 27, 255);
    private static readonly Color Yellow = new Color32(255, 213, 0, 255);

    // Orden del arcoíris de las referencias (de arriba hacia abajo)
    private static readonly Color[] Rainbow = { Teal, Sky, Navy, Purple, Magenta, Red, Orange, Amber, Yellow };
    // Franjas diagonales tipo casete VHS
    private static readonly Color[] VhsStripes = { Yellow, Orange, Red, Magenta, Purple };
    // Sombra escalonada de los títulos (tipo póster "Hey")
    private static readonly Color[] TitleShadow = { Teal, Sky, Purple, Magenta, Red, Orange, Yellow };

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
        StyleCamera();

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

        // Mensajes de estado (abajo, encima del pie de página)
        TMP_Text status = CreateText(root, "StatusMessage", "", 26, TextAlignmentOptions.Center, Ink);
        status.fontStyle = FontStyles.Bold;
        Anchor(status.rectTransform, new Vector2(0.1f, 0f), new Vector2(0.9f, 0f), new Vector2(0, 60), new Vector2(0, 110));
        var statusMessage = status.gameObject.AddComponent<StatusMessage>();
        Set(statusMessage, "_label", status);

        // Pie de página con el nombre completo (se oculta mientras se juega)
        TMP_Text footerText = CreateText(root, "AuthorFooter", ProjectInfo.AuthorFullName, 22, TextAlignmentOptions.Center, Ink);
        footerText.characterSpacing = 8;
        Anchor(footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0, 14), new Vector2(0, 50));
        var footer = footerText.gameObject.AddComponent<AuthorFooter>();
        Set(footer, "_label", footerText);

        Set(uiManager, "_loginPanel", loginPanel);
        Set(uiManager, "_registerPanel", registerPanel);
        Set(uiManager, "_resetPasswordPanel", resetPanel);
        Set(uiManager, "_homePanel", homePanel);
        Set(uiManager, "_gamePanel", gamePanel);
        Set(uiManager, "_gameOverPanel", gameOverPanel);
        Set(uiManager, "_footer", footerText.gameObject);

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
        GameObject panel = CreatePanel(root, "LoginPanel", true);
        Transform card = CreateCard(panel.transform, "Card", 620);

        CreateRetroTitle(card, ProjectInfo.GameName.ToLower(), 120);
        TMP_InputField email = CreateInput(card, "EmailField", "correo", TMP_InputField.ContentType.EmailAddress);
        TMP_InputField password = CreateInput(card, "PasswordField", "contraseña", TMP_InputField.ContentType.Password);

        Button login = CreatePrimaryButton(card, "LoginButton", "ENTRAR");
        var buttonLogin = login.gameObject.AddComponent<ButtonLogin>();
        Set(buttonLogin, "_loginButton", login);
        Set(buttonLogin, "_emailInputField", email);
        Set(buttonLogin, "_passwordInputField", password);

        CreateNavigationButton(card, "GoToRegisterButton", "CREAR CUENTA", AppScreen.Register);
        CreateNavigationButton(card, "ForgotPasswordButton", "olvidé mi contraseña", AppScreen.ResetPassword, true);
        return panel;
    }

    private static GameObject BuildRegisterPanel(Transform root)
    {
        GameObject panel = CreatePanel(root, "RegisterPanel", true);
        Transform card = CreateCard(panel.transform, "Card", 620);

        CreateRetroTitle(card, "registro", 100);
        TMP_InputField username = CreateInput(card, "UsernameField", "usuario", TMP_InputField.ContentType.Alphanumeric);
        username.characterLimit = 16;
        TMP_InputField email = CreateInput(card, "EmailField", "correo", TMP_InputField.ContentType.EmailAddress);
        TMP_InputField password = CreateInput(card, "PasswordField", "contraseña", TMP_InputField.ContentType.Password);

        Button register = CreatePrimaryButton(card, "RegisterButton", "REGISTRARME");
        var buttonRegister = register.gameObject.AddComponent<ButtonRegister>();
        Set(buttonRegister, "_registerButton", register);
        Set(buttonRegister, "_usernameInputField", username);
        Set(buttonRegister, "_emailInputField", email);
        Set(buttonRegister, "_passwordInputField", password);

        CreateNavigationButton(card, "BackToLoginButton", "VOLVER", AppScreen.Login);
        return panel;
    }

    private static GameObject BuildResetPasswordPanel(Transform root)
    {
        GameObject panel = CreatePanel(root, "ResetPasswordPanel", true);
        Transform card = CreateCard(panel.transform, "Card", 620);

        CreateRetroTitle(card, "recuperar", 100);
        TMP_InputField email = CreateInput(card, "EmailField", "correo", TMP_InputField.ContentType.EmailAddress);

        Button send = CreatePrimaryButton(card, "SendResetButton", "ENVIAR");
        var buttonReset = send.gameObject.AddComponent<ButtonResetPassword>();
        Set(buttonReset, "_resetButton", send);
        Set(buttonReset, "_emailInputField", email);

        CreateNavigationButton(card, "BackToLoginButton", "VOLVER", AppScreen.Login);
        return panel;
    }

    private static GameObject BuildHomePanel(Transform root)
    {
        GameObject panel = CreatePanel(root, "HomePanel", false);

        var columns = CreateUIObject(panel.transform, "Columns");
        var columnsRect = columns.GetComponent<RectTransform>();
        columnsRect.anchorMin = columnsRect.anchorMax = new Vector2(0.5f, 0.5f);
        columnsRect.sizeDelta = new Vector2(1440, 760);
        columnsRect.anchoredPosition = new Vector2(0, 20);
        var columnsLayout = columns.AddComponent<HorizontalLayoutGroup>();
        columnsLayout.spacing = 60;
        columnsLayout.childControlWidth = true;
        columnsLayout.childControlHeight = true;
        columnsLayout.childForceExpandWidth = false;
        columnsLayout.childForceExpandHeight = true;

        // Columna izquierda: título, usuario, récord y acciones (sin tarjeta, sobre el fondo)
        Transform profile = CreateColumn(columns.transform, "ProfileColumn", 560, false);
        CreateRetroTitle(profile, ProjectInfo.GameName.ToLower(), 150, TextAlignmentOptions.Left);

        TMP_Text username = CreateText(profile, "Username", "", 44, TextAlignmentOptions.Left, Ink, 60);
        username.fontStyle = FontStyles.Bold;

        TMP_Text best = CreateText(profile, "BestScore", "-", 110, TextAlignmentOptions.Left, Ink, 130);
        best.fontStyle = FontStyles.Bold;
        TMP_Text caption = CreateText(profile, "BestScoreCaption", "RÉCORD", 22, TextAlignmentOptions.Left, Muted, 30);
        caption.characterSpacing = 10;

        var spacer = CreateUIObject(profile, "Spacer");
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1;

        var profileLabels = profile.gameObject.AddComponent<ProfileLabels>();
        Set(profileLabels, "_usernameLabel", username);
        Set(profileLabels, "_bestScoreLabel", best);

        Button play = CreatePrimaryButton(profile, "PlayButton", "JUGAR");
        AddGameButton(play, GameButtonAction.StartGame);

        Button logout = CreateSecondaryButton(profile, "LogoutButton", "SALIR");
        var buttonLogout = logout.gameObject.AddComponent<ButtonLogout>();
        Set(buttonLogout, "_logoutButton", logout);

        // Columna derecha: tabla de puntajes en tarjeta negra con franja arcoíris
        Transform board = CreateColumn(columns.transform, "LeaderboardCard", 820, true);
        CreateStripeBar(board, "RainbowBar", Rainbow, 12);
        TMP_Text boardTitle = CreateText(board, "Title", "TOP 10", 44, TextAlignmentOptions.Left, Cream, 64);
        boardTitle.fontStyle = FontStyles.Bold;
        boardTitle.characterSpacing = 12;

        var rows = CreateUIObject(board, "Rows");
        var rowsLayout = rows.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 4;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;
        rows.AddComponent<LayoutElement>().flexibleHeight = 1;

        GameObject template = CreateLeaderboardRow(rows.transform, "RowTemplate", "01", "usuario", "0", 30, Cream);
        template.SetActive(false);

        TMP_Text empty = CreateText(rows.transform, "EmptyLabel", "...", 26, TextAlignmentOptions.Center, Cream, 60);

        var leaderboard = board.gameObject.AddComponent<Leaderboard>();
        Set(leaderboard, "_rowsContainer", rows.transform);
        Set(leaderboard, "_rowTemplate", template);
        Set(leaderboard, "_emptyLabel", empty);

        return panel;
    }

    private static GameObject BuildGamePanel(Transform root, SnakeGameManager gameManager)
    {
        // Panel transparente: el juego se ve detrás (mundo 2D).
        GameObject panel = CreateUIObject(root, "GamePanel");
        Stretch(panel.GetComponent<RectTransform>());

        var topBar = CreateImage(panel.transform, "TopBar", Ink);
        Anchor(topBar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -90), new Vector2(0, 0));

        Transform stripe = CreateStripeBar(panel.transform, "RainbowBar", Rainbow, 10);
        Anchor((RectTransform)stripe, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -100), new Vector2(0, -90));

        TMP_Text score = CreateText(topBar.transform, "ScoreLabel", "Puntaje: 0", 38, TextAlignmentOptions.Left, Cream);
        score.fontStyle = FontStyles.Bold;
        Anchor(score.rectTransform, new Vector2(0f, 0f), new Vector2(0.4f, 1f), new Vector2(40, 0), new Vector2(0, 0));

        TMP_Text length = CreateText(topBar.transform, "LengthLabel", "Longitud: 3", 38, TextAlignmentOptions.Right, Yellow);
        length.fontStyle = FontStyles.Bold;
        Anchor(length.rectTransform, new Vector2(0.6f, 0f), new Vector2(1f, 1f), new Vector2(0, 0), new Vector2(-40, 0));

        Button end = CreateButton(topBar.transform, "EndGameButton", "SALIR", Cream, Ink, 26);
        Object.DestroyImmediate(end.GetComponent<LayoutElement>());
        var endRect = end.GetComponent<RectTransform>();
        endRect.anchorMin = endRect.anchorMax = new Vector2(0.5f, 0.5f);
        endRect.sizeDelta = new Vector2(200, 56);
        AddGameButton(end, GameButtonAction.EndGame);

        Set(gameManager, "_scoreLabel", score);
        Set(gameManager, "_lengthLabel", length);
        return panel;
    }

    private static GameObject BuildGameOverPanel(Transform root, SnakeGameManager gameManager)
    {
        GameObject panel = CreatePanel(root, "GameOverPanel", true);
        panel.GetComponent<Image>().color = new Color(Cream.r, Cream.g, Cream.b, 0.94f);
        Transform card = CreateCard(panel.transform, "Card", 620);

        CreateRetroTitle(card, "fin", 120);
        TMP_Text finalScore = CreateText(card, "FinalScore", "0", 130, TextAlignmentOptions.Center, Ink, 150);
        finalScore.fontStyle = FontStyles.Bold;
        TMP_Text record = CreateText(card, "RecordLabel", "", 28, TextAlignmentOptions.Center, Ink, 44);
        record.fontStyle = FontStyles.Bold;

        Button again = CreatePrimaryButton(card, "PlayAgainButton", "OTRA VEZ");
        AddGameButton(again, GameButtonAction.StartGame);
        CreateNavigationButton(card, "MenuButton", "MENÚ", AppScreen.Home);

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

    // Fondo crema con una franja arcoíris arriba y (opcional) franjas diagonales tipo VHS.
    private static GameObject CreatePanel(Transform parent, string name, bool withVhsStripes)
    {
        Image image = CreateImage(parent, name, Cream);
        Stretch(image.rectTransform);

        Transform topStripe = CreateStripeBar(image.transform, "RainbowBar", Rainbow, 14);
        Anchor((RectTransform)topStripe, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -14), new Vector2(0, 0));

        if (withVhsStripes) CreateDiagonalStripes(image.transform);
        return image.gameObject;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = CreateUIObject(parent, name);
        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    // Franjas horizontales de colores (como el lomo del casete).
    private static Transform CreateStripeBar(Transform parent, string name, Color[] colors, float height)
    {
        GameObject bar = CreateUIObject(parent, name);
        var layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        bar.AddComponent<LayoutElement>().preferredHeight = height;

        for (int i = 0; i < colors.Length; i++)
        {
            Image segment = CreateImage(bar.transform, "Color " + (i + 1), colors[i]);
            segment.raycastTarget = false;
        }
        return bar.transform;
    }

    // Franjas diagonales que cruzan la esquina inferior izquierda (como la portada VHS).
    private static void CreateDiagonalStripes(Transform parent)
    {
        GameObject group = CreateUIObject(parent, "VhsStripes");
        var rect = group.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(2600, 0);
        rect.anchoredPosition = new Vector2(420, 260);
        rect.localRotation = Quaternion.Euler(0f, 0f, 30f);

        const float stripeHeight = 46f;
        const float gap = 8f;
        for (int i = 0; i < VhsStripes.Length; i++)
        {
            Image stripe = CreateImage(group.transform, "Stripe " + (i + 1), VhsStripes[i]);
            stripe.raycastTarget = false;
            RectTransform stripeRect = stripe.rectTransform;
            stripeRect.anchorMin = new Vector2(0f, 0.5f);
            stripeRect.anchorMax = new Vector2(1f, 0.5f);
            stripeRect.sizeDelta = new Vector2(0, stripeHeight);
            stripeRect.anchoredPosition = new Vector2(0, -i * (stripeHeight + gap));
        }
    }

    // Tarjeta clara con borde negro y sombra dura morada.
    private static Transform CreateCard(Transform parent, string name, float width)
    {
        Image image = CreateImage(parent, name, Paper);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, 0);
        rect.anchoredPosition = new Vector2(0, 20);
        AddBorder(image.gameObject, 4);
        AddHardShadow(image.gameObject, Purple, 16);

        AddVerticalLayout(image.gameObject);
        var fitter = image.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return image.transform;
    }

    private static Transform CreateColumn(Transform parent, string name, float width, bool dark)
    {
        GameObject go;
        if (dark)
        {
            Image image = CreateImage(parent, name, Ink);
            AddHardShadow(image.gameObject, Orange, 16);
            go = image.gameObject;
        }
        else
        {
            go = CreateUIObject(parent, name);
        }

        AddVerticalLayout(go);
        if (!dark) go.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 10, 10);
        go.AddComponent<LayoutElement>().preferredWidth = width;
        return go.transform;
    }

    private static void AddVerticalLayout(GameObject go)
    {
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 40, 44);
        layout.spacing = 18;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void AddBorder(GameObject go, float width)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Ink;
        outline.effectDistance = new Vector2(width, width);
    }

    private static void AddHardShadow(GameObject go, Color color, float distance)
    {
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = new Vector2(distance, -distance);
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

    // Título negro con copias de colores desplazadas hacia abajo (efecto retro en capas).
    private static void CreateRetroTitle(Transform parent, string text, float size,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        float step = Mathf.Max(3f, size * 0.045f);
        float height = size * 1.05f + step * TitleShadow.Length;

        GameObject container = CreateUIObject(parent, "Title");
        container.AddComponent<LayoutElement>().preferredHeight = height;

        // Primero las capas de color (la más lejana primero), al final el texto negro encima.
        for (int i = TitleShadow.Length - 1; i >= -1; i--)
        {
            bool isTop = i < 0;
            TMP_Text layer = CreateText(container.transform, isTop ? "Text" : "Layer " + (i + 1), text, size, alignment,
                isTop ? Ink : TitleShadow[i]);
            layer.fontStyle = FontStyles.Bold;
            layer.characterSpacing = -6;
            layer.lineSpacing = 0;
            RectTransform rect = layer.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0, size * 1.05f);
            rect.anchoredPosition = new Vector2(0, -(i + 1) * step);
        }
    }

    private static TMP_InputField CreateInput(Transform parent, string name, string placeholder, TMP_InputField.ContentType contentType)
    {
        GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        go.name = name;
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        go.GetComponent<Image>().color = Color.white;
        AddBorder(go, 3);
        var input = go.GetComponent<TMP_InputField>();
        input.contentType = contentType;
        input.pointSize = 30;
        input.textComponent.color = Ink;
        input.caretColor = Ink;
        input.customCaretColor = true;
        input.selectionColor = new Color(Yellow.r, Yellow.g, Yellow.b, 0.5f);

        if (input.placeholder is TMP_Text placeholderText)
        {
            placeholderText.text = placeholder;
            placeholderText.color = Muted;
            placeholderText.fontStyle = FontStyles.Normal;
        }

        go.AddComponent<LayoutElement>().preferredHeight = 66;
        return input;
    }

    private static Button CreateButton(Transform parent, string name, string text, Color background, Color textColor, float fontSize = 30)
    {
        GameObject go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
        go.name = name;
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        go.GetComponent<Image>().color = background;
        var label = go.GetComponentInChildren<TMP_Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 6;
        label.color = textColor;

        var button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        button.colors = colors;

        go.AddComponent<LayoutElement>().preferredHeight = 70;
        return button;
    }

    // Botón negro con sombra dura naranja.
    private static Button CreatePrimaryButton(Transform parent, string name, string text)
    {
        Button button = CreateButton(parent, name, text, Ink, Cream);
        AddHardShadow(button.gameObject, Orange, 7);
        return button;
    }

    // Botón crema con borde negro.
    private static Button CreateSecondaryButton(Transform parent, string name, string text)
    {
        Button button = CreateButton(parent, name, text, Cream, Ink);
        AddBorder(button.gameObject, 3);
        return button;
    }

    // Enlace de texto sin fondo.
    private static Button CreateLinkButton(Transform parent, string name, string text)
    {
        Button button = CreateButton(parent, name, text, new Color(0, 0, 0, 0), Ink, 24);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.fontStyle = FontStyles.Underline;
        label.characterSpacing = 0;
        button.GetComponent<LayoutElement>().preferredHeight = 44;
        return button;
    }

    private static void CreateNavigationButton(Transform parent, string name, string text, AppScreen target, bool asLink = false)
    {
        Button button = asLink ? CreateLinkButton(parent, name, text) : CreateSecondaryButton(parent, name, text);
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
        Image background = CreateImage(parent, name, new Color(1f, 1f, 1f, 0.05f));
        var layout = background.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 0, 0);
        layout.spacing = 12;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        background.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;

        TMP_Text rankText = CreateText(background.transform, "Rank", rank, size, TextAlignmentOptions.Left, color);
        rankText.fontStyle = FontStyles.Bold;
        rankText.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;

        TMP_Text nameText = CreateText(background.transform, "Name", player, size, TextAlignmentOptions.Left, color);
        nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        TMP_Text scoreText = CreateText(background.transform, "Score", score, size, TextAlignmentOptions.Right, color);
        scoreText.fontStyle = FontStyles.Bold;
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

    // Fondo crema detrás del tablero del juego.
    private static void StyleCamera()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        Undo.RecordObject(camera, "Color de cámara");
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Cream;
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
