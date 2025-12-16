using Sandbox;
using Sandbox.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class Bomb : Component
{
    public float GetTickRate()
    {
        return _tickRate;
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
        Log.Warning( $"Bomb time has reduced by {reductionTime}" );
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

				_tickRate = _sinceLastTick;
				Log.Info( $"tick rate: {_tickRate}" );
				_sinceLastTick = 0;

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
		_gameManager.GetWinner();
		Log.Info( "Explode" );
		IsActive = false;

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
	public void BombTick()
    {
		if (!_finishedTick) { return; }
		_ = BombTickAsync();

    }


	async private Task BombTickAsync()
	{
		_finishedTick = false;
		await GameTask.DelaySeconds( Time / OriginalTime );
		if ( Time > 0 )
		{
			BombTickScaleLerp();
			Time -= 1;

			//Log.Info( Time );

		}
		else
		{
			Explode();

		}
	}



    private void BombTickScaleLerp()
    {
        float lerpTime = Time / OriginalTime;
        float tickTime = (1f - lerpTime) * _lerpScaleMultiplier;
        _tickSize = _originalSize + (_originalSize * tickTime);

		Easing.Function easer = null;
		if ( Time >= OriginalTime * 0.7f)
		{
			easer = Easing.SineEaseInOut;
		}
		else if ( Time >= OriginalTime * 0.3f  )
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

        if ( IsActive && _finishedTick )
        {
            BombTick();
        }


    }


    [Property] public SoundPointComponent TickSound { get; set; }
    [Property] public SoundEvent ExplodeSound { get; set; }
    [Property] public SoundPointComponent JingleSound { get; set; }
    [Property] public GameObject ExplosionRef { get; set; }
    [Property] public SoundPointComponent InputSoundRef { get; set; }

	public float OriginalTime = 0f;

	public int BombMinTime { get; private set; } = 60;
	public int BombMaxTime { get; private set; } = 600;
	[Sync] public float Time { get; set; } = 0f;

    [Sync] public bool IsActive { get; set; } = false;


    private const float _lerpScaleMultiplier = 2f;
    private Vector3 _originalSize;
    private Vector3 _tickSize;
    [Sync] private bool _finishedTick { get; set; } = true;
    private GameManager _gameManager = null;

    [Sync] private float _tickRate { get; set; } = 0f;
    [Sync] private TimeSince _sinceLastTick { get; set; } = 0;



}
