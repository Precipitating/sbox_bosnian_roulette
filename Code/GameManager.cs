using Sandbox;
using Sandbox.Html;
using Sandbox.UI;
using System.Threading.Tasks;


public sealed class GameManager : Component
{
	public void SetCamera(GameObject cameraGameObject, string excludeName)
	{
		cameraGameObject.Enabled = true;
		CameraComponent cam = cameraGameObject.GetComponent<CameraComponent>();
		cam.IsMainCamera = true;
		cam.RenderExcludeTags = new TagSet();
		cam.RenderExcludeTags.Add(excludeName);

	}

	// set camera and player refs
	public void AssignPlayer()
	{
		Player1Ref = Scene.Directory.FindByGuid( new System.Guid( "9fc4e073-1e10-460d-ae6f-754d9bd0a602" ) );
		Player2Ref = Scene.Directory.FindByGuid( new System.Guid( "00f9e4ed-b579-40e0-b035-f692be32203d" ) );
		if (!_player1Occupied)
		{
			SetCamera(Player1Camera, "player1" );
			_player1Occupied = true;

			_currentPlayer = Player1Ref;
			Log.Warning( $"Player1 camera chosen");


		}
		else if ( !_player2Occupied )
		{
			SetCamera( Player2Camera, "player2" );
			_player2Occupied = true;
			_currentPlayer = Player2Ref;
			Log.Warning( $"Player2 camera chosen" );

		}

	}

	async public Task NextTurn()
	{
		if (_bombRef.IsValid)
		{
			Log.Info( "VALID BOMB REF" );
		}
		if (!GameComplete )
		{
			CurrentTurn = !CurrentTurn;
			Log.Info( $"Is it your turn: {CurrentTurn}" );

			if ( !CurrentTurn && AIMode )
			{
				await _aiComponent.SimulateTurn();

				if ( GameComplete ) { return; }

				await NextTurn();
			}

		}

	}


	public void DetermineWinner()
	{
		if ( GameComplete ) { return; }
		GameComplete = true;
		YouWon = CurrentTurn ? false : true;
		CurrentTurn = false;

		if (YouWon)
		{
			var loserPlayer = !_player1Occupied ? Player1Ref.GetComponent<Prop>() : Player2Ref.GetComponent<Prop>();
			loserPlayer.IsStatic = false;
		}
		else
		{
			_currentPlayer.GetComponent<Prop>().IsStatic = false;

		}

		Log.Warning( $"Did you win? {YouWon}" );

	}


	protected override void OnAwake()
	{
		Instance = this;
		Log.Warning( "GameManager set." );
	}
	protected override void OnStart()
	{
		AssignPlayer();
		_bombRef = Scene.Directory.FindComponentByGuid( new System.Guid( "ad824361-8cfc-4f59-bfe5-0fae8b2a0b63" ) ) as Bomb;
		_aiComponent = Scene.Directory.FindComponentByGuid( new System.Guid( "0684f319-631a-4ead-84a5-41213a1e27d0" ) ) as AiMode;
		Log.Warning( $"MANAGER BombRef instance: {_bombRef?.GetHashCode()}" );
	}

	public static GameManager Instance { get; private set; } = null;
	[Property] public GameObject Player1Camera { get; private set; } = null;
	[Property] public GameObject Player2Camera { get; private set; } = null;
	private AiMode _aiComponent;
	private Bomb _bombRef { get; set; }
	[Property] public bool YouWon { get; set; } = false;
	public bool AIMode = true;
	public bool CurrentTurn { get; set; } = true;
	public bool GameComplete { get; private set; } = false;

	private bool _player1Occupied = false;
	private bool _player2Occupied = false;
	private GameObject Player1Ref = null;
	private GameObject Player2Ref= null;
	private GameObject _currentPlayer = null;





}
