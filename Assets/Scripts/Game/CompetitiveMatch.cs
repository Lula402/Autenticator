using System.Collections.Generic;
using Firebase.Database;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Partida competitiva en un room: cada uno juega SU Snake y ve el puntaje del rival en vivo.
//   rooms/{roomId}/ready/{ronda}/{uid}   = true
//   rooms/{roomId}/live/{ronda}/{uid}    = { score, alive }
//   rooms/{roomId}/rematch/{ronda}/{uid} = true
//   rooms/{roomId}/left/{uid}            = true
// Cada cliente calcula el resultado con los mismos datos compartidos (sin servidor).
// Va en un GameObject siempre activo.
public class CompetitiveMatch : MonoBehaviour
{
    [Header("HUD del juego")]
    [SerializeField]
    private TMP_Text _rivalLabel;

    [Header("Panel GameOver")]
    [SerializeField]
    private TMP_Text _resultLabel;
    [SerializeField]
    private Button _exitButton;
    [SerializeField]
    private Button _rematchButton;
    // Lo que solo es del modo solitario ("Jugar de nuevo", "Menú", récord personal): se oculta mientras estoy en un room
    [SerializeField]
    private GameObject[] _soloButtons;

    private DatabaseReference _roomReference;
    private DataSnapshot _room;
    private bool _dirty;

    private string _myUid;
    private string _rivalUid;
    private string _rivalName = "Rival";
    private int _round;
    private bool _started;   // los dos marcaron ready y empezó la ronda
    private bool _finished;  // la ronda ya tiene resultado
    private string _title = "";
    private float _countdown; // > 0 mientras corre la cuenta regresiva antes de empezar

    private int _myScore;
    private bool _myAlive;
    private int _rivalScore;
    private bool _rivalAlive;
    private bool _rivalLeft;

    public bool InMatch => _roomReference != null;

    void Start()
    {
        _exitButton.onClick.AddListener(Exit);
        _rematchButton.onClick.AddListener(Rematch);
        SetMatchUI(false);
    }

    // Lo llama Matchmaking cuando me asignan un room.
    public void Join(string roomId)
    {
        _myUid = FirebaseService.Auth.CurrentUser.UserId;
        _rivalUid = null;
        _rivalName = "Rival";
        _rivalLeft = false;

        _roomReference = FirebaseService.Database.RootReference.Child("rooms").Child(roomId);
        _roomReference.Child("left").Child(_myUid).OnDisconnect().SetValue(true);
        _roomReference.ValueChanged += HandleRoomChanged;

        SetMatchUI(true);
        StartRound(1);
        StatusMessage.Show("¡Rival encontrado! Esperando que esté listo...");
    }

    private void StartRound(int round)
    {
        _round = round;
        _started = false;
        _finished = false;
        _title = "";
        _myScore = 0;
        _myAlive = true;
        _rivalScore = 0;
        _rivalAlive = true;
        _rematchButton.gameObject.SetActive(false);
        _roomReference.Child("ready").Child(_round.ToString()).Child(_myUid).SetValueAsync(true);
    }

