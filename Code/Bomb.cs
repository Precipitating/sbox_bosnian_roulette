using Sandbox;
using Sandbox.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class Bomb : Component
{

	class BombEffects
	{
		public BombEffects( CardDatabase.PersistingEffects name, int turns)
		{
			Name = name;
			TurnsLeft = turns;
		}
		public CardDatabase.PersistingEffects Name;
		public int TurnsLeft;


	}


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
		bool ticked = false;

		while ( timeSince < seconds )
		{
			float t = (timeSince / seconds).Clamp( 0f, 1f );
			float pingPong = 1f - MathF.Abs( t * 2f - 1f );

			float eased = easer(pingPong);
			WorldScale = Vector3.Lerp( from, to, easer( eased ) );

			// trigger tick once at midpoint
			if ( !ticked && t >= 0.5f )
			{
				TickSfx();
				ticked = true;
			}

			await Task.Frame();
		}

		WorldScale = from;
		_finishedTick = true;
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

		var explodeSound = Sound.Play( ExplodeSound );
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
			Log.Info( Time );
		}

	}

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
						LeTrolleModeToggle();
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
		float realTime = _isTrollMode ? _fakeTime : Time;
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



    private void BombTickScaleLerp()
    {
		float realTime = _isTrollMode ? _fakeTime : Time;
        float lerpTime = realTime / OriginalTime;
        float tickTime = (1f - lerpTime) * _lerpScaleMultiplier;
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


		LerpSize( lerpTime, _tickSize, easer );


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

    async protected override void OnUpdate()
    {

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
	public void LeTrolleModeToggle()
	{
		_isTrollMode = !_isTrollMode;

		if ( _isTrollMode )
		{
			_effectsQueue.Add( new BombEffects(CardDatabase.PersistingEffects.ElTrolle, 2));
		}


	}



    [Property] public SoundPointComponent TickSound { get; set; }
    [Property] public SoundEvent ExplodeSound { get; set; }
    [Property] public SoundPointComponent JingleSound { get; set; }
    [Property] public GameObject ExplosionRef { get; set; }

	public float OriginalTime = 0f;

	public int BombMinTime { get; private set; } = 60;
	public int BombMaxTime { get; private set; } = 600;
	[Sync] public float Time { get; set; } = 0f;



	[Sync] public bool IsActive { get; set; } = false;

	// private
    private const float _lerpScaleMultiplier = 2f;
    private Vector3 _originalSize;
    private Vector3 _tickSize;
    [Sync] private bool _finishedTick { get; set; } = true;
    private GameManager _gameManager = null;

    [Sync] private TimeSince _timeElapsed { get; set; } = 0;

	private const float _minLerpDelay = 0.2f;
	private const float _maxLerpDelay = 1f;

	[Sync] private bool _isTrollMode { get; set; } = false;
	private const float _fakeTime = 10f;

	private List<BombEffects> _effectsQueue = new List<BombEffects>();

}
