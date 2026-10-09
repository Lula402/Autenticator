using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú "SID2 > Agregar UI social y competitiva": SOLO AGREGA lo nuevo a la escena que ya existe
// (no borra ni reconstruye nada) y conecta las referencias. Se puede deshacer con Ctrl+Z.
//   HomePanel     -> botón SOCIAL arriba a la derecha + SocialPanel (en línea, solicitudes, amigos, buscar rival)
//   GamePanel     -> RivalLabel
//   GameOverPanel -> ResultLabel, ExitButton, RematchButton
//   Social        -> objeto raíz con OnlineUsersHandler, FriendRequests, FriendsList, Matchmaking, CompetitiveMatch
public static class SocialSceneAdder
{
    private static readonly Color Cream = new Color32(242, 238, 220, 255);
    private static readonly Color Paper = new Color32(251, 249, 240, 255);
    private static readonly Color Ink = new Color32(27, 27, 27, 255);
    private static readonly Color Magenta = new Color32(229, 40, 126, 255);

    [MenuItem("SID2/Agregar UI social y competitiva")]
    public static void Add()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject canvas = FindRoot(scene, "FirebaseUI");
        GameObject game = FindRoot(scene, "Game");
        if (canvas == null || game == null)
        {
            EditorUtility.DisplayDialog("Agregar UI social", "No encontré FirebaseUI o Game en la escena.", "OK");
            return;
        }
        if (FindRoot(scene, "Social") != null)
        {
            EditorUtility.DisplayDialog("Agregar UI social", "Ya existe el objeto Social: no se agregó nada.", "OK");
            return;
        }

        Transform home = canvas.transform.Find("HomePanel");
        Transform gamePanel = canvas.transform.Find("GamePanel");
        Transform gameOverCard = canvas.transform.Find("GameOverPanel/Card");
        if (home == null || gamePanel == null || gameOverCard == null)
        {
            EditorUtility.DisplayDialog("Agregar UI social", "No encontré HomePanel, GamePanel o GameOverPanel/Card.", "OK");
            return;
        }

