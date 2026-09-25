using TMPro;
using UnityEngine;

// Pie de página visible en todas las pantallas con el nombre del autor.
public class AuthorFooter : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _label;

    private void Reset()
    {
        _label = GetComponent<TMP_Text>();
    }

    void Awake()
    {
        _label.text = ProjectInfo.AuthorFullName;
    }
}
