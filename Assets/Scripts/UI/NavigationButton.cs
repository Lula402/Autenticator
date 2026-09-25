using UnityEngine;
using UnityEngine.UI;

// Botón que cambia de pantalla (por ejemplo "Crear cuenta" o "Volver").
public class NavigationButton : MonoBehaviour
{
    [SerializeField]
    private Button _button;
    [SerializeField]
    private AppScreen _target;

    private void Reset()
    {
        _button = GetComponent<Button>();
    }

    void Start()
    {
        _button.onClick.AddListener(() => UIManager.Instance.Show(_target));
    }
}
