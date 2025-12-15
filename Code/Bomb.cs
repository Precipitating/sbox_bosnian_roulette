using Sandbox;
using Sandbox.Utility;
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


	async Task LerpSizeAsync( float seconds, Vector3 to, Easing.Function easer )
	{
		TimeSince timeSince = 0;
		Vector3 from = WorldScale;
		float half = seconds * 0.5f;

		while ( timeSince < half )
		{
			var size = Vector3.Lerp( from, to, easer( timeSince / seconds ) );
			WorldScale = size;
			await Task.Frame(); // wait one frame
		}

		TickSfx();

		_tickRate = _sinceLastTick;
		Log.Info( $"tick rate: {_tickRate}" );
		_sinceLastTick = 0;

		timeSince = 0;
		while ( timeSince < half )
		{
			float t = timeSince / (seconds * 0.5f);
			WorldScale = Vector3.Lerp( to, from, easer( t ) );
			await Task.Frame();
		}

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
		await GameTask.DelaySeconds( Time / _originalTime );
		if ( Time > 0 )
		{
			await BombTickScaleLerp();
			Time -= 1;

			//Log.Info( Time );

		}
		else
		{
			Explode();

		}
	}



    async private Task BombTickScaleLerp()
    {
        float lerpTime = Time / _originalTime;
        float tickTime = (1f - lerpTime) * _lerpScaleMultiplier;
        _tickSize = _originalSize + (_originalSize * tickTime);

        LerpSize( lerpTime, _tickSize, Easing.BounceInOut );


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
        Time = Game.Random.Int( 60, 500 );
        //Time = 100;
        _originalTime = Time;
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

    [Sync] public float Time { get; set; } = 0f;

    [Sync] public bool IsActive { get; set; } = false;
    private float _originalTime = 0f;

    private const float _lerpScaleMultiplier = 4f;
    private Vector3 _originalSize;
    private Vector3 _tickSize;
    [Sync] private bool _finishedTick { get; set; } = true;
    private GameManager _gameManager = null;

    [Sync] private float _tickRate { get; set; } = 0f;
    [Sync] private TimeSince _sinceLastTick { get; set; } = 0;



}
