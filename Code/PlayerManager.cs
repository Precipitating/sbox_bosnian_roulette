using Sandbox;
using System;
using System.Threading.Tasks;

public sealed class PlayerManager : Component
{

	public PlayerType GetNewPlayerId()
	{
		PlayerType validPlayer;

		if (Networking.IsHost)
		{
			validPlayer = PlayerType.Player1;

		}
		else
		{
			validPlayer = PlayerType.Player2;
		}

		return validPlayer;
	}

	public GameObject GetPlayerModel(PlayerType playerType)
	{
		return (playerType == PlayerType.Player1) ? Player1Ref : Player2Ref;
	}

	public GameObject GetPlayerCamera( PlayerType playerType )
	{
		return (playerType == PlayerType.Player1) ? _player1Camera: _player2Camera;
	}


	public GameObject GetPlayerNeck( PlayerType playerType )
	{
		return (playerType == PlayerType.Player1) ? _player1Neck : _player2Neck;
	}


	void AddPlayerToList( PlayerType id, Player player)
	{
		PlayerList[id] = player;
		_assignedPlayers[id] = true;
		Log.Info( $"{id} added to player list" );
		Log.Info( _assignedPlayers );
	}

	
	public Player AddPlayer(bool aiMode = false)
	{
		PlayerType validId = (aiMode ? PlayerType.Player2 : GetNewPlayerId());
		GameObject playerModel = GetPlayerModel( validId );
		GameObject playerNeck = GetPlayerNeck( validId );
		GameObject playerCamera= GetPlayerCamera( validId );
		Player newPlayer = new Player( validId, new PlayerReferences( playerModel, playerNeck, playerCamera));

		AddPlayerToList( validId, newPlayer );
		Log.Info( $"Player added: {newPlayer}" );

		CurrentPlayer = (Networking.IsHost || aiMode) ? PlayerList[PlayerType.Player1] : PlayerList[PlayerType.Player2];

		return newPlayer;
	}

	public void RemovePlayer(PlayerType playerToRemove)
	{
		_assignedPlayers.Remove(playerToRemove);
		PlayerList.Remove( playerToRemove );

	}

	public async Task RotateHeadY(PlayerType playerType, float targetRoll )
	{
		Player player = null;
		if (!PlayerList.TryGetValue(playerType, out player) ) { return; }

		var neck = player.PlayerRef.PlayerNeck;
		var targetRot = new Angles( 0f, targetRoll, 0f ).ToRotation();
		while ( !neck.LocalRotation.AlmostEqual( targetRot, 0.001f ) )
		{
			float frac = MathF.Min( 1f, 10f * Time.Delta );
			neck.LocalRotation = neck.LocalRotation.LerpTo( targetRot, frac );

			await Task.Frame();
		}

		neck.LocalRotation = targetRot;
	}


	public Dictionary<PlayerType, Player> PlayerList {  get; private set; } = new Dictionary<PlayerType, Player>();

	public Player CurrentPlayer { get; private set; }

	[Sync] private Dictionary<PlayerType, bool> _assignedPlayers { get; set; } = new Dictionary<PlayerType, bool>();

	[Property] public GameObject Player1Ref { get; set; }
	[Property] private GameObject _player1Neck { get; set; }
	[Property] private GameObject _player1Camera { get; set; }
	[Property] public GameObject Player2Ref { get; set; }
	[Property] private GameObject _player2Neck{ get; set; }
	[Property] private GameObject _player2Camera{ get; set; }





}
