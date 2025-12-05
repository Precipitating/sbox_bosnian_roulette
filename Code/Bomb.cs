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





	public void ReduceBombTime(float reductionTime)
	{
		_time = float.Max( 0, _time - reductionTime );
		Log.Warning( $"Bomb time has reduced by {reductionTime}" );
		Log.Warning(_time);
		if (_time <= 0)
		{
			_gameManager.DetermineWinner();
		}

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

		IsActive = false;
		_gameManager.DetermineWinner();
		JingleSound.StartSound();
		await GameTask.DelaySeconds( 1 );
		GameObject.Destroy();
		JingleSound.StopSound();
		ExplosionRef.Enabled = true;
		Sound.Play( ExplodeSound );

	}

	async public Task BombTick()
	{
		_finishedTick = false;
		await GameTask.DelaySeconds( _time / _originalTime);
		if ( _time > 0 )
		{
			_ = BombTickScaleLerp();
			_time -= 1;
			
			Log.Info( _time );

		}
		else
		{
			_= Explode();

		}

	}


	async private Task BombTickScaleLerp()
	{
		float lerpTime = _time / _originalTime;
		float tickTime =  (1f - lerpTime ) * _lerpScaleMultiplier;
		_tickSize = _originalSize + (_originalSize * tickTime);

		_ = LerpSize( lerpTime, _tickSize, Easing.BounceInOut );


	}




	protected override void OnStart()
	{

		Log.Warning( $"BOMB BombRef instance: {this?.GetHashCode()}" );
		if ( !IsValid )
		{
			Log.Info( "Can't find bomb reference" );
			return;
		}
		_originalTime = _time;
		_originalSize = LocalScale;
		_tickSize = _originalSize * _lerpScaleMultiplier;


	}

	protected override void OnUpdate()
	{

		if ( IsActive && _finishedTick )
		{
			_ =  BombTick();
		}


	}


	[Property] public SoundPointComponent TickSound { get; set; }
	[Property] public SoundEvent ExplodeSound { get; set; }
	[Property] public SoundPointComponent JingleSound { get; set; }
	[Property] public GameObject ExplosionRef { get; set; }
	public bool IsActive { get; set; } = true;
	private float _originalTime = 0f;
	private float _time = 0f;
	private const float _lerpScaleMultiplier = 4f;
	private Vector3 _originalSize;
	private Vector3 _tickSize;
	private bool _finishedTick = true;
	private GameManager _gameManager = GameManager.Instance;


	private float _tickRate = 0f;
	private TimeSince _sinceLastTick = 0;








}
