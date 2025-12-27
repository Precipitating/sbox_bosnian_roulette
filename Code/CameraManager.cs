using Sandbox;
using System;
using System.Threading.Tasks;

public sealed class CameraManager : Component
{
	/// <summary>
	/// Change current camera view locally. Disable previously active camera.
	/// </summary>
	/// <param name="cameraGameObject"></param>
	/// <param name="excludeName"></param>
	public void SetCamera( GameObject cameraGameObject, string excludeName = null )
	{
		cameraGameObject.Enabled = true;
		CameraComponent cam = cameraGameObject.GetComponent<CameraComponent>();
		cam.IsMainCamera = true;

		if ( !string.IsNullOrEmpty( excludeName ) )
		{
			cam.RenderExcludeTags = new TagSet();
			cam.RenderExcludeTags.Add( excludeName );
		}

		if ( ActiveCamera != null )
		{
			CameraComponent oldCam = ActiveCamera.GetComponent<CameraComponent>();
			oldCam.IsMainCamera = false;
			ActiveCamera.Enabled = false;
			ActiveCamera = null;
		}
		ActiveCamera = cameraGameObject;
	}


	/// <summary>
	/// Used for transitioning the camera from main menu to player
	/// </summary>
	/// <param name="target"></param>
	/// <param name="excludeTag"></param>
	/// <returns></returns>
	public async Task LerpTransitionCameraTo( GameObject target, string excludeTag = null )
	{
		IsLerping = true;
		SetCamera( TransitionCamera );
		if ( !string.IsNullOrEmpty( excludeTag ) )
		{
			TransitionCamera.GetComponent<CameraComponent>().RenderExcludeTags.Add( excludeTag );
		}

		Scene.TimeScale = 1;
		while ( !(TransitionCamera.WorldPosition.Distance( target.WorldPosition ) < 0.10f) )
		{
			TransitionCamera.WorldTransform = TransitionCamera.WorldTransform.LerpTo( target.WorldTransform, Time.Delta * 2f );

			await Task.Frame();
		}


		IsLerping = false;
	}


	public GameObject ActiveCamera { get; set; } = null;
	public bool IsLerping { get; set; } = false;
	[Property] public GameObject TransitionCamera { get; private set; } = null;
	[Property] public GameObject OverheadCamera { get; private set; } = null;
}

