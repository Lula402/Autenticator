using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;

// Lista de amigos: escucha friends/{yo} en tiempo real y muestra el username de cada uno.
public class FriendsList : MonoBehaviour
{
    [SerializeField]
    private Transform _rowsContainer;
    // Hijo: Name (TMP_Text)
    [SerializeField]
    private GameObject _rowTemplate;

    private readonly List<GameObject> _rows = new List<GameObject>();

    private DatabaseReference _reference;
    private DataSnapshot _friends;
    private string _myUid;
    private bool _dirty;

    void Start()
    {
        _rowTemplate.SetActive(false);
    }

    void Update()
    {
        if (!FirebaseService.IsReady) return;

        FirebaseUser user = FirebaseService.Auth.CurrentUser;
        string uid = user != null ? user.UserId : null;
        if (uid != _myUid)
        {
            if (_reference != null) _reference.ValueChanged -= HandleValueChanged;
            _reference = null;
            _friends = null;
            _dirty = true;

            _myUid = uid;
            if (uid != null)
            {
                _reference = FirebaseService.Database.RootReference.Child("friends").Child(uid);
                _reference.ValueChanged += HandleValueChanged;
            }
        }

        if (_dirty)
        {
            _dirty = false;
            Render();
        }
    }

    private void HandleValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;
        _friends = args.Snapshot;
        _dirty = true;
    }

    private void Render()
    {
        foreach (GameObject row in _rows) Destroy(row);
        _rows.Clear();
        if (_friends == null) return;

        foreach (DataSnapshot friend in _friends.Children)
        {
            GameObject row = Instantiate(_rowTemplate, _rowsContainer);
            row.name = friend.Key;
            row.SetActive(true);
            _rows.Add(row);

            TMP_Text label = row.transform.Find("Name").GetComponent<TMP_Text>();
            label.text = "...";
            FirebaseService.Users.Child(friend.Key).Child("username").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (label == null || task.IsFaulted || task.IsCanceled) return;
                label.text = task.Result.Value != null ? task.Result.Value.ToString() : "?";
            });
        }
    }
}
