using Sandbox;
using Sandbox.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;
class BombEffects
{
	public BombEffects( CardDatabase.PersistingEffects name, int turns )
	{
		Name = name;
		TurnsLeft = turns;
	}
	public CardDatabase.PersistingEffects Name;
	public int TurnsLeft;


}
public sealed class Bomb : Component
{

	[Rpc.Host]
    public void ReduceBombTime( float reductionTime )
    {

		if ( Time <= 0 )
		{
			Explode();
			return;

		}
		Time = float.Max( 0, Time - reductionTime );
		ApplyEffects();
		//Log.Warning( $"Bomb time has reduced by {reductionTime}" );
		//Log.Warning( Time );



	}

    public void LerpSize( float seconds, Vector3 to, Easing.Function easer )
    {
		_= LerpSizeAsync( seconds, to, easer );
    }


	async Task LerpSizeAsync( float seconds,Vector3 to, Easing.Function easer )
	{
		TimeSince timeSince = 0f;
		Vector3 from = WorldScale;


		TickSfx();
		while ( timeSince < seconds )
		{
			float t = (timeSince / seconds).Clamp( 0f, 1f );
			float pingPong = 1f - MathF.Abs( t * 2f - 1f );

			float eased = easer(pingPong);
			WorldScale = Vector3.Lerp( from, to, easer(eased ));


			await Task.Frame();
		}

		WorldScale = from;
		_finishedTick = true;
		Log.Info( "Done lerp" );
	}

	[Rpc.Broadcast]
	private void TickSfx()
	{
		TickSound.StopSound();
		TickSound.StartSound();
	}




	[Rpc.Broadcast]
    public void Explode()
    {
		if ( !IsActive ) { return; }
		_ = ExplodeAsync();


	}


	private async Task ExplodeAsync()
	{
		IsActive = false;
		_gameManager.GetWinner();
		Log.Info( "Explode" );


		_gameManager.SetCamera( _gameManager.OverheadCamera );
		_gameManager.BombUI.Enabled = false;

		JingleSound.StartSound();
		await GameTask.DelaySeconds( 1 );
		JingleSound.StopSound();

		ExplosionRef.Enabled = true;

		SoundManager.Play2DByPath( "Explosion", "Sound/Explosion.sound");
		await GameTask.Delay( 100 );

		_gameManager.DetermineWinner();
	}




	[Rpc.Host]
	public void BombScale()
    {
		if (IsProxy) { return; }
		_ = BombScaleAsync();

    }

	[Rpc.Host]
	private void BombTick()
	{
		if (_timeElapsed > 1)
		{
			_timeElapsed = 0;

			Time = float.Max( 0, Time - 1 );

			if ( Time <= 0 )
			{
				Explode();
			}
			//Log.Info( Time );
		}

	}


	/// <summary>
	/// Bomb effects that get applied when it lasts for over a round
	/// </summary>
	[Rpc.Host]
	public void ApplyEffects()
	{
		for ( int i = _effectsQueue.Count - 1; i >= 0; i-- )
		{

			if ( _effectsQueue[i].TurnsLeft <= 0 )
			{
				switch ( _effectsQueue[i].Name )
				{
					case CardDatabase.PersistingEffects.ElTrolle:
						LeTrolleModeToggle(false);
						Log.Info( "Disable el trolle" );
						break;
				}

				_effectsQueue.RemoveAt( i );
			}
			else
			{
				Log.Info( $"Reduce turn for {_effectsQueue[i].Name}, turns left: {_effectsQueue[i].TurnsLeft}" );
				--_effectsQueue[i].TurnsLeft; 
			}
		}

	}


	async private Task BombScaleAsync()
	{
		_finishedTick = false;
		float realTime = IsTrollMode ? _fakeTime : Time;
		float ratio = 1f - (realTime / OriginalTime);
		float delay = MathX.Lerp( _maxLerpDelay, _minLerpDelay, ratio );
		await GameTask.DelaySeconds( delay );
		//Log.Info( $"Lerp delay = {delay}" );
		if ( realTime > 0 )
		{
			BombTickScaleLerp();
			//Log.Info( Time );

		}

	}


	/// <summary>
	/// Lerp the bomb's size up and down based on time left
	/// </summary>
    private void BombTickScaleLerp()
    {
		float realTime = IsTrollMode ? _fakeTime : Time;
		CurrentTickTime = realTime / OriginalTime;
        float tickTime = (1f - CurrentTickTime);
        _tickSize = _originalSize + (_originalSize * tickTime);

		Easing.Function easer = null;
		if ( realTime >= OriginalTime * 0.7f)
		{
			easer = Easing.SineEaseInOut;
		}
		else if ( realTime >= OriginalTime * 0.3f  )
		{
			easer = Easing.QuadraticInOut;
		}
		else
		{
			easer = Easing.ExpoInOut;
		}


		LerpSize( CurrentTickTime, _tickSize, easer );


    }

    protected override void OnStart()
    {

        Log.Warning( $"BOMB BombRef instance: {this?.GetHashCode()}" );
        if ( !IsValid )
        {
            Log.Info( "Can't find bomb reference" );
            return;
        }
        _gameManager = GameManager.Instance;
        Time = Game.Random.Int( BombMinTime, BombMaxTime);
		//Time = 100;
		OriginalTime = Time;
        _originalSize = LocalScale;
        _tickSize = _originalSize * _lerpScaleMultiplier;



    }

	[Rpc.Host]
    protected override void OnUpdate()
    {
		if (IsProxy) { return; }

        if ( IsActive )
        {
			if ( _finishedTick )
			{
				BombScale();
			}

			BombTick();

        }


    }
	[Rpc.Host]
	public void LeTrolleModeToggle(bool isOn)
	{
		IsTrollMode = isOn;

		if ( IsTrollMode )
		{
			// check if already exists and add on turns if so
			BombEffects exists = _effectsQueue.Find( c => c.Name == CardDatabase.PersistingEffects.ElTrolle );

			if (exists != null)
			{
				exists.TurnsLeft += 2;

			}
			else
			{
				_effectsQueue.Add( new BombEffects( CardDatabase.PersistingEffects.ElTrolle, 2 ) );
			}
				
		}


	}



    [Property] public SoundPointComponent TickSound { get; set; }
    [Property] public SoundPointComponent JingleSound { get; set; }
    [Property] public GameObject ExplosionRef { get; set; }

	public float OriginalTime = 0f;

	public bool UnoReversed { get; set; } = false;

	public int BombMinTime { get; private set; } = 60;
	public int BombMaxTime { get; private set; } = 600;

	public float CurrentTickTime = 0f;
	[Sync] public float Time { get; set; } = 0f;

	[Sync] public bool IsTrollMode { get; set; } = false;

	[Sync] public bool IsActive { get; set; } = false;

	// private
    private const float _lerpScaleMultiplier = 2f;
    private Vector3 _originalSize;
    private Vector3 _tickSize;
    [Sync] private bool _finishedTick { get; set; } = true;
    private GameManager _gameManager = null;

    [Sync] private TimeSince _timeElapsed { get; set; } = 0;

	private const float _minLerpDelay = 0.1f;
	private const float _maxLerpDelay = 1f;


	private const float _fakeTime = 10f;

	private List<BombEffects> _effectsQueue = new List<BombEffects>();

	

}
