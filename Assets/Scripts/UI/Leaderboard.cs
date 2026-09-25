using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tabla de puntajes más altos. Se suscribe con ValueChanged, así que se actualiza
// sola (en tiempo real) cuando cualquier jugador supera su récord.
public class Leaderboard : MonoBehaviour
{
    [SerializeField]
    private int _maxEntries = 10;
    [SerializeField]
    private Transform _rowsContainer;
    [SerializeField]
    private GameObject _rowTemplate;
    [SerializeField]
    private TMP_Text _emptyLabel;
    [SerializeField]
    private Color _rowColor = new Color(1f, 1f, 1f, 0.04f);
    [SerializeField]
    private Color _currentUserRowColor = new Color(0.13f, 0.67f, 0.35f, 0.35f);

    private Query _query;
    private readonly List<GameObject> _rows = new List<GameObject>();

    private struct Entry
    {
        public string UserId;
        public string Username;
        public long Score;
    }

    void OnEnable()
    {
        if (_rowTemplate != null) _rowTemplate.SetActive(false);
        SetEmptyText("Cargando puntajes...");
        FirebaseService.WhenReady(Subscribe);
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (!isActiveAndEnabled || _query != null) return;
        if (FirebaseService.Auth.CurrentUser == null) return;

        _query = FirebaseService.Users.OrderByChild("score").LimitToLast(_maxEntries);
        _query.ValueChanged += HandleValueChanged;
    }

    private void Unsubscribe()
    {
        if (_query == null) return;
        _query.ValueChanged -= HandleValueChanged;
        _query = null;
    }

    private void HandleValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError("Error leyendo el leaderboard: " + args.DatabaseError.Message);
            SetEmptyText("No se pudo cargar la tabla de puntajes.");
            return;
        }

        var entries = new List<Entry>();
        foreach (DataSnapshot child in args.Snapshot.Children)
        {
            if (!child.HasChild("score")) continue;

            string username = child.Child("username").Value as string;
            entries.Add(new Entry
            {
                UserId = child.Key,
                Username = string.IsNullOrEmpty(username) ? "Anónimo" : username,
                Score = FirebaseService.ToLong(child.Child("score").Value)
            });
        }

        // LimitToLast entrega de menor a mayor; la tabla se muestra de mayor a menor.
        entries.Sort((a, b) => b.Score.CompareTo(a.Score));
        Render(entries);
    }

    private void Render(List<Entry> entries)
    {
        foreach (GameObject row in _rows) Destroy(row);
        _rows.Clear();

        SetEmptyText(entries.Count == 0 ? "Aún no hay puntajes. ¡Sé el primero!" : "");

        FirebaseUser user = FirebaseService.Auth.CurrentUser;
        string currentUserId = user != null ? user.UserId : null;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            GameObject row = Instantiate(_rowTemplate, _rowsContainer);
            row.name = "Row " + (i + 1);
            row.SetActive(true);

            SetChildText(row, "Rank", "#" + (i + 1));
            SetChildText(row, "Name", entry.Username);
            SetChildText(row, "Score", entry.Score.ToString());

            Image background = row.GetComponent<Image>();
            if (background != null)
            {
                background.color = entry.UserId == currentUserId ? _currentUserRowColor : _rowColor;
            }

            _rows.Add(row);
        }
    }

    private static void SetChildText(GameObject row, string childName, string text)
    {
        Transform child = row.transform.Find(childName);
        if (child != null && child.TryGetComponent(out TMP_Text label)) label.text = text;
    }

    private void SetEmptyText(string text)
    {
        if (_emptyLabel == null) return;
        _emptyLabel.text = text;
        _emptyLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }
}
