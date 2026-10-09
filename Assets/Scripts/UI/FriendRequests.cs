using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Solicitudes de amistad:
// - Enviar: escribe sent/{yo}/{otro} y received/{otro}/{yo} en una sola llamada.
// - Buzón: escucha received/{yo}; Aceptar crea MI friends/{yo}/{otro} y marca el status en MI buzón.
// - Mis enviadas pendientes: escucho received/{otro}/{yo}; si el otro aceptó, creo MI friends/{yo}/{otro}.
public class FriendRequests : MonoBehaviour
{
    [SerializeField]
    private Transform _rowsContainer;
    // Hijos: Name (TMP_Text), AcceptButton (Button), RejectButton (Button)
    [SerializeField]
    private GameObject _rowTemplate;

    private readonly List<GameObject> _rows = new List<GameObject>();
    // Solicitudes que envié y siguen pendientes: uid destino -> received/{destino}/{yo}
    private readonly Dictionary<string, DatabaseReference> _sentPending = new Dictionary<string, DatabaseReference>();

    private DatabaseReference _inboxReference;
    private DataSnapshot _inbox;
    private string _myUid;
    private bool _dirty;

    private DatabaseReference Root => FirebaseService.Database.RootReference;

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
            Unsubscribe();
            _myUid = uid;
            if (uid != null) Subscribe();
        }

        if (_dirty)
        {
            _dirty = false;
            Render();
        }
    }

    private void Subscribe()
    {
        _inboxReference = Root.Child("friendRequests").Child("received").Child(_myUid);
        _inboxReference.ValueChanged += HandleInboxChanged;

        // Retomo la vigilancia de las solicitudes que dejé pendientes en otra sesión.
        string uid = _myUid;
        Root.Child("friendRequests").Child("sent").Child(uid).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled || uid != _myUid) return;
            foreach (DataSnapshot child in task.Result.Children)
            {
                if ((child.Child("status").Value as string) == "pending") WatchSent(child.Key);
            }
        });
    }

    private void Unsubscribe()
    {
        if (_inboxReference != null) _inboxReference.ValueChanged -= HandleInboxChanged;
        _inboxReference = null;
        _inbox = null;

        foreach (DatabaseReference reference in _sentPending.Values) reference.ValueChanged -= HandleSentStatusChanged;
        _sentPending.Clear();
        _dirty = true;
    }

    // Lo llama el botón "Agregar amigo" de la lista de usuarios en línea.
    public void Send(string targetUid)
    {
        if (_myUid == null) return;
        if (targetUid == _myUid)
        {
            StatusMessage.Show("No puedes enviarte una solicitud a ti mismo.", true);
            return;
        }
        if (_sentPending.ContainsKey(targetUid))
        {
            StatusMessage.Show("Ya le enviaste una solicitud.", true);
            return;
        }

        Root.Child("friends").Child(_myUid).Child(targetUid).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                StatusMessage.Show("No se pudo enviar la solicitud.", true);
                return;
            }
            if (task.Result.Exists)
            {
                StatusMessage.Show("Ya son amigos.", true);
                return;
            }

            var request = new Dictionary<string, object>
            {
                { "status", "pending" },
                { "timestamp", ServerValue.Timestamp }
            };
            var updates = new Dictionary<string, object>
            {
                { "friendRequests/sent/" + _myUid + "/" + targetUid, request },
                { "friendRequests/received/" + targetUid + "/" + _myUid, request }
            };

            Root.UpdateChildrenAsync(updates).ContinueWithOnMainThread(sendTask =>
            {
                if (sendTask.IsFaulted || sendTask.IsCanceled)
                {
                    StatusMessage.Show("No se pudo enviar la solicitud.", true);
                    return;
                }
                StatusMessage.Show("Solicitud enviada.");
                WatchSent(targetUid);
            });
        });
    }

    private void WatchSent(string targetUid)
    {
        if (_sentPending.ContainsKey(targetUid)) return;
        DatabaseReference reference = Root.Child("friendRequests").Child("received").Child(targetUid).Child(_myUid);
        reference.ValueChanged += HandleSentStatusChanged;
        _sentPending[targetUid] = reference;
    }

    private void HandleSentStatusChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;

        string targetUid = args.Snapshot.Reference.Parent.Key;
        string status = args.Snapshot.Child("status").Value as string;
        if (status != "accepted" && status != "rejected") return;

        // El otro respondió: dejo de vigilar y limpio MI enviada. Si aceptó, creo MI registro de amistad.
        if (_sentPending.TryGetValue(targetUid, out DatabaseReference reference))
        {
            reference.ValueChanged -= HandleSentStatusChanged;
            _sentPending.Remove(targetUid);
        }

        var updates = new Dictionary<string, object>
        {
            { "friendRequests/sent/" + _myUid + "/" + targetUid, null }
        };
        if (status == "accepted") updates["friends/" + _myUid + "/" + targetUid] = true;
        Root.UpdateChildrenAsync(updates);
    }

    private void HandleInboxChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;
        _inbox = args.Snapshot;
        _dirty = true;
    }

    private void Accept(string fromUid)
    {
        var updates = new Dictionary<string, object>
        {
            { "friends/" + _myUid + "/" + fromUid, true },
            { "friendRequests/received/" + _myUid + "/" + fromUid + "/status", "accepted" }
        };
        Root.UpdateChildrenAsync(updates);
    }

    private void Reject(string fromUid)
    {
        Root.Child("friendRequests").Child("received").Child(_myUid).Child(fromUid).Child("status").SetValueAsync("rejected");
    }

    private void Render()
    {
        foreach (GameObject row in _rows) Destroy(row);
        _rows.Clear();
        if (_inbox == null) return;

        foreach (DataSnapshot request in _inbox.Children)
        {
            if ((request.Child("status").Value as string) != "pending") continue;

            string fromUid = request.Key;
            GameObject row = Instantiate(_rowTemplate, _rowsContainer);
            row.name = fromUid;
            row.SetActive(true);
            row.transform.Find("AcceptButton").GetComponent<Button>().onClick.AddListener(() => Accept(fromUid));
            row.transform.Find("RejectButton").GetComponent<Button>().onClick.AddListener(() => Reject(fromUid));
            _rows.Add(row);

            TMP_Text label = row.transform.Find("Name").GetComponent<TMP_Text>();
            label.text = "...";
            FirebaseService.Users.Child(fromUid).Child("username").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (label == null || task.IsFaulted || task.IsCanceled) return;
                label.text = task.Result.Value != null ? task.Result.Value.ToString() : "?";
            });
        }
    }
}
