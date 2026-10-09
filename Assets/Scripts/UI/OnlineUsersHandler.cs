using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Lista de usuarios en línea: escucha users-online con ChildAdded / ChildRemoved.
// También avisa cuando un amigo se conecta o se desconecta.
// Va en un GameObject siempre activo; las filas se dibujan en _rowsContainer (dentro del Home).
public class OnlineUsersHandler : MonoBehaviour
{
    [SerializeField]
    private Transform _rowsContainer;
    // Hijos: Name (TMP_Text), AddButton (Button)
    [SerializeField]
    private GameObject _rowTemplate;
    [SerializeField]
    private FriendRequests _friendRequests;

    // uid -> username de los que están en línea
    private readonly Dictionary<string, string> _online = new Dictionary<string, string>();
    private readonly List<GameObject> _rows = new List<GameObject>();
    // Mis amigos, cargados UNA vez al iniciar sesión
    private readonly HashSet<string> _friends = new HashSet<string>();
    // Los que ya estaban en línea al entrar: por ellos no se avisa
    private readonly HashSet<string> _alreadyOnline = new HashSet<string>();

    private DatabaseReference _reference;
    private string _myUid;
    private bool _dirty;
    private string _notice;

    void Start()
    {
        _rowTemplate.SetActive(false);
    }

    void Update()
    {
        if (!FirebaseService.IsReady) return;

        // Me suscribo al iniciar sesión y me desuscribo al cerrarla.
        FirebaseUser user = FirebaseService.Auth.CurrentUser;
        string uid = user != null ? user.UserId : null;
        if (uid != _myUid)
        {
            Unsubscribe();
            _myUid = uid;
            if (uid != null) Subscribe();
        }

        // Los eventos de Firebase solo guardan datos; la UI se toca aquí, en el hilo principal.
        if (_notice != null)
        {
            StatusMessage.Show(_notice);
            _notice = null;
        }
        if (_dirty)
        {
            _dirty = false;
            Render();
        }
    }

    private void Subscribe()
    {
        string uid = _myUid;
        DatabaseReference root = FirebaseService.Database.RootReference;

        // 1) Cargo mis amigos una vez. 2) Anoto quién ya estaba en línea. 3) Empiezo a escuchar.
        root.Child("friends").Child(uid).GetValueAsync().ContinueWithOnMainThread(friendsTask =>
        {
            root.Child("users-online").GetValueAsync().ContinueWithOnMainThread(onlineTask =>
            {
                if (uid != _myUid || _reference != null) return; // cerró sesión mientras cargaba

                if (!friendsTask.IsFaulted && !friendsTask.IsCanceled)
                {
                    foreach (DataSnapshot friend in friendsTask.Result.Children) _friends.Add(friend.Key);
                }
                if (!onlineTask.IsFaulted && !onlineTask.IsCanceled)
                {
                    foreach (DataSnapshot online in onlineTask.Result.Children) _alreadyOnline.Add(online.Key);
                }

                _reference = root.Child("users-online");
                _reference.ChildAdded += HandleChildAdded;
                _reference.ChildRemoved += HandleChildRemoved;
            });
        });
    }

    private void Unsubscribe()
    {
        if (_reference != null)
        {
            _reference.ChildAdded -= HandleChildAdded;
            _reference.ChildRemoved -= HandleChildRemoved;
            _reference = null;
        }
        _online.Clear();
        _friends.Clear();
        _alreadyOnline.Clear();
        _dirty = true;
    }

    private void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;
        DataSnapshot snapshot = args.Snapshot;
        string username = snapshot.Value != null ? snapshot.Value.ToString() : "?";
        Debug.Log("User online: " + username);
        _online[snapshot.Key] = username;
        _dirty = true;

        bool wasAlreadyOnline = _alreadyOnline.Remove(snapshot.Key);
        if (!wasAlreadyOnline && _friends.Contains(snapshot.Key)) _notice = username + " se conectó";
    }

    private void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;
        DataSnapshot snapshot = args.Snapshot;
        string username = snapshot.Value != null ? snapshot.Value.ToString() : "?";
        Debug.Log("User offline: " + username);
        _online.Remove(snapshot.Key);
        _alreadyOnline.Remove(snapshot.Key);
        _dirty = true;

        if (_friends.Contains(snapshot.Key)) _notice = username + " se desconectó";
    }

    private void Render()
    {
        foreach (GameObject row in _rows) Destroy(row);
        _rows.Clear();

        foreach (KeyValuePair<string, string> pair in _online)
        {
            if (pair.Key == _myUid) continue; // no me muestro a mí

            string uid = pair.Key;
            GameObject row = Instantiate(_rowTemplate, _rowsContainer);
            row.name = uid; // el UID queda guardado en la fila (no se muestra)
            row.SetActive(true);
            row.transform.Find("Name").GetComponent<TMP_Text>().text = pair.Value;
            row.transform.Find("AddButton").GetComponent<Button>().onClick.AddListener(() => _friendRequests.Send(uid));
            _rows.Add(row);
        }
    }

    private void OnApplicationQuit()
    {
        if (_myUid == null) return;
        FirebaseService.Database.RootReference.Child("users-online").Child(_myUid).SetValueAsync(null);
    }
}
