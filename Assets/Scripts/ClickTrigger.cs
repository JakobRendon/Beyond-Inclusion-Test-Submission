﻿using UnityEngine;

public class ClickTrigger : MonoBehaviour
{
	TicTacToeAI _ai;

	[SerializeField]
	private int _myCoordX = 0;
	[SerializeField]
	private int _myCoordY = 0;

	[SerializeField]
	private bool canClick;

	private void Awake()
	{
		_ai = FindObjectOfType<TicTacToeAI>();
	}

	private void Start()
    {

		_ai.onGameStarted.AddListener(AddReference);
		_ai.onGameStarted.AddListener(() => SetInputEndabled(true));
		_ai.onPlayerWin.AddListener((win) => SetInputEndabled(false));
	}

	private void SetInputEndabled(bool val)
    {
		canClick = val;
	}

	private void AddReference()
	{
		_ai.RegisterTransform(_myCoordX, _myCoordY, this);
		canClick = true;
	}

	private void OnMouseDown()
	{
		if(canClick){
			_ai.PlayerSelects(_myCoordX, _myCoordY);
		}
	}

    /// <summary>
    /// Interface function so the TicTacToeAI can tell selected triggers they are no longer clickable
    /// </summary>
    public void TurnOffTrigger()
    {
        canClick = false;
    }
}