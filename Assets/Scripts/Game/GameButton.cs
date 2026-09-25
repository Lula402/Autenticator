using UnityEngine;
using UnityEngine.UI;

public enum GameButtonAction
{
    StartGame,
    EndGame
}

// Botón "Jugar" / "Jugar de nuevo" / "Terminar partida".
public class GameButton : MonoBehaviour
{
    [SerializeField]
    private Button _button;
    [SerializeField]
    private GameButtonAction _action;

    private void Reset()
    {
        _button = GetComponent<Button>();
    }

    void Start()
    {
        _button.onClick.AddListener(() =>
        {
            if (_action == GameButtonAction.StartGame) SnakeGameManager.Instance.StartGame();
            else SnakeGameManager.Instance.EndGame();
        });
    }
}
