using Sandbox;
using Sandbox.Html;
using Sandbox.UI;
using System;
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

		}
		else
		{

			_currentPlayer.GetComponent<Prop>().IsStatic = false;
			Player1Camera.Enabled = false;

		}

		BombRadiusDmg.Enabled = true;
		_bombRef.GameObject.Destroy();

		Log.Warning( $"Did you win? {_youWon}" );
		await GameTask.DelaySeconds( 5 );
		ReloadGame();

	}



	public void ReloadGame()
	{
		SceneLoadOptions currScene = new SceneLoadOptions();
		currScene.SetScene("scenes/main.scene");
		currScene.DeleteEverything = true;
		Game.ChangeScene(currScene);
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


	[Rpc.Broadcast]
	public void UpdateMatchmakingCount(bool join)
	{
		if (join)
		{
			MatchmakingPlayers = Math.Max( 0, MatchmakingPlayers + 1 );
		}
		else
		{
			MatchmakingPlayers = Math.Max( 0, MatchmakingPlayers - 1 );
		}
		
	}

	public void PlayCoop()
	{
		if (MatchmakingPlayers == 2)
		{
			AssignPlayer();

		}
		else
		{
			GameObject MatchmakeUI = Scene.Directory.FindByName( "MatchMakeUI" ).FirstOrDefault();
			if (MatchmakeUI == null)
			{
				MatchmakeUI = Scene.CreateObject();
				MatchmakeUI.Name = "MatchmakeUI";
				MatchmakeUI.Parent = UIParent;
				MatchmakeUI.AddComponent<MatchmakeUI>();
				MatchmakeUI.AddComponent<ScreenPanel>();
				MatchmakeUI.NetworkMode = NetworkMode.Never;
			}

			IsMatchmaking = true;
			//MatchmakeUI.GetComponent<MatchmakeUI>().IsMatchmaking = true;
			UpdateMatchmakingCount(true);
		}

	}

	public void CreateUI()
	{
		UIParent = Scene.CreateObject();
		UIParent.Name = "UI";
		BombUI = Scene.CreateObject();
		BombUI.Name = "BombUI";
		BombUI.Parent = UIParent;
		BombUI.Enabled = false;
		BombUI.AddComponent<BombTimerInput>();
		BombUI.AddComponent<ScreenPanel>();


		GameObject MainMenu = Scene.CreateObject();
		MainMenu.Name = "MainMenuUI";
		MainMenu.Parent = UIParent;
		MainMenu.AddComponent<MainMenuUI>();
		MainMenu.AddComponent<ScreenPanel>();

	}

	protected override void OnStart()
	{
		_bombRef = Scene.Directory.FindComponentByGuid( new System.Guid( "ad824361-8cfc-4f59-bfe5-0fae8b2a0b63" ) ) as Bomb;
		if (BombRadiusDmg == null)
		{
			BombRadiusDmg = _bombRef.GetComponent<RadiusDamage>();
		}
		BombRadiusDmg.Enabled = false;
		_aiComponent = Scene.Directory.FindComponentByGuid( new System.Guid( "0684f319-631a-4ead-84a5-41213a1e27d0" ) ) as AiMode;
		Log.Warning( $"MANAGER BombRef instance: {_bombRef?.GetHashCode()}" );

		// Create UI dynamically.
		CreateUI();

		MainMenu();


	}

	// public
	public static GameManager Instance { get; private set; } = null;
	[Property] public GameObject Player1Camera { get; private set; } = null;
	[Property] public GameObject Player2Camera { get; private set; } = null;
	[Property] public GameObject OverheadCamera { get; private set; } = null;
	[Property] public RadiusDamage BombRadiusDmg { get; set; }
	[Property] public GameObject Player1Model { get; set; }
	[Property] public GameObject Player2Model { get; set; }
	[Property] public GameObject BombUI { get; set; }
	public bool GameStarted { get; set; } = false;

	[Sync] public int MatchmakingPlayers { get; set; } = 0;

	public bool AIMode = true;
	public bool CurrentTurn { get; set; } = true;
	public bool GameComplete { get; private set; } = false;

	public bool IsMatchmaking { get; set; } = false;
	// private
	private bool _youWon { get; set; } = false;
	[Sync] private bool _player1Occupied { get; set; } = false;
	private bool _player2Occupied { get; set; } = false;

	private GameObject _currentPlayer = null;
	
	private AiMode _aiComponent;
	private Bomb _bombRef { get; set; }
	private GameObject _activeCamera = null;

	private GameObject UIParent{ get; set; } = null;



}
