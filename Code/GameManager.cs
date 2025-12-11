using Sandbox;
using Sandbox.Html;
using Sandbox.Network;
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

		if ( _activeCamera != null )
		{
			CameraComponent oldCam = _activeCamera.GetComponent<CameraComponent>();
			oldCam.IsMainCamera = false;
			_activeCamera.Enabled = false;
			_activeCamera = null;
		}
		_activeCamera = cameraGameObject;
	}


	// set camera
	public void AssignPlayer()
	{
		if (!IsProxy)
		{
			_currentPlayer = Player1Model;
			Log.Warning( $"Player1 slot chosen" );
		}
		else
		{
			_currentPlayer = Player2Model;
			Log.Warning( $"Player2 slot chosen" );
		}
	}

	[Rpc.Broadcast]
	public void InitPlayerCoop()
	{
		if ( _currentPlayer == Player1Model || AIMode  )
		{
			SetCamera( Player1Camera );


		}
		else if (_currentPlayer == Player2Model )
		{
			SetCamera( Player2Camera );
		}

	}


	public void InitPlayerAI()
	{
		if ( _currentPlayer == Player1Model || AIMode )
		{
			SetCamera( Player1Camera );


		}
		else if ( _currentPlayer == Player2Model )
		{
			SetCamera( Player2Camera );
		}

	}

	async public Task NextTurn()
	{
		if ( _bombRef.IsActive )
		{
			CurrentTurn = !CurrentTurn;
			Log.Info( $"Is it your turn: {CurrentTurn}" );

			// ai
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
		BombUI.Enabled = false;


		if ( _youWon )
		{
			var loserPlayer = (_currentPlayer == Player1Model) ? Player1Model.GetComponent<Prop>() : Player2Model.GetComponent<Prop>();
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
		currScene.IsAdditive = false;
		Game.ChangeScene(currScene);
	}


	private void MainMenu()
	{
		CreateMainMenuUI();
		SetCamera( OverheadCamera );
	}
	protected override void OnAwake()
	{
		Instance = this;
		Log.Warning( "GameManager set." );
	}

	public void PlayAI()
	{

		AIMode = true;

		AssignPlayer();
		InitPlayerAI();

		CreateBombUI();
		BombUI.Enabled = true;
		Scene.TimeScale = 1;

		_bombRef.IsActive = true;

		CurrentTurn = true;

		GameStarted = true;

		Log.Info( $"IsActive: {_bombRef.IsActive}. GameStarted: {GameStarted}" );

	}


	[Rpc.Broadcast]
	public void StartCoop()
	{
		if ( MatchmakingPlayers == 2 )
		{
			Log.Info( "2 Players detected, initiating game!" );
			// set cameras
			InitPlayerCoop();

			// start ticking bomb
			_bombRef.IsActive = true;

			CreateBombUI();
			BombUI.Enabled = true;
			Scene.TimeScale = 1;

			IsMatchmaking = false;
			MatchmakeUI.Enabled = false;

			if (!IsProxy)
			{
				CurrentTurn = true;
			}
			else
			{
				CurrentTurn = false;
			}

			GameStarted = true;






			Log.Warning( $"Game started: {GameStarted} CurrentTurn: {CurrentTurn} IsBombActive: {_bombRef.IsActive}" );
		}
	}

	[Rpc.Broadcast]
	public void UpdateMatchmakingCount(bool join)
	{
		MatchmakingPlayers = join ? Math.Max( 0, MatchmakingPlayers + 1 ) : Math.Max( 0, MatchmakingPlayers - 1 );
		Log.Info( $"Matchmaking players: {MatchmakingPlayers}" );

	}

	public void MatchmakeCoop()
	{
		CreateMatchMakeUI();
		MatchmakeUI.Enabled = true;
		IsMatchmaking = true;
		AssignPlayer();
		UpdateMatchmakingCount(true);
		Scene.TimeScale = 1;


	}

	public void CreateBombUI()
	{
		BombUI = Scene.Directory.FindByName( "BombUI" ).FirstOrDefault();


		if ( BombUI == null )
		{
			BombUI = Scene.CreateObject();
			BombUI.Name = "BombUI";
			BombUI.Parent = UIParent;
			BombUI.Enabled = false;
			BombUI.AddComponent<BombTimerInput>();
			BombUI.AddComponent<ScreenPanel>();
			BombUI.NetworkMode = NetworkMode.Never;

		}


	}

	public void CreateMatchMakeUI()
	{
		MatchmakeUI = Scene.Directory.FindByName( "MatchMakeUI" ).FirstOrDefault();
		if ( MatchmakeUI == null )
		{
			MatchmakeUI = Scene.CreateObject();
			MatchmakeUI.Name = "MatchmakeUI";
			MatchmakeUI.Parent = UIParent;
			MatchmakeUI.AddComponent<MatchmakeUI>();
			MatchmakeUI.AddComponent<ScreenPanel>();
			MatchmakeUI.NetworkMode = NetworkMode.Never;
			MenuUI.Enabled = false;

		}

	}
	public void CreateMainMenuUI()
	{
		UIParent = Scene.CreateObject();
		UIParent.Name = "UI";
		UIParent.NetworkMode = NetworkMode.Never;


		MenuUI = Scene.Directory.FindByName( "MainMenuUI" ).FirstOrDefault();

		if ( MenuUI == null )
		{
			MenuUI = Scene.CreateObject();
			MenuUI.Name = "MainMenuUI";
			MenuUI.Parent = UIParent;
			MenuUI.AddComponent<MainMenuUI>();
			MenuUI.AddComponent<ScreenPanel>();
			MenuUI.NetworkMode = NetworkMode.Never;
			MenuUI.Enabled = false;
		}

		Scene.TimeScale = 0;

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
		
		MainMenu();
		MenuUI.Enabled = true;


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
	[Property] public GameObject MatchmakeUI { get; set; }
	[Property] public GameObject MenuUI { get; set; }

	public bool GameStarted { get; set; } = false;

	public int MatchmakingPlayers { get; set; } = 0;

	public bool AIMode = false;
	[Sync] public bool CurrentTurn { get; set; } = false;
	public bool GameComplete { get; private set; } = false;

	public bool IsMatchmaking { get; set; } = false;
	// private
	[Sync] private bool _youWon { get; set; } = false;


	private GameObject _currentPlayer = null;
	
	private AiMode _aiComponent;
	private Bomb _bombRef { get; set; }
	private GameObject _activeCamera = null;

	private GameObject UIParent{ get; set; } = null;



}
