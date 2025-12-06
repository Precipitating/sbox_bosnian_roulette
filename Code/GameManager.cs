using Sandbox;
using Sandbox.Html;
using Sandbox.UI;
using System.Threading.Tasks;
public sealed class GameManager : Component
{
	public void SetCamera(GameObject cameraGameObject, string excludeName, ref bool assignVar)
	{
		cameraGameObject.Enabled = true;
		CameraComponent cam = cameraGameObject.GetComponent<CameraComponent>();
		cam.IsMainCamera = true;
		cam.RenderExcludeTags = new TagSet();
		cam.RenderExcludeTags.Add(excludeName);
		assignVar = true;

	}
	public void AssignPlayer()
	{
		if (!Player1Assigned)
		{
			SetCamera(Player1Camera, "player1", ref Player1Assigned );
			Player1Assigned = true;


		}
		else if (!Player2Assigned )
		{
			SetCamera( Player2Camera, "player2", ref Player2Assigned );
			Player2Assigned = true;

		}
	}

	public async Task<float> NextTurn()
	{
		if (!GameComplete )
		{
			CurrentTurn = !CurrentTurn;
			Log.Info( $"Is it your turn: {CurrentTurn}" );

			if ( !CurrentTurn && AIMode )
			{
				float enemyVal = await _aiComponent.SimulateTurn();
				if ( GameComplete ) { return -1; }
				return enemyVal;
			}

		}
		return -1;

	}


	public void DetermineWinner()
	{
		if ( GameComplete ) { return; }
		GameComplete = true;
		YouWon = CurrentTurn ? false : true;
		CurrentTurn = false;

		Log.Warning( $"Did you win? {YouWon}" );

	}

	protected override void OnStart()
	{
		Instance = this;
		AssignPlayer();
		_bombRef = Scene.Directory.FindComponentByGuid( new System.Guid( "ad824361-8cfc-4f59-bfe5-0fae8b2a0b63" ) ) as Bomb;
		_aiComponent = Scene.Directory.FindComponentByGuid( new System.Guid( "0684f319-631a-4ead-84a5-41213a1e27d0" ) ) as AiMode;
		Log.Warning( $"MANAGER BombRef instance: {_bombRef?.GetHashCode()}" );
	}

	public static GameManager Instance { get; private set; }
	[Property] public GameObject Player1Camera { get; private set; }
	[Property] public GameObject Player2Camera { get; private set; }
	private AiMode _aiComponent;
	private Bomb _bombRef { get; set; }
	[Property] public bool YouWon { get; set; } = false;
	public bool AIMode = true;
	public bool CurrentTurn { get; set; } = true;
	public bool GameComplete { get; private set; } = false;

	private bool Player1Assigned = false;
	private bool Player2Assigned = false;





}
