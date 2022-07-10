using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum TicTacToeState{none, cross, circle}

[System.Serializable]
public class WinnerEvent : UnityEvent<int>
{
}

public class TicTacToeAI : MonoBehaviour
{
	public UnityEvent onGameStarted;

	//Call This event with the player number to denote the winner
	public WinnerEvent onPlayerWin;

	[SerializeField]
	private bool _isPlayerTurn = true;

	[SerializeField]
	private bool gameOver = false;

	[SerializeField]
	private int _gridSize = 3;

	[SerializeField]
	private GameObject _xPrefab;

	[SerializeField]
	private GameObject _oPrefab;
	
    private TicTacToeState[,] boardState;

	private TicTacToeState playerState = TicTacToeState.circle;
    private TicTacToeState aiState = TicTacToeState.cross;

	private ClickTrigger[,] _triggers;

    private readonly YieldInstruction waitTime = new WaitForSeconds(0.75f);

    //These two arrays are used to decide where to place the AI flag if all other checks return null
    //Manually created for most optimal AI performance in my opinion. The order these lower arrays are checked
    //can be swapped to block certain known guaranteed player wins like claiming 3 corners
    private int[][] cornerPosArray = new int[][]
    {
        //Check the corner positions to block triangle strategy if player claimed center position
        new int[] { 0, 0 },
        new int[] { 2, 2 },
        new int[] { 0, 2 },
        new int[] { 2, 0 },
    };
    private int[][] centerSidePosArray = new int[][]
    {
        //Check the center side positions to block 3 corner strategy if ai claimed center position
        new int[] { 0, 1 },
        new int[] { 1, 2 },
        new int[] { 2, 1 },
        new int[] { 1, 0 },
    };
	
	private void Awake()
	{
		if(onPlayerWin == null)
        {
			onPlayerWin = new WinnerEvent();
		}
	}

	public void StartAI()
    {
		StartGame();
	}

	public void RegisterTransform(int myCoordX, int myCoordY, ClickTrigger clickTrigger)
	{
		_triggers[myCoordX, myCoordY] = clickTrigger;
	}

	private void StartGame()
	{
        _isPlayerTurn = true;
        _triggers = new ClickTrigger[3,3];
		boardState = new TicTacToeState[3,3];
		onGameStarted.Invoke();
	}

	public void PlayerSelects(int coordX, int coordY)
    {
        if (!_isPlayerTurn)
            return;

		SetVisual(coordX, coordY, playerState);
        _isPlayerTurn = false;
        StartCoroutine(AIDecisionSequence());
    }

	public void AiSelects(int coordX, int coordY)
    {
		SetVisual(coordX, coordY, aiState);
        _isPlayerTurn = true;
    }

	private void SetVisual(int coordX, int coordY, TicTacToeState targetState)
	{
		Instantiate(
			targetState == TicTacToeState.circle ? _oPrefab : _xPrefab,
			_triggers[coordX, coordY].transform.position,
			Quaternion.identity
		);
        _triggers[coordX, coordY].TurnOffTrigger();
        boardState[coordX, coordY] = targetState;

        //Checking to see if someone has won
        StraightChecks(true, coordX);
        StraightChecks(false, coordY);
        DiagonalChecks();

#if UNITY_EDITOR
        //LogBoardState();
#endif
    }

    /// <summary>
    /// After the player has made their choice this function will be called and the AI will make their turn
    /// </summary>
    private IEnumerator AIDecisionSequence()
    {
        int[] coords = StraightChecks(true);
        if (coords == null && !gameOver)
        {
            coords = StraightChecks(false);
            if (coords == null && !gameOver)
                coords = DiagonalChecks();
        }

        if(!gameOver)
        {
            yield return waitTime;
            Debug.Log($"[{coords[0]},{coords[1]}]");
            AiSelects(coords[0], coords[1]);
        }
    }

