using Sandbox;
using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class AiMode : Component
{
	async public Task SimulateTurn()
	{
		Log.Info( "AI simulate turn:" );
		await GameTask.DelaySeconds( Game.Random.Int( 1, 5 ) );

		
		if (!BombRef.IsActive ) return;

		float timeSinceLastTick = BombRef.GetTickRate();


		foreach (var entry in reductionTable)
		{
			Log.Info( $"{entry.Key} >= {timeSinceLastTick}" );
			if ( timeSinceLastTick >= entry.Key)
			{
				Log.Info( "KEY" );
				if (lastKeyExecuted == entry.Key)
				{
					comboCount += 1;
				}
				else
				{
					comboCount = 1;
				}
				lastKeyExecuted = entry.Key;
				entry.Value();
				_gameManager.NextTurn();
				Log.Info( $"AI reduced time! no. {entry.Key}" );
				break;
			}
		}



	}

	private void BuildReductionTable()
	{
		reductionTable = new()
		{
			{1.8f,() => BombRef.ReduceBombTime( Game.Random.Int( 30 * comboCount, 50 * comboCount ))},
			{1.4f,() => BombRef.ReduceBombTime( Game.Random.Int( 25 * comboCount, 35 * comboCount ))},
			{1.2f,() => BombRef.ReduceBombTime( Game.Random.Int( 20 * comboCount, 30 * comboCount ))},
			{1f,() => BombRef.ReduceBombTime( Game.Random.Int( 15 * comboCount, 25 * comboCount ))},
			{0.5f,() => BombRef.ReduceBombTime( Game.Random.Int( 10 * comboCount, 20 * comboCount ))},
			{0.1f,() => BombRef.ReduceBombTime( 1 )},
		};
	}

	protected override void OnStart()
	{
		base.OnStart();
		BuildReductionTable();


	}
	protected override void OnUpdate()
	{

	}

	[Property] public Bomb BombRef;
	private int comboCount = 1;
	private float lastKeyExecuted = -1;
	private GameManager _gameManager = GameManager.Instance;
	private Dictionary<float, Action> reductionTable;
}
