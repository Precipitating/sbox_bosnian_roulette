using Sandbox;

public sealed class PlayerManager : Component
{

	public PlayerType GetNewPlayerId()
	{
		PlayerType validPlayer = (_assignedPlayers[PlayerType.Player1] ? PlayerType.Player2 : PlayerType.Player1);
		_assignedPlayers[validPlayer] = true;

		return validPlayer;
	}

	public GameObject GetPlayerModel(PlayerType playerType)
	{
		return (playerType == PlayerType.Player1) ? _player1Ref : _player2Ref;
	}

	public GameObject GetPlayerCamera( PlayerType playerType )
	{
		return (playerType == PlayerType.Player1) ? _player1Camera: _player2Camera;
	}


	public GameObject GetPlayerNeck( PlayerType playerType )
	{
		return (playerType == PlayerType.Player1) ? _player1Neck : _player2Neck;
	}

	public void AddPlayer()
	{
		PlayerType validId = GetNewPlayerId();

		GameObject playerModel = GetPlayerModel( validId );
		GameObject playerNeck = GetPlayerNeck( validId );
		GameObject playerCamera= GetPlayerCamera( validId );
		Player newPlayer = new Player( validId, new PlayerReferences( playerModel, playerNeck, playerCamera));

		PlayerList.Add(newPlayer);
	}

	public void RemovePlayer(Player playerToRemove)
	{
		PlayerList.Remove( playerToRemove );
		_assignedPlayers.Remove( playerToRemove.PlayerId );
	}


	public List<Player> PlayerList {  get; private set; } = new List<Player>();

	private Dictionary<PlayerType, bool> _assignedPlayers = new Dictionary<PlayerType, bool>();

	[Property] private GameObject _player1Ref { get; set; }
	[Property] private GameObject _player1Neck { get; set; }
	[Property] private GameObject _player1Camera { get; set; }
	[Property] private GameObject _player2Ref { get; set; }
	[Property] private GameObject _player2Neck{ get; set; }
	[Property] private GameObject _player2Camera{ get; set; }


}
