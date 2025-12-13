using Sandbox;
using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class AiMode : Component
{
	async public Task SimulateTurn()
	{
		Log.Warning( $"AI simulate turn, current time = {_bombRef.Time}, active: {_bombRef.IsActive}" );

		await GameTask.DelaySeconds( Game.Random.Int( 1, 4 ) );
		float timeSinceLastTick = _bombRef.GetTickRate();
		Log.Warning( $"AI's detected tick rate: {timeSinceLastTick}" );
		bool isActiveAtStart = _bombRef.IsActive;



		if ( !isActiveAtStart )
		{
			Log.Info( "Bomb is not active" );
			return;
		}
		foreach (var entry in reductionTable)
		{


			if ( timeSinceLastTick >= entry.Key)
			{
				Log.Info( $"{entry.Key} decrease setup selected for AI." );
				if (lastKeyExecuted == entry.Key)
				{
					comboCount = Math.Min( comboCount + 1, 3);
				}
				else
				{
					comboCount = 1;
					lastKeyExecuted = -1;
				}
				lastKeyExecuted = entry.Key;

				float result = entry.Value() * comboCount;
				if ( result != -1 )
					_bombRef.ReduceBombTime( result );
				else
					_bombRef.ReduceBombTime( 1 );

				Log.Info( $"AI reduced time! no. {entry.Key}, combo: {comboCount}" );
				return;
			}
		}
	}

	private void BuildReductionTable()
	{
		reductionTable = new()
		{
			{1.8f,() => Game.Random.Int( 30, 50)},
			{1.4f,() => Game.Random.Int( 25, 35)},
			{1.2f,() => Game.Random.Int( 20, 30)},
			{1f,() => Game.Random.Int( 15, 25)},
			{0.5f,() => Game.Random.Int( 10, 20)},
			{0.1f,() => Game.Random.Int( 1, 2)},
		};
	}

	protected override void OnStart()
	{
		base.OnStart();
		_gameManager = GameManager.Instance;
		BuildReductionTable();
		_bombRef = Scene.Directory.FindByName( "BombModel" ).First().GetComponent<Bomb>();
		Log.Warning( $"AIMODE BombRef instance: {_bombRef?.GetHashCode()}" );

	}


	private int comboCount = 1;
	private float lastKeyExecuted = -1;
	private GameManager _gameManager = null;
	private Dictionary<float, Func<float>> reductionTable;
	Bomb _bombRef = null;
}
