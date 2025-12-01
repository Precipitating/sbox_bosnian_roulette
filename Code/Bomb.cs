using Sandbox;
using Sandbox.Utility;
using System.Threading.Tasks;

public sealed class Bomb : Component
{
	[Property] [Description( "The seconds it takes before the bomb explodes." )] [Range( 0, 1000 )]
	public float GetTime {
		get 
		{
			return _time;
		}
		set 
		{
			_time = value;
		} 
	}

	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			_isActive = value;
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

		timeSince = 0;
		while ( timeSince < half )
		{
			float t = timeSince / (seconds * 0.5f);
			WorldScale = Vector3.Lerp( to, from, easer( t ) );
			await Task.Frame();
		}

		_finishedTick = true;
	}

	async public Task BombTick( float waitTime )
	{
		_finishedTick = false;
		await Task.DelaySeconds( waitTime );
		if ( _time > 0 )
		{
			await BombTickScaleLerp(waitTime);
			_time -= 1;
			Log.Info( _time );

		}
		else
		{
			_isActive = false;

		}

	}
	async private Task BombTickScaleLerp(float seconds)
	{
		float lerpTime = _time / _originalTime;
		float tickTime =  (1f - lerpTime )* _lerpScale;
		_tickSize = _originalSize + (_originalSize * tickTime);
		Log.Info( tickTime );

		await LerpSize( lerpTime, _tickSize, Easing.BounceInOut );




	}


	private float _originalTime = 0f;
	private float _time = 0f;
	private bool _isActive = false;
	private float _lerpScale = 4f;
	private Vector3 _originalSize;
	private Vector3 _tickSize;
	private bool _finishedTick = true;





	protected override void OnStart()
	{
		if (!IsValid)
		{
			Log.Info( "Can't find bomb reference" );
			return;
		}
		_originalTime = _time;
		_originalSize = LocalScale;
		_tickSize = _originalSize * _lerpScale;
		_isActive = true;

	}

	async protected override void OnUpdate()
	{
		if ( _isActive && _finishedTick )
		{
			await BombTick( 1 );
		}
		
		
	}



}
