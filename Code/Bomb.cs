using Sandbox;
using Sandbox.Utility;
using System.Threading;
using System.Threading.Tasks;

public sealed class Bomb : Component
{
	[Property] [Description( "The seconds it takes before the bomb explodes." )] [Range( 0, 1000 )]
	public float GetTime {
		get 
		{
			return _time;
		}
		private set 
		{
			_time = value;
		} 
	}

	public float GetTickRate()
	{
		return _tickRate;
	}





	public bool ReduceBombTime(float reductionTime)
	{
		_time = float.Max( 0, _time - reductionTime );
		Log.Warning( $"Bomb time has reduced by {reductionTime}" );
		Log.Warning(_time);

		if (_time <= 0)
		{
			_ = Explode();
			return true;
		}

		return false;

	}


	async Task LerpSize( float seconds, Vector3 to, Easing.Function easer )
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
		TickSound.StopSound();
		TickSound.StartSound();
		_tickRate = _sinceLastTick;
		Log.Info($"tick rate: {_tickRate}");
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

	async public Task Explode()
	{
		if (IsActive)
		{
			Log.Info( "Explode" );
			IsActive = false;
			_gameManager.SetCamera( _gameManager.OverheadCamera );
			JingleSound.StartSound();
			await GameTask.DelaySeconds( 1 );
			JingleSound.StopSound();
			ExplosionRef.Enabled = true;
			var explodeSound = Sound.Play( ExplodeSound );
			await GameTask.Delay( 100 );
			_ = _gameManager.DetermineWinner();
		}


	}

	async public Task BombTick()
	{
		_finishedTick = false;
		await GameTask.DelaySeconds( _time / _originalTime);
		if ( _time > 0 )
		{
			await BombTickScaleLerp();
			_time -= 1;
			
			Log.Info( _time );

		}
		else
		{
			await Explode();

		}

	}


	async private Task BombTickScaleLerp()
	{
		float lerpTime = _time / _originalTime;
		float tickTime =  (1f - lerpTime ) * _lerpScaleMultiplier;
		_tickSize = _originalSize + (_originalSize * tickTime);

		await LerpSize( lerpTime, _tickSize, Easing.BounceInOut );


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
		//_time = Game.Random.Int( 60, 500 );
		_time = 30;
		_originalTime = _time;
		_originalSize = LocalScale;
		_tickSize = _originalSize * _lerpScaleMultiplier;
		

	}

	async protected override void OnUpdate()
	{

		if ( IsActive && _finishedTick )
		{
			await BombTick();
		}


	}


	[Property] public SoundPointComponent TickSound { get; set; }
	[Property] public SoundEvent ExplodeSound { get; set; }
	[Property] public SoundPointComponent JingleSound { get; set; }
	[Property] public GameObject ExplosionRef { get; set; }
	public bool IsActive { get; set; } = false;
	private float _originalTime = 0f;
	private float _time = 0f;
	private const float _lerpScaleMultiplier = 4f;
	private Vector3 _originalSize;
	private Vector3 _tickSize;
	private bool _finishedTick = true;
	private GameManager _gameManager = null;

	private float _tickRate = 0f;
	private TimeSince _sinceLastTick = 0;



}
