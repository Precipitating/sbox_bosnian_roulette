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

	private T ByDifficulty<T>( T easy, T normal, T hard ) =>
	AIDifficulty.Difficulty switch
	{
		Difficulty.Easy => easy,
		Difficulty.Normal => normal,
		Difficulty.Hard => hard,
		_ => default
	};


	private bool ShouldUseYannow()
	{
		float max = ByDifficulty( 30f, 15f, 5f );
		return _bombRef.Time <= Game.Random.Float( 1f, max );
	}

	private bool ShouldUseDoubleTrouble( float reduction )
	{
		float maxReduction = ByDifficulty( 40f, 60f, 80f );
		float minTime = ByDifficulty( 180f, 120f, 120f );

		return reduction <= maxReduction && _bombRef.Time >= minTime;
	}

	private bool ShouldUseHollup()
	{
		float threshold = ByDifficulty( 200f, 100f, 50f );
		return _bombRef.Time <= threshold;
	}

	private bool ShouldUseLeTrolle()
	{
		float threshold = ByDifficulty( 50f, 100f, 200f );
		return _bombRef.Time <= threshold;
	}


	private bool ShouldUseGandering()
	{
		float threshold = ByDifficulty( 100f, 70f, 50f );

		return _bombRef.Time < threshold;
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

			case CardEnum.Calma:
				{
					UseCard( ref reduction );
					break;
				}
			case CardEnum.Gandering:
				{
					if ( ShouldUseGandering() )
					{
						UseCard( ref reduction );
						// just give ai 20% chance of correctly guessing instead for consistency
						if ( Game.Random.Float() <= _ganderingChance)
						{
							// ai knows the range due to card's successful activation, so next turn queue a logical (random) time
							int logicalGuess = (int)Game.Random.Int( 1, Math.Max( 1, (int)_bombRef.Time - 1 ));
							queuedInputs.Enqueue( logicalGuess );
							//Log.Info( $"AI queued a val {logicalGuess}" );
						}
					}
					break;
				}


		}


	}


	float CalculateReduction()
	{
		// calculate reduction to apply depending on difficulty
		float progress = 1f - (_bombRef.Time / _bombRef.OriginalTime).Clamp( 0f, 1f );
		float hazard = MathF.Pow( 1f - progress, AIDifficulty.HazardExponent );



		float minReduction = (_bombRef.IsTrollMode ? Game.Random.Int( 1, 5 ) : _bombRef.Time) * AIDifficulty.MinReductionPercent;
		float maxReduction = (_bombRef.IsTrollMode ? Game.Random.Int( 6, 10 ) : _bombRef.Time) * AIDifficulty.MaxReductionPercent;


		float reduction = MathX.Lerp( minReduction, maxReduction, hazard );
		reduction *= Game.Random.Float( 1f - AIDifficulty.Variance, 1f + AIDifficulty.Variance );

		float finalReduction = Math.Max( 1, (int)MathF.Round( reduction ) );

		return finalReduction;
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


		// get reduction depending on difficulty, used if queuedInputs is empty.
		float finalReduction = CalculateReduction();


		// use card if necessary
		HandleCardUsage( ref finalReduction );

		// queued inputs > 0 means that a card was used that gives the AI a rough idea on what to pick next.
		if (queuedInputs.Count > 0 && finalReduction != 0)
		{
			finalReduction = queuedInputs.Dequeue(); 
			//Log.Info( "AI using queued input" );
		}

		_bombRef.ReduceBombTime( finalReduction );
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
		MinReductionPercent = 0.03f,
		MaxReductionPercent = 0.08f,
		HazardExponent = 1.2f,
		Variance = 0.30f,
		MinDelay = 3.5f,
		MaxDelay = 6.5f
	};


	private BombAIDifficulty _normalDifficulty = new BombAIDifficulty
	{
		Difficulty = Difficulty.Normal,
		MinReductionPercent = 0.05f,
		MaxReductionPercent = 0.20f,
		HazardExponent = 1.8f,
		Variance = 0.25f,
		MinDelay = 1.5f,
		MaxDelay = 3.5f
	};


	private BombAIDifficulty _hardDifficulty = new BombAIDifficulty
	{
		Difficulty = Difficulty.Hard,
		MinReductionPercent = 0.06f,
		MaxReductionPercent = 0.35f,
		HazardExponent = 2.2f,
		Variance = 0.20f,
		MinDelay = 0.6f,
		MaxDelay = 1.2f
	};



	private Queue<int> queuedInputs = new Queue<int>();

	private Bomb _bombRef = null;
	private Card _chosenCard;
	private const float _ganderingChance = 0.9f;
	private bool _cardUsed = false;

}
