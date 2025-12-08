using Sandbox;
using Sandbox.Html;
using Sandbox.UI;
using System.Threading.Tasks;


public sealed class GameManager : Component
{
	public void SetCamera(GameObject cameraGameObject, string excludeName = null )
	{
		cameraGameObject.Enabled = true;
		CameraComponent cam = cameraGameObject.GetComponent<CameraComponent>();
		cam.IsMainCamera = true;

		if ( !string.IsNullOrEmpty( excludeName ) )
		{
			cam.RenderExcludeTags = new TagSet();
			cam.RenderExcludeTags.Add( excludeName );
		}

		if ( _activeCamera is not null )
		{
			CameraComponent oldCam = _activeCamera.GetComponent<CameraComponent>();
			oldCam.IsMainCamera = false;
			_activeCamera.Enabled = false;
			_activeCamera = null;
		}
		_activeCamera = cameraGameObject;
	}


	//public void ResetGame()
	//{
	//	Reset();
	//	//GameStarted = false;
	//	//GameComplete = false;
	//	//CurrentTurn = true;
	//	//MainMenu();


	//}

	// set camera and player refs
	public void AssignPlayer()
	{

		if ( !_player1Occupied )
		{
			SetCamera( Player1Camera );
			_player1Occupied = true;

			_currentPlayer = Player1Model;
			Log.Warning( $"Player1 camera chosen" );


		}
		else if ( !_player2Occupied )
		{
			SetCamera( Player2Camera );
			_player2Occupied = true;
			_currentPlayer = Player2Model;
			Log.Warning( $"Player2 camera chosen" );

		}

	}

	async public Task NextTurn()
	{
		if ( _bombRef.IsActive )
		{
			CurrentTurn = !CurrentTurn;
			Log.Info( $"Is it your turn: {CurrentTurn}" );

			if ( !CurrentTurn && AIMode )
			{
				await _aiComponent.SimulateTurn();

				if ( !_bombRef.IsActive ) { return; }

				await NextTurn();
			}

		}

	}


	[Button]
	public async Task DetermineWinner()
	{
		if ( GameComplete ) { return; }
		Log.Warning( "Determining Winner..." );
		GameComplete = true;
		_youWon = CurrentTurn ? false : true;
		CurrentTurn = false;

		if ( _youWon )
		{
			var loserPlayer = !_player1Occupied ? Player1Model.GetComponent<Prop>() : Player2Model.GetComponent<Prop>();
			loserPlayer.IsStatic = false;
			bombRadiusDmg.Enabled = true;
			_bombRef.GameObject.Enabled = false;

		}
		else
		{

			_currentPlayer.GetComponent<Prop>().IsStatic = false;
			Player1Camera.Enabled = false;
			bombRadiusDmg.Enabled = true;
			_bombRef.GameObject.Enabled = false;


		}

		Log.Warning( $"Did you win? {_youWon}" );
		await GameTask.DelaySeconds( 5 );
		Scene.LoadFromFile( "scenes/main.scene" );

	}


	private void MainMenu()
	{
		SetCamera( OverheadCamera );
	}
	protected override void OnAwake()
	{
		Instance = this;
		Log.Warning( "GameManager set." );
	}

	public void PlayAI()
	{
		AssignPlayer();
		_bombRef.IsActive = true;
		GameStarted = true;
		BombUI.Enabled = true;

		Log.Info( $"IsActive: {_bombRef.IsActive}. GameStarted: {GameStarted}" );
	}
	protected override void OnStart()
	{
		_bombRef = Scene.Directory.FindComponentByGuid( new System.Guid( "ad824361-8cfc-4f59-bfe5-0fae8b2a0b63" ) ) as Bomb;
		bombRadiusDmg = _bombRef.GameObject.GetComponent<RadiusDamage>();
		bombRadiusDmg.Enabled = false;
		_aiComponent = Scene.Directory.FindComponentByGuid( new System.Guid( "0684f319-631a-4ead-84a5-41213a1e27d0" ) ) as AiMode;
		Log.Warning( $"MANAGER BombRef instance: {_bombRef?.GetHashCode()}" );
		MainMenu();
	}

	// public
	public static GameManager Instance { get; private set; } = null;
	[Property] public GameObject Player1Camera { get; private set; } = null;
	[Property] public GameObject Player2Camera { get; private set; } = null;
	[Property] public GameObject OverheadCamera { get; private set; } = null;

	[Property] public GameObject Player1Model { get; set; }
	[Property] public GameObject Player2Model { get; set; }
	[Property] public GameObject BombUI { get; set; }
	public bool GameStarted { get; set; } = false;

	public bool AIMode = true;
	public bool CurrentTurn { get; set; } = true;
	public bool GameComplete { get; private set; } = false;


	// private
	private bool _youWon { get; set; } = false;
	private bool _player1Occupied = false;
	private bool _player2Occupied = false;

	private GameObject _currentPlayer = null;
	private RadiusDamage bombRadiusDmg { get; set; }
	private AiMode _aiComponent;
	private Bomb _bombRef { get; set; }
	private GameObject _activeCamera = null;



}