    private void HandleRoomChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;
        _room = args.Snapshot;
        _dirty = true;
    }

    void Update()
    {
        if (!InMatch) return;

        // Cuenta regresiva de 3 s; al llegar a 0 arranca mi Snake.
        if (_countdown > 0)
        {
            _countdown -= Time.deltaTime;
            if (_countdown > 0)
            {
                StatusMessage.Show("La partida empieza en " + Mathf.CeilToInt(_countdown) + "...");
            }
            else
            {
                SnakeGameManager.Instance.StartGame();
                WriteLive();
            }
        }

        if (!_dirty || _room == null) return;
        _dirty = false;

        ReadRoom();
        if (_rivalUid == null) return;
        string round = _round.ToString();

        // Revancha: si los dos la pidieron, pasamos a la siguiente ronda en el mismo room.
        if (_finished && _room.Child("rematch").Child(round).ChildrenCount == 2)
        {
            StartRound(_round + 1);
            return;
        }

        // Inicio: cuando los dos marcaron ready, empieza la cuenta regresiva de cada uno.
        if (!_started && !_rivalLeft && _room.Child("ready").Child(round).ChildrenCount == 2)
        {
            _started = true;
            _countdown = 3f;
        }

        Evaluate();

        if (_finished && _rivalLeft)
        {
            _rematchButton.gameObject.SetActive(false);
            StatusMessage.Show("El rival salió.");
        }
        RefreshLabels();
    }

    private void ReadRoom()
    {
        if (_rivalUid == null)
        {
            foreach (DataSnapshot player in _room.Child("players").Children)
            {
                if (player.Key == _myUid) continue;
                _rivalUid = player.Key;
                _rivalName = player.Child("username").Value as string ?? "Rival";
            }
            if (_rivalUid == null) return;
        }

        _rivalLeft = _room.Child("left").HasChild(_rivalUid);
        DataSnapshot live = _room.Child("live").Child(_round.ToString()).Child(_rivalUid);
        _rivalScore = (int)FirebaseService.ToLong(live.Child("score").Value);
        _rivalAlive = !live.HasChild("alive") || (bool)live.Child("alive").Value;
    }

    // Lo llama SnakeGameManager cada vez que como (cambia mi puntaje).
    public void OnScoreChanged(int score)
    {
        if (!InMatch) return;
        _myScore = score;
        WriteLive();
        Evaluate();
        RefreshLabels();
    }

    // Lo llama SnakeGameManager al final de EndGame (choqué, o terminé por superar al rival).
    public void OnMyGameOver(int finalScore)
    {
        if (!InMatch) return;
        _myScore = finalScore;
        _myAlive = false;
        WriteLive();
        Evaluate();
        RefreshLabels();
    }

    // Regla de resultado, evaluada localmente con los datos compartidos.
    private void Evaluate()
    {
        if (_finished || _rivalUid == null) return;

        if (_rivalLeft)
        {
            // Si estoy jugando, termino mi partida; EndGame vuelve a llamar aquí con _myAlive = false.
            if (SnakeGameManager.Instance.IsPlaying) SnakeGameManager.Instance.EndGame();
            else Finish("VICTORIA");
            return;
        }
        if (!_started) return;

        if (_myAlive)
        {
            // El rival ya murió y lo superé: termino mi partida (guarda el puntaje) y gano.
            if (!_rivalAlive && _myScore > _rivalScore) SnakeGameManager.Instance.EndGame();
            return;
        }

        // Yo ya morí.
        if (_rivalAlive)
        {
            if (_rivalScore > _myScore) Finish("DERROTA");
            else _title = "ESPERANDO al rival...";
            return;
        }

        // Los dos murieron: gana el de mayor puntaje final.
        if (_myScore > _rivalScore) Finish("VICTORIA");
        else if (_myScore < _rivalScore) Finish("DERROTA");
        else Finish("EMPATE");
    }

    private void Finish(string title)
    {
        _finished = true;
        _countdown = 0;
        _title = title;
        if (UIManager.Instance.Current != AppScreen.GameOver) UIManager.Instance.Show(AppScreen.GameOver);
        _rematchButton.gameObject.SetActive(!_rivalLeft);
    }

    private void WriteLive()
    {
        var live = new Dictionary<string, object>
        {
            { "score", _myScore },
            { "alive", _myAlive }
        };
        _roomReference.Child("live").Child(_round.ToString()).Child(_myUid).SetValueAsync(live);
    }

    private void RefreshLabels()
    {
        _rivalLabel.text = "RIVAL (" + _rivalName + "): " + _rivalScore;
        _resultLabel.text = _title + "\nTú: " + _myScore + "   " + _rivalName + ": " + _rivalScore;
    }

    private void Rematch()
    {
        _roomReference.Child("rematch").Child(_round.ToString()).Child(_myUid).SetValueAsync(true);
        _rematchButton.gameObject.SetActive(false);
        StatusMessage.Show("Revancha: esperando al rival...");
    }

    private void Exit()
    {
        _roomReference.Child("left").Child(_myUid).SetValueAsync(true);
        _roomReference.ValueChanged -= HandleRoomChanged;
        _roomReference = null;
        _room = null;
        _countdown = 0;

        SetMatchUI(false);
        UIManager.Instance.Show(AppScreen.Home);
    }

    private void SetMatchUI(bool inMatch)
    {
        _rivalLabel.gameObject.SetActive(inMatch);
        _resultLabel.gameObject.SetActive(inMatch);
        _exitButton.gameObject.SetActive(inMatch);
        _rematchButton.gameObject.SetActive(false);
        foreach (GameObject button in _soloButtons) button.SetActive(!inMatch);
    }
}