        // ---------- Home: botón SOCIAL y panel social (encima de todo dentro del Home) ----------
        Button socialButton = CreateButton(home, "SocialButton", "SOCIAL", 220);
        Anchor((RectTransform)socialButton.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-250, -95), new Vector2(-30, -30));
        Undo.RegisterCreatedObjectUndo(socialButton.gameObject, "Agregar UI social");

        Image socialPanel = CreateImage(home, "SocialPanel", new Color(Cream.r, Cream.g, Cream.b, 0.97f));
        Anchor(socialPanel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Vertical(socialPanel.gameObject, 60, 20);
        Undo.RegisterCreatedObjectUndo(socialPanel.gameObject, "Agregar UI social");

        GameObject lists = CreateUIObject(socialPanel.transform, "Lists");
        Horizontal(lists, 30);
        lists.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
        lists.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        lists.AddComponent<LayoutElement>().flexibleHeight = 1;

        Transform onlineList = CreateListColumn(lists.transform, "OnlineColumn", "EN LÍNEA");
        GameObject onlineRow = CreateRow(onlineList, "OnlineRowTemplate");
        CreateButton(onlineRow.transform, "AddButton", "AGREGAR", 170);

        Transform inboxList = CreateListColumn(lists.transform, "InboxColumn", "SOLICITUDES");
        GameObject inboxRow = CreateRow(inboxList, "InboxRowTemplate");
        CreateButton(inboxRow.transform, "AcceptButton", "SÍ", 80);
        CreateButton(inboxRow.transform, "RejectButton", "NO", 80);

        Transform friendsList = CreateListColumn(lists.transform, "FriendsColumn", "AMIGOS");
        GameObject friendRow = CreateRow(friendsList, "FriendRowTemplate");

        GameObject buttons = CreateUIObject(socialPanel.transform, "Buttons");
        Horizontal(buttons, 30);
        buttons.AddComponent<LayoutElement>().preferredHeight = 70;
        Button searchButton = CreateButton(buttons.transform, "SearchButton", "BUSCAR RIVAL", 320);
        Button cancelButton = CreateButton(buttons.transform, "CancelButton", "CANCELAR", 260);
        Button closeButton = CreateButton(buttons.transform, "CloseButton", "CERRAR", 220);

        // Abrir / cerrar el panel social sin código: listeners persistentes de SetActive.
        UnityEventTools.AddBoolPersistentListener(socialButton.onClick, new UnityAction<bool>(socialPanel.gameObject.SetActive), true);
        UnityEventTools.AddBoolPersistentListener(closeButton.onClick, new UnityAction<bool>(socialPanel.gameObject.SetActive), false);
        socialPanel.gameObject.SetActive(false);

        // ---------- Juego: puntaje del rival en vivo ----------
        TMP_Text rivalLabel = CreateText(gamePanel, "RivalLabel", "RIVAL: 0", 34, Magenta);
        rivalLabel.alignment = TextAlignmentOptions.Center;
        Anchor(rivalLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -160), new Vector2(0, -105));
        Undo.RegisterCreatedObjectUndo(rivalLabel.gameObject, "Agregar UI social");

        // ---------- GameOver: resultado, Salir y Revancha ----------
        TMP_Text resultLabel = CreateText(gameOverCard, "ResultLabel", "", 34, Ink);
        resultLabel.alignment = TextAlignmentOptions.Center;
        resultLabel.fontStyle = FontStyles.Bold;
        resultLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
        Transform recordLabel = gameOverCard.Find("RecordLabel");
        if (recordLabel != null) resultLabel.transform.SetSiblingIndex(recordLabel.GetSiblingIndex() + 1);
        Button rematchButton = CreateButton(gameOverCard, "RematchButton", "REVANCHA", 0);
        Button exitButton = CreateButton(gameOverCard, "ExitButton", "SALIR", 0);
        Undo.RegisterCreatedObjectUndo(resultLabel.gameObject, "Agregar UI social");
        Undo.RegisterCreatedObjectUndo(rematchButton.gameObject, "Agregar UI social");
        Undo.RegisterCreatedObjectUndo(exitButton.gameObject, "Agregar UI social");

        // ---------- Objeto Social (siempre activo) con los scripts ----------
        var social = new GameObject("Social");
        Undo.RegisterCreatedObjectUndo(social, "Agregar UI social");
        var friendRequests = social.AddComponent<FriendRequests>();
        var online = social.AddComponent<OnlineUsersHandler>();
        var friends = social.AddComponent<FriendsList>();
        var match = social.AddComponent<CompetitiveMatch>();
        var matchmaking = social.AddComponent<Matchmaking>();

        Set(online, "_rowsContainer", onlineList);
        Set(online, "_rowTemplate", onlineRow);
        Set(online, "_friendRequests", friendRequests);

        Set(friendRequests, "_rowsContainer", inboxList);
        Set(friendRequests, "_rowTemplate", inboxRow);

        Set(friends, "_rowsContainer", friendsList);
        Set(friends, "_rowTemplate", friendRow);

        Set(matchmaking, "_searchButton", searchButton);
        Set(matchmaking, "_cancelButton", cancelButton);
        Set(matchmaking, "_match", match);

        Set(match, "_rivalLabel", rivalLabel);
        Set(match, "_resultLabel", resultLabel);
        Set(match, "_exitButton", exitButton);
        Set(match, "_rematchButton", rematchButton);
        var serializedMatch = new SerializedObject(match);
        SerializedProperty solo = serializedMatch.FindProperty("_soloButtons");
        Transform[] soloButtons = { gameOverCard.Find("PlayAgainButton"), gameOverCard.Find("MenuButton") };
        solo.arraySize = 0;
        foreach (Transform button in soloButtons)
        {
            if (button == null) continue;
            solo.arraySize++;
            solo.GetArrayElementAtIndex(solo.arraySize - 1).objectReferenceValue = button.gameObject;
        }
        serializedMatch.ApplyModifiedProperties();

        SnakeGameManager gameManager = game.GetComponent<SnakeGameManager>();
        if (gameManager != null) Set(gameManager, "_match", match);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = social;
        Debug.Log("UI social y competitiva agregada. Revisa y guarda la escena (Ctrl+S).");
    }

    // ================= Helpers =================

    // Columna con título y un contenedor de filas (el contenedor es lo que se devuelve).
    private static Transform CreateListColumn(Transform parent, string name, string title)
    {
        Image column = CreateImage(parent, name, Paper);
        column.gameObject.AddComponent<Outline>().effectColor = Ink;
        Vertical(column.gameObject, 24, 12);

        TMP_Text titleLabel = CreateText(column.transform, "Title", title, 34, Ink);
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;

        GameObject rows = CreateUIObject(column.transform, "Rows");
        Vertical(rows, 0, 8);
        rows.AddComponent<LayoutElement>().flexibleHeight = 1;
        return rows.transform;
    }

    // Fila con un texto "Name" que ocupa el espacio libre; los botones se agregan después.
    private static GameObject CreateRow(Transform parent, string name)
    {
        GameObject row = CreateUIObject(parent, name);
        Horizontal(row, 10);
        row.AddComponent<LayoutElement>().preferredHeight = 60;

        TMP_Text label = CreateText(row.transform, "Name", "usuario", 28, Ink);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        row.SetActive(false);
        return row;
    }

    private static GameObject CreateUIObject(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        Image image = CreateUIObject(parent, name).AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float size, Color color)
    {
        var label = CreateUIObject(parent, name).AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    // width = 0: el ancho lo decide el layout del padre.
    private static Button CreateButton(Transform parent, string name, string text, float width)
    {
        GameObject go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
        go.name = name;
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = Ink;

        TMP_Text label = go.GetComponentInChildren<TMP_Text>();
        label.text = text;
        label.fontSize = 26;
        label.fontStyle = FontStyles.Bold;
        label.color = Cream;

        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = 60;
        if (width > 0) layout.preferredWidth = width;
        return go.GetComponent<Button>();
    }

    private static void Vertical(GameObject go, int padding, float spacing)
    {
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void Horizontal(GameObject go, float spacing)
    {
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void Set(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
        }
        return null;
    }
}
