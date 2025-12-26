using Sandbox;
using System;
using System.Threading;
using System.Threading.Tasks;

public static class CardActivationChance
{
	public const float Gandering = 0.35f;
}


public sealed class AiMode : Component, IGameManagerEvent
{

	/// <summary>
	/// Returns a value depending on what the AI difficulty mode is.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="easy"></param>
	/// <param name="normal"></param>
	/// <param name="hard"></param>
	/// <returns>Value depending on AI difficulty mode set</returns>
	private T ByDifficulty<T>( T easy, T normal, T hard ) =>
	AIDifficulty.Difficulty switch
	{
		Difficulty.Easy => easy,
		Difficulty.Normal => normal,
		Difficulty.Hard => hard,
		_ => default
	};


	private bool ShouldUseCard(CardEnum cardType, float reduction = 0)
	{
		bool result = false;

		switch (cardType)
		{
			case CardEnum.Hollup:
			case CardEnum.LeTrolle:
				{
					var thresholdData = (cardType == CardEnum.Hollup) ? MoveThresholdConfig.Hollup : MoveThresholdConfig.LeTrolle; 
					float threshold = ByDifficulty( thresholdData.Easy, thresholdData.Normal, thresholdData.Hard );
					result = _bombRef.Time <= threshold;
					break;
				}
			case CardEnum.Yannow:
				{
					var thresholdData = MoveThresholdConfig.Yannow;
					float max = ByDifficulty( thresholdData.Easy, thresholdData.Normal, thresholdData.Hard );
					result = _bombRef.Time <= Game.Random.Float( 1f, max );
					break;
				}
			case CardEnum.DoubleTrouble:
				{
					MoveThreshold thresholdMin = MoveThresholdConfig.DoubleTroubleMin;
					MoveThreshold thresholdMax = MoveThresholdConfig.DoubleTroubleMax;

					float maxReduction = ByDifficulty( thresholdMax.Easy, thresholdMax.Normal, thresholdMax.Hard );
					float minTime = ByDifficulty( thresholdMin.Easy, thresholdMin.Normal, thresholdMin.Hard );

					result = reduction <= maxReduction && _bombRef.Time >= minTime;
					break;
				}
			case CardEnum.Gandering:
				{
					MoveThreshold thresholdData = MoveThresholdConfig.Gandering;
					float threshold = ByDifficulty( thresholdData.Easy, thresholdData.Normal, thresholdData.Hard );
					result = _bombRef.Time < threshold;
					break;
				}
					

		}

		return result;

	}

	void UseCard( ref float reduction )
	{
		reduction = _chosenCard.Use( reduction );
		SoundManager.PlayAcrossClients( "CardActivation" );
		_cardUsed = true;
	}

	/// <summary>
	/// Determine the player's card, if it can be used, and activate it if so.
	/// </summary>
	/// <param name="reduction"></param>
	private void HandleCardUsage(ref float reduction)
	{
		if (_cardUsed) {  return; }
		switch (_chosenCard.CardID )
		{
			case CardEnum.Yannow:
				{
					if (ShouldUseCard(CardEnum.Yannow))
					{
						Log.Info( "AI using Yannow" );
						UseCard( ref reduction );
					}
					break;
				}

			case CardEnum.DoubleTrouble:
				if (ShouldUseCard(CardEnum.DoubleTrouble, reduction))
				{
					Log.Info( "AI using Double Trouble" );
					UseCard(ref reduction );
				}
				break;
			case CardEnum.Hollup:
				if ( ShouldUseCard(CardEnum.Hollup))
				{
					Log.Info( "AI using Hollup");
					UseCard( ref reduction );
				}
				break;

			case CardEnum.LeTrolle:
				if ( ShouldUseCard(CardEnum.LeTrolle))
				{
					Log.Info( "AI using LeTrolle" );
					UseCard( ref reduction );
				}
				break;

			case CardEnum.Calma:
				{
					Log.Info( "AI using Calma" );
					UseCard( ref reduction );
					break;
				}
			case CardEnum.Gandering:
				{

					if (ShouldUseCard(CardEnum.Gandering))
					{
						UseCard( ref reduction );
						if ( Game.Random.Float() <= CardActivationChance.Gandering)
						{
							Log.Info( "AI using Gandering" );
							// simulate successful card activation for gandering
							// AI will use random input range from 1 -> bomb time for next turn
							int logicalGuess = (int)Game.Random.Int( 1, Math.Max( 1, (int)_bombRef.Time - 1 ));
							_queuedInputs.Enqueue( logicalGuess );
						}
					}
					break;
				}
			case CardEnum.PassOff:
				UseCard(ref reduction);
				break;


		}


	}


	/// <summary>
	/// Calculate the time to reduce from the bomb depending on the AI difficulty.
	/// </summary>
	/// <returns>Final time to shave off the bomb</returns>
	private float CalculateReduction()
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


	/// <summary>
	/// AI simulation of the turn, handling the time to reduce, when to use a card and how long to wait.
	/// </summary>
	/// <returns></returns>
	async public Task SimulateTurn()
	{
		await GameTask.DelaySeconds( Game.Random.Float( AIDifficulty.MinDelay, AIDifficulty.MaxDelay) );

		if ( !_bombRef.IsActive )
		{
			Log.Info( "Bomb is not active" );
			return;
		}

		float finalReduction = CalculateReduction();

		HandleCardUsage( ref finalReduction );

		// queued inputs have priority, so override finalReduction
		if (_queuedInputs.Count > 0 && finalReduction != 0)
		{
			finalReduction = _queuedInputs.Dequeue(); 
		}

		_bombRef.ReduceBombTime( finalReduction );
		SoundManager.Play2D( "Input" );



		//Log.Warning( $"AI's reduction = {finalReduction}" );

	}

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

	public void SetAIDifficulty(Difficulty difficultyType)
	{
		switch ( difficultyType )
		{
			case Difficulty.Easy:
				AIDifficulty = BombAIDifficultyConfig.All[Difficulty.Easy];
				break;
			case Difficulty.Normal:
				AIDifficulty = BombAIDifficultyConfig.All[Difficulty.Normal];
				break;
			case Difficulty.Hard:
				AIDifficulty = BombAIDifficultyConfig.All[Difficulty.Hard];
				break;
			default:
				AIDifficulty = BombAIDifficultyConfig.All[Difficulty.Normal];
				break;
		}

		Log.Info( $"Difficulty set to {difficultyType}" );
	}

	public Difficulty DifficultyEnum{ get; set; }
	public BombAIDifficulty AIDifficulty { get; private set; }


	// private variables
	private Queue<int> _queuedInputs = new Queue<int>();

	private Bomb _bombRef = null;
	private Card _chosenCard;
	private bool _cardUsed = false;

}
