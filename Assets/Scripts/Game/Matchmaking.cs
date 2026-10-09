using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;

public class Matchmaking : MonoBehaviour
{
    private const float WaitSeconds = 5f;

    [SerializeField]
    private Button _searchButton;
    [SerializeField]
    private Button _cancelButton;
    [SerializeField]
    private CompetitiveMatch _match;

    private DatabaseReference _queueReference;
    private DatabaseReference _matchReference;
    private DataSnapshot _queue;
    private string _myUid;     
    private string _roomId;  
    private bool _queueDirty;
    private bool _creatingRoom;
    private readonly Dictionary<string, float> _firstSeen = new Dictionary<string, float>();

    private DatabaseReference Root => FirebaseService.Database.RootReference;

    void Start()
    {
        _searchButton.onClick.AddListener(Search);
        _cancelButton.onClick.AddListener(Cancel);
    }

    private void Search()
    {
        FirebaseUser user = FirebaseService.Auth.CurrentUser;
        if (user == null || _myUid != null || _match.InMatch) return;

        string uid = user.UserId;
        StatusMessage.Show("Buscando rival...");

        FirebaseService.Users.Child(uid).Child("username").GetValueAsync().ContinueWithOnMainThread(nameTask =>
        {
            FirebaseService.Scores.Child(uid).LimitToLast(3).GetValueAsync().ContinueWithOnMainThread(scoresTask =>
            {
                if (scoresTask.IsFaulted || scoresTask.IsCanceled)
                {
                    StatusMessage.Show("No se pudo leer tu historial.", true);
                    return;
                }

                double sum = 0;
                int count = 0;
                foreach (DataSnapshot score in scoresTask.Result.Children)
                {
                    sum += FirebaseService.ToLong(score.Child("score").Value);
                    count++;
                }
                double average = count > 0 ? sum / count : 0;

                string username = null;
                if (!nameTask.IsFaulted && !nameTask.IsCanceled && nameTask.Result.Value != null) username = nameTask.Result.Value.ToString();
                if (string.IsNullOrEmpty(username)) username = user.DisplayName;

                _myUid = uid;
                var entry = new Dictionary<string, object>
                {
                    { "username", username },
                    { "avg", average }
                };
                _queueReference = Root.Child("matchmaking");
                _queueReference.Child(uid).OnDisconnect().RemoveValue();
                _queueReference.Child(uid).SetValueAsync(entry);
                _queueReference.ValueChanged += HandleQueueChanged;

                _matchReference = Root.Child("matches").Child(uid);
                _matchReference.ValueChanged += HandleMatchChanged;

                StatusMessage.Show("Buscando rival... (tu nivel: " + average.ToString("0") + ")");
            });
        });
    }

    private void Cancel()
    {
        if (_myUid == null) return;
        Root.Child("matchmaking").Child(_myUid).RemoveValueAsync();
        StopSearching();
        StatusMessage.Show("Búsqueda cancelada.");
    }

    private void StopSearching()
    {
        if (_queueReference != null) _queueReference.ValueChanged -= HandleQueueChanged;
        if (_matchReference != null) _matchReference.ValueChanged -= HandleMatchChanged;
        _queueReference = null;
        _matchReference = null;
        _queue = null;
        _myUid = null;
        _creatingRoom = false;
        _firstSeen.Clear();
    }

    private void HandleQueueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;
        _queue = args.Snapshot;
        _queueDirty = true;
    }

    private void HandleMatchChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null || args.Snapshot.Value == null) return;
        _roomId = args.Snapshot.Value.ToString();
    }

    void Update()
    {
        if (_roomId != null)
        {
            string roomId = _roomId;
            _roomId = null;
            if (_myUid == null) return;

            Root.Child("matches").Child(_myUid).RemoveValueAsync();
            Root.Child("matchmaking").Child(_myUid).RemoveValueAsync();
            StopSearching();
            _match.Join(roomId);
            return;
        }

        if (_queueDirty && _queue != null)
        {
            _queueDirty = false;
            foreach (DataSnapshot player in _queue.Children)
            {
                if (!_firstSeen.ContainsKey(player.Key)) _firstSeen[player.Key] = Time.time;
            }
        }

        TryMatch();
    }

    private bool HasWaited(string uid)
    {
        return _firstSeen.TryGetValue(uid, out float seen) && Time.time - seen >= WaitSeconds;
    }

    private void TryMatch()
    {
        if (_myUid == null || _creatingRoom || _queue == null || !_queue.HasChild(_myUid)) return;

        string rival = Closest(_myUid);
        if (rival == null || Closest(rival) != _myUid) return;           
        if (string.CompareOrdinal(_myUid, rival) > 0) return;           
        if (!HasWaited(_myUid) || !HasWaited(rival)) return;         

        _creatingRoom = true;
        string roomId = Root.Child("rooms").Push().Key;
        var players = new Dictionary<string, object>
        {
            { _myUid, PlayerEntry(_myUid) },
            { rival, PlayerEntry(rival) }
        };
        var updates = new Dictionary<string, object>
        {
            { "rooms/" + roomId + "/players", players },
            { "matches/" + rival, roomId },
            { "matches/" + _myUid, roomId },
            { "matchmaking/" + _myUid, null },
            { "matchmaking/" + rival, null }
        };
        Root.UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("No se pudo crear el room: " + task.Exception);
                _creatingRoom = false;
            }
        });
    }

    private string Closest(string uid)
    {
        double average = Average(uid);
        string best = null;
        double bestDifference = 0;
        foreach (DataSnapshot player in _queue.Children)
        {
            if (player.Key == uid) continue;
            double difference = Math.Abs(Average(player.Key) - average);
            if (best == null || difference < bestDifference)
            {
                best = player.Key;
                bestDifference = difference;
            }
        }
        return best;
    }

    private double Average(string uid)
    {
        return Convert.ToDouble(_queue.Child(uid).Child("avg").Value);
    }

    private Dictionary<string, object> PlayerEntry(string uid)
    {
        return new Dictionary<string, object>
        {
            { "username", _queue.Child(uid).Child("username").Value },
            { "avg", _queue.Child(uid).Child("avg").Value }
        };
    }
}