    /// <summary>
    /// Function that performs 3 straight checks across the board depending on the boolean passed in.
    /// If startPos is passed in the checks start at that position and check for AI win
    /// </summary>
    /// <param name="horizontalCheck">True if the 3 checks performed should be horizontal from top to bottom.
    /// False if the 3 checks performed should be vertical from left to right.</param>
    /// <param name="startPos">Used when checking if AI has won. Pass in one of the coords from the AIs recently selected tile.</param>
    /// <returns>Returns an empty position on the board if a victory or loss is detected, else returns null.</returns>
    private int[] StraightChecks(bool horizontalCheck, int startPos = 0)
    {
        if(horizontalCheck)
            Debug.Log("HorizontalChecks");
        else
            Debug.Log("VerticalChecks");

        for (int i = 0 + startPos; i < _gridSize; i++)
        {
            int[] emptyPos = null;
            int aICount = 0, playerCount = 0;
            for (int j = 0; j < _gridSize; j++)
            {
                TicTacToeState tileState = horizontalCheck ? boardState[i, j] : boardState[j, i];
                switch (tileState)
                {
                    //PlayerState
                    case TicTacToeState.circle:
                        playerCount++;
                        break;
                    //AIState
                    case TicTacToeState.cross:
                        aICount++;
                        break;
                    case TicTacToeState.none:
                        //save empty position for future use
                        if (emptyPos == null)
                            emptyPos = horizontalCheck ? new int[] { i, j } : new int[] { j, i };
                        else//If this state has been detected twice then theres no change for win or loss on this check, continue
                            continue;
                        break;
                }
                if(playerCount == 3 || aICount == 3)
                {
                    gameOver = true;
                    onPlayerWin.Invoke(playerCount == 3 ? 0 : 1);
                }
                //If a potential victory or loss is detected return emptyPosition
                else if ((playerCount == 2 || aICount == 2) && emptyPos != null)
                    return emptyPos;
            }
        }
        return null;
    }

    /// <summary>
    /// Runs the Diagonal checks and returns a value for the AI to place their mark
    /// </summary>
    /// <returns>Returns an empty position along the diagonal checks if no potential loss detected</returns>
    private int[] DiagonalChecks()
    {
        Debug.Log("DiagonalChecks");
        int aICount = 0, playerCount = 0, r = 0;
        int[] emptyPos = null;
        bool cFlipped = false;

        for (int c = -_gridSize; c < _gridSize; c++)
        {
            int appliedC = c < 0 ? _gridSize + c : _gridSize - 1 - c;
            //Debug.Log($"[{r},{appliedC}] | actual C: {c}");

            if(!cFlipped && c >= 0)
            {
                aICount = playerCount = 0;
                cFlipped = true;
                emptyPos = null;
            }

            switch (boardState[r, appliedC])
            {
                //PlayerState
                case TicTacToeState.circle:
                    playerCount++;
                    break;
                //AIState
                case TicTacToeState.cross:
                    aICount++;
                    break;
                case TicTacToeState.none:
                    //Save empty position for future use
                    if (emptyPos == null)
                        emptyPos = new int[] { r, appliedC };
                    else//If this state has been detected twice then theres no change for win or loss on this check, continue
                        continue;
                    break;
            }
            if (playerCount == 3 || aICount == 3)
            {
                gameOver = true;
                onPlayerWin.Invoke(playerCount == 3 ? 0 : 1);
            }
            //If a potential victory or loss is detected return emptyPosition
            else if ((playerCount >= 2 || aICount >= 2) && emptyPos != null)
                return emptyPos;
            r = (r+1) % 3;
        }

        emptyPos = FindEmptyPosition();
        if (emptyPos == null)
        {
            gameOver = true;
            onPlayerWin.Invoke(-1);
        }
        return emptyPos;
    }

    /// <summary>
    /// Finds the most optimal empty position for the AI to place their flag by iterating through a position array that was manually created
    /// </summary>
    /// <returns>Returns the first empty position found or null if all positions are filled</returns>
    private int[] FindEmptyPosition()
    {
        Debug.Log("FindEmptyPosition");

        //Used to make sure that the 2 check arrays are both parsed for available positions
        bool GoToUsed = false;

        switch (boardState[1, 1])
        {
            //Try to claim center for defensive play
            case TicTacToeState.none:
                return new int[] { 1, 1 };
            //If ai claimed center block 3 corner strategy
            case TicTacToeState.cross:
                crossCheck:
                foreach(int[] pos in centerSidePosArray)
                {
                    if (boardState[pos[0], pos[1]] == TicTacToeState.none)
                        return pos;
                }
                if(!GoToUsed)
                {
                    GoToUsed = true;
                    goto circleCheck;
                }
                break;
            //If player claimed center block triangle strategy
            case TicTacToeState.circle:
                circleCheck:
                foreach (int[] pos in cornerPosArray)
                {
                    if (boardState[pos[0], pos[1]] == TicTacToeState.none)
                        return pos;
                }
                if (!GoToUsed)
                {
                    GoToUsed = true;
                    goto crossCheck;
                }
                break;
        }
        return null;
    }

#if UNITY_EDITOR
    private void LogBoardState()
    {
        string logString = string.Empty;

        for(int r = 0; r < _gridSize; r++)
        {
            for (int c = 0; c < _gridSize; c++)
            {
                switch (boardState[r,c])
                {
                    case TicTacToeState.circle:
                        logString += "[O]";
                        break;
                    case TicTacToeState.cross:
                        logString += "[X]";
                        break;
                    case TicTacToeState.none:
                        logString += "[_]";
                        break;
                }
            }
            logString += "\n";
        }
        Debug.Log(logString);
    }
#endif
}
