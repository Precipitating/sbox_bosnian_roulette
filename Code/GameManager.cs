using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class GameManager : Component
{
	private void StartCoopMatch()
	{
		_bombRef.IsActive = true;
		CreateBombUI();
		BombUI.Enabled = true;

	}

	[Rpc.Broadcast]
	public void InitPlayerCoop()
	{
		var currentPlayer = PlayerManager.CurrentPlayer;
		GameObject camera = currentPlayer.PlayerRef.PlayerCamera;

		string playerName = (currentPlayer.PlayerId == PlayerType.Player1) ? "Player 1 (red)" : "Player 2 (green)";
		ChosenCard = Cards.GetRandomCard();

		IsMatchmaking = false;
		CameraManager.LerpTransitionCameraTo(camera, currentPlayer.TagName ).ContinueWith( async _ =>
		{
			await GameTask.MainThread();
			GameStarted = true;
			CameraManager.SetCamera( camera );
			Log.Info( $"{playerName} selected" );

			StartCoopMatch();

			CurrentTurn = !IsProxy;
			Log.Info( $"Your turn? {CurrentTurn}" );
		} );
	}

	[Rpc.Broadcast]
	public void NextTurn()
	{
		if ( _bombRef.Time <= 0 ) { return; }

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
	
	public void ActivateCard(bool cardUsed = true)
	{
		SoundManager.PlayAcrossClients( "CardActivation" );

		CardUsed = cardUsed;
		

	}
	public void DetermineWinner()
	{

		_ = GameEnd();
	}


	[Rpc.Host]
	public void SetUnoReversal(bool val)
	{

		UnoReversed = val;
	}


	/// <summary>
	/// Swap the winner when uno reverse card has been activated succesfully.
	/// </summary>
	[Rpc.Host]
	void HandleUnoReversal()
	{
		if ( !Rpc.Caller.IsHost || IsProxy ) { return; }
		SoundManager.PlayAcrossClients( "Yanno", true, "sound/yanno.sound" );
		// invert the winner
		LoserPlayerIndex = 3 - LoserPlayerIndex;
		UnoReversed = false;
		Log.Info( "Uno reverse activated" );
	}

	private void UnanchorLoser(int loserIdx)
	{
		// network cant serialize custom Player type so we have to manually find the prop
		var loserProp = (loserIdx == 1) ?
			Scene.Directory.FindByName( "Player1" ).First().GetComponent<Prop>():
			Scene.Directory.FindByName( "Player2" ).First().GetComponent<Prop>();

		loserProp.IsStatic = false;

	}

	[Rpc.Host]
	public void GetWinner()
	{
		if (!Rpc.Caller.IsHost || IsProxy) { return; }
		Log.Warning( "Determining Winner..." );

		LoserPlayerIndex = CurrentTurn ? 1 : 2;
		if (UnoReversed)
		{
			HandleUnoReversal();
		}

		//LoserProp = (LoserPlayerIndex == 1) ? PlayerManager.CurrentPlayer.PlayerRef.PlayerModel.GetComponent<Prop>(): 
		//									  PlayerManager.PlayerList[PlayerType.Player2].PlayerRef.PlayerModel.GetComponent<Prop>();


	

	}
	public async Task GameEnd()
	{
		if ( GameComplete ) { return; }
		GameComplete = true;

		CreateWinnerUI();

		CurrentTurn = false;
		BombUI.Enabled = false;
		UnanchorLoser( LoserPlayerIndex );
		//LoserProp.IsStatic = false;

		var playerCam = PlayerManager.CurrentPlayer.PlayerRef.PlayerCamera;
		Log.Info( playerCam );
		playerCam.Enabled = false;

		BombRadiusDmg.Enabled = true;
		if (_bombRef != null && _bombRef.IsValid)
		{
			_bombRef.GameObject.Destroy();


		}

		// rotate head up
		if ( LoserPlayerIndex == 1 )
			_ = PlayerManager.RotateHeadY( PlayerType.Player2, -90 );
		else
			_ = PlayerManager.RotateHeadY( PlayerType.Player1, -90 );


		if ((int)PlayerManager.CurrentPlayer.PlayerId != LoserPlayerIndex)
		{
			Log.Info( "Won achievement" );
			Sandbox.Services.Achievements.Unlock( "won_a_game" );

			if (_aiComponent.DifficultyEnum == Difficulty.Hard)
			{
				Sandbox.Services.Achievements.Unlock( "hard_mode_won" );
				Log.Info( "Won hard mode" );
			}


		}
		else
		{
			Log.Info( "Lose acheivement" );
			Sandbox.Services.Achievements.Unlock( "lost_a_game" );
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

	public void SetAIDifficulty(Difficulty difficulty)
	{
		_aiComponent.SetAIDifficulty(difficulty);
	}
	private void MainMenu()
	{
		CreateMainMenuUI();
		CameraManager.SetCamera( CameraManager.OverheadCamera );
	}
	protected override void OnAwake()
	{
		Instance = this;
		Log.Warning( "GameManager set." );
	}

	public void PlayAI()
	{
		SoundManager.StopPathSound( "ambience", 1f );
		AIMode = true;
		Player currPlayer = PlayerManager.AddPlayer();
		PlayerManager.AddPlayer(true);

		ChosenCard = Cards.GetRandomCard();
		GameStarted = true;
		CameraManager.LerpTransitionCameraTo( currPlayer.PlayerRef.PlayerCamera, currPlayer.TagName ).ContinueWith( async task =>
		{
			await Task.MainThread();

			CreateBombUI();
			CameraManager.SetCamera( currPlayer.PlayerRef.PlayerCamera );

			CurrentTurn = true;

			_bombRef.IsActive = true;
			BombUI.Enabled = true;

			Log.Info( $"IsActive: {_bombRef.IsActive}. GameStarted: {GameStarted}" );

		} );



	}


	[Rpc.Host]
	public void StartCoop()
	{
		if ( MatchmakingPlayers == 2 && Connection.All.Count == 2)
		{
			SoundManager.StopPathSound( "ambience", 1f);
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
		PlayerManager.AddPlayer();
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
		SoundManager.InitializeSounds();
		_bombRef = Scene.Directory.FindByName( "BombModel" ).First().GetComponent<Bomb>();
		Cards = new CardDatabase( _bombRef );
		PlayerManager = GetComponent<PlayerManager>();
		CameraManager = GetComponent<CameraManager>();

		if (BombRadiusDmg == null)
		{
			BombRadiusDmg = _bombRef.GetComponent<RadiusDamage>();
		}
		BombRadiusDmg.Enabled = false;
		_aiComponent = GetComponent<AiMode>();
		Log.Warning( $"MANAGER BombRef instance: {_bombRef?.GetHashCode()}" );

		// Create UI dynamically.
		
		MainMenu();
		MenuUI.Enabled = true;


		IGameManagerEvent.Post(x => x.OnInitialized());




	}

	// public
	public static GameManager Instance { get; private set; } = null;
	//[Property] public GameObject Player1Camera { get; private set; } = null;
	//[Property] public GameObject Player2Camera { get; private set; } = null;
	//[Property] public GameObject TransitionCamera{ get; private set; } = null;
	//[Property] public GameObject OverheadCamera { get; private set; } = null;
	[Property] public RadiusDamage BombRadiusDmg { get; set; }

	//[Property] public GameObject Player1Model { get; set; }
	//[Property] public GameObject Player2Model { get; set; }
	[Property] public GameObject BombUI { get; set; }
	[Property] public GameObject MatchmakeUI { get; set; }
	public CameraManager CameraManager { get; private set; }
	public PlayerManager PlayerManager { get; private set; }

	//[Property] public GameObject Player1Neck { get; set; }
	//[Property] public GameObject Player2Neck { get; set; }
	[Property] public GameObject MenuUI { get; set; }
	//[Sync] public Prop LoserProp { get; set; }



	[Sync] public bool UnoReversed { get; set; } = false;

	public GameObject CurrentPlayer = null;

	[Sync] public int LoserPlayerIndex { get; set; } = -1;

	public Card ChosenCard { get; set; } = null;
	//public int PlayerIndex { get; set; } = -1;

	public bool GameStarted { get; set; } = false;

	public int MatchmakingPlayers { get; set; } = 0;

	public bool AIMode = false;
	public bool CurrentTurn { get; set; } = false;
	public bool GameComplete { get; private set; } = false;

	public bool IsMatchmaking { get; set; } = false;

	public bool CardUsed { get; set; } = false;

	public CardDatabase Cards { get; set; }



	// private
	private AiMode _aiComponent;
	private Bomb _bombRef { get; set; }
	//private GameObject _activeCamera = null;

	private GameObject UIParent{ get; set; } = null;








}
