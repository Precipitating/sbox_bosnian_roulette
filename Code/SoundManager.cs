using Sandbox;
public static class SoundManager
{
	public static void Play2DByPath(string key, string path)
	{
		if ( _pathHandles.TryGetValue( key, out SoundHandle handle ) )
		{
			if ( handle != null )
			{
				handle.Stop();
			}
		}

		_pathHandles[key] = Sound.Play( path );
		Log.Info( $"Should play {key}" );


	}

	public static void InitializeSounds()
	{
		_sounds = new Dictionary<string, SoundEvent>
		{
			["Input"] = Cloud.SoundEvent( "igrotronika.click8" ),
			["Wrong"] = Cloud.SoundEvent( "ipvfour.buzzerincorrect" ),
		};


	}

	public static void StopPathSound( string key, float fadeTime = 0f )
	{
		if (_pathHandles.TryGetValue(key, out SoundHandle handle))
		{
			handle.Stop( fadeTime );
			Log.Info( $"Stopped path sound {key}" );
		}
		

	}
	public static void Play2D( string name )
	{
		Sound.Play( _sounds[name] );
		Log.Info( $"Should play {name}" );
	}

	[Rpc.Broadcast]
	public static void PlayAcrossClients( string name, bool byPath = false, string path = "")
	{

		if (!byPath )
		{
			if ( _sounds.TryGetValue( name, out SoundEvent sound ) )
			{
				Sound.Play( sound );
				Log.Info( $"Should play {name}" );

			}
		}
		else
		{
			 Play2DByPath(name, path);
		}

	}



	private static Dictionary<string, SoundEvent> _sounds;

	private static readonly Dictionary<string, SoundHandle> _pathHandles = new();
}


