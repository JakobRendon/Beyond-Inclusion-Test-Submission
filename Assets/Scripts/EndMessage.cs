using TMPro;
using UnityEngine;

public class EndMessage : MonoBehaviour
{
	[SerializeField]
	private TMP_Text _playerMessage = null;

    /// <summary>
    /// Function that is called to announce the winner of the game.
    /// </summary>
    /// <param name="winner">Passing -1 means theres a tie, 1 means AI wins, and 0 means player wins</param>
	public void OnGameEnded(int winner)
	{
		_playerMessage.text = winner == -1 ? "Tie" : winner == 1 ? "AI wins" : "Player wins";
	}
}
