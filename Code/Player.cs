using Sandbox;
using System.Runtime.CompilerServices;
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
		_playerReferences = references;
		PlayerId = type;
	}



	private PlayerReferences _playerReferences;
	public PlayerType PlayerId { get; set; } = PlayerType.None;



}
