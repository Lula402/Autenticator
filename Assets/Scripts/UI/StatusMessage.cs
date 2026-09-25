using TMPro;
using UnityEngine;

// Texto global para mostrar mensajes de estado y errores al usuario.
public class StatusMessage : MonoBehaviour
{
    private static StatusMessage _instance;

    [SerializeField]
    private TMP_Text _label;

    [SerializeField]
    private Color _infoColor = new Color(0.106f, 0.106f, 0.106f);
    [SerializeField]
    private Color _errorColor = new Color(0.9f, 0.196f, 0.153f);

    private void Reset()
    {
        _label = GetComponent<TMP_Text>();
    }

    void Awake()
    {
        _instance = this;
        _label.text = "";
    }

    public static void Show(string message, bool isError = false)
    {
        if (isError) Debug.LogWarning(message); else Debug.Log(message);

        if (_instance == null || _instance._label == null) return;
        _instance._label.color = isError ? _instance._errorColor : _instance._infoColor;
        _instance._label.text = message;
    }

    public static void Clear()
    {
        if (_instance != null && _instance._label != null) _instance._label.text = "";
    }
}
