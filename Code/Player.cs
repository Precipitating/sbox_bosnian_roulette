using System;
using System.Threading.Tasks;
using Sandbox;

public enum PlayerType
{
	None = 0,
	Player1 = 1,
	Player2 = 2,
}

public sealed record PlayerReferences
(
	GameObject PlayerModel,
	GameObject PlayerNeck,
	GameObject PlayerCamera
);

public sealed class Player
{

	public Player(PlayerType type, PlayerReferences references)
	{
		PlayerRef = references;
		PlayerId = type;

		switch ( type )
		{
			case PlayerType.Player1: TagName = "player1"; break;
			case PlayerType.Player2: TagName = "player2"; break;
		}
	}



	public PlayerReferences PlayerRef { get; set; }
	public PlayerType PlayerId { get; set; } = PlayerType.None;
	public string TagName { get; set; }



}
