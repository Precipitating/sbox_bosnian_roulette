using Sandbox;
using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class AiMode : Component, IGameManagerEvent
{
	public enum Difficulty
	{
		None = 0,
		Easy = 1,
		Normal = 2,
		Hard = 3,
	}


	private bool ShouldUseYannow()
	{
		return AIDifficulty.Difficulty switch
		{
			Difficulty.Easy => (_bombRef.Time <= Game.Random.Float( 1f, 30f )),
			Difficulty.Normal => (_bombRef.Time <= Game.Random.Float( 1f, 15f )),
			Difficulty.Hard => (_bombRef.Time <= Game.Random.Float(1f,5f)),
			_ => false
		};
	}



	private bool ShouldUseDoubleTrouble(float reduction)
	{
		return AIDifficulty.Difficulty switch
		{
			Difficulty.Easy => (reduction <= 40f && _bombRef.Time >= 180f),
			Difficulty.Normal => (reduction <= 60f && _bombRef.Time >= 120f),
			Difficulty.Hard => (reduction <= 80f && _bombRef.Time >= 120f),
			_ => false
		};
	}


	private bool ShouldUseHollup()
	{
		return AIDifficulty.Difficulty switch
		{
			Difficulty.Easy => (_bombRef.Time <= 200f),
			Difficulty.Normal => (_bombRef.Time <= 100f),
			Difficulty.Hard => (_bombRef.Time <= 50f),
			_ => false
		};
	}
	private bool ShouldUseLeTrolle()
	{
		return AIDifficulty.Difficulty switch
		{
			Difficulty.Easy => (_bombRef.Time <= 50f),
			Difficulty.Normal => (_bombRef.Time <= 100f),
			Difficulty.Hard => (_bombRef.Time <= 200f),
			_ => false
		};
	}
	private void HandleCardUsage(ref float reduction)
	{
		if (_cardUsed) {  return; }


		void UseCard(ref float reduction)
		{
			reduction = _chosenCard.Use( reduction );
			SoundManager.PlayAcrossClients( "CardActivation" );
			_cardUsed = true;
		}

		switch (_chosenCard.CardID )
		{
			case CardEnum.Yannow:
				{
					if (ShouldUseYannow())
					{
						Log.Info( "AI using Yannow" );
						UseCard( ref reduction );
						//Log.Info( $"AI's reduction = {reduction}" );
					}
					break;

				
				}

			case CardEnum.DoubleTrouble:
				if (ShouldUseDoubleTrouble(reduction))
				{
					Log.Info( "AI using Double Trouble" );
					UseCard(ref reduction );
					//Log.Info( $"AI's reduction = {reduction}" );
				}
				break;
			case CardEnum.Hollup:
				if ( ShouldUseHollup() )
				{
					Log.Info( "AI using Hollup" );
					UseCard( ref reduction );
					//Log.Info( $"AI's reduction = {reduction}" );
				}
				break;

			case CardEnum.LeTrolle:
				if ( ShouldUseLeTrolle())
				{
					Log.Info( "AI using LeTrolle" );
					UseCard( ref reduction );
					//Log.Info( $"AI's reduction = {reduction}" );
				}
				break;
		}


	}


	async public Task SimulateTurn()
	{
		//Log.Info( AIDifficulty );
		//Log.Warning( $"AI simulate turn, current time = {_bombRef.Time}, active: {_bombRef.IsActive}" );
		await GameTask.DelaySeconds( Game.Random.Float( AIDifficulty.MinDelay, AIDifficulty.MaxDelay) );

		if ( !_bombRef.IsActive )
		{
			Log.Info( "Bomb is not active" );
			return;
		}

		// calculate reduction to apply depending on difficulty
		float progress = 1f - (_bombRef.Time / _bombRef.OriginalTime).Clamp( 0f, 1f );
		float hazard = MathF.Pow( 1f - progress, AIDifficulty.HazardExponent );

		float minReduction = _bombRef.Time * AIDifficulty.MinReductionPercent;
		float maxReduction = _bombRef.Time * AIDifficulty.MaxReductionPercent;

		float reduction = MathX.Lerp( minReduction, maxReduction, hazard );
		reduction *= Game.Random.Float( 1f - AIDifficulty.Variance, 1f + AIDifficulty.Variance );

		float finalReduction = Math.Max( 1, (int)MathF.Round( reduction ) );	

		// use card if necessary
		HandleCardUsage( ref finalReduction );


		_bombRef.ReduceBombTime( finalReduction);
		SoundManager.Play2D( "Input" );



		//Log.Warning( $"AI's reduction = {finalReduction}" );

	}

	// guarantees game manager is initialized, so we can set references safely here
	void IGameManagerEvent.OnInitialized()
	{
		_bombRef = Scene.Directory.FindByName( "BombModel" ).First().GetComponent<Bomb>();
		Log.Warning( $"AIMODE BombRef instance: {_bombRef?.GetHashCode()}" );

		_chosenCard = GameManager.Instance.Cards.GetRandomCard();
	}

	protected override void OnStart()
	{
		base.OnStart();
	}


	public struct BombAIDifficulty
	{
		public Difficulty Difficulty;
		public float MinReductionPercent;   // of original time
		public float MaxReductionPercent;   // of original time
		public float HazardExponent;        // curve shape
		public float Variance;              // randomness

		public float MinDelay;              // seconds
		public float MaxDelay;
	}


	public void SetAIDifficulty(string difficultyType)
	{
		switch ( difficultyType )
		{
			case "Easy":
				AIDifficulty = _easyDifficulty;
				break;
			case "Normal":
				AIDifficulty = _normalDifficulty;
				break;
			case "Hard":
				AIDifficulty = _hardDifficulty;
				break;
			default:
				AIDifficulty = _normalDifficulty;
				break;
		}

		DifficultyString = difficultyType;
		Log.Info( $"Difficulty set to {difficultyType}" );
	}


	public string DifficultyString;
	public BombAIDifficulty AIDifficulty { get; private set; }


	private BombAIDifficulty _easyDifficulty = new BombAIDifficulty
	{
		Difficulty = Difficulty.Easy,
		MinReductionPercent = 0.02f, 
		MaxReductionPercent = 0.06f, 
		HazardExponent = 1.5f,
		Variance = 0.20f,
		MinDelay = 3f,
		MaxDelay = 6f
	};

	private BombAIDifficulty _normalDifficulty = new BombAIDifficulty
	{
		Difficulty = Difficulty.Normal,
		MinReductionPercent = 0.05f, 
		MaxReductionPercent = 0.15f, 
		HazardExponent = 2.5f,
		Variance = 0.15f,
		MinDelay = 1.5f,
		MaxDelay = 4f
	};

	private BombAIDifficulty _hardDifficulty = new BombAIDifficulty
	{
		Difficulty = Difficulty.Hard,
		MinReductionPercent = 0.01f, 
		MaxReductionPercent = 0.30f, 
		HazardExponent = 3.5f,
		Variance = 0.01f,
		MinDelay = 0.5f,
		MaxDelay = 1f
	};



	private Bomb _bombRef = null;
	private Card _chosenCard;
	private bool _cardUsed = false;
}
