using Sandbox;
using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class AiMode : Component
{
	async public Task SimulateTurn()
	{
		Log.Info( AIDifficulty );
		//Log.Warning( $"AI simulate turn, current time = {_bombRef.Time}, active: {_bombRef.IsActive}" );
		await GameTask.DelaySeconds( Game.Random.Float( AIDifficulty.MinDelay, AIDifficulty.MaxDelay) );

		if ( !_bombRef.IsActive )
		{
			Log.Info( "Bomb is not active" );
			return;
		}

		float progress = 1f - (_bombRef.Time / _bombRef.OriginalTime).Clamp( 0f, 1f );
		float hazard = MathF.Pow( 1f - progress, AIDifficulty.HazardExponent );

		float minReduction = _bombRef.Time * AIDifficulty.MinReductionPercent;
		float maxReduction = _bombRef.Time * AIDifficulty.MaxReductionPercent;

		float reduction = MathX.Lerp( minReduction, maxReduction, hazard );
		reduction *= Game.Random.Float( 1f - AIDifficulty.Variance, 1f + AIDifficulty.Variance );

		int finalReduction = Math.Max( 1, (int)MathF.Round( reduction ) );
		_bombRef.ReduceBombTime( finalReduction);

		//Log.Warning( $"AI's reduction = {finalReduction}" );

	}



	protected override void OnStart()
	{
		base.OnStart();
		_bombRef = Scene.Directory.FindByName( "BombModel" ).First().GetComponent<Bomb>();
		Log.Warning( $"AIMODE BombRef instance: {_bombRef?.GetHashCode()}" );
		//SetAIDifficulty( "Hard" );

	}


	public struct BombAIDifficulty
	{
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
		MinReductionPercent = 0.02f, // 2%
		MaxReductionPercent = 0.06f, // 6%
		HazardExponent = 1.5f,
		Variance = 0.10f,
		MinDelay = 3f,
		MaxDelay = 6f
	};

	private BombAIDifficulty _normalDifficulty = new BombAIDifficulty
	{
		MinReductionPercent = 0.05f, // 5%
		MaxReductionPercent = 0.15f, // 15%
		HazardExponent = 2.5f,
		Variance = 0.15f,
		MinDelay = 1.5f,
		MaxDelay = 4f
	};

	private BombAIDifficulty _hardDifficulty = new BombAIDifficulty
	{
		MinReductionPercent = 0.1f, // 10%
		MaxReductionPercent = 0.30f, // 30%
		HazardExponent = 3.5f,
		Variance = 0.20f,
		MinDelay = 0.5f,
		MaxDelay = 1f
	};





	Bomb _bombRef = null;
}
