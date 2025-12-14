using Sandbox;
using Sandbox.Html;
using Sandbox.Network;
using Sandbox.UI;
using System;
using System.Threading.Tasks;


public sealed class GameManager : Component
{
	public static async Task<SoundFile> DownloadSound( string packageIdent )
	{
		var package = await Package.Fetch( packageIdent, false );
		if ( package == null || package.Revision == null )
		{
			// Package not found
			return null;
		}
		// If the package was found, mount it (download the content)
		await package.MountAsync();

		// Get the path to the primary asset (vmdl for Model, vsnd for Sound, ect.)
		var primaryAsset = package.GetMeta( "PrimaryAsset", "" );
		return SoundFile.Load( primaryAsset );
	}

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
		if (Networking.IsHost)
		{
			CurrentPlayer = Player1Model;
			Log.Warning( $"Player1 slot chosen" );
		}
		else
		{
			CurrentPlayer = Player2Model;
			Log.Warning( $"Player2 slot chosen" );
		}
	}

	[Rpc.Broadcast]
	public void InitPlayerCoop()
	{
		// host
		if ( CurrentPlayer == Player1Model || AIMode  )
		{
			SetCamera( Player1Camera );
			PlayerIndex = 1;
			Log.Info( "Player 1 selected (red)" );


		}
		// AI or non host
		else if ( CurrentPlayer == Player2Model )
		{
			SetCamera( Player2Camera );
			PlayerIndex = 2;
			Log.Info( "Player 2 selected (green)" );
		}

		IsMatchmaking = false;

		// start ticking bomb
		_bombRef.IsActive = true;

		CreateBombUI();
		BombUI.Enabled = true;

		Scene.TimeScale = 1;



		if (!IsProxy )
		{
			CurrentTurn = true;
		}
		else
		{
			CurrentTurn = false;
		}

		Log.Info( $"Your turn? {CurrentTurn}" );
		GameStarted = true;

	}


	public void InitPlayerAI()
	{
		if ( CurrentPlayer == Player1Model || AIMode )
		{
			SetCamera( Player1Camera );


		}
		else if ( CurrentPlayer == Player2Model )
		{
			SetCamera( Player2Camera );
		}

	}

	[Rpc.Broadcast]
	public void NextTurn()
	{
		if ( _bombRef.Time <= 0 ) { return; }
		_bombRef.InputSoundRef.StartSound();
		_ = NextTurnAsync();

	}

	async public Task NextTurnAsync()
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

				NextTurn();
			}



		}
	}

	public void DetermineWinner()
	{

		_ = GameEnd();
	}


	// host determines the winner 
	// host is always player 1
	// when detonation occurs, current turn is not swapped
	public void GetWinner()
	{
		if (!Networking.IsHost) { return; }
		Log.Warning( "Determining Winner..." );
		LoserPlayerIndex = CurrentTurn ? 1 : 2;	


		LoserProp = (LoserPlayerIndex == 1) ? Player1Model.GetComponent<Prop>() : Player2Model.GetComponent<Prop>();
		

		Log.Info( $"Loser Detected: Player {LoserPlayerIndex}" );

	}
	public async Task GameEnd()
	{
		if ( GameComplete ) { return; }
		GameComplete = true;

		CreateWinnerUI();

		CurrentTurn = false;
		BombUI.Enabled = false;
		LoserProp.IsStatic = false;

		if (Player1Camera != null)
		{
			Player1Camera.Enabled = false;
		}

		if ( Player2Camera != null )
		{
			Player2Camera.Enabled = false;
		}

		BombRadiusDmg.Enabled = true;
		if (_bombRef != null && _bombRef.IsValid)
		{
			_bombRef.GameObject.Destroy();


		}

		await GameTask.DelaySeconds( 5 );
		ReloadGame();
	}


	public void ReloadGame()
	{
		SceneLoadOptions currScene = new SceneLoadOptions();
		currScene.SetScene("scenes/main.scene");
		currScene.IsAdditive = false;
		currScene.DeleteEverything = true;
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
		Sound.StopAll(1);
		PlayerIndex = 1;
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


	[Rpc.Host]
	public void StartCoop()
	{
		if ( MatchmakingPlayers == 2 )
		{
			Sound.StopAll( 0.5f );
			Log.Info( "2 Players detected, initiating game!" );

			// set cameras
			InitPlayerCoop();




			Log.Warning( $"Game started: {GameStarted} CurrentTurn: {CurrentTurn} IsBombActive: {_bombRef.IsActive} IsMatchmaking: {IsMatchmaking}" );
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
			BombUI.NetworkMode = NetworkMode.Never;
			BombUI.Name = "BombUI";
			BombUI.Parent = UIParent;
			BombUI.Enabled = false;
			BombUI.AddComponent<BombTimerInput>();
			BombUI.AddComponent<ScreenPanel>();


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

	public void CreateWinnerUI()
	{
		MatchmakeUI = Scene.Directory.FindByName( "WinnerUI" ).FirstOrDefault();
		if ( MatchmakeUI == null )
		{
			MatchmakeUI = Scene.CreateObject();
			MatchmakeUI.Name = "WinnerUI";
			MatchmakeUI.Parent = UIParent;
			MatchmakeUI.AddComponent<ShowWinnerUI>();
			MatchmakeUI.AddComponent<ScreenPanel>();
			MatchmakeUI.NetworkMode = NetworkMode.Never;
			MenuUI.Enabled = true;

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
		_bombRef = Scene.Directory.FindByName( "BombModel" ).First().GetComponent<Bomb>();
		if (BombRadiusDmg == null)
		{
			BombRadiusDmg = _bombRef.GetComponent<RadiusDamage>();
		}
		BombRadiusDmg.Enabled = false;
		_aiComponent = GetComponent<AiMode>() as AiMode;
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
	[Sync] public Prop LoserProp { get; set; }

	public GameObject CurrentPlayer = null;

	[Sync] public int LoserPlayerIndex { get; set; } = -1;
	public int PlayerIndex { get; set; } = -1;

	public bool GameStarted { get; set; } = false;

	public int MatchmakingPlayers { get; set; } = 0;

	public bool AIMode = false;
	public bool CurrentTurn { get; set; } = false;
	public bool GameComplete { get; private set; } = false;

	public bool IsMatchmaking { get; set; } = false;


	// private
	private AiMode _aiComponent;
	private Bomb _bombRef { get; set; }
	private GameObject _activeCamera = null;

	private GameObject UIParent{ get; set; } = null;



}
