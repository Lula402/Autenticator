using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Snake clásico sobre una cuadrícula. Comida roja = +10, comida dorada (temporal) = +30.
// Cada comida alarga la serpiente y la acelera un poco. Chocar con el borde o con
// el propio cuerpo termina la partida.
public class SnakeGameManager : MonoBehaviour
{
    public static SnakeGameManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField]
    private TMP_Text _scoreLabel;
    [SerializeField]
    private TMP_Text _lengthLabel;

    [Header("Game Over")]
    [SerializeField]
    private TMP_Text _finalScoreLabel;
    [SerializeField]
    private TMP_Text _recordLabel;

    [Header("Tablero")]
    [SerializeField]
    private int _columns = 24;
    [SerializeField]
    private int _rows = 13;
    [SerializeField]
    private int _startLength = 3;

    [Header("Velocidad (segundos por paso)")]
    [SerializeField]
    private float _startStepInterval = 0.16f;
    [SerializeField]
    private float _minStepInterval = 0.06f;
    [SerializeField]
    private float _speedUpPerFood = 0.004f;

    [Header("Puntos")]
    [SerializeField]
    private int _foodPoints = 10;
    [SerializeField]
    private int _bonusPoints = 30;
    [SerializeField]
    private float _bonusChance = 0.25f;
    [SerializeField]
    private float _bonusLifetime = 6f;

    [Header("Colores")]
    [SerializeField]
    private Color _boardColor = new Color32(27, 27, 27, 255);
    [SerializeField]
    private Color _headColor = new Color32(242, 238, 220, 255);
    // El cuerpo va rotando por los colores del arcoíris
    [SerializeField]
    private Color[] _bodyColors =
    {
        new Color32(0, 164, 153, 255),
        new Color32(16, 167, 224, 255),
        new Color32(140, 70, 190, 255),
        new Color32(229, 40, 126, 255),
        new Color32(240, 138, 36, 255),
        new Color32(247, 167, 27, 255)
    };
    [SerializeField]
    private Color _foodColor = new Color32(230, 50, 39, 255);
    [SerializeField]
    private Color _bonusColor = new Color32(255, 213, 0, 255);

    // Swipe mínimo en píxeles para cambiar de dirección en pantalla táctil.
    private const float MinSwipeDistance = 40f;

    private readonly List<Vector2Int> _body = new List<Vector2Int>();
    private readonly List<SpriteRenderer> _segments = new List<SpriteRenderer>();
    private readonly List<Vector2Int> _directionQueue = new List<Vector2Int>();

    private Vector2Int _direction;
    private Vector2Int _food;
    private Vector2Int _bonus;
    private bool _hasBonus;
    private float _bonusTimer;

    private SpriteRenderer _boardRenderer;
    private SpriteRenderer _foodRenderer;
    private SpriteRenderer _bonusRenderer;

    private float _cellSize;
    private Vector2 _origin;
    private float _stepTimer;
    private float _stepInterval;
    private int _score;
    private Vector2 _swipeStart;
    private bool _swipeActive;

    public bool IsPlaying { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void StartGame()
    {
        ComputeLayout();
        EnsureBoard();

        _body.Clear();
        Vector2Int start = new Vector2Int(_columns / 2, _rows / 2);
        for (int i = 0; i < _startLength; i++) _body.Add(start - new Vector2Int(i, 0));

        _direction = Vector2Int.right;
        _directionQueue.Clear();
        _swipeActive = false;
        _hasBonus = false;
        _score = 0;
        _stepInterval = _startStepInterval;
        _stepTimer = 0f;

        SpawnFood();
        SetBoardVisible(true);
        Render();
        UpdateHud();

        IsPlaying = true;
        UIManager.Instance.Show(AppScreen.Game);
    }

    public void EndGame()
    {
        if (!IsPlaying) return;
        StopGame();

        UIManager.Instance.Show(AppScreen.GameOver);
        _finalScoreLabel.text = _score.ToString();
        _recordLabel.text = "Guardando puntaje...";

        int finalScore = _score;
        ScoreService.SubmitScore(finalScore, (success, isNewRecord, best) =>
        {
            if (_recordLabel == null) return;
            if (!success)
            {
                _recordLabel.text = "No se pudo guardar el puntaje.";
            }
            else if (isNewRecord && finalScore > 0)
            {
                _recordLabel.text = "¡Nuevo récord personal!";
            }
            else
            {
                _recordLabel.text = "Tu récord: " + best;
            }
        });
    }

    public void StopGame()
    {
        IsPlaying = false;
        SetBoardVisible(false);
    }

    void Update()
    {
        if (!IsPlaying) return;

        ReadInput();

        if (_hasBonus)
        {
            _bonusTimer -= Time.deltaTime;
            if (_bonusTimer <= 0f)
            {
                _hasBonus = false;
                Render();
            }
            else
            {
                float pulse = 0.75f + Mathf.PingPong(Time.time * 2f, 0.25f);
                _bonusRenderer.transform.localScale = Vector3.one * _cellSize * pulse;
            }
        }

        _stepTimer += Time.deltaTime;
        if (_stepTimer >= _stepInterval)
        {
            _stepTimer -= _stepInterval;
            Step();
        }
    }

    // ================= Lógica =================

    private void Step()
    {
        if (_directionQueue.Count > 0)
        {
            _direction = _directionQueue[0];
            _directionQueue.RemoveAt(0);
        }

        Vector2Int newHead = _body[0] + _direction;
        bool eatsFood = newHead == _food;
        bool eatsBonus = _hasBonus && newHead == _bonus;
        bool grows = eatsFood || eatsBonus;

        if (!IsInsideBoard(newHead) || HitsBody(newHead, grows))
        {
            EndGame();
            return;
        }

        _body.Insert(0, newHead);
        if (!grows) _body.RemoveAt(_body.Count - 1);

        if (eatsFood)
        {
            _score += _foodPoints;
            _stepInterval = Mathf.Max(_minStepInterval, _stepInterval - _speedUpPerFood);
            SpawnFood();
            if (!_hasBonus && Random.value < _bonusChance) SpawnBonus();
        }
        if (eatsBonus)
        {
            _score += _bonusPoints;
            _hasBonus = false;
        }

        Render();
        if (grows) UpdateHud();
    }

    private bool IsInsideBoard(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < _columns && cell.y >= 0 && cell.y < _rows;
    }

    private bool HitsBody(Vector2Int cell, bool grows)
    {
        // Si no crece, la cola se mueve en este paso y esa celda queda libre.
        int count = grows ? _body.Count : _body.Count - 1;
        for (int i = 0; i < count; i++)
        {
            if (_body[i] == cell) return true;
        }
        return false;
    }

    private void SpawnFood()
    {
        _food = RandomFreeCell();
    }

    private void SpawnBonus()
    {
        _bonus = RandomFreeCell();
        if (_bonus == _food) return;
        _hasBonus = true;
        _bonusTimer = _bonusLifetime;
    }

    private Vector2Int RandomFreeCell()
    {
        var free = new List<Vector2Int>();
        for (int x = 0; x < _columns; x++)
        {
            for (int y = 0; y < _rows; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!_body.Contains(cell) && cell != _food) free.Add(cell);
            }
        }
        return free.Count > 0 ? free[Random.Range(0, free.Count)] : Vector2Int.zero;
    }

    // ================= Entrada =================

    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) QueueDirection(Vector2Int.up);
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) QueueDirection(Vector2Int.down);
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) QueueDirection(Vector2Int.left);
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) QueueDirection(Vector2Int.right);
        }

        // Swipe con el dedo (o arrastrando con el mouse en el editor).
        Pointer pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
        {
            _swipeStart = pointer.position.ReadValue();
            _swipeActive = true;
        }
        else if (_swipeActive && pointer.press.isPressed)
        {
            Vector2 delta = pointer.position.ReadValue() - _swipeStart;
            if (delta.magnitude >= MinSwipeDistance)
            {
                QueueDirection(Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                    ? (delta.x > 0 ? Vector2Int.right : Vector2Int.left)
                    : (delta.y > 0 ? Vector2Int.up : Vector2Int.down));
                // Permite encadenar giros sin levantar el dedo.
                _swipeStart = pointer.position.ReadValue();
            }
        }
        else if (pointer.press.wasReleasedThisFrame)
        {
            _swipeActive = false;
        }
    }

    // Guarda hasta 2 giros para que los giros rápidos no se pierdan entre pasos.
    private void QueueDirection(Vector2Int direction)
    {
        Vector2Int last = _directionQueue.Count > 0 ? _directionQueue[_directionQueue.Count - 1] : _direction;
        if (direction == last || direction == -last) return;
        if (_directionQueue.Count < 2) _directionQueue.Add(direction);
    }

    // ================= Dibujo =================

    private void ComputeLayout()
    {
        Camera cam = Camera.main;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float worldPerPixel = halfHeight * 2f / Screen.height;

        // Se deja espacio arriba para el HUD y abajo para el mensaje de estado y el pie de página.
        float topMargin = 110f * worldPerPixel * Screen.height / 1080f;
        float bottomMargin = 150f * worldPerPixel * Screen.height / 1080f;
        float sideMargin = 0.4f;

        float availableWidth = halfWidth * 2f - sideMargin * 2f;
        float availableHeight = halfHeight * 2f - topMargin - bottomMargin;
        _cellSize = Mathf.Min(availableWidth / _columns, availableHeight / _rows);

        Vector3 center = cam.transform.position;
        float boardCenterY = center.y + (bottomMargin - topMargin) / 2f;
        _origin = new Vector2(
            center.x - _columns * _cellSize / 2f + _cellSize / 2f,
            boardCenterY - _rows * _cellSize / 2f + _cellSize / 2f);
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(_origin.x + cell.x * _cellSize, _origin.y + cell.y * _cellSize, 0f);
    }

    private void EnsureBoard()
    {
        if (_boardRenderer == null)
        {
            _boardRenderer = CreateRenderer("Board", SpriteFactory.Square, _boardColor, 5);
            _foodRenderer = CreateRenderer("Food", SpriteFactory.Circle, _foodColor, 12);
            _bonusRenderer = CreateRenderer("Bonus", SpriteFactory.Circle, _bonusColor, 12);
        }

        _boardRenderer.transform.position = new Vector3(
            _origin.x + (_columns - 1) * _cellSize / 2f,
            _origin.y + (_rows - 1) * _cellSize / 2f, 0f);
        _boardRenderer.transform.localScale = new Vector3(_columns * _cellSize, _rows * _cellSize, 1f);
        _foodRenderer.transform.localScale = Vector3.one * _cellSize * 0.8f;
    }

    private SpriteRenderer CreateRenderer(string name, Sprite sprite, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
        return spriteRenderer;
    }

    private void SetBoardVisible(bool visible)
    {
        if (_boardRenderer != null) _boardRenderer.gameObject.SetActive(visible);
        if (_foodRenderer != null) _foodRenderer.gameObject.SetActive(visible);
        if (_bonusRenderer != null) _bonusRenderer.gameObject.SetActive(visible && _hasBonus);
        foreach (SpriteRenderer segment in _segments) segment.gameObject.SetActive(visible);
    }

    private void Render()
    {
        while (_segments.Count < _body.Count)
        {
            _segments.Add(CreateRenderer("Segment", SpriteFactory.RoundedBox, _headColor, 10));
        }
        while (_segments.Count > _body.Count)
        {
            Destroy(_segments[_segments.Count - 1].gameObject);
            _segments.RemoveAt(_segments.Count - 1);
        }

        for (int i = 0; i < _body.Count; i++)
        {
            SpriteRenderer segment = _segments[i];
            segment.gameObject.SetActive(true);
            segment.transform.position = CellToWorld(_body[i]);
            segment.transform.localScale = Vector3.one * _cellSize * (i == 0 ? 0.95f : 0.85f);
            segment.color = i == 0 || _bodyColors.Length == 0 ? _headColor : _bodyColors[(i - 1) % _bodyColors.Length];
            segment.sortingOrder = i == 0 ? 11 : 10;
        }

        _foodRenderer.transform.position = CellToWorld(_food);
        _bonusRenderer.gameObject.SetActive(_hasBonus);
        if (_hasBonus) _bonusRenderer.transform.position = CellToWorld(_bonus);
    }

    private void UpdateHud()
    {
        _scoreLabel.text = "Puntaje: " + _score;
        _lengthLabel.text = "Longitud: " + _body.Count;
    }
}
